using UnityEngine;
using System.Threading;
using Microsoft.Azure.Kinect.Sensor;

public class TestingUtilities : MonoBehaviour
{
    // ReSharper disable Unity.PerformanceAnalysis
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            PythonManager.Connect();
            Thread.Sleep(1000);
            PythonManager.StartObjectDetection();
            Thread.Sleep(1000);
            Debug.Log(PythonManager.GetSandboxObjects());
            Thread.Sleep(1000);
            // PythonManager.SendColorImage(new Image(ImageFormat.ColorMJPG, 1280, 720));
            Thread.Sleep(1000);
            Debug.Log(PythonManager.GetSandboxObjects());
            Thread.Sleep(1000);
            Debug.Log(PythonManager.GetSandboxObjects());
            Thread.Sleep(1000);
            PythonManager.StopObjectDetection();
            Thread.Sleep(5000);
            PythonManager.Disconnect();
        }
    }
}