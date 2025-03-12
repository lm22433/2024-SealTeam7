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

left_wrist_history = []
right_wrist_history = []
projected_left_wrist = np.array([0, 0, 0])
projected_right_wrist = np.array([0, 0, 0])
left_hand_absent_count = 0
right_hand_absent_count = 0

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
                if len(hand_landmarking_result.hand_landmarks) == 0:
                    left = None
                    right = None
                    left_hand_absent_count += 1
                    right_hand_absent_count += 1

                    # Fill in missing hands with projected positions if absent for <= 2 frames
                    if left_hand_absent_count <= 2:
                        left = [{"x": projected_left_wrist[0],
                               "y": projected_left_wrist[1],
                               "z": projected_left_wrist[2]}] * 21  # Replicate wrist position for all landmarks
                    if right_hand_absent_count <= 2:
                        right = [{"x": projected_right_wrist[0],
                                "y": projected_right_wrist[1],
                                "z": projected_right_wrist[2]}] * 21
                
                elif len(hand_landmarking_result.hand_landmarks) <= 1:
                    left = None
                    right = None
                    for i, handedness in enumerate(hand_landmarking_result.handedness):  # iterates over each hand
                        if handedness[0].category_name == "Left":  # the [0] is ignoreable
                            landmarks = hand_landmarking_result.hand_landmarks[i]
                            left = [{"x": landmark.x * COLOUR_IMAGE_FULL_WIDTH,
                                        "y": -landmark.z * COLOUR_IMAGE_FULL_WIDTH,  # z is depth so it's the negative y coordinate
                                        "z": landmark.y * COLOUR_IMAGE_FULL_HEIGHT}
                                    for landmark in landmarks] if landmarks is not None else None
                            right_hand_absent_count += 1
                            left_hand_absent_count = 0
                        else:
                            landmarks = hand_landmarking_result.hand_landmarks[i]
                            right = [{"x": landmark.x * COLOUR_IMAGE_FULL_WIDTH,
                                        "y": -landmark.z * COLOUR_IMAGE_FULL_WIDTH,
                                        "z": landmark.y * COLOUR_IMAGE_FULL_HEIGHT}
                                        for landmark in landmarks] if landmarks is not None else None
                            left_hand_absent_count += 1
                            right_hand_absent_count = 0
                    
                    # Fill in missing hands with projected positions if absent for <= 2 frames
                    if left is None and left_hand_absent_count <= 2:
                        left = [{"x": projected_left_wrist[0],
                               "y": projected_left_wrist[1],
                               "z": projected_left_wrist[2]}] * 21  # Replicate wrist position for all landmarks
                    if right is None and right_hand_absent_count <= 2:
                        right = [{"x": projected_right_wrist[0],
                                "y": projected_right_wrist[1],
                                "z": projected_right_wrist[2]}] * 21
                    
                    print("len=1, left=", left, "right=", right)

                elif len(hand_landmarking_result.hand_landmarks) == 2:
                    left_hand_absent_count = 0
                    right_hand_absent_count = 0
                    
                    if hand_landmarking_result.handedness[0][0].category_name == hand_landmarking_result.handedness[1][0].category_name:
                        # both hands have same handedness -> determine handedness manually
                        left = None
                        right = None
                        for i, landmarks in enumerate(hand_landmarking_result.hand_landmarks):
                            real_wrist = np.array([landmarks[0].x, landmarks[0].y, landmarks[0].z])
                            if (np.linalg.norm(real_wrist - projected_left_wrist) < np.linalg.norm(real_wrist - projected_right_wrist) and \
                                    left is None) or right is not None:
                                left = [{"x": landmark.x * COLOUR_IMAGE_FULL_WIDTH,
                                            "y": -landmark.z * COLOUR_IMAGE_FULL_WIDTH,
                                            "z": landmark.y * COLOUR_IMAGE_FULL_HEIGHT}
                                        for landmark in landmarks] if landmarks is not None else None
                            else:
                                right = [{"x": landmark.x * COLOUR_IMAGE_FULL_WIDTH,
                                            "y": -landmark.z * COLOUR_IMAGE_FULL_WIDTH,
                                            "z": landmark.y * COLOUR_IMAGE_FULL_HEIGHT}
                                            for landmark in landmarks] if landmarks is not None else None
                        print("len=2, manual handedness, left=", left, "right=", right)

                    else:
                        left = None
                        right = None
                        for i, handedness in enumerate(hand_landmarking_result.handedness):
                            if handedness[0].category_name == "Left":
                                landmarks = hand_landmarking_result.hand_landmarks[i]
                                left = [{"x": landmark.x * COLOUR_IMAGE_FULL_WIDTH,
                                            "y": -landmark.z * COLOUR_IMAGE_FULL_WIDTH,
                                            "z": landmark.y * COLOUR_IMAGE_FULL_HEIGHT}
                                        for landmark in landmarks] if landmarks is not None else None
                            else:
                                landmarks = hand_landmarking_result.hand_landmarks[i]
                                right = [{"x": landmark.x * COLOUR_IMAGE_FULL_WIDTH,
                                            "y": -landmark.z * COLOUR_IMAGE_FULL_WIDTH,
                                            "z": landmark.y * COLOUR_IMAGE_FULL_HEIGHT}
                                            for landmark in landmarks] if landmarks is not None else None
                        print("len=2, auto handedness, left=", left, "right=", right)
                else:
                    print("Warning: More than 2 hands detected.")
                    left = None
                    right = None
                    for i, handedness in enumerate(hand_landmarking_result.handedness):
                        if handedness[0].category_name == "Left":
                            landmarks = hand_landmarking_result.hand_landmarks[i]
                            left = [{"x": landmark.x * COLOUR_IMAGE_FULL_WIDTH,
                                        "y": -landmark.z * COLOUR_IMAGE_FULL_WIDTH,
                                        "z": landmark.y * COLOUR_IMAGE_FULL_HEIGHT}
                                    for landmark in landmarks] if landmarks is not None else None
                        else:
                            landmarks = hand_landmarking_result.hand_landmarks[i]
                            right = [{"x": landmark.x * COLOUR_IMAGE_FULL_WIDTH,
                                        "y": -landmark.z * COLOUR_IMAGE_FULL_WIDTH,
                                        "z": landmark.y * COLOUR_IMAGE_FULL_HEIGHT}
                                        for landmark in landmarks] if landmarks is not None else None
                    print("len>2, left=", left, "right=", right)

            if VISUALISE_INFERENCE_RESULTS:
                with timer("Visualising results"):
                    scale = 1
                    vis_frame = cv2.cvtColor(colour_image_data, cv2.COLOR_RGB2BGR)
                    for landmarks, colour in ((left, (0, 255, 0)), (right, (0, 0, 255))):
                        if landmarks is not None:
                            for connection in HandLandmarksConnections.HAND_CONNECTIONS:
                                cv2.line(vis_frame, (int(landmarks[connection.start]["x"]),
                                                        int(landmarks[connection.start]["z"])),
                                            (int(landmarks[connection.end]["x"]),
                                            int(landmarks[connection.end]["z"])),
                                            colour, 1)
                            for landmark in landmarks:
                                cv2.circle(vis_frame, (int(landmark["x"]),
                                                        int(landmark["z"])), 3, colour, -1)
                    for points in left_wrist_history:
                        cv2.circle(vis_frame, (int(points[0]), int(points[2])), 3, (255, 0, 0), -1)
                    for points in right_wrist_history:
                        cv2.circle(vis_frame, (int(points[0]), int(points[2])), 3, (255, 0, 0), -1)
                    if "projected_left_wrist" in vars(): cv2.circle(vis_frame, (int(projected_left_wrist[0]), int(projected_left_wrist[2])), 3, (255, 0, 255), -1)
                    if "projected_right_wrist" in vars(): cv2.circle(vis_frame, (int(projected_right_wrist[0]), int(projected_right_wrist[2])), 3, (255, 0, 255), -1)
                    cv2.imshow("Inference visualisation", vis_frame)
                    key = cv2.waitKey(1)
                    # Check if window close button was clicked or window was closed
                    if key == 27:
                        cv2.destroyWindow("Inference visualisation")
                        VISUALISE_INFERENCE_RESULTS = False

            # Keep track of previous results
            if len(left_wrist_history) >= 2:
                left_wrist_history.pop(0)
                right_wrist_history.pop(0)
            left_wrist_history.append(np.array([left[0]["x"], left[0]["y"], left[0]["z"]]) if left is not None else np.array([0, 0, 0]))
            right_wrist_history.append(np.array([right[0]["x"], right[0]["y"], right[0]["z"]]) if right is not None else np.array([0, 0, 0]))
            
            # Calculate projection to fill in gaps where model doesn't find hands
            if len(left_wrist_history) == 1:
                # use previous location of wrist
                projected_left_wrist = left_wrist_history[0]
                projected_right_wrist = right_wrist_history[0]
            else:
                # extrapolate wrist location
                projected_left_wrist = left_wrist_history[-1] + (left_wrist_history[-1] - left_wrist_history[-2])
                projected_right_wrist = right_wrist_history[-1] + (right_wrist_history[-1] - right_wrist_history[-2])

            # Write the hand landmarks
            with timer("Writing results"):
                hand_landmarks_buffer.seek(0)
                for i, hand in enumerate((left, right)):
                    if hand is None:
                        for j in range(21):
                            struct.pack_into("<fff", hand_landmarks_buffer, i*21*3*4 + j*3*4, 0, 0, 0)
                    else:
                        for j, landmark in enumerate(hand):
                            struct.pack_into("<fff", hand_landmarks_buffer, i*21*3*4 + j*3*4, landmark["x"], landmark["y"], landmark["z"])

            if not shutdown_flag:
                win32event.SetEvent(done_event)

    finally:
        # Clean up
        cv2.destroyAllWindows()
        del mp_image
        del colour_image_data
        colour_image_buffer.close()
        hand_landmarks_buffer.close()