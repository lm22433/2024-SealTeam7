import socket
import json
import time
import cv2
import mediapipe as mp
from mediapipe.tasks.python import BaseOptions
from mediapipe.tasks.python.components.containers.detections import DetectionResult
from mediapipe.tasks.python.vision import ObjectDetector, ObjectDetectorOptions, RunningMode


model_path = 'model.tflite'
object_detection_done = False


def object_detector_callback(result, output_image, timestamp_ms):
    print(f'detection result: {result}')
    
    
def inference_frame(frame):
    mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=frame)
    timestamp_ms = int(time.time() * 1000)
    object_detector.detect_async(mp_image, timestamp_ms)
    # block until callback called
    return {"objects": [{"type": "spawner", "x": 34, "y": 52}]}


options = ObjectDetectorOptions(
    base_options=BaseOptions(model_asset_path=model_path),
    running_mode=RunningMode.LIVE_STREAM,
    max_results=5,
    result_callback=object_detector_callback)

object_detector = ObjectDetector.create_from_options(options)


def handle_client(client_socket):
    client_socket.setblocking(False)  # when receiving, if nothing is in the buffer just skip
    object_detection_running = False
    video_capture = None

    while True:
        try:
            # Check if any control signals in buffer
            try:
                message = client_socket.recv(1024).decode()
                if message:
                    message = message.strip()
                    print(f"Control signal received: {message}")
                    if message == "START":
                        object_detection_running = True
                        video_capture = cv2.VideoCapture(0)
                    elif message == "STOP":
                        object_detection_running = False
                        video_capture.release()
                    elif message == "EXIT":
                        break
            except BlockingIOError:
                pass  # No control signal in buffer, continue

            if object_detection_running:
                success, frame = video_capture.read()
                if success:
                    data = inference_frame(frame)
                    data_json = json.dumps(data)
                    client_socket.send(data_json.encode())
                else:
                    print("Failed to read frame.")

        except ConnectionResetError:
            print("Connection lost.")
            break
            
        except KeyboardInterrupt:
            print("Shutting down server.")
            break
            
        except Exception as error:
            print(error)
            print("Error, shutting down server.")
            break

        time.sleep(0.1)

    # clean up
    if video_capture is not None and video_capture.isOpened():
        video_capture.release()
    client_socket.close()
    object_detector.close()


def start_server(host="127.0.0.1", port=9455):  # 127.0.0.1 is localhost
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.bind((host, port))
    server.listen(5)
    print(f"Server listening on {host}:{port}")

    client_socket, addr = server.accept()
    print(f"Connection from {addr}")
    handle_client(client_socket) 


start_server()