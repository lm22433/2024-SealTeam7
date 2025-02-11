using UnityEngine;
using Microsoft.Azure.Kinect.Sensor;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Debug = UnityEngine.Debug;

public class TestingUtilities : MonoBehaviour
{
    private float _nextBeep = float.MaxValue;
    private float _nextCapture = float.MaxValue;
    private Device _kinect;
    private AudioSource _audioSource;


    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }


    // ReSharper disable Unity.PerformanceAnalysis
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            Debug.Log("Connecting to Python and starting Kinect cameras...");
            PythonManager.Connect();
            _kinect = Device.Open();
            _kinect.StartCameras(new DeviceConfiguration {
                ColorFormat = ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R720p,
                DepthMode = DepthMode.NFOV_Unbinned,
                SynchronizedImagesOnly = true,
                CameraFPS = FPS.FPS30
            });
            Debug.Log("Connected and started.");

            _nextBeep = Time.realtimeSinceStartup + 5;
            _nextCapture = Time.realtimeSinceStartup + 6;
        }

        if (Time.realtimeSinceStartup > _nextBeep)
        {
            _audioSource.Play();
            _nextBeep = Time.realtimeSinceStartup + 6;
        }

        if (Time.realtimeSinceStartup > _nextCapture)
        {
            _audioSource.Play();
            
            try
            {
                var capture = _kinect.GetCapture();
                PythonManager.SendColorImage(capture.Color);
            }
            catch (AzureKinectException e)
            {
                Debug.LogWarning("Failed to get capture due to the following error:");
                Debug.LogError(e);
            }

            _nextCapture = Time.realtimeSinceStartup + 6;
        }

        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            Debug.Log("Connecting to Python and starting Kinect cameras...");
            PythonManager.Connect();
            // Thread.Sleep(5000);
            PythonManager.StartInference();
            _kinect = Device.Open();
            _kinect.StartCameras(new DeviceConfiguration {
                ColorFormat = ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R720p,
                DepthMode = DepthMode.NFOV_Unbinned,
                SynchronizedImagesOnly = true,
                CameraFPS = FPS.FPS30
            });
            Debug.Log("Connected and started.");
        }

        if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            var colorImage = _kinect.GetCapture().Color;
            PythonManager.SendColorImage(colorImage);
            Debug.Log("Image capture taken.");
        }
        
        if (Input.GetKeyDown(KeyCode.Keypad4))
        {
            Debug.Log("Disconnecting from Python and stopping Kinect cameras...");
            _nextBeep = float.MaxValue;
            _nextCapture = float.MaxValue;
            PythonManager.StopInference();
            // Thread.Sleep(5000);
            PythonManager.Disconnect();
            _kinect.StopCameras();
            _kinect.Dispose();
            Debug.Log("Disconnected and stopped.");
        }

        if (PythonManager.IsConnected())
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var capture = _kinect.GetCapture();
                stopwatch.Stop();
                Debug.Log($"Kinect.GetCapture: {stopwatch.ElapsedMilliseconds} ms");

                stopwatch.Restart();
                PythonManager.SendColorImage(capture.Color);
                stopwatch.Stop();
                Debug.Log($"PythonManager.SendColorImage: {stopwatch.ElapsedMilliseconds} ms");
            
                capture.Dispose();
            }
            catch (AzureKinectException e)
            {
                Debug.LogError("Failed to get capture due to the following error:");
                Debug.LogError(e);
                Debug.LogError("Log messages:");
                Debug.LogError(ToString<LogMessage>(e.LogMessages));
                
                _kinect.StopCameras();
                _kinect.Dispose();
                _kinect = Device.Open();
                _kinect.StartCameras(new DeviceConfiguration {
                    ColorFormat = ImageFormat.ColorBGRA32,
                    ColorResolution = ColorResolution.R720p,
                    DepthMode = DepthMode.NFOV_Unbinned,
                    SynchronizedImagesOnly = true,
                    CameraFPS = FPS.FPS30
                });
            }
            
            stopwatch.Restart();
            Debug.Log(ToString(PythonManager.GetSandboxObjects()));
            stopwatch.Stop();
            Debug.Log($"PythonManager.GetSandboxObjects: {stopwatch.ElapsedMilliseconds} ms");
        }
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