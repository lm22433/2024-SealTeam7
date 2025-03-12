print("Initialising server...")

import struct
import mmap
import time
import signal
from contextlib import contextmanager

import win32event
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
READY_EVENT_NAME = "SealTeam7ColourImageReady"
DONE_EVENT_NAME = "SealTeam7HandLandmarksDone"

OBJECT_DETECTION_MODEL_PATH = 'object_detection_model.tflite'
HAND_LANDMARKING_MODEL_PATH = 'hand_landmarking_model.task'
VISUALISE_INFERENCE_RESULTS = True
"""Display the video overlaid with hand landmarks and bounding boxes around detected objects"""

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
    # print(f"{name}: {int((end - start)*1000)} ms")

# Create events
ready_event = win32event.CreateEvent(None, 0, 0, READY_EVENT_NAME)
done_event = win32event.CreateEvent(None, 0, 0, DONE_EVENT_NAME)

# Initialise the mapping
colour_image_buffer = mmap.mmap(-1, COLOUR_IMAGE_FULL_SIZE, access=mmap.ACCESS_WRITE, tagname=COLOUR_IMAGE_FILE_NAME)
hand_landmarks_buffer = mmap.mmap(-1, HAND_LANDMARKS_SIZE, access=mmap.ACCESS_WRITE, tagname=HAND_LANDMARKS_FILE_NAME)

hand_landmarker_options = HandLandmarkerOptions(
    base_options=BaseOptions(model_asset_path=HAND_LANDMARKING_MODEL_PATH),
    running_mode=RunningMode.VIDEO,
    num_hands=3,  # temporarily > 2 to help tune confidence thresholds
    min_hand_detection_confidence=0.1,
    min_hand_presence_confidence=0.5,
    min_tracking_confidence=0.5)

# Replace hand history with numpy arrays - shape: (history_size, 21, 3) for each hand
left_hand_history = []  # Will store numpy arrays of shape (21, 3)
right_hand_history = []  # Will store numpy arrays of shape (21, 3)
projected_left_hand = np.zeros((21, 3))  # Project all landmarks
projected_right_hand = np.zeros((21, 3))  # Project all landmarks

left_hand_absent_count = 0
right_hand_absent_count = 0

def landmarks_to_array(landmarks):
    """Convert MediaPipe landmarks to numpy array with shape (21, 3)"""
    if landmarks is None:
        return None
    arr = np.array([[landmark.x * COLOUR_IMAGE_FULL_WIDTH,
                     -landmark.z * COLOUR_IMAGE_FULL_WIDTH,
                     landmark.y * COLOUR_IMAGE_FULL_HEIGHT] for landmark in landmarks])
    return arr

with HandLandmarker.create_from_options(hand_landmarker_options) as hand_landmarker:
    print("Ready.")

    try:
        while not shutdown_flag:
            # Wait for new frame with a timeout to check shutdown flag
            result = win32event.WaitForSingleObject(ready_event, 100)  # 100ms timeout
            if result == win32event.WAIT_TIMEOUT:
                continue
            elif result != win32event.WAIT_OBJECT_0:
                # This should never happen
                raise Exception("WaitForSingleObject returned unexpected result: " + str(result))

            # Read the frame
            with timer("Reading frame"):
                colour_image_buffer.seek(0)
                colour_image_data = np.frombuffer(colour_image_buffer, dtype=np.uint8).reshape(
                    (COLOUR_IMAGE_FULL_HEIGHT, COLOUR_IMAGE_FULL_WIDTH, COLOUR_IMAGE_NUM_CHANNELS))

            # Perform hand landmarking
            with timer("Creating mp image"):
                mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=colour_image_data)
            with timer("Hand landmarking"):
                hand_landmarking_result = hand_landmarker.detect_for_video(mp_image, int(time.monotonic()*1000))

            # determine handedness and scale to pixel coordinates
            with timer("Processing results"):
                using_projected_hands = False

                if len(hand_landmarking_result.hand_landmarks) == 0:
                    left = None
                    right = None
                    left_hand_absent_count += 1
                    right_hand_absent_count += 1

                    # Fill in missing hands with projected positions if absent for <= 2 frames
                    if left_hand_absent_count <= 2:
                        left = projected_left_hand
                        using_projected_hands = True
                    if right_hand_absent_count <= 2:
                        right = projected_right_hand
                        using_projected_hands = True
                        
                elif len(hand_landmarking_result.hand_landmarks) <= 1:
                    left = None
                    right = None
                    for i, handedness in enumerate(hand_landmarking_result.handedness):
                        if handedness[0].category_name == "Left":
                            left = landmarks_to_array(hand_landmarking_result.hand_landmarks[i])
                            right_hand_absent_count += 1
                            left_hand_absent_count = 0
                        else:
                            right = landmarks_to_array(hand_landmarking_result.hand_landmarks[i])
                            left_hand_absent_count += 1
                            right_hand_absent_count = 0
                    
                    # Fill in missing hands with projected positions if absent for <= 2 frames
                    if left is None and left_hand_absent_count <= 2:
                        left = projected_left_hand
                        using_projected_hands = True
                    if right is None and right_hand_absent_count <= 2:
                        right = projected_right_hand
                        using_projected_hands = True

                elif len(hand_landmarking_result.hand_landmarks) == 2:
                    left_hand_absent_count = 0
                    right_hand_absent_count = 0
                    
                    if hand_landmarking_result.handedness[0][0].category_name == hand_landmarking_result.handedness[1][0].category_name:
                        # both hands have same handedness -> determine handedness manually
                        left = None
                        right = None
                        for i, landmarks in enumerate(hand_landmarking_result.hand_landmarks):
                            real_wrist = np.array([landmarks[0].x, landmarks[0].y, landmarks[0].z])
                            if (np.linalg.norm(real_wrist - projected_left_hand[0]) < np.linalg.norm(real_wrist - projected_right_hand[0]) and \
                                    left is None) or right is not None:
                                left = landmarks_to_array(landmarks)
                            else:
                                right = landmarks_to_array(landmarks)
                    else:
                        left = None
                        right = None
                        for i, handedness in enumerate(hand_landmarking_result.handedness):
                            if handedness[0].category_name == "Left":
                                left = landmarks_to_array(hand_landmarking_result.hand_landmarks[i])
                            else:
                                right = landmarks_to_array(hand_landmarking_result.hand_landmarks[i])
                else:
                    print("Warning: More than 2 hands detected.")
                    left = None
                    right = None
                    for i, handedness in enumerate(hand_landmarking_result.handedness):
                        if handedness[0].category_name == "Left":
                            left = landmarks_to_array(hand_landmarking_result.hand_landmarks[i])
                        else:
                            right = landmarks_to_array(hand_landmarking_result.hand_landmarks[i])

            if VISUALISE_INFERENCE_RESULTS:
                with timer("Visualising results"):
                    scale = 1
                    vis_frame = cv2.cvtColor(colour_image_data, cv2.COLOR_RGB2BGR)
                    for landmarks, colour in ((left, (0, 255, 0)), (right, (0, 0, 255))):
                        if landmarks is not None:
                            for connection in HandLandmarksConnections.HAND_CONNECTIONS:
                                cv2.line(vis_frame, 
                                       (int(landmarks[connection.start, 0]),
                                        int(landmarks[connection.start, 2])),
                                       (int(landmarks[connection.end, 0]),
                                        int(landmarks[connection.end, 2])),
                                       colour, 1)
                            for landmark in landmarks:
                                cv2.circle(vis_frame, 
                                         (int(landmark[0]), int(landmark[2])), 
                                         3, colour, -1)
                    for landmarks in left_hand_history:
                        cv2.circle(vis_frame, (int(landmarks[0, 0]), int(landmarks[0, 2])), 3, (255, 0, 0), -1)
                    for landmarks in right_hand_history:
                        cv2.circle(vis_frame, (int(landmarks[0, 0]), int(landmarks[0, 2])), 3, (255, 0, 0), -1)
                    cv2.imshow("Inference visualisation", vis_frame)
                    key = cv2.waitKey(1)
                    # Check if window close button was clicked or window was closed
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
                projected_left_hand = left_hand_history[0]
                projected_right_hand = right_hand_history[0]
            else:
                # extrapolate all landmark locations using numpy operations
                last_left = left_hand_history[-1]
                prev_left = left_hand_history[-2]
                projected_left_hand = last_left + (last_left - prev_left)

                last_right = right_hand_history[-1]
                prev_right = right_hand_history[-2]
                projected_right_hand = last_right + (last_right - prev_right)

            # Write the hand landmarks
            with timer("Writing results"):
                hand_landmarks_buffer.seek(0)
                for i, hand in enumerate((left, right)):
                    if hand is None:
                        zeros = np.zeros((21, 3))
                        for j in range(21):
                            struct.pack_into("<fff", hand_landmarks_buffer, i*21*3*4 + j*3*4, 
                                           zeros[j, 0], zeros[j, 1], zeros[j, 2])
                    else:
                        for j in range(21):
                            struct.pack_into("<fff", hand_landmarks_buffer, i*21*3*4 + j*3*4,
                                           hand[j, 0], hand[j, 1], hand[j, 2])

            if not shutdown_flag:
                win32event.SetEvent(done_event)

    finally:
        # Clean up
        cv2.destroyAllWindows()
        del mp_image
        del colour_image_data
        colour_image_buffer.close()
        hand_landmarks_buffer.close()