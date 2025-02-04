using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using Microsoft.Azure.Kinect.Sensor;

public class TestingUtilities : MonoBehaviour
{
    private int _pythonTestIndex = 0;
    private Device _kinect;
    
    // ReSharper disable Unity.PerformanceAnalysis
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            PythonManager.Connect();
            _kinect = Device.Open();
            Thread.Sleep(1000);
            _kinect.StartCameras(new DeviceConfiguration {
                ColorFormat = ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R720p,
                DepthMode = DepthMode.NFOV_2x2Binned,
                SynchronizedImagesOnly = true,
                CameraFPS = FPS.FPS30
            });
            PythonManager.StartObjectDetection();
            Thread.Sleep(100);
            _pythonTestIndex = 1;
        }

        // test PythonManager
        if (_pythonTestIndex is >= 1 and <= 2000)
        {
            Debug.Log(ToString(PythonManager.GetSandboxObjects()));
            Thread.Sleep(10);
            // Debug.Log("[PythonManager] Getting capture...");
            var capture = _kinect.GetCapture();
            // Debug.Log("[PythonManager] Sending color image...");
            Debug.Log("[TestingUtilities] capture shape: " + capture.Color.WidthPixels + "x" + capture.Color.HeightPixels);
            PythonManager.SendColorImage(capture.Color);
            Thread.Sleep(10);
            _pythonTestIndex++;
        }
        else if (_pythonTestIndex > 2000)
        {
            PythonManager.StopObjectDetection();
            _kinect.StopCameras();
            Thread.Sleep(1000);
            _kinect.Dispose();
            PythonManager.Disconnect();
            _pythonTestIndex = 0;
        }

        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            PythonManager.Connect();
            _kinect = Device.Open();
            _kinect.StartCameras(new DeviceConfiguration {
                ColorFormat = ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R720p,
                DepthMode = DepthMode.NFOV_2x2Binned,
                SynchronizedImagesOnly = true,
                CameraFPS = FPS.FPS30
            });
        }

        if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            var colorImage = _kinect.GetCapture().Color;
            PythonManager.SendColorImage(colorImage);
        }
        
        if (Input.GetKeyDown(KeyCode.Keypad4))
        {
            PythonManager.Disconnect();
            _kinect.StopCameras();
            _kinect.Dispose();
        }
    }


    public static string ToString<T>(IEnumerable<T> array)
    {
        return $"[{string.Join(", ", array)}]";
    }
}