using UnityEngine;
using System.Threading;

public class TestingUtilities : MonoBehaviour
{
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
            Debug.Log(PythonManager.GetSandboxObjects());
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