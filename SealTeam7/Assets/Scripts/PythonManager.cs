using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Azure.Kinect.Sensor;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class PythonManager
{
    private const string Host = "localhost";
    private const int Port = 9455;
    private static readonly Encoding Encoding = Encoding.UTF8;
    
    private static TcpClient _detectionsClient;
    private static NetworkStream _detectionsStream;
    private static TcpClient _colorImageClient;
    private static NetworkStream _colorImageStream;
    private static Thread _receiveMessagesThread;
    private static bool _stopReceivingMessages = false;
    private static ConcurrentBag<SandboxObject> _sandboxObjects = new();
    
    
    public static void Connect()
    {
        if (_detectionsClient != null)
        {
            Debug.LogWarning("Already connected to the Python server.");
            return;
        }
        
        Debug.Log("Connecting to the Python server...");
        _detectionsClient = new TcpClient(Host, Port);
        _detectionsStream = _detectionsClient.GetStream();
        _colorImageClient = new TcpClient(Host, Port);
        _colorImageStream = _colorImageClient.GetStream();
        _stopReceivingMessages = false;
        _receiveMessagesThread = new Thread(ReceiveMessages);
        _receiveMessagesThread.Start();
        Debug.Log("Connected.");
    }
    
    
    public static void Disconnect()
    {
        if (_detectionsStream == null)
        {
            Debug.LogWarning("Not connected to the Python server.");
            return;
        }
        
        Debug.Log("Disconnecting from the Python server...");
        _stopReceivingMessages = true;
        _detectionsStream.Close();
        _detectionsClient.Close();
        _colorImageStream.Close();
        _colorImageClient.Close();
        _receiveMessagesThread.Join();
        _detectionsStream = null;
        _detectionsClient = null;
        Debug.Log("Disconnected.");
    }
    
    
    public static void StartObjectDetection()
    {
        if (_detectionsStream == null)
        {
            Debug.LogWarning("Not connected to the Python server.");
            return;
        }
        
        _detectionsStream.Write(Encoding.GetBytes("START"));
        Debug.Log("Sent: START");
    }
    
    
    public static void StopObjectDetection()
    {
        if (_detectionsStream == null)
        {
            Debug.LogWarning("Not connected to the Python server.");
            return;
        }

        _detectionsStream.Write(Encoding.GetBytes("STOP"));
        Debug.Log("Sent: STOP");
    }
    
    
    public static SandboxObject[] GetSandboxObjects()
    {
        return _sandboxObjects.ToArray();
    }
    
    
    public static void SendColorImage(Image colorImage)
    {
        if (_colorImageStream == null)
        {
            Debug.LogWarning("Not connected to the Python server.");
            return;
        }
        
        // Debug.Log($"Image.Format: {colorImage.Format.ToString()}");
        // Debug.Log($"Memory.Length: {colorImage.Memory.ToArray().Length}");  // number of bytes, as Memory<byte>
        
        _colorImageStream.Write(colorImage.Memory.ToArray());
    }
    
    
    private static void ReceiveMessages()
    {
        try
        {
            var buffer = new byte[1024];
            int bytesRead;
            // Good practice to check if bytesRead > 0 in case the connection is closed
            while ((bytesRead = _detectionsStream.Read(buffer, 0, buffer.Length)) > 0 && !_stopReceivingMessages)
            {
                var receivedData = Encoding.GetString(buffer, 0, bytesRead);
                // Debug.Log($"Received: {receivedData}");
                var objects = JObject.Parse(receivedData)["objects"];
                _sandboxObjects.Clear();
                foreach (var obj in objects!)
                {
                    var objName = obj["type"]!.ToString();
                    var x = obj["x"]!.ToObject<float>();
                    var y = obj["y"]!.ToObject<float>();
                    SandboxObject sandboxObject = objName switch
                    {
                        "Bunker" => new SandboxObject.Bunker(x, y),
                        "Spawner" => new SandboxObject.Spawner(x, y),
                        _ => throw new Exception($"Unknown object type: {objName}")
                    };
                    // Debug.Log(sandboxObject);
                    _sandboxObjects.Add(sandboxObject);
                }
            }
        }
        catch (IOException)
        {
            Debug.Log("IOException (normal while closing the connection)");
        }
    }
}