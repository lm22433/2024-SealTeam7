using UnityEngine;
using Microsoft.Azure.Kinect.Sensor;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Debug = UnityEngine.Debug;

namespace Python
{
    public class PythonTesting : MonoBehaviour
    {
        private Device _kinect;


        private void OnEnable()
        {
            Debug.Log("Connecting to Python and starting Kinect cameras...");
            PythonManager.Connect();
            // Thread.Sleep(5000);
            PythonManager.StartInference();
            _kinect = Device.Open();
            _kinect.StartCameras(new DeviceConfiguration
            {
                ColorFormat = ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R1080p,
                DepthMode = DepthMode.NFOV_Unbinned,
                SynchronizedImagesOnly = true,
                CameraFPS = FPS.FPS30
            });
            Debug.Log("Connected and started.");
        }


        // ReSharper disable Unity.PerformanceAnalysis
        private void Update()
        {
            if (PythonManager.IsConnected())
            {
                // var stopwatch = Stopwatch.StartNew();
                try
                {
                    var capture = _kinect.GetCapture();
                    // stopwatch.Stop();
                    // Debug.Log($"Kinect.GetCapture: {stopwatch.ElapsedMilliseconds} ms");
                
                    // stopwatch.Restart();
                    PythonManager.SendColorImage(capture.Color);
                    // stopwatch.Stop();
                    // Debug.Log($"PythonManager.SendColorImage: {stopwatch.ElapsedMilliseconds} ms");
                
                    capture.Dispose();
                }
                catch (AzureKinectException e)
                {
                    Debug.LogError("Failed to get capture due to the following error:");
                    Debug.LogError(e);
                }

                Debug.Log(PythonManager.HandLandmarks);
            }
        }


        private void OnDisable()
        {
            Debug.Log("Disconnecting from Python and stopping Kinect cameras...");
            PythonManager.StopInference();
            // Thread.Sleep(5000);
            PythonManager.Disconnect();
            _kinect.StopCameras();
            _kinect.Dispose();
            Debug.Log("Disconnected and stopped.");
        }


        public static string ToString<T>(T[] array)
        {
            return $"[{string.Join(", ", array)}]";
        }

        public static string ToString<T>(ICollection<T> collection)
        {
            return $"[{string.Join(", ", (string[])collection.Select(x => x.ToString()))}]";
        }
    }
}