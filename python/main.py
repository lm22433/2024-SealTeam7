import json
import socket
import threading
import time

import cv2
import mediapipe as mp
import numpy as np
from mediapipe.tasks.python import BaseOptions
from mediapipe.tasks.python.vision import ObjectDetector, ObjectDetectorOptions, RunningMode


MODEL_PATH = 'model.tflite'
HOST = "127.0.0.1"
PORT = 9455

MOCK_KINECT = True
"""Mock the Kinect camera using OpenCV to read from a webcam"""

object_detection_result = None
object_detection_done = threading.Event()
color_image = np.zeros((1280, 720, 4), dtype=np.uint8)  # 720p BGRA image
color_image_lock = threading.Lock()


def object_detector_callback(result, output_image, timestamp_ms):
    global object_detection_result

    object_detection_result = result
    object_detection_done.set()


def inference_frame(object_detector, frame):
    # not sure if image_format specifies the required format for data, or the format of new mp.Image. If it's the
    # former, we should convert the frame to RGB before passing it to mp.Image
    mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=frame)
    timestamp_ms = int(time.time() * 1000)
    object_detector.detect_async(mp_image, timestamp_ms)
    object_detection_done.wait()  # block until object_detector_callback called
    object_detection_done.clear()
    return {"objects": [{"type": detection.categories[0].category_name,
                         "x": detection.bounding_box.origin_x + detection.bounding_box.width / 2,
                         "y": detection.bounding_box.origin_y + detection.bounding_box.height / 2}
                        for detection in object_detection_result.detections]}


def detections_connection(conn):
    conn.setblocking(True)  # block until START control signal received
    object_detection_running = False

    options = ObjectDetectorOptions(
        base_options=BaseOptions(model_asset_path=MODEL_PATH),
        running_mode=RunningMode.LIVE_STREAM,
        max_results=5,
        result_callback=object_detector_callback)

    with conn, ObjectDetector.create_from_options(options) as object_detector:
        while True:
            try:
                # Check if any control signals in buffer
                if not object_detection_running:
                    print("Waiting for control signal...")
                try:
                    message = conn.recv(1024).decode()
                    print(f"Control signal received: {message}")
                    if message == "START":
                        object_detection_running = True
                        conn.setblocking(False)  # no longer should block and wait for control signals
                        if MOCK_KINECT: video_capture = cv2.VideoCapture(0)
                    elif message == "STOP":
                        object_detection_running = False
                        conn.setblocking(True)  # block until START control signal received again
                        if MOCK_KINECT: video_capture.release()
                    elif message == "":  # Empty string means client disconnected
                        print("Client disconnected, closing connection.")
                        break
                except BlockingIOError:
                    pass  # No control signal in buffer - that's normal, continue

                # Main logic - read frame from camera, run object detection, send result to client
                if object_detection_running:
                    if MOCK_KINECT:
                        success, frame = video_capture.read()
                        if not success:
                            print("Failed to read frame from VideoCapture.")
                            continue
                    else:
                        color_image_lock.acquire()
                        frame = color_image.copy()
                        color_image_lock.release()

                    data = inference_frame(object_detector, frame)
                    data_json = json.dumps(data)
                    conn.send(data_json.encode())

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


def color_image_connection(conn):
    conn.setblocking(True)

    with conn:
        while True:
            try:
                print("Waiting for color image message...")
                message = conn.recv(3686400)  # 1280 * 720 * 4 bytes per pixel
                print(f"Message: {message}")
                if message == b'':  # Empty string means client disconnected
                    print("Client disconnected, closing connection.")
                    break
                else:  # Decode message as image and store in color_image
                    color_image_lock.acquire()
                    np.copyto(color_image, np.frombuffer(message, dtype=np.uint8).reshape(color_image.shape))
                    color_image_lock.release()

            except KeyboardInterrupt:
                print("Shutting down server.")
                break

            except Exception as error:
                print(error)
                print("Error, shutting down server.")
                break


def main():
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server:
        server.bind((HOST, PORT))
        server.listen(5)
        print(f"Server listening on {HOST}:{PORT}")

        det_conn, addr = server.accept()
        print(f"Detections connection from {addr}")
        threading.Thread(target=detections_connection, args=(det_conn,)).start()

        color_im_conn, addr = server.accept()
        print(f"Color image connection from {addr}")
        color_image_connection(color_im_conn)

    # exiting - clean up
    cv2.destroyAllWindows()


main()

# # debug why it stops with SIGSEGV
# for thread in threading.enumerate():
#     print(f"Thread name: {thread.name}, Is daemon: {thread.daemon}, Is alive: {thread.is_alive()}")
# proc = psutil.Process()
# print("Open files:", proc.open_files())
# print("Child processes:", proc.children())
# print("Threads:", proc.threads())