using UnityEngine;
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Kinect.Sensor;
using FishNet.Object;
using FishNet.Connection;
using Map;
using FishNet.Transporting;
using Unity.Mathematics;
using Unity.Multiplayer;
using UnityEngine.Serialization;

namespace Kinect
{
    public class KinectAPI : NetworkBehaviour
    {

        [Header("Depth Calibrations")] [SerializeField, Range(300f, 1000f)]
        private ushort minimumSandDepth;

        [SerializeField, Range(600f, 2000f)] private ushort maximumSandDepth;

        [Header("IR Calibrations")] [SerializeField, Range(0, 255f)]
        private int irThreshold;

        [Header("Similarity Threshold")] [SerializeField, Range(0.5f, 1f)]
        private float similarityThreshold;

        //Internal Variables
        private Device _kinect;
        private Transformation _transformation;
        [SerializeField] private MapGenerator mapGenerator;

        private int _colourWidth;
        private int _colourHeight;

        private half[] _depthMapArray;
        [SerializeField] private int dimensions;
        [SerializeField] private bool isKinectPresent;
        private bool _running;

        public override void OnStartServer()
        {
            if (isKinectPresent) {
                return;
            }

            if (minimumSandDepth > maximumSandDepth)
            {
                Debug.LogError("Minimum depth is greater than maximum depth");
            }

            this._kinect = Device.Open();

            // Configure camera modes
            this._kinect.StartCameras(new DeviceConfiguration
            {
                ColorFormat = ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R1080p,
                DepthMode = DepthMode.NFOV_Unbinned,
                SynchronizedImagesOnly = true,
                CameraFPS = FPS.FPS30
            });

            Debug.Log("AKDK Serial Number: " + this._kinect.SerialNum);

            // Initialize the transformation engine
            this._transformation = this._kinect.GetCalibration().CreateTransformation();

            this._colourWidth = this._kinect.GetCalibration().ColorCameraCalibration.ResolutionWidth;
            this._colourHeight = this._kinect.GetCalibration().ColorCameraCalibration.ResolutionHeight;

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

        private void StartKinect()
        {
            _depthMapArray = new half[dimensions * dimensions];

            _running = true;
            Task.Run(GetCaptureAsync);
        }
        
        void OnApplicationQuit()
        {
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask == MultiplayerRoleFlags.Server) {
                _running = false;
                _kinect.StopCameras();
                _kinect.Dispose();
            }
        }

        public void RequestTexture(ushort lod, ushort chunkSize, int z, int x) {
            RequestChunkTextureServerRpc(Owner.ClientId, lod, chunkSize, x, z); 
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestChunkTextureServerRpc(int clientId, ushort lod, ushort chunkSize, int x, int z)
        {
            half[] depths = GetChunkTexture(lod, chunkSize, x, z);

            // Send the depth data back to the requesting client
            NetworkConnection targetConnection = NetworkManager.ServerManager.Clients[clientId];

            if (targetConnection != null)
            {
                SendChunkTextureTargetRpc(targetConnection, depths, x, z);
            }
        }

        [TargetRpc]
        private void SendChunkTextureTargetRpc(NetworkConnection conn, half[] depths, int x, int z)
        {
            mapGenerator.GetChunk(z, x).SetHeights(depths);
        }
        
        public half[] GetChunkTexture(ushort lod, ushort chunkSize, int chunkX, int chunkY)
        {
            var lodFactor = lod == 0 ? 1 : lod * 2;
            
            //float similarity = 0;
            half[] depths = new half[(chunkSize / lodFactor + 1) * (chunkSize / lodFactor + 1)];

            var resolution = chunkSize / lodFactor;
            
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
            for (int y = 0; y < resolution + 1; y++)
            {
                for (int x = 0; x < resolution + 1; x++)
                {
                    depths[y * (resolution + 1) + x] = _depthMapArray[(lodFactor * y + yChunkOffset) * dimensions + xChunkOffset + lodFactor * x];
                }
            }

            return depths;
        }

        public async Task GetCaptureAsync()
        {
            while (_running)
            {
                using Image transformedDepth = new Image(ImageFormat.Depth16, _colourWidth, _colourHeight, _colourWidth * sizeof(UInt16));
                using Capture capture = await Task.Run(() => _kinect.GetCapture());
                GetDepthTextureFromKinect(capture, transformedDepth);
            }
            
        }

        private void GetDepthTextureFromKinect(Capture capture, Image transformedDepth)
        {
            //using (Capture capture = kinect.GetCapture())
            // Transform the depth image to the colour camera perspective
            _transformation.DepthImageToColorCamera(capture, transformedDepth);

            // Create Depth Buffer
            Span<ushort> depthBuffer = transformedDepth.GetPixels<ushort>().Span;
            Span<ushort> irBuffer = capture.IR.GetPixels<ushort>().Span;

            int imageXOffset = (_colourWidth - dimensions) / 2;
            int imageYOffset = (_colourHeight - dimensions) / 2;

            // Create a new image with data from the depth and colour image
            for (int y = 0; y < dimensions; y++)
            {
                for (int x = 0; x < dimensions; x++)
                {
                    var depth = depthBuffer[(y + imageYOffset) * _colourWidth + imageXOffset + x];
                    var ir = 0; //irBuffer[(y + imageYOffset) * colourWidth + imageXOffset + x];

                    // Calculate pixel values
                    half depthRange = (half)(maximumSandDepth - minimumSandDepth);
                    half pixelValue = (half)(maximumSandDepth - depth);

                    if (ir < irThreshold)
                    {
                        half val = (half) 0;
                        if (depth == 0 || depth >= maximumSandDepth) // No depth image
                        {
                            val = (half) 0;

                        }
                        else if (depth < minimumSandDepth)
                        {

                            val = (half) 1;

                        }
                        else
                        {
                            val = (half) (pixelValue / depthRange);

                        }

                        _depthMapArray[y * dimensions + x] = val;
                    }
                }
            }
        }
    }
}