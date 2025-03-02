print("Initialising server...")

import struct
import mmap
import time
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
VISUALISE_INFERENCE_RESULTS = False
"""Display the video overlaid with hand landmarks and bounding boxes around detected objects"""

@contextmanager
def timer(name):
    start = time.time()
    yield
    end = time.time()
    print(f"{name}: {int((end - start)*1000)} ms")

# Create events
ready_event = win32event.CreateEvent(None, 0, 0, READY_EVENT_NAME)
done_event = win32event.CreateEvent(None, 0, 0, DONE_EVENT_NAME)

# Initialise the mapping
colour_image_buffer = mmap.mmap(-1, COLOUR_IMAGE_FULL_SIZE, access=mmap.ACCESS_WRITE, tagname=COLOUR_IMAGE_FILE_NAME)
hand_landmarks_buffer = mmap.mmap(-1, HAND_LANDMARKS_SIZE, access=mmap.ACCESS_WRITE, tagname=HAND_LANDMARKS_FILE_NAME)

hand_landmarker_options = HandLandmarkerOptions(
    base_options=BaseOptions(model_asset_path=HAND_LANDMARKING_MODEL_PATH),
    running_mode=RunningMode.VIDEO,
    num_hands=2,
    min_hand_detection_confidence=0.5,
    min_hand_presence_confidence=0.5,
    min_tracking_confidence=0.5)

with HandLandmarker.create_from_options(hand_landmarker_options) as hand_landmarker:
    print("Ready.")

    try:
        while True:
            # Wait for new frame
            result = win32event.WaitForSingleObject(ready_event, win32event.INFINITE)
            if result != win32event.WAIT_OBJECT_0:
                print("Error: Sender not responding")
                break

            # Read the frame
            with timer("Reading frame"):
                colour_image_buffer.seek(0)
                colour_image_data = np.frombuffer(colour_image_buffer, dtype=np.uint8).reshape(
                    (COLOUR_IMAGE_FULL_HEIGHT, COLOUR_IMAGE_FULL_WIDTH, COLOUR_IMAGE_NUM_CHANNELS))

            # Perform hand landmarking
            with timer("Hand landmarking"):
                mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=colour_image_data)
                hand_landmarking_result = hand_landmarker.detect_for_video(mp_image, int(time.monotonic()*1000))

            # determine handedness and scale to pixel coordinates
            with timer("Processing results"):
                left = None
                right = None
                for i, handedness in enumerate(hand_landmarking_result.handedness):  # iterates over each hand
                    if handedness[0].category_name == "Left":  # the [0] is ignoreable
                        landmarks = hand_landmarking_result.hand_landmarks[i]
                        left = [{"x": landmark.x * COLOUR_IMAGE_FULL_WIDTH,
                                 "y": -landmark.z * COLOUR_IMAGE_FULL_WIDTH,  # z is depth so it's the negative y coordinate
                                 "z": landmark.y * COLOUR_IMAGE_FULL_HEIGHT}
                                for landmark in landmarks] if landmarks is not None else None
                    else:
                        landmarks = hand_landmarking_result.hand_landmarks[i]
                        right = [{"x": landmark.x * COLOUR_IMAGE_FULL_WIDTH,
                                  "y": -landmark.z * COLOUR_IMAGE_FULL_WIDTH,
                                  "z": landmark.y * COLOUR_IMAGE_FULL_HEIGHT}
                                 for landmark in landmarks] if landmarks is not None else None
                    # todo break if both are found, also change this so that we're not making a new list each time

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
                    cv2.imshow("Inference visualisation", vis_frame)
                    cv2.waitKey(1)

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

            # Signal that frame has been processed
            win32event.SetEvent(done_event)

    except KeyboardInterrupt:
        print("Shutting down server.")

    finally:
        # Clean up
        cv2.destroyAllWindows()
        colour_image_buffer.close()
        hand_landmarks_buffer.close()
        win32event.CloseHandle(ready_event)
        win32event.CloseHandle(done_event)