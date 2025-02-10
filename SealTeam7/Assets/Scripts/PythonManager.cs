using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Azure.Kinect.Sensor;
using Newtonsoft.Json.Linq;
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


    private static byte[] ResizeAndPad(byte[] src, int srcWidth, int srcHeight)
    {
        const int dstSize = 256;  // Output image is dstSize x dstSize pixels
        const int cropX = 356;
        const int cropY = 74;
        const int cropWidth = 571;
        const int cropHeight = 420;
        
        // Allocate output buffer: 256x256 pixels, 4 bytes per pixel
        var dst = new byte[dstSize * dstSize * 4];

        // Fill the output image with black (B=0, G=0, R=0) and opaque alpha (255)
        for (var i = 0; i < dst.Length; i += 4)
        {
            dst[i]     = 0;   // Blue
            dst[i + 1] = 0;   // Green
            dst[i + 2] = 0;   // Red
            dst[i + 3] = 255; // Alpha
        }

        // Calculate scale factor: fit the crop into dstSize while maintaining aspect ratio
        const float scaleX = (float)dstSize / cropWidth;
        const float scaleY = (float)dstSize / cropHeight;
        var scale = Math.Min(scaleX, scaleY);

        // Determine scaled image size (might be less than 256 in one dimension)
        var scaledWidth  = (int)(cropWidth * scale);
        var scaledHeight = (int)(cropHeight * scale);

        // Compute offsets so the scaled image is centered in the 256x256 output
        var offsetX = (dstSize - scaledWidth) / 2;
        var offsetY = (dstSize - scaledHeight) / 2;

        // Loop over each pixel in the scaled image region
        for (var y = 0; y < scaledHeight; y++)
        {
            var dstY = y + offsetY;
            for (var x = 0; x < scaledWidth; x++)
            {
                var dstX = x + offsetX;
                // Find the corresponding source pixel using nearest neighbor.
                // (x/scale, y/scale) gives the relative position in the crop region.
                var srcRelX = (int)(x / scale);
                var srcRelY = (int)(y / scale);
                var srcXCoord = cropX + srcRelX;
                var srcYCoord = cropY + srcRelY;

                // (Optional) Bounds check in case the crop rectangle is at the edge.
                if (srcXCoord < 0 || srcXCoord >= srcWidth || srcYCoord < 0 || srcYCoord >= srcHeight)
                    continue;

                // Compute indices into the BGRA byte arrays.
                var srcIndex = (srcYCoord * srcWidth + srcXCoord) * 4;
                var dstIndex = (dstY * dstSize + dstX) * 4;

                dst[dstIndex]     = src[srcIndex];     // Blue
                dst[dstIndex + 1] = src[srcIndex + 1]; // Green
                dst[dstIndex + 2] = src[srcIndex + 2]; // Red
                dst[dstIndex + 3] = src[srcIndex + 3]; // Alpha
            }
        }

        return dst;
    }
}