using UnityEngine;
using System;
using Microsoft.Azure.Kinect.Sensor;

using UnityEngine.Profiling;

public class KinectAPI : MonoBehaviour
{

    [Header("Depth Calibrations")]
    [SerializeField, Range(300f, 1000f)] private int _MinimumSandDepth;
    [SerializeField, Range(600f, 1500f)] private int _MaximumSandDepth;

    [Header("IR Calibrations")]
    [SerializeField, Range(0, 255f)] private int _IRThreshold;

    [Header("Similarity Threshold")]
    [SerializeField, Range(0.5f, 1f)] private float _SimilarityThreshold;

    //Internal Variables
    private Device kinect = null;
    private Transformation transformation = null;

    private int colourWidth = 0;

    private int colourHeight = 0;

    private Texture2D depthMapTexture;
    private bool running;
    private int chuckSize;

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

    public void StartKinect(int dimensions) {
        depthMapTexture = new Texture2D(dimensions, dimensions);

        running = true;
    }

    void OnApplicationQuit()
    {
        running = false;
        this.kinect.Dispose();
    }

    public void GetChunkTexture(ref Texture2D texture, int chunkX, int chunkY) {
        Color32[] colourArray = texture.GetPixels32();
        float similarity = 0;

        int yChunkOffset = chunkY * chuckSize - 1;
        int xChunkOffset = chunkX * chuckSize - 1;

        //Similarity Check
        for (int y = 0; y <= chuckSize + 1; y++ ) {
            for (int x = 0; x <= chuckSize + 1; x++) {
                var col = colourArray[y * chuckSize + x];
                var curr = depthMapTexture.GetPixel(y + yChunkOffset, xChunkOffset + x);

                similarity += Mathf.Pow(Mathf.Abs(col.r - curr.r), 2);
            }
        }

        similarity = Mathf.Sqrt(similarity) / chuckSize;

        if (similarity > _SimilarityThreshold) {
            return;
        }

        //Write changed texture
        for (int y = (chunkY * chuckSize) - 1; y < (chuckSize * (chunkY + 1)); y++ ) {
            for (int x = (chunkX * chuckSize) - 1; x < (chuckSize * (chunkX + 1)); x++ ) {
                colourArray[y * chuckSize + x] = depthMapTexture.GetPixel(y + yChunkOffset, xChunkOffset + x);
            }
        }

        texture.SetPixels32(colourArray);
        texture.Apply();

    }

    private void Update() {
        if (running){
            GetDepthTextureFromKinect();
        }
    }

    private void GetDepthTextureFromKinect() {

        using (Capture capture = kinect.GetCapture())
        using (Image transformedDepth = new Image(ImageFormat.Depth16, colourWidth, colourHeight, colourWidth * sizeof(UInt16))) {
            // Transform the depth image to the colour capera perspective

            transformation.DepthImageToColorCamera(capture, transformedDepth);

            // Create Depth Buffer
            Span<ushort> depthBuffer = transformedDepth.GetPixels<ushort>().Span;
            Span<ushort> IRBuffer = capture.IR.GetPixels<ushort>().Span;

            Color32[] colourArray = depthMapTexture.GetPixels32();
            int imageXOffset = (colourWidth - depthMapTexture.width) / 2;
            int imageYOffset = (colourHeight - depthMapTexture.height) / 2;

            // Create a new image with data from the depth and colour image
            for (int y = 0; y < depthMapTexture.height; y++) {
                for (int x = 0; x < depthMapTexture.width; x++) {
                    var depth = depthBuffer[(y + imageYOffset) * colourWidth + imageXOffset + x];
                    var ir = IRBuffer[(y + imageYOffset) * colourWidth + imageXOffset + x];

                    // Calculate pixel values
                    float depthRange = _MaximumSandDepth - _MinimumSandDepth;
                    float pixelValue = 255 - ((depth - _MinimumSandDepth) / depthRange * 255);

                    if(ir < _IRThreshold) {    
                        Color32 colour;

                        if (depth == 0 || depth >= _MaximumSandDepth) // No depth image
                        {
                            colour = new Color32(0, 0, 0, 0);

                        } else if (depth < _MinimumSandDepth) {

                            colour = new Color32(Convert.ToByte(255), 0, 0, 0);

                        } else {
                            colour = new Color32(Convert.ToByte((int) pixelValue), 0, 0, 0);

                        }

                        colourArray[y * depthMapTexture.width + x] = colour;
                    } 
                }
            }

            // Set texture pixels
            depthMapTexture.SetPixels32(colourArray);
            depthMapTexture.Apply();

            return;
        }
    }
}

