import os
import socket
import json
import threading

import time
import cv2
import mediapipe as mp
from mediapipe.tasks.python import BaseOptions
from mediapipe.tasks.python.components.containers.detections import DetectionResult
from mediapipe.tasks.python.vision import ObjectDetector, ObjectDetectorOptions, RunningMode


model_path = 'model.tflite'
object_detection_result = None
object_detection_done = threading.Event()


def object_detector_callback(result, output_image, timestamp_ms):
    global object_detection_result

    print(f'detection result: {result}')
    object_detection_result = result
    object_detection_done.set()


def inference_frame(object_detector, frame):
    mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=frame)
    timestamp_ms = int(time.time() * 1000)
    object_detector.detect_async(mp_image, timestamp_ms)
    object_detection_done.wait()  # block until object_detector_callback called
    object_detection_done.clear()
    return {"objects": [{"type": detection.categories[0].category_name,
                         "x": detection.bounding_box.origin_x + detection.bounding_box.width / 2,
                         "y": detection.bounding_box.origin_y + detection.bounding_box.height / 2}
                        for detection in object_detection_result.detections]}


def handle_client(conn):
    conn.setblocking(True)  # block until START control signal received
    object_detection_running = False
    video_capture = None

    options = ObjectDetectorOptions(
        base_options=BaseOptions(model_asset_path=model_path),
        running_mode=RunningMode.LIVE_STREAM,
        max_results=5,
        result_callback=object_detector_callback)

    with ObjectDetector.create_from_options(options) as object_detector:
        while True:
            try:
                # Check if any control signals in buffer
                if not object_detection_running:
                    print("Waiting for START control signal...")
                try:
                    message = conn.recv(1024).decode()
                    print(f"Control signal received: {message}")
                    if message == "START":
                        object_detection_running = True
                        conn.setblocking(False)  # no longer should block and wait for control signals
                        video_capture = cv2.VideoCapture(0)
                    elif message == "STOP":
                        object_detection_running = False
                        conn.setblocking(True)  # block until START control signal received again
                        video_capture.release()
                    elif message == "":  # Empty string means client disconnected
                        print("Client disconnected, closing server.")
                        break
                except BlockingIOError:
                    pass  # No control signal in buffer - that's normal, continue

                # Main logic - read frame from camera, run object detection, send result to client
                if object_detection_running:
                    success, frame = video_capture.read()
                    if success:
                        data = inference_frame(object_detector, frame)
                        data_json = json.dumps(data)
                        conn.send(data_json.encode())
                    else:
                        print("Failed to read frame.")

            except KeyboardInterrupt:
                print("Shutting down server.")
                break

            except Exception as error:
                print(error)
                print("Error, shutting down server.")
                break

    # clean up
    if video_capture is not None and video_capture.isOpened():
        video_capture.release()
    cv2.destroyAllWindows()


def start_server(host="127.0.0.1", port=9455):  # 127.0.0.1 is localhost
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server:
        server.bind((host, port))
        server.listen(5)
        print(f"Server listening on {host}:{port}")

        conn, addr = server.accept()
        print(f"Connection from {addr}")
        with conn:
            handle_client(conn)


start_server()

# # debug why it stops with SIGSEGV
# for thread in threading.enumerate():
#     print(f"Thread name: {thread.name}, Is daemon: {thread.daemon}, Is alive: {thread.is_alive()}")
# proc = psutil.Process()
# print("Open files:", proc.open_files())
# print("Child processes:", proc.children())
# print("Threads:", proc.threads())