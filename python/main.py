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

MOCK_KINECT = False
"""Mock the Kinect camera using OpenCV to read from a webcam"""

VISUALISE_DETECTIONS = True
"""Display a window with the video and bounding boxes around detected objects"""

stop_server = False
object_detection_result = None
object_detection_done = threading.Event()
color_image = np.zeros((720, 1280, 4), dtype=np.uint8)  # 720p BGRA image
color_image_lock = threading.Lock()


def object_detector_callback(result, output_image, timestamp_ms):
    global object_detection_result

    object_detection_result = result
    object_detection_done.set()


def inference_frame(object_detector, frame):
    mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=cv2.cvtColor(frame, cv2.COLOR_BGRA2RGB))
    timestamp_ms = int(time.time() * 1000)
    object_detector.detect_async(mp_image, timestamp_ms)
    object_detection_done.wait()  # block until object_detector_callback called
    object_detection_done.clear()
    
    if VISUALISE_DETECTIONS:
        for detection in object_detection_result.detections:
            bbox = detection.bounding_box
            cv2.rectangle(frame, (int(bbox.origin_x), int(bbox.origin_y)), 
                          (int(bbox.origin_x + bbox.width), int(bbox.origin_y + bbox.height)), (0, 255, 0), 2)
            cv2.putText(frame, detection.categories[0].category_name, (int(bbox.origin_x), int(bbox.origin_y + 10)),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.9, (36,255,12), 2)
            
        cv2.imshow("Object Detection", frame)
        cv2.waitKey(1)
    
    return {"objects": [{"type": detection.categories[0].category_name,
                         "x": detection.bounding_box.origin_x + detection.bounding_box.width / 2,
                         "y": detection.bounding_box.origin_y + detection.bounding_box.height / 2}
                        for detection in object_detection_result.detections]}


def detections_connection(conn):
    global stop_server
    
    conn.setblocking(True)  # block until START control signal received
    conn.settimeout(0.5)  # instead of blocking for ages, check every 0.5s, in case of KeyboardInterrupt
    object_detection_running = False

    options = ObjectDetectorOptions(
        base_options=BaseOptions(model_asset_path=MODEL_PATH),
        running_mode=RunningMode.LIVE_STREAM,
        max_results=5,
        result_callback=object_detector_callback)

    with conn, ObjectDetector.create_from_options(options) as object_detector:
        while not stop_server:
            try:
                # Check if any control signals in buffer
                if not object_detection_running:
                    # print("[detections_connection] Waiting for control signal...")
                    pass
                try:
                    message = conn.recv(1024).decode()
                    print(f"[detections_connection] Received message: {message}")
                    if message == "START":
                        object_detection_running = True
                        conn.setblocking(False)  # no longer should block and wait for control signals
                        if MOCK_KINECT: video_capture = cv2.VideoCapture(0)
                    elif message == "STOP":
                        object_detection_running = False
                        conn.setblocking(True)  # block until START control signal received again
                        if MOCK_KINECT: video_capture.release()
                    elif message == "":  # Empty string means client disconnected
                        print("[detections_connection] Client disconnected, closing connection.")
                        break
                except BlockingIOError:
                    pass  # No control signal in buffer - that's normal, continue with object detection
                except socket.timeout:
                    # print("[detections_connection] Temp - timeout. stop_server:", stop_server)
                    pass  # Still waiting for control signal - OK

                # Main logic - read frame from camera, run object detection, send result to client
                if object_detection_running:
                    if MOCK_KINECT:
                        success, frame = video_capture.read()
                        if not success:
                            print("[detections_connection] Failed to read frame from VideoCapture.")
                            continue
                    else:
                        # print("[detections_connection] Acquiring lock...")
                        color_image_lock.acquire()
                        frame = color_image.copy()
                        color_image_lock.release()

                    data = inference_frame(object_detector, frame)
                    data_json = json.dumps(data)
                    conn.send(data_json.encode())

            except KeyboardInterrupt:
                print("[detections_connection] Shutting down server.")
                break

            except Exception as error:
                print(error)
                print("[detections_connection] Error, shutting down server.")
                break
                
        stop_server = True  # if this thread is stopping, all threads should stop

    # clean up
    if 'video_capture' in locals() and video_capture.isOpened():
        video_capture.release()


def color_image_connection(conn):
    global stop_server
    
    conn.setblocking(True)
    conn.settimeout(0.5)
    cumulative_message = b''

    with conn:
        while not stop_server:
            try:
                # handle receiving message
                # print("[color_image_connection] Waiting for color image message...")
                message = conn.recv(3686500)  # 1280 * 720 * 4 bytes per pixel + some extra
                print("[color_image_connection] Received message.")
                if message == b'':  # Empty string means client disconnected
                    print("[color_image_connection] Client disconnected, closing connection.")
                    break
                elif len(message) == 3686400:
                    cumulative_message = message
                elif 1 <= len(message) < 3686400:
                    cumulative_message += message
                    print(f"[color_image_connection] Partial message received. "
                          f"(length: {len(message)}, cumulative length: {len(cumulative_message)})")
                else:
                    print(f"[color_image_connection] Invalid message received, ignoring. (length: {len(message)})")

                # Decode full message as image and store in color_image
                if len(cumulative_message) == 3686400:
                    #temp
                    image = np.frombuffer(cumulative_message, dtype=np.uint8).reshape(color_image.shape)
                    image = image[90:470, 370:870, :3]
                    cv2.imwrite(f"{time.strftime("%Y-%m-%d_%H-%M-%S")}_{str(time.time())[11:14]}.png", image)
                    
                    # print("[color_image_connection] Acquiring lock...")
                    color_image_lock.acquire()
                    # print("[color_image_connection] Copying image.")
                    np.copyto(color_image, np.frombuffer(cumulative_message, dtype=np.uint8).reshape(color_image.shape))
                    color_image_lock.release()
                    cumulative_message = b''
                
            except socket.timeout:
                # print("[color_image_connection] Temp - timeout")
                pass

            except KeyboardInterrupt:
                print("[color_image_connection] Shutting down server.")
                break

            except Exception as error:
                print(error)
                print("[color_image_connection] Error, shutting down server.")
                break
                
        stop_server = True  # if this thread is stopping, all threads should stop


def main():    
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server:
        server.bind((HOST, PORT))
        server.listen(5)
        print(f"Server listening on {HOST}:{PORT}...")

        det_conn, addr = server.accept()
        print(f"Detections connection from {addr}")
        det_thread = threading.Thread(target=detections_connection, args=(det_conn,))
        det_thread.start()

        color_im_conn, addr = server.accept()
        print(f"Color image connection from {addr}")
        color_image_connection(color_im_conn)

        det_thread.join()

    # exiting - clean up
    cv2.destroyAllWindows()


main()
