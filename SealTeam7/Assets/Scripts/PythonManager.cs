using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Azure.Kinect.Sensor;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Logger = Microsoft.Azure.Kinect.Sensor.Logger;

public static class PythonManager
{
    private const string Host = "localhost";
    private const int Port = 65465;
    private static readonly Encoding Encoding = Encoding.UTF8;
    
    private static TcpClient _inferenceClient;
    private static NetworkStream _inferenceStream;
    private static TcpClient _imageClient;
    private static NetworkStream _imageStream;
    private static Thread _receiveMessagesThread;
    private static bool _stopReceivingMessages = false;
    private static ConcurrentBag<SandboxObject> _sandboxObjects = new();
    
    
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
    
    
    public static SandboxObject[] GetSandboxObjects()
    {
        return _sandboxObjects.ToArray();
    }
    
    
    public static void SendColorImage(Image colorImage)
    {
        if (!IsConnected())
        {
            Debug.LogWarning("Cannot send color image: Not connected to the Python server.");
            return;
        }
        
        _imageStream.Write(colorImage.Memory.ToArray());
    }
    
    
    private static void ReceiveInferenceResults()
    {
        try
        {
            var buffer = new byte[1024];
            int bytesRead;
            // Good practice to check if bytesRead > 0 in case the connection is closed
            while ((bytesRead = _inferenceStream.Read(buffer, 0, buffer.Length)) > 0 && !_stopReceivingMessages)
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