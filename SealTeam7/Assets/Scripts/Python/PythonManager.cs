using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Azure.Kinect.Sensor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Python
{
    public static class PythonManager
    {
        private const string Host = "localhost";
        private const int Port = 65465;
        private static readonly Encoding Encoding = Encoding.UTF8;

        private const int PythonImageWidth = 256;
        private const int PythonImageHeight = 256;
        private const int ImageCropX = 356;
        private const int ImageCropY = 130;
        private const int ImageCropWidth = 575;
        private const int ImageCropHeight = 440;

        private static readonly float ImageScale =
            Math.Min((float)PythonImageWidth / ImageCropWidth, (float)PythonImageHeight / ImageCropHeight);

        private static readonly int ImageScaledWidth = (int)(ImageCropWidth * ImageScale);
        private static readonly int ImageScaledHeight = (int)(ImageCropHeight * ImageScale);
        private static readonly int ImageOffsetX = (PythonImageWidth - ImageScaledWidth) / 2;
        private static readonly int ImageOffsetY = (PythonImageHeight - ImageScaledHeight) / 2;

        private static TcpClient _inferenceClient;
        private static NetworkStream _inferenceStream;
        private static TcpClient _imageClient;
        private static NetworkStream _imageStream;
        private static Thread _receiveMessagesThread;
        private static bool _stopReceivingMessages = false;
        private static HandLandmarks _handLandmarks;
        private static Vector3[] _leftHandLandmarks;  // Keep a reference separately as they can be null in HandLandmarks
        private static Vector3[] _rightHandLandmarks;


        public static bool Connect()
        {
            if (IsConnected())
            {
                Debug.LogWarning("Already connected to the Python server.");
                return true;
            }

            Debug.Log("Connecting to the Python server...");
            try
            {
                _inferenceClient = new TcpClient(Host, Port);
                _inferenceStream = _inferenceClient.GetStream();
                _imageClient = new TcpClient(Host, Port);
                _imageStream = _imageClient.GetStream();
            }
            catch (SocketException e)
            {
                Debug.LogError(e);
                Debug.LogError("Error connecting to the Python server. Is it running?");
                return false;
            }

            _stopReceivingMessages = false;
            _receiveMessagesThread = new Thread(ReceiveInferenceResults);
            _receiveMessagesThread.Start();
            
            HandLandmarks = new HandLandmarks
            {
                Left = null,
                Right = null
            };
            _leftHandLandmarks = new Vector3[20];
            _rightHandLandmarks = new Vector3[20];
            
            Debug.Log("Connected.");
            return true;
        }


        public static bool Disconnect()
        {
            if (!IsConnected())
            {
                Debug.LogWarning("Already disconnected from the Python server.");
                return true;
            }

            Debug.Log("Disconnecting from the Python server...");
            _stopReceivingMessages = true;
            _inferenceStream.Close();
            _inferenceClient.Close();
            _imageStream.Close();
            _imageClient.Close();
            _receiveMessagesThread.Join();
            _inferenceStream = null;
            _inferenceClient = null;
            _imageStream = null;
            _imageClient = null;
            Debug.Log("Disconnected.");
            return true;
        }


        public static bool IsConnected()
        {
            return _inferenceClient != null;
        }


        public static bool StartInference()
        {
            if (!IsConnected())
            {
                Debug.LogWarning("Cannot start inference: Not connected to the Python server.");
                return false;
            }

            _inferenceStream.Write(Encoding.GetBytes("START"));
            Debug.Log("Sent: START");
            return true;
        }


        public static bool StopInference()
        {
            if (!IsConnected())
            {
                Debug.LogWarning("Cannot stop inference: Not connected to the Python server.");
                return false;
            }

            _inferenceStream.Write(Encoding.GetBytes("STOP"));
            Debug.Log("Sent: STOP");
            return true;
        }


        // public static SandboxObject[] GetSandboxObjects()
        // {
        //     return _sandboxObjects.ToArray();
        // }
        
        
        public static HandLandmarks HandLandmarks { 
            get => _handLandmarks;
            private set => _handLandmarks = value;
        }


        public static void SendColorImage(Image colorImage)
        {
            if (!IsConnected())
            {
                Debug.LogWarning("Cannot send color image: Not connected to the Python server.");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var span = colorImage.Memory.Span;
            stopwatch.Stop();
            Debug.Log($"\u250fMemory.Span: {stopwatch.ElapsedMilliseconds} ms");

            stopwatch.Restart();
            var resizedImage = ResizeAndPad(span, colorImage.WidthPixels, colorImage.HeightPixels);
            stopwatch.Stop();
            Debug.Log($"\u2523PythonManager.ResizeAndPad: {stopwatch.ElapsedMilliseconds} ms");

            stopwatch.Restart();
            _imageStream.Write(resizedImage); // 256x256 BGRA image
            stopwatch.Stop();
            Debug.Log($"\u2523ImageStream.Write: {stopwatch.ElapsedMilliseconds} ms");
        }


        private static void ReceiveInferenceResults()
        {
            try
            {
                var buffer = new byte[1024];
                int bytesRead;
                // When the connection is closed, a message of length 0 will be sent so this loop will break
                while ((bytesRead = _inferenceStream.Read(buffer, 0, buffer.Length)) > 0 && !_stopReceivingMessages)
                {
                    var receivedData = Encoding.GetString(buffer, 0, bytesRead);
                    // Debug.Log($"Received: {receivedData}");

                    try
                    {
                        var receivedJson = JObject.Parse(receivedData);
                        // var objects = receivedJson["objects"];
                        // _sandboxObjects.Clear();
                        // foreach (var obj in objects!)
                        // {
                        //     var objName = obj["type"]!.ToString();
                        //     var x = obj["x"]!.ToObject<float>();
                        //     var y = obj["y"]!.ToObject<float>();
                        //
                        //     // Convert from cropped 256x256 image coordinates to 1280x720 image coordinates
                        //     x = (x - ImageOffsetX) / ImageScale + ImageCropX;
                        //     y = (y - ImageOffsetY) / ImageScale + ImageCropY;
                        //
                        //     SandboxObject sandboxObject;
                        //     switch (objName)
                        //     {
                        //         case "Bunker":
                        //             sandboxObject = new SandboxObject.Bunker(x, y);
                        //             break;
                        //         case "Spawner":
                        //             sandboxObject = new SandboxObject.Spawner(x, y);
                        //             break;
                        //         default:
                        //             // Sometimes the model outputs "background" for some reason
                        //             continue; // Just ignore and move onto the next object
                        //     }
                        //
                        //     // Debug.Log(sandboxObject);
                        //     _sandboxObjects.Add(sandboxObject);
                        // }
                        var handLandmarks = receivedJson["hand_landmarks"]!;
                        var left = handLandmarks["left"];
                        if (left != null)
                        {
                            for (var i = 0; i < 20; i++)
                            {
                                _leftHandLandmarks[i].x = (left[i]!["x"]!.ToObject<float>()
                                    - ImageOffsetX) / ImageScale + ImageCropX;
                                _leftHandLandmarks[i].y = (left[i]!["y"]!.ToObject<float>()
                                    - ImageOffsetY) / ImageScale + ImageCropY;
                                _leftHandLandmarks[i].z = left[i]!["z"]!.ToObject<float>() / ImageScale;
                            }
                            _handLandmarks.Left = _leftHandLandmarks;
                        }
                        else
                        {
                            _handLandmarks.Left = null;
                        }
                        var right = handLandmarks["right"];
                        if (right != null)
                        {
                            for (var i = 0; i < 20; i++)
                            {
                                _rightHandLandmarks[i].x = (right[i]!["x"]!.ToObject<float>()
                                    - ImageOffsetX) / ImageScale + ImageCropX;
                                _rightHandLandmarks[i].y = (right[i]!["y"]!.ToObject<float>()
                                    - ImageOffsetY) / ImageScale + ImageCropY;
                                _rightHandLandmarks[i].z = right[i]!["z"]!.ToObject<float>() / ImageScale;
                            }
                            _handLandmarks.Right = _rightHandLandmarks;
                        }
                        else
                        {
                            _handLandmarks.Right = null;
                        }
                    }
                    catch (JsonReaderException)
                    {
                        Debug.LogError("Failed to parse JSON. (This happens from time to time and is ok to ignore.)");
                    }
                }
            }
            catch (IOException)
            {
                Debug.Log("IOException (normal while closing the connection)");
            }
        }


        private static byte[] ResizeAndPad(Span<byte> src, int srcWidth, int srcHeight)
        {
            // Allocate output buffer: 256x256 pixels, 4 bytes per pixel
            var dst = new byte[PythonImageWidth * PythonImageHeight * 4];

            // Fill the output image with black (B=0, G=0, R=0) and opaque alpha (255)
            for (var i = 0; i < dst.Length; i += 4)
            {
                dst[i] = 0; // Blue
                dst[i + 1] = 0; // Green
                dst[i + 2] = 0; // Red
                dst[i + 3] = 255; // Alpha
            }

            // Loop over each pixel in the scaled image region
            for (var y = 0; y < ImageScaledHeight; y++)
            {
                var dstY = y + ImageOffsetY;
                for (var x = 0; x < ImageScaledWidth; x++)
                {
                    var dstX = x + ImageOffsetX;
                    // Find the corresponding source pixel using nearest neighbor.
                    // (x/scale, y/scale) gives the relative position in the crop region.
                    var srcRelX = (int)(x / ImageScale);
                    var srcRelY = (int)(y / ImageScale);
                    var srcXCoord = ImageCropX + srcRelX;
                    var srcYCoord = ImageCropY + srcRelY;

                    // (Optional) Bounds check in case the crop rectangle is at the edge.
                    if (srcXCoord < 0 || srcXCoord >= srcWidth || srcYCoord < 0 || srcYCoord >= srcHeight)
                        continue;

                    // Compute indices into the BGRA byte arrays.
                    var srcIndex = (srcYCoord * srcWidth + srcXCoord) * 4;
                    var dstIndex = (dstY * PythonImageWidth + dstX) * 4;

                    dst[dstIndex] = src[srcIndex]; // Blue
                    dst[dstIndex + 1] = src[srcIndex + 1]; // Green
                    dst[dstIndex + 2] = src[srcIndex + 2]; // Red
                    dst[dstIndex + 3] = src[srcIndex + 3]; // Alpha
                }
            }

            return dst;
        }
    }
}