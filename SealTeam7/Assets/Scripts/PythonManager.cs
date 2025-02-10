using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Azure.Kinect.Sensor;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Color = UnityEngine.Color;
using Debug = UnityEngine.Debug;

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

        var stopwatch = Stopwatch.StartNew();
        var resizedImage = ResizeAndPad(colorImage.Memory.ToArray(), colorImage.WidthPixels, colorImage.HeightPixels);
        stopwatch.Stop();
        Debug.Log($"\u250fPythonManager.ResizeAndPad: {stopwatch.ElapsedMilliseconds} ms");
        
        stopwatch.Restart();
        _imageStream.Write(resizedImage);  // 256x256 BGRA image
        stopwatch.Stop();
        Debug.Log($"\u2523ImageStream.Write: {stopwatch.ElapsedMilliseconds} ms");
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
                    SandboxObject sandboxObject;
                    switch (objName)
                    {
                        case "Bunker":
                            sandboxObject = new SandboxObject.Bunker(x, y);
                            break;
                        case "Spawner":
                            sandboxObject = new SandboxObject.Spawner(x, y);
                            break;
                        default:
                            // Sometimes the model outputs "background" for some reason
                            continue; // Just ignore and move onto the next object
                    }

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


    private static ReadOnlySpan<byte> ResizeAndPad(byte[] bgraBytes, int originalWidth, int originalHeight)
    {
        var targetSize = new[] { 256, 256 };  // final width, height to give to model
        var crop = new[] { 356, 74, 571, 420 };  // x, y, width, height (in original image)

        // Convert BGRA to Texture2D
        var stopwatch = Stopwatch.StartNew();
        var texture2D = new Texture2D(originalWidth, originalHeight, TextureFormat.BGRA32, false);
        texture2D.LoadRawTextureData(bgraBytes);
        stopwatch.Stop();
        Debug.Log($"   \u250fConvert BGRA to Texture2D: {stopwatch.ElapsedMilliseconds} ms");

        // Crop (probably won't be necessary in the final version as it should already be cropped)
        stopwatch.Restart();
        var croppedPixels = texture2D.GetPixels(crop[0], crop[1], crop[2], crop[3]);
        texture2D.Reinitialize(crop[2], crop[3]);
        texture2D.SetPixels(croppedPixels);
        texture2D.Apply();
        stopwatch.Stop();
        Debug.Log($"   \u2523Crop: {stopwatch.ElapsedMilliseconds} ms");

        // Determine scaling factor while maintaining aspect ratio
        var scale = Mathf.Min((float)targetSize[0] / crop[2], (float)targetSize[1] / crop[3]);
        var newWidth = Mathf.RoundToInt(crop[2] * scale);
        var newHeight = Mathf.RoundToInt(crop[3] * scale);

        // Resize the texture - need to use RenderTexture (GPU texture) as only it supports resizing
        stopwatch.Restart();
        var renderTexture = RenderTexture.GetTemporary(newWidth, newHeight, 
            0, RenderTextureFormat.BGRA32, RenderTextureReadWrite.Default);
        RenderTexture.active = renderTexture;
        Graphics.Blit(texture2D, renderTexture);  // this is what resizes the image
        stopwatch.Stop();
        Debug.Log($"   \u2523Resize with Blit: {stopwatch.ElapsedMilliseconds} ms");

        // Reinitialise the texture with the new size
        stopwatch.Restart();
        texture2D.Reinitialize(targetSize[0], targetSize[1]);
        var blackPixels = new Color[targetSize[0] * targetSize[1]];
        for (var i = 0; i < blackPixels.Length; i++)
        {
            blackPixels[i] = Color.black;
        }
        texture2D.SetPixels(blackPixels);
        stopwatch.Stop();
        Debug.Log($"   \u2523Reinitialise with new size: {stopwatch.ElapsedMilliseconds} ms");

        // Draw the resized image in the centre
        stopwatch.Restart();
        var offsetX = (targetSize[0] - newWidth) / 2;
        var offsetY = (targetSize[1] - newHeight) / 2;
        texture2D.ReadPixels(new Rect(0, 0, newWidth, newHeight), offsetX, offsetY);
        RenderTexture.ReleaseTemporary(renderTexture);
        stopwatch.Stop();
        Debug.Log($"   \u2523Draw in centre: {stopwatch.ElapsedMilliseconds} ms");

        // Debug.Log(texture2D.width);
        // Debug.Log(texture2D.height);
        // Debug.Log(texture2D.format);

        return texture2D.GetPixelData<byte>(0);
    }
}