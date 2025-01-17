using UnityEngine;
using System;
using Microsoft.Azure.Kinect.Sensor;

using UnityEngine.Profiling;

public class KinectAPI : MonoBehaviour
{

    [Header("Depth Calibrations")]
    [SerializeField, Range(300f, 1000f)] private int _MinimumSandDepth;
    [SerializeField, Range(600f, 1500f)] private int _MaximumSandDepth;


    //Internal Variables
    private Device kinect = null;
    private Transformation transformation = null;

    private int colourWidth = 0;

    private int colourHeight = 0;

    void Awake()
    {
        if (_MinimumSandDepth > _MaximumSandDepth) {
            Debug.LogError("Minimum depth is greater than maximum depth");
        }

        this.kinect = Device.Open();

        // Configure camera modes
        this.kinect.StartCameras(new DeviceConfiguration
        {
            ColorFormat = ImageFormat.ColorBGRA32,
            ColorResolution = ColorResolution.R1080p,
            DepthMode = DepthMode.NFOV_2x2Binned,
            SynchronizedImagesOnly = true
        });

        Debug.Log("AKDK Serial Number: " + this.kinect.SerialNum);

        // Initialize the transformation engine
        this.transformation = this.kinect.GetCalibration().CreateTransformation();

        this.colourWidth = this.kinect.GetCalibration().ColorCameraCalibration.ResolutionWidth;
        this.colourHeight = this.kinect.GetCalibration().ColorCameraCalibration.ResolutionHeight;

    }

    void OnApplicationQuit()
    {
        this.kinect.Dispose();
    }

    [SerializeField] private bool running = false;

    public int getWidth() {
        return colourWidth;
    }

    public int getHeight() {
        return colourHeight;
    }

    public void GetDepthTextureFromKinect(ref Texture2D depthMapTexture) {

        using (Capture capture = kinect.GetCapture())
        using (Image transformedDepth = new Image(ImageFormat.Depth16, colourWidth, colourHeight, colourWidth * sizeof(UInt16))) {
            // Transform the depth image to the colour capera perspective

            transformation.DepthImageToColorCamera(capture, transformedDepth);

            // Create Depth Buffer
            Span<ushort> depthBuffer = transformedDepth.GetPixels<ushort>().Span;

            Color32[] colourArray = new Color32[depthBuffer.Length];

            // Create a new image with data from the depth and colour image
            for (int i = 0; i < depthBuffer.Length; i++) {
                var depth = depthBuffer[i];

                // Calculate pixel values
                float depthRange = _MaximumSandDepth - _MinimumSandDepth;
                float pixelValue = 255 - ((depth - _MinimumSandDepth) / depthRange * 255);

                Color32 colour;
                if (depth == 0) // No depth image
                {
                    colour = new Color32(0, 0, 0, 0);

                } else if (depth >= _MaximumSandDepth) {
                    colour = new Color32(0, 0, 0, 0);

                } else if (depth < _MinimumSandDepth) {

                    colour = new Color32(Convert.ToByte(255), 0, 0, 0);

                } else {
                    colour = new Color32(Convert.ToByte((int) pixelValue), 0, 0, 0);

                }

                colourArray[i] = colour;

            }

            // Set texture pixels
            depthMapTexture.SetPixels32(colourArray);
            depthMapTexture.Apply();

            return;
        }
    }
}

