print("Initialising server...")

import argparse
import sys
import struct
import mmap
import time
import signal
import os
from contextlib import contextmanager

import cv2
import mediapipe as mp
import numpy as np
from mediapipe.tasks.python import BaseOptions
from mediapipe.tasks.python.vision import *


# Configuration
COLOUR_IMAGE_FULL_WIDTH = 1920
COLOUR_IMAGE_FULL_HEIGHT = 1080
COLOUR_IMAGE_NUM_CHANNELS = 3
COLOUR_IMAGE_FULL_SIZE = COLOUR_IMAGE_FULL_WIDTH * COLOUR_IMAGE_FULL_HEIGHT * COLOUR_IMAGE_NUM_CHANNELS
COLOUR_IMAGE_FILE_NAME = "colour_image"
HAND_LANDMARKS_SIZE = 21 * 3 * 2 * 4  # 21 landmarks, 3 coordinates per landmark, 2 hands, 4 bytes per float
HAND_LANDMARKS_FILE_NAME = "hand_landmarks"
GESTURES_SIZE = 2 * 4  # 2 hands, 4 bytes per int
GESTURES_FILE_NAME = "gestures"
READY_EVENT_NAME = "SealTeam7ColourImageReady"
DONE_EVENT_NAME = "SealTeam7HandLandmarksDone"
GESTURE_RECOGNITION_MODEL_PATH = 'hand_landmarking_model.task'
VISUALISE_INFERENCE_RESULTS = False
"""Display the video overlaid with hand landmarks and bounding boxes around detected objects"""


class IPC:
    def __init__(self, platform):
        self.platform = platform
        self.colour_image_buffer = None
        self.hand_landmarks_buffer = None
        # self.gestures_buffer = None
            
    def wait_ready(self):
        pass
    
    def set_done(self):
        pass

    def close(self):
        self.colour_image_buffer.close()
        self.hand_landmarks_buffer.close()
        # self.gestures_buffer.close()

   
class WindowsIPC(IPC):
    def __init__(self):
        super().__init__('windows')
        import win32event
        self.__ready_event = win32event.CreateEvent(None, 0, 0, READY_EVENT_NAME)
        self.__done_event = win32event.CreateEvent(None, 0, 0, DONE_EVENT_NAME)
        self.colour_image_buffer = mmap.mmap(-1, 1920 * 1080 * 3, access=mmap.ACCESS_WRITE, tagname=COLOUR_IMAGE_FILE_NAME)
        self.hand_landmarks_buffer = mmap.mmap(-1, 21 * 3 * 2 * 4, access=mmap.ACCESS_WRITE, tagname=HAND_LANDMARKS_FILE_NAME)
        # self.gestures_buffer = mmap.mmap(-1, 2 * 4, access=mmap.ACCESS_WRITE, tagname="gestures")
        
    def wait_ready(self):
        import win32event
        while not shutdown_flag:
            # Wait for new frame with a timeout to check shutdown flag
            result = win32event.WaitForSingleObject(self.__ready_event, 100)  # 100ms timeout
            if result == win32event.WAIT_TIMEOUT:
                continue  # Continue waiting
            elif result != win32event.WAIT_OBJECT_0:
                # This should never happen
                raise Exception("WaitForSingleObject returned unexpected result: " + str(result))
            else:
                break  # Done waiting
                
    def set_done(self):
        import win32event
        win32event.SetEvent(self.__done_event)
        
    def close(self):
        super().close()
                
                
class LinuxIPC(IPC):
    def __init__(self):
        super().__init__('linux')
        import posix_ipc
        self.__ready_event = posix_ipc.Semaphore(READY_EVENT_NAME, posix_ipc.O_CREAT, initial_value=0)
        self.__done_event = posix_ipc.Semaphore(DONE_EVENT_NAME, posix_ipc.O_CREAT, initial_value=0)
        self.__colour_image_shm = posix_ipc.SharedMemory(COLOUR_IMAGE_FILE_NAME, posix_ipc.O_CREX, size=COLOUR_IMAGE_FULL_SIZE)
        self.__hand_landmarks_shm = posix_ipc.SharedMemory(HAND_LANDMARKS_FILE_NAME, posix_ipc.O_CREX, size=HAND_LANDMARKS_SIZE)
        self.colour_image_buffer = mmap.mmap(self.__colour_image_shm.fd, COLOUR_IMAGE_FULL_SIZE, access=mmap.ACCESS_WRITE)
        self.hand_landmarks_buffer = mmap.mmap(self.__hand_landmarks_shm.fd, HAND_LANDMARKS_SIZE, access=mmap.ACCESS_WRITE)
        self.__colour_image_shm.close_fd()
        self.__hand_landmarks_shm.close_fd()
        # self.gestures_buffer = mmap.mmap(-1, 2 * 4, access=mmap.ACCESS_WRITE, tagname="gestures")
        
    def wait_ready(self):
        import posix_ipc
        while not shutdown_flag:
            # Wait for new frame with a timeout to check shutdown flag
            try:
                self.__ready_event.acquire(100)  # wait for 100ms at a time
                break  # Done waiting
            except posix_ipc.BusyError:
                continue  # Continue waiting
                
    def set_done(self):
        self.__done_event.release()
        
    def close(self):
        self.__ready_event.close()
        self.__done_event.close()
        super().close()
        self.__colour_image_shm.unlink()
        self.__hand_landmarks_shm.unlink()


# Parse command line arguments
parser = argparse.ArgumentParser(description='Shifting Sands hand landmarking server.')
parser.add_argument('--platform', choices=['windows', 'linux'], required=True, help='Platform to run the server on ("windows" or "linux").')
args = parser.parse_args()

ipc = WindowsIPC() if args.platform == 'windows' else LinuxIPC()

# Global flag for graceful shutdown
shutdown_flag = False

def signal_handler(signum, frame):
    global shutdown_flag
    shutdown_flag = True

# Register signal handler for SIGINT (Ctrl+C)
signal.signal(signal.SIGINT, signal_handler)

@contextmanager
def timer(name):
    start = time.time()
    yield
    end = time.time()
    print(f"{name}: {int((end - start)*1000)} ms")
    
def get_path(path):
    dir = os.path.dirname(os.path.abspath(__file__))
    return os.path.join(dir, path)

gesture_recognizer_options = HandLandmarkerOptions(
    base_options=BaseOptions(model_asset_path=get_path(GESTURE_RECOGNITION_MODEL_PATH)),
    running_mode=RunningMode.VIDEO,
    num_hands=2,
    min_hand_detection_confidence=0.05,
    min_hand_presence_confidence=0.5,
    min_tracking_confidence=0.5,
    )

left_hand_history = []  # Will store numpy arrays of shape (21, 3)
right_hand_history = []
projected_left_hand = np.zeros((21, 3))
projected_right_hand = np.zeros((21, 3))

left_hand_absent_count = 0
right_hand_absent_count = 0

# Gesture mapping
GESTURE_MAP = {
    "None": 0,
    "Closed_Fist": 1,
    "Open_Palm": 2,
    "Pointing_Up": 3,
    "Thumb_Down": 4,
    "Thumb_Up": 5,
    "Victory": 6,
    "Spock": 7,
    "Calling": 8,
    "ILoveYou": 9
}

def gesture_to_int(gesture_name):
    return GESTURE_MAP.get(gesture_name, 0)  # Default to 0 (None) if gesture not found

def landmarks_to_array(landmarks):
    """Convert MediaPipe landmarks to numpy array with shape (21, 3)"""
    if landmarks is None:
        return None
    arr = np.array([[landmark.x * COLOUR_IMAGE_FULL_WIDTH,
                     -landmark.z * COLOUR_IMAGE_FULL_WIDTH,
                     landmark.y * COLOUR_IMAGE_FULL_HEIGHT] for landmark in landmarks])
    return arr

with HandLandmarker.create_from_options(gesture_recognizer_options) as gesture_recognizer:
    print("Ready.")

    try:
        while not shutdown_flag:
            ipc.wait_ready()

            # Read the frame
            with timer("Reading frame"):
                ipc.colour_image_buffer.seek(0)
                colour_image_data = np.frombuffer(ipc.colour_image_buffer, dtype=np.uint8).reshape(
                    (COLOUR_IMAGE_FULL_HEIGHT, COLOUR_IMAGE_FULL_WIDTH, COLOUR_IMAGE_NUM_CHANNELS))

            # Perform hand landmarking and gesture recognition
            with timer("Creating mp image"):
                mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=colour_image_data)
            with timer("Gesture recognition"):
                gesture_recognizer_result = gesture_recognizer.detect_for_video(mp_image, int(time.monotonic()*1000))

            # determine handedness and scale to pixel coordinates
            with timer("Processing results"):
                using_left_projected_hand = False
                using_right_projected_hand = False

                if len(gesture_recognizer_result.hand_landmarks) == 0:
                    left = None
                    right = None
                    left_hand_absent_count += 1
                    right_hand_absent_count += 1

                    # # Fill in missing hands with projected positions if absent for <= 2 frames
                    # if left_hand_absent_count <= 2:
                    #     left = projected_left_hand
                    #     using_left_projected_hand = True
                    # if right_hand_absent_count <= 2:
                    #     right = projected_right_hand
                    #     using_right_projected_hand = True
                        
                elif len(gesture_recognizer_result.hand_landmarks) <= 1:
                    left = None
                    right = None
                    for i, handedness in enumerate(gesture_recognizer_result.handedness):
                        if handedness[0].category_name == "Left":
                            left = landmarks_to_array(gesture_recognizer_result.hand_landmarks[i])
                            right_hand_absent_count += 1
                            left_hand_absent_count = 0
                        else:
                            right = landmarks_to_array(gesture_recognizer_result.hand_landmarks[i])
                            left_hand_absent_count += 1
                            right_hand_absent_count = 0
                    
                    # # Fill in missing hands with projected positions if absent for <= 2 frames
                    # if left is None and left_hand_absent_count <= 2:
                    #     left = projected_left_hand
                    #     using_left_projected_hand = True
                    # if right is None and right_hand_absent_count <= 2:
                    #     right = projected_right_hand
                    #     using_right_projected_hand = True

                elif len(gesture_recognizer_result.hand_landmarks) == 2:
                    left_hand_absent_count = 0
                    right_hand_absent_count = 0
                    
                    if gesture_recognizer_result.handedness[0][0].category_name == gesture_recognizer_result.handedness[1][0].category_name:
                        # both hands have same handedness -> determine handedness manually
                        left = None
                        right = None
                        for i, hand_landmarks in enumerate(gesture_recognizer_result.hand_landmarks):
                            landmarks = landmarks_to_array(hand_landmarks)
                            real_wrist = landmarks[0]
                            projected_left_wrist = projected_left_hand[0]
                            projected_right_wrist = projected_right_hand[0]
                            if (np.linalg.norm(real_wrist - projected_left_wrist) < np.linalg.norm(real_wrist - projected_right_wrist) and \
                                    left is None) or right is not None:
                                left = landmarks
                            else:
                                right = landmarks

                    else:
                        # hands have different handedness -> use mediapipe's handedness
                        left = None
                        right = None
                        for i, handedness in enumerate(gesture_recognizer_result.handedness):
                            if handedness[0].category_name == "Left":
                                left = landmarks_to_array(gesture_recognizer_result.hand_landmarks[i])
                            else:
                                right = landmarks_to_array(gesture_recognizer_result.hand_landmarks[i])

                else:
                    print("Warning: More than 2 hands detected.")
                    left = None
                    right = None
                    for i, handedness in enumerate(gesture_recognizer_result.handedness):
                        if handedness[0].category_name == "Left":
                            left = landmarks_to_array(gesture_recognizer_result.hand_landmarks[i])
                        else:
                            right = landmarks_to_array(gesture_recognizer_result.hand_landmarks[i])

            # # Process gestures
            # left_gesture = 0  # Default to None
            # right_gesture = 0  # Default to None
            # if gesture_recognizer_result.gestures:
            #     for i, gesture in enumerate(gesture_recognizer_result.gestures):
            #         if gesture_recognizer_result.handedness[i][0].category_name == "Left":
            #             left_gesture = gesture_to_int(gesture[0].category_name)
            #         else:
            #             right_gesture = gesture_to_int(gesture[0].category_name)

            if VISUALISE_INFERENCE_RESULTS:
                with timer("Visualising results"):
                    scale = 1
                    vis_frame = cv2.cvtColor(colour_image_data, cv2.COLOR_RGB2BGR)
                    for landmarks, colour in ((left, (0, 255, 0)), (right, (0, 0, 255))):
                        if landmarks is not None:
                            # If using projected hand, draw it in magenta
                            if (landmarks == projected_left_hand).all():
                                colour = (255, 0, 255)
                            elif (landmarks == projected_right_hand).all():
                                colour = (255, 0, 255)

                            # Draw current connections
                            for connection in HandLandmarksConnections.HAND_CONNECTIONS:
                                cv2.line(vis_frame, 
                                       (int(landmarks[connection.start, 0]),
                                        int(landmarks[connection.start, 2])),
                                       (int(landmarks[connection.end, 0]),
                                        int(landmarks[connection.end, 2])),
                                       colour, 1)

                            # Draw current landmarks
                            for landmark in landmarks:
                                cv2.circle(vis_frame, 
                                         (int(landmark[0]), int(landmark[2])), 
                                         3, colour, -1)

                    # Draw previous wrist points
                    for landmarks in left_hand_history:
                        cv2.circle(vis_frame, (int(landmarks[0, 0]), int(landmarks[0, 2])), 3, (255, 0, 0), -1)
                    for landmarks in right_hand_history:
                        cv2.circle(vis_frame, (int(landmarks[0, 0]), int(landmarks[0, 2])), 3, (255, 0, 0), -1)

                    # Draw projected wrist points
                    cv2.circle(vis_frame, (int(projected_left_hand[0, 0]), int(projected_left_hand[0, 2])), 3, (255, 0, 255), -1)
                    cv2.circle(vis_frame, (int(projected_right_hand[0, 0]), int(projected_right_hand[0, 2])), 3, (255, 0, 255), -1)
                    
                    # # Draw gestures
                    # if left_gesture > 0:
                    #     cv2.putText(vis_frame, f"Left: {list(GESTURE_MAP.keys())[list(GESTURE_MAP.values()).index(left_gesture)]}", 
                    #               (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
                    # if right_gesture > 0:
                    #     cv2.putText(vis_frame, f"Right: {list(GESTURE_MAP.keys())[list(GESTURE_MAP.values()).index(right_gesture)]}", 
                    #               (10, 60), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 2)
                    
                    # Handle window
                    cv2.imshow("Inference visualisation", vis_frame)
                    key = cv2.waitKey(1)
                    if key == 27:
                        cv2.destroyWindow("Inference visualisation")
                        VISUALISE_INFERENCE_RESULTS = False

            # Keep track of previous results
            if len(left_hand_history) >= 2:
                left_hand_history.pop(0)
                right_hand_history.pop(0)
            left_hand_history.append(left if left is not None else np.zeros((21, 3)))
            right_hand_history.append(right if right is not None else np.zeros((21, 3)))
            
            # Calculate projection to fill in gaps where model doesn't find hands
            if len(left_hand_history) == 1:
                # use previous location of hands
                projected_left_hand = left_hand_history[-1]
                projected_right_hand = right_hand_history[-1]
            else:
                # assume constant velocity and rigid hand
                last_wrist = left_hand_history[-1][0]
                prev_wrist = left_hand_history[-2][0]
                delta = last_wrist - prev_wrist
                projected_left_hand = left_hand_history[-1] + delta

                last_right = right_hand_history[-1][0]
                prev_right = right_hand_history[-2][0]
                delta = last_right - prev_right
                projected_right_hand = right_hand_history[-1] + delta

            # Write the hand landmarks and gestures
            with timer("Writing results"):
                # Write hand landmarks
                ipc.hand_landmarks_buffer.seek(0)
                for i, hand in enumerate((left, right)):
                    if hand is None:
                        zeros = np.zeros((21, 3))
                        for j in range(21):
                            struct.pack_into("<fff", ipc.hand_landmarks_buffer, i*21*3*4 + j*3*4, 
                                           zeros[j, 0], zeros[j, 1], zeros[j, 2])
                    else:
                        for j in range(21):
                            struct.pack_into("<fff", ipc.hand_landmarks_buffer, i*21*3*4 + j*3*4,
                                           hand[j, 0], hand[j, 1], hand[j, 2])
                
                # Write gestures
                # gestures_buffer.seek(0)
                # struct.pack_into("<ii", gestures_buffer, 0, left_gesture, right_gesture)

            if not shutdown_flag:
                ipc.set_done()

    finally:
        # Clean up
        cv2.destroyAllWindows()
        if "mp_image" in vars(): del mp_image
        if "colour_image_data" in vars(): del colour_image_data
        ipc.close()