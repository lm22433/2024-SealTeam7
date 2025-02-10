import json
import socket
import threading
import time
import traceback
from typing import Optional

import cv2
import mediapipe as mp
import numpy as np
from mediapipe.tasks.python import BaseOptions
from mediapipe.tasks.python.vision import *


OBJECT_DETECTION_MODEL_PATH = 'object_detection_model.tflite'
HAND_LANDMARKING_MODEL_PATH = 'hand_landmarking_model.task'
HOST = "127.0.0.1"
PORT = 65465

MOCK_KINECT = False
"""Mock the Kinect camera using OpenCV to read from a webcam"""

VISUALISE_INFERENCE_RESULTS = True
"""Display the video overlaid with hand landmarks and bounding boxes around detected objects"""

stop_server = False
object_detection_result: Optional[ObjectDetectorResult] = None
object_detection_done = threading.Event()
hand_landmarking_result: Optional[HandLandmarkerResult] = None
hand_landmarking_done = threading.Event()
color_image = np.zeros((256, 256, 4), dtype=np.uint8)  # 256x256 BGRA image
color_image_lock = threading.Lock()


def object_detector_callback(result, output_image, timestamp_ms):
    global object_detection_result

    object_detection_result = result
    object_detection_done.set()
    
    
def hand_landmarker_callback(result, output_image, timestamp_ms):
    global hand_landmarking_result

    hand_landmarking_result = result
    hand_landmarking_done.set()


def inference_frame(object_detector, hand_landmarker, frame):
    mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=cv2.cvtColor(frame, cv2.COLOR_BGRA2RGB))
    timestamp_ms = int(time.time() * 1000)
    object_detector.detect_async(mp_image, timestamp_ms)
    hand_landmarker.detect_async(mp_image, timestamp_ms)
    object_detection_done.wait()  # block until inference is done for this frame
    hand_landmarking_done.wait()
    object_detection_done.clear()
    hand_landmarking_done.clear()
    
    # filter out object detections that are obviously wrong
    detections = list(filter(lambda d: d.bounding_box.width < 25 and d.bounding_box.height < 25, 
                        object_detection_result.detections))
    
    if VISUALISE_INFERENCE_RESULTS:
        scale = 3
        frame = cv2.resize(frame, (frame.shape[1] * scale, frame.shape[0] * scale), 
                           interpolation=cv2.INTER_NEAREST)
        for detection in detections:
            bbox = detection.bounding_box
            x = int(bbox.origin_x * scale)
            y = int(bbox.origin_y * scale)
            w = int(bbox.width * scale)
            h = int(bbox.height * scale)
            cv2.rectangle(frame, (x, y), (x+w, y+h), (0, 255, 0), 1)
            cv2.putText(frame, detection.categories[0].category_name, (x, y-4),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.4, (0, 255, 0), 1)
        for landmarks in hand_landmarking_result.hand_landmarks:  # for each hand
            for connection in HandLandmarksConnections.HAND_CONNECTIONS:
                cv2.line(frame, (int(landmarks[connection.start].x * frame.shape[1]), 
                                 int(landmarks[connection.start].y * frame.shape[0])),
                                (int(landmarks[connection.end].x * frame.shape[1]), 
                                 int(landmarks[connection.end].y * frame.shape[0])), 
                                (0, 255, 0), 1)
            for landmark in landmarks:
                cv2.circle(frame, (int(landmark.x * frame.shape[1]), 
                                   int(landmark.y * frame.shape[0])), 3, (0, 255, 0), -1)
        cv2.imshow("Inference visualisation", frame)
        cv2.waitKey(1)
    
    return {"objects": [{"type": detection.categories[0].category_name,
                         "x": detection.bounding_box.origin_x + detection.bounding_box.width / 2,
                         "y": detection.bounding_box.origin_y + detection.bounding_box.height / 2}
                        for detection in detections]}


def inference_connection(conn):
    global stop_server
    
    conn.setblocking(True)  # block until START control signal received
    conn.settimeout(0.5)  # instead of blocking for ages, check every 0.5s, in case of KeyboardInterrupt
    inference_running = False

    object_detector_options = ObjectDetectorOptions(
        base_options=BaseOptions(model_asset_path=OBJECT_DETECTION_MODEL_PATH),
        running_mode=RunningMode.LIVE_STREAM,
        max_results=5,
        result_callback=object_detector_callback)
    hand_landmarker_options = HandLandmarkerOptions(
        base_options=BaseOptions(model_asset_path=HAND_LANDMARKING_MODEL_PATH),
        running_mode=RunningMode.LIVE_STREAM,
        num_hands=2,
        min_hand_detection_confidence=0.5,
        min_hand_presence_confidence=0.5,
        min_tracking_confidence=0.5,
        result_callback=hand_landmarker_callback)

    with conn, ObjectDetector.create_from_options(object_detector_options) as object_detector, \
            HandLandmarker.create_from_options(hand_landmarker_options) as hand_landmarker:
        while not stop_server:
            try:
                # Check if any control signals in buffer
                try:
                    # print("[inference_connection] conn.recv()")
                    message = conn.recv(1024).decode()
                    print(f"[inference_connection] Received message: {message}")
                    if message == "START":
                        inference_running = True
                        conn.setblocking(False)  # no longer should block and wait for control signals
                        if MOCK_KINECT: video_capture = cv2.VideoCapture(0)
                    elif message == "STOP":
                        inference_running = False
                        conn.setblocking(True)  # block until START control signal received again
                        if MOCK_KINECT: video_capture.release()
                    elif message == "":  # Empty string means client disconnected
                        print("[inference_connection] Client disconnected, closing connection.")
                        break
                except BlockingIOError:
                    pass  # No control signal in buffer - that's normal, continue with object detection
                except socket.timeout:
                    pass  # Still waiting for control signal - OK

                # Main logic - read frame from camera, run object detection, send result to client
                if inference_running:
                    if MOCK_KINECT:
                        success, frame = video_capture.read()
                        if not success:
                            print("[inference_connection] Failed to read frame from VideoCapture.")
                            continue
                    else:
                        # print("[inference_connection] Acquiring lock...")
                        color_image_lock.acquire()
                        frame = color_image.copy()
                        color_image_lock.release()

                    data = inference_frame(object_detector, hand_landmarker, frame)
                    data_json = json.dumps(data)
                    try:
                        conn.send(data_json.encode())
                    except BlockingIOError:
                        print("[inference_connection] Failed to send data, buffer full.")

            except KeyboardInterrupt:
                print("[inference_connection] Closing connection.")
                break

            except Exception:
                print(traceback.format_exc(), end="")
                print("[inference_connection] Error, closing connection.")
                break
                
        stop_server = True  # if this thread is stopping, all threads should stop

    # clean up
    cv2.destroyAllWindows()
    if 'video_capture' in locals() and video_capture.isOpened():
        video_capture.release()


def image_connection(conn):
    global stop_server
    
    conn.setblocking(True)
    conn.settimeout(0.5)
    cumulative_message = b''

    with conn:
        while not stop_server:
            try:
                # handle receiving message
                # print("[image_connection] conn.recv()")
                image_length = 256 * 256 * 4
                message = conn.recv(image_length)
                if message == b'':  # Empty string means client disconnected
                    print("[image_connection] Client disconnected, closing connection.")
                    break
                elif len(message) == image_length:
                    # print("[image_connection] Full message received.")
                    cumulative_message = message
                elif 1 <= len(message) < image_length:
                    cumulative_message += message
                    # print(f"[image_connection] Partial message received. "
                    #       f"(length: {len(message)}, cumulative length: {len(cumulative_message)})")
                else:
                    print(f"[image_connection] Invalid message received, ignoring. (length: {len(message)})")
                    continue

                # Decode full message as image and store in color_image
                if len(cumulative_message) == image_length:
                    # Save image onto disk - temp
                    # image = np.frombuffer(cumulative_message, dtype=np.uint8).reshape(color_image.shape)
                    # image = image[90:470, 370:870, :3]
                    # cv2.imwrite(f"{time.strftime("%Y-%m-%d_%H-%M-%S")}_{str(time.time())[11:14]}.png", image)
                    
                    # print("[image_connection] Acquiring lock...")
                    color_image_lock.acquire()
                    # print("[image_connection] Copying image.")
                    np.copyto(color_image, np.frombuffer(cumulative_message, dtype=np.uint8).reshape(color_image.shape))
                    color_image_lock.release()
                    cumulative_message = b''
                
            except socket.timeout:
                # print("[color_image_connection] Temp - timeout")
                pass

            except KeyboardInterrupt:
                print("[image_connection] Closing connection.")
                break

            except Exception:
                print(traceback.format_exc(), end="")
                print("[image_connection] Error, closing connection.")
                break
                
        stop_server = True  # if this thread is stopping, all threads should stop


def main():
    global stop_server
    
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server:
        server.settimeout(5)
        server.bind((HOST, PORT))
        server.listen()
        print(f"Server listening on {HOST}:{PORT}...")
        
        while True:
            try:
                # print("server.accept()")
                inf_conn, addr = server.accept()  # this will frequently raise a timeout exception and skip the below
                print(f"Inference connection from {addr}")
                inf_thread = threading.Thread(target=inference_connection, args=(inf_conn,))
                inf_thread.start()
        
                im_conn, addr = server.accept()
                print(f"Image connection from {addr}")
                image_connection(im_conn)
                
                # at this point the client has disconnected -> join thread and then listen again
                inf_thread.join()
                stop_server = False
                print(f"Server listening on {HOST}:{PORT}...")
                
            except socket.timeout:
                pass  # OK, just keep listening
            except KeyboardInterrupt:
                print("Shutting down server.")
                break
            except Exception:
                print(traceback.format_exc(), end="")
                print("Error, shutting down server.")
                break


main()
