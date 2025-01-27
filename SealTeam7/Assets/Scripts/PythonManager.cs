using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public static class PythonManager
{
    private const string Host = "localhost";
    private const int Port = 9455;
    private static Encoding _encoding = Encoding.UTF8;
    
    private static TcpClient _client;
    private static NetworkStream _stream;
    
    private static byte[] _buffer = new byte[1024];
    private static int _bytesRead;
    
    public static void Connect()
    {
        if (_client != null)
        {
            Debug.Log("Already connected to the Python server.");
            return;
        }
        _client = new TcpClient("localhost", 9455);
        _stream = _client.GetStream();
        Debug.Log("Connected to the Python server.");
    }
    
    
    public static void StartObjectDetection()
    {
        _stream.Write(Encoding.UTF8.GetBytes("START"));
        Debug.Log("Sent: START");
    }
    
    [ContextMenu("Main")]
    static void Main()
    {
        try
        {
            using (TcpClient client = new TcpClient("localhost", 9455))
            using (NetworkStream stream = client.GetStream())
            {
                
                    
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log($"Received: {receivedData}");

                    // Parse JSON data
                    // var data = JObject.Parse(receivedData);
                    // string objName = data["objects"].ToString();
                    // int x = data["x"].ToObject<int>();
                    // int y = data["y"].ToObject<int>();
                    // Console.WriteLine($"Object: {objName}, X: {x}, Y: {y}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"Error: {ex.Message}");
        }
    }
}