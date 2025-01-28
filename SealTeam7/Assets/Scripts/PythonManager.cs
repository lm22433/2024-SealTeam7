using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public static class PythonManager
{
    private const string Host = "localhost";
    private const int Port = 9455;
    private static Encoding _encoding = Encoding.UTF8;
    
    private static TcpClient _client;
    private static NetworkStream _stream;
    private static byte[] _buffer = new byte[1024];
    
    private static Thread _receiveMessagesThread;
    private static bool _stopReceivingMessages = false;
    
    
    public static void Connect()
    {
        if (_client != null)
        {
            Debug.LogWarning("Already connected to the Python server.");
            return;
        }
        
        Debug.Log("Connecting to the Python server...");
        _client = new TcpClient("localhost", 9455);
        _stream = _client.GetStream();
        _stopReceivingMessages = false;
        _receiveMessagesThread = new Thread(ReceiveMessages);
        _receiveMessagesThread.Start();
        Debug.Log("Connected.");
    }
    
    
    public static void StartObjectDetection()
    {
        if (_stream == null)
        {
            Debug.LogWarning("Not connected to the Python server.");
            return;
        }
        
        _stream.Write(Encoding.UTF8.GetBytes("START"));
        Debug.Log("Sent: START");
    }
    
    
    private static void ReceiveMessages()
    {
        try
        {
            int bytesRead;
            // Good practice to check if bytesRead > 0 in case the connection is closed
            while ((bytesRead = _stream.Read(_buffer, 0, _buffer.Length)) > 0 && !_stopReceivingMessages)
            {
                string receivedData = Encoding.UTF8.GetString(_buffer, 0, bytesRead);
                Debug.Log($"Received: {receivedData}");

                // Parse JSON data
                // var data = JObject.Parse(receivedData);
                // string objName = data["objects"].ToString();
                // int x = data["x"].ToObject<int>();
                // int y = data["y"].ToObject<int>();
                // Console.WriteLine($"Object: {objName}, X: {x}, Y: {y}");
            }
        }
        catch (SocketException e)
        {
            Debug.Log("SocketException (normal while closing the connection I think)");
        }
    }
    
    
    public static void StopObjectDetection()
    {
        if (_stream == null)
        {
            Debug.LogWarning("Not connected to the Python server.");
            return;
        }

        _stream.Write(Encoding.UTF8.GetBytes("STOP"));
        Debug.Log("Sent: STOP");
    }

    
    public static void Disconnect()
    {
        if (_stream == null)
        {
            Debug.LogWarning("Not connected to the Python server.");
            return;
        }
        
        Debug.Log("Disconnecting from the Python server...");
        _stopReceivingMessages = true;
        _stream.Close();
        _client.Close();
        _receiveMessagesThread.Join();
        _stream = null;
        _client = null;
        Debug.Log("Disconnected.");
    }
}