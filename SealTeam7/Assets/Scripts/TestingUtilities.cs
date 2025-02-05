using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using Microsoft.Azure.Kinect.Sensor;
using UnityEngine.Rendering.UI;

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
    void Update()
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
            PythonManager.Disconnect();
            _kinect.StopCameras();
            _kinect.Dispose();
            Debug.Log("Disconnected and stopped.");
        }
    }


    public static string ToString<T>(IEnumerable<T> array)
    {
        return $"[{string.Join(", ", array)}]";
    }
}