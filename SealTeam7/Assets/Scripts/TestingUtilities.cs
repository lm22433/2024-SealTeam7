using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using Microsoft.Azure.Kinect.Sensor;

public class TestingUtilities : MonoBehaviour
{
    private int _pythonTestIndex = 0;
    
    // ReSharper disable Unity.PerformanceAnalysis
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            PythonManager.Connect();
            Thread.Sleep(1000);
            PythonManager.StartObjectDetection();
            Thread.Sleep(100);
            _pythonTestIndex = 1;
        }

        // test PythonManager
        if (_pythonTestIndex is >= 1 and <= 10)
        {
            Debug.Log(ToString(PythonManager.GetSandboxObjects()));
            Thread.Sleep(500);
            // PythonManager.SendColorImage(new Image(ImageFormat.ColorBGRA32, 1280, 720));
            Thread.Sleep(500);
            _pythonTestIndex++;
        }
        else if (_pythonTestIndex > 10)
        {
            PythonManager.StopObjectDetection();
            Thread.Sleep(1000);
            PythonManager.Disconnect();
            _pythonTestIndex = 0;
        }
    }


    public static string ToString<T>(IEnumerable<T> array)
    {
        return $"[{string.Join(", ", array)}]";
    }
}