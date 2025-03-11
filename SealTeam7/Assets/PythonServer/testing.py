import cv2
import numpy as np
import mmap
import struct
import time
from contextlib import contextmanager
import posix_ipc

# Configuration from main.py
COLOUR_IMAGE_FULL_WIDTH = 1920
COLOUR_IMAGE_FULL_HEIGHT = 1080
COLOUR_IMAGE_NUM_CHANNELS = 3
COLOUR_IMAGE_FULL_SIZE = COLOUR_IMAGE_FULL_WIDTH * COLOUR_IMAGE_FULL_HEIGHT * COLOUR_IMAGE_NUM_CHANNELS
COLOUR_IMAGE_FILE_NAME = "colour_image"
READY_EVENT_NAME = "/SealTeam7ColourImageReady"
DONE_EVENT_NAME = "/SealTeam7HandLandmarksDone"

@contextmanager
def timer(name):
    start = time.time()
    yield
    end = time.time()
    print(f"{name}: {int((end - start)*1000)} ms")

# Create/open semaphores for synchronization
ready_event = posix_ipc.Semaphore(READY_EVENT_NAME, posix_ipc.O_CREAT)
done_event = posix_ipc.Semaphore(DONE_EVENT_NAME, posix_ipc.O_CREAT)

# Initialize shared memory
colour_image_buffer = mmap.mmap(-1, COLOUR_IMAGE_FULL_SIZE, tagname=COLOUR_IMAGE_FILE_NAME)

# Initialize webcam
cap = cv2.VideoCapture(0)
cap.set(cv2.CAP_PROP_FRAME_WIDTH, COLOUR_IMAGE_FULL_WIDTH)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, COLOUR_IMAGE_FULL_HEIGHT)

try:
    while True:
        # Capture frame from webcam
        ret, frame = cap.read()
        if not ret:
            print("Failed to capture frame")
            continue
            
        # Convert BGR to RGB since Unity expects RGB
        frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        
        # Resize if needed
        if frame_rgb.shape != (COLOUR_IMAGE_FULL_HEIGHT, COLOUR_IMAGE_FULL_WIDTH, COLOUR_IMAGE_NUM_CHANNELS):
            frame_rgb = cv2.resize(frame_rgb, (COLOUR_IMAGE_FULL_WIDTH, COLOUR_IMAGE_FULL_HEIGHT))
        
        # Write frame to shared memory
        with timer("Writing frame"):
            colour_image_buffer.seek(0)
            colour_image_buffer.write(frame_rgb.tobytes())
            
        # Signal that new frame is ready
        ready_event.release()
        
        # Wait for processing to complete
        done_event.acquire()

finally:
    # Cleanup
    cap.release()
    colour_image_buffer.close()
    ready_event.close()
    done_event.close()
    ready_event.unlink()
    done_event.unlink()