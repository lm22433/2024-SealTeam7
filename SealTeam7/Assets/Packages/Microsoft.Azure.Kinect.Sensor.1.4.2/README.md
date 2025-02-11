## About
`Microsoft.Azure.Kinect.Sensor` contains cross platform libraries to build applications with Azure Kinect Dev Kit (AKDK)

## Quick links:
* [Azure Kinect Sensor SDK GitHub Repo](https://github.com/microsoft/Azure-Kinect-Sensor-SDK)
* [Using Azure Kinect SDK](https://github.com/microsoft/Azure-Kinect-Sensor-SDK/blob/develop/docs/usage.md)
* [API Documentation](https://microsoft.github.io/Azure-Kinect-Sensor-SDK/)
* [Azure Kinect DK documentation](https://learn.microsoft.com/en-us/azure/kinect-dk/)

## How to Use

#### Connect to AKDK and Capture Images (C#)

```C#
using System;
using Microsoft.Azure.Kinect.Sensor;

class Program
{
    private static void Main(string[] args)
    {
        using Device device = Device.Open(0);

        // Print Device Serial Number from Azure Kinect
        Console.WriteLine("AKDK Serial Number: {0}", device.SerialNum);

        // Start Cameras
        device.StartCameras(new DeviceConfiguration
        {
            ColorFormat = ImageFormat.ColorBGRA32,
            ColorResolution = ColorResolution.R720p,
            DepthMode = DepthMode.NFOV_2x2Binned,
            SynchronizedImagesOnly = true,
        });

        // Get 10 Captures from Azure Kinect       
        for (int i = 0; i < 10; i++)
        {
            using Capture capture = device.GetCapture();
            // Print Capture Temperature     
            Console.WriteLine("Capture {0}, Temperature: {1:F} C", i, capture.Temperature);           
        }       
    }
}
```


## Change Log
SDK v1.4.2 contains security fixes



