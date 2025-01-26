using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class PythonManager : MonoBehaviour
{
    // // Temporary way to test
    // private void Start()
    // {
    //     Main();
    // }
        
    
    [ContextMenu("Main")]
    void Main()
    {
        try
        {
            using (TcpClient client = new TcpClient("localhost", 9455))
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[1024];
                int bytesRead;

                Debug.Log("Connected to the Python server.");
                    
                stream.Write(Encoding.UTF8.GetBytes("START"));
                Debug.Log("Sent: START");
                    
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