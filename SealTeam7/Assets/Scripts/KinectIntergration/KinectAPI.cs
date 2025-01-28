using UnityEngine;
using System;
using Microsoft.Azure.Kinect.Sensor;

using UnityEngine.Profiling;
using FishNet.Object;
using Unity.Multiplayer;
using FishNet.Connection;

public class KinectAPI : NetworkBehaviour
{

    [Header("Depth Calibrations")]
    [SerializeField, Range(300f, 1000f)] private ushort _MinimumSandDepth;
    [SerializeField, Range(600f, 1500f)] private ushort _MaximumSandDepth;

    [Header("IR Calibrations")]
    [SerializeField, Range(0, 255f)] private int _IRThreshold;

    [Header("Similarity Threshold")]
    [SerializeField, Range(0.5f, 1f)] private float _SimilarityThreshold;

    //Internal Variables
    private Device kinect = null;
    private Transformation transformation = null;

    private int colourWidth = 0;

    private int colourHeight = 0;

    private ushort[] depthMapArray;
    [SerializeField] private int dimensions;
    private bool running;
    private int chunkSize;

    override public void OnStartServer()
    {   
        //base.OnStartServer();
        if (MultiplayerRolesManager.ActiveMultiplayerRoleMask != MultiplayerRoleFlags.Server) {
            return;
        }

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

        StartKinect();

    }

    private void StartKinect() {
        depthMapArray = new ushort[dimensions * dimensions];

        running = true;
    }

    void OnApplicationQuit()
    {
        running = false;
        this.kinect.Dispose();
    }

    [TargetRpc]
    public void GetChunkTexture(NetworkConnection conn, ushort[] depths, int chunkX, int chunkY) {
        //float similarity = 0;

        int yChunkOffset = chunkY * chunkSize - 1;
        int xChunkOffset = chunkX * chunkSize - 1;

        //Similarity Check
        /*
        for (int y = 0; y <= chunkSize + 1; y++ ) {
            for (int x = 0; x <= chunkSize + 1; x++) {
                var col = depths[y * chunkSize + x];
                var curr = depthMapArray[(y + yChunkOffset) * chunkSize + xChunkOffset + x];

                similarity += Mathf.Pow(Mathf.Abs(col - curr), 2);
            }
        }

        similarity = Mathf.Sqrt(similarity) / chunkSize;

        if (similarity > _SimilarityThreshold) {
            return;
        }
        */

        //Write changed texture
        for (int y = (chunkY * chunkSize) - 1; y < (chunkSize * (chunkY + 1)); y++ ) {
            for (int x = (chunkX * chunkSize) - 1; x < (chunkSize * (chunkX + 1)); x++ ) {
                depths[y * chunkSize + x] = depthMapArray[(y + yChunkOffset) * chunkSize + xChunkOffset + x];
            }
        }

    }

    private void Update() {
        if (MultiplayerRolesManager.ActiveMultiplayerRoleMask == MultiplayerRoleFlags.Server && running){
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
            //Span<ushort> IRBuffer = capture.IR.GetPixels<ushort>().Span;

            int imageXOffset = (colourWidth - dimensions) / 2;
            int imageYOffset = (colourHeight - dimensions) / 2;

            // Create a new image with data from the depth and colour image
            for (int y = 0; y < dimensions; y++) {
                for (int x = 0; x < dimensions; x++) {
                    var depth = depthBuffer[(y + imageYOffset) * colourWidth + imageXOffset + x];
                    //var ir = IRBuffer[(y + imageYOffset) * colourWidth + imageXOffset + x];

                    // Calculate pixel values
                    ushort depthRange = (ushort)(_MaximumSandDepth - _MinimumSandDepth);
                    ushort pixelValue = (ushort)(depth - _MinimumSandDepth);

                    if(-1 < _IRThreshold) {    
                        ushort val = 0;
                        if (depth == 0 || depth >= _MaximumSandDepth) // No depth image
                        {
                            val = 0;

                        } else if (depth < _MinimumSandDepth) {

                            val = depthRange;

                        } else {
                            val =  pixelValue;

                        }

                        depthMapArray[y * dimensions + x] = val;
                    } 
                }
            }
        }
    }
}

