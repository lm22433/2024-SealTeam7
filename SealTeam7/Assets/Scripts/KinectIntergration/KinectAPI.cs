using UnityEngine;
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Kinect.Sensor;
using FishNet.Object;
using FishNet.Connection;
using Map;
using FishNet.Transporting;

namespace Kinect
{
    public class KinectAPI : NetworkBehaviour
    {

        [Header("Depth Calibrations")] [SerializeField, Range(300f, 1000f)]
        private ushort _MinimumSandDepth;

        [SerializeField, Range(600f, 1500f)] private ushort _MaximumSandDepth;

        [Header("IR Calibrations")] [SerializeField, Range(0, 255f)]
        private int _IRThreshold;

        [Header("Similarity Threshold")] [SerializeField, Range(0.5f, 1f)]
        private float _SimilarityThreshold;

        //Internal Variables
        private Device kinect = null;
        private Transformation transformation = null;

        private int colourWidth = 0;

        private int colourHeight = 0;

        private ushort[] depthMapArray;
        [SerializeField] private int dimensions;
        [SerializeField] private int chunkSize;
        bool running = false;

        override public void OnStartServer()
        {
            if (_MinimumSandDepth > _MaximumSandDepth)
            {
                Debug.LogError("Minimum depth is greater than maximum depth");
            }

            this.kinect = Device.Open();

            // Configure camera modes
            this.kinect.StartCameras(new DeviceConfiguration
            {
                ColorFormat = ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R1080p,
                DepthMode = DepthMode.NFOV_2x2Binned,
                SynchronizedImagesOnly = true,
                CameraFPS = FPS.FPS30
            });

            Debug.Log("AKDK Serial Number: " + this.kinect.SerialNum);

            // Initialize the transformation engine
            this.transformation = this.kinect.GetCalibration().CreateTransformation();

            this.colourWidth = this.kinect.GetCalibration().ColorCameraCalibration.ResolutionWidth;
            this.colourHeight = this.kinect.GetCalibration().ColorCameraCalibration.ResolutionHeight;

            StartKinect();
            ServerManager.OnRemoteConnectionState += OnClientConnected;
        }

        private void OnClientConnected(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Started)
            {
                GetComponent<NetworkObject>().GiveOwnership(conn);
            }
        }

        public void StartKinect()
        {
            depthMapArray = new ushort[dimensions * dimensions];

            running = true;
            Task.Run(() => GetCaptureAsync());
        }
        
        void OnApplicationQuit()
        {
            running = false;
            kinect.StopCameras();
            kinect.Dispose();
        }

        public void RequestTexture(int z, int x) {
            RequestChunkTextureServerRpc(Owner.ClientId, x, z); 

        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestChunkTextureServerRpc(int clientId, int x, int z)
        {
            ushort[] depths = GetChunkTexture(x, z);

            // Send the depth data back to the requesting client
            NetworkConnection targetConnection = NetworkManager.ServerManager.Clients[clientId];

            if (targetConnection != null)
            {
                SendChunkTextureTargetRpc(targetConnection, depths, x, z);
            }
        }

        [TargetRpc]
        private void SendChunkTextureTargetRpc(NetworkConnection conn, ushort[] depths, int x, int z)
        {
            FindFirstObjectByType<MapGenerator>().GetChunk(z, x).SetHeights(depths);
        }
        
        public ushort[] GetChunkTexture(int chunkX, int chunkY)
        {
            //float similarity = 0;
            ushort[] depths = new ushort[(chunkSize + 2) * (chunkSize + 2)];

            int yChunkOffset = chunkY * chunkSize;
            int xChunkOffset = chunkX * chunkSize;

            /*
            //Similarity Check
            for (int y = 0; y <= chunkSize + 1; y++ ) {
                for (int x = 0; x <= chunkSize + 1; x++) {
                    var col = depths[y * chunkSize + x];
                    var curr = depthMapArray[(y + yChunkOffset) * chunkSize + xChunkOffset + x];

                    similarity += Mathf.Pow(Mathf.Abs(col - curr), 2);
                }
            }

            similarity = Mathf.Sqrt(similarity) / chunkSize;

            if (similarity > _SimilarityThreshold) {
                //return;
            }
            */
            //Write changed texture
            for (int y = 0; y < chunkSize + 2; y++)
            {
                for (int x = 0; x < chunkSize + 2; x++)
                {
                    depths[y * (chunkSize + 2) + x] = depthMapArray[(y + yChunkOffset) * dimensions + xChunkOffset + x];
                }
            }

            return depths;
        }

        public async Task GetCaptureAsync()
        {
            while (running)
            {
                using (Image transformedDepth = new Image(ImageFormat.Depth16, colourWidth, colourHeight, colourWidth * sizeof(UInt16)))
                using (Capture capture = await Task.Run(() => kinect.GetCapture()))
                {
                    GetDepthTextureFromKinect(capture, transformedDepth);
                }
            }
            
        }

        private void GetDepthTextureFromKinect(Capture capture, Image transformedDepth)
        {
            //using (Capture capture = kinect.GetCapture())
            // Transform the depth image to the colour camera perspective
            transformation.DepthImageToColorCamera(capture, transformedDepth);

            // Create Depth Buffer
            Span<ushort> depthBuffer = transformedDepth.GetPixels<ushort>().Span;
            Span<ushort> IRBuffer = capture.IR.GetPixels<ushort>().Span;

            int imageXOffset = (colourWidth - dimensions) / 2;
            int imageYOffset = (colourHeight - dimensions) / 2;

            // Create a new image with data from the depth and colour image
            for (int y = 0; y < dimensions; y++)
            {
                for (int x = 0; x < dimensions; x++)
                {
                    var depth = depthBuffer[(y + imageYOffset) * colourWidth + imageXOffset + x];
                    var ir = 0; //IRBuffer[(y + imageYOffset) * colourWidth + imageXOffset + x];

                    // Calculate pixel values
                    ushort depthRange = (ushort)(_MaximumSandDepth - _MinimumSandDepth);
                    ushort pixelValue = (ushort)(_MaximumSandDepth - depth);

                    if (ir < _IRThreshold)
                    {
                        ushort val = 0;
                        if (depth == 0 || depth >= _MaximumSandDepth) // No depth image
                        {
                            val = 0;

                        }
                        else if (depth < _MinimumSandDepth)
                        {

                            val = depthRange;

                        }
                        else
                        {
                            val = pixelValue;

                        }

                        depthMapArray[y * dimensions + x] = val;
                    }
                }
            }
        }
    }
}