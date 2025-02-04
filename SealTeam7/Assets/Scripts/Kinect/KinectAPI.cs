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

        [Header("Depth Calibrations")] [SerializeField, Range(300f, 2000f)]
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

        [Header("Position Calibrations")]
        [SerializeField] private int _width;
        [SerializeField] private int _height;
        [SerializeField, Range(0f, 640f)] private int _xOffsetStart;
        [SerializeField, Range(0f, 640f)] private int _xOffsetEnd;
        [SerializeField, Range(0f, 576f)] private int _yOffsetStart;
        [SerializeField, Range(0f, 576f)] private int _yOffsetEnd;

        [SerializeField] private bool isKinectPresent;

        [SerializeField] Texture2D texture;
        private bool _running;

        public override void OnStartServer()
        {
            if (!isKinectPresent) {
                return;
            }

            if (minimumSandDepth > maximumSandDepth)
            {
                Debug.LogError("Minimum depth is greater than maximum depth");
            }

            _kinect = Device.Open();

            // Configure camera modes
            _kinect.StartCameras(new DeviceConfiguration
            {
                ColorFormat = ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R1080p,
                DepthMode = DepthMode.NFOV_Unbinned,
                SynchronizedImagesOnly = true,
                CameraFPS = FPS.FPS30
            });

            Debug.Log("AKDK Serial Number: " + _kinect.SerialNum);

            // Initialize the transformation engine
            _transformation = _kinect.GetCalibration().CreateTransformation();

            this._colourWidth = this._kinect.GetCalibration().DepthCameraCalibration.ResolutionWidth;
            this._colourHeight = this._kinect.GetCalibration().DepthCameraCalibration.ResolutionHeight;

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
            _depthMapArray = new half[_width * _height];
            texture = new Texture2D(_width, _height);

            _running = true;
            Task.Run(GetCaptureAsync);
        }
        
        void OnApplicationQuit()
        {
            if (_kinect != null) {
                _running = false;
                _kinect.StopCameras();
                _kinect.Dispose();
            }
        }

        public void RequestTexture(ushort lod, ushort chunkSize, int x, int z) {
            RequestChunkTextureServerRpc(Owner.ClientId, lod, chunkSize, x, z); 
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestChunkTextureServerRpc(int clientId, ushort lod, ushort chunkSize, int x, int z)
        {
            half[] depths = GetChunkTexture(lod, chunkSize, x, z);
            //Debug.Log("RPC recieved");

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
            mapGenerator.GetChunk(x, z).SetHeights(depths);
        }
        
        public half[] GetChunkTexture(ushort lod, ushort chunkSize, int chunkX, int chunkZ)
        {
            
            //float similarity = 0;

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
            var lodFactor = lod == 0 ? 1 : lod * 2;
            var resolution = chunkSize / lodFactor;
            int zChunkOffset = chunkZ * chunkSize;
            int xChunkOffset = chunkX * chunkSize;
            
            var depth = new half[(resolution + 1) * (resolution + 1)];
            
            for (int z = 0; z < resolution + 1; z++)
            {
                for (int x = 0; x < resolution + 1; x++)
                {
                    depth[z * (resolution + 1) + x] = _depthMapArray[(lodFactor * z + zChunkOffset) * _width + xChunkOffset + lodFactor * x];
                }
            }

            return depth;
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
            // Transform the depth image to the colour camera perspective
            //_transformation.DepthImageToColorCamera(capture, transformedDepth);

            // Create Depth Buffer
            Span<ushort> depthBuffer = capture.Depth.GetPixels<ushort>().Span;
            Debug.Log(depthBuffer.Length);
            //Span<ushort> irBuffer = capture.IR.GetPixels<ushort>().Span;

            int rangeX = _xOffsetEnd - _xOffsetStart;
            int rangeY = _yOffsetEnd - _yOffsetStart;

            float samplingRateX = rangeX / _width;
            float samplingRateY = rangeY / _height;

            // Create a new image with data from the depth and colour image
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {

                    int lowerX = (int)Mathf.Floor(x * samplingRateX + _xOffsetStart);
                    int upperX = (int)Mathf.Ceil(x * samplingRateX + _xOffsetStart);
                    int lowerY = (int)Mathf.Floor(y * samplingRateY + _xOffsetStart);
                    int upperY = (int)Mathf.Ceil(y * samplingRateY + _xOffsetStart);

                    ushort lowerSample = depthBuffer[lowerY * _width + lowerX];
                    ushort upperSample = depthBuffer[upperY * _width + upperX];

                    half depth = (half) ((half) (lowerSample + upperSample) / 2f);

                    //var ir = 0; //irBuffer[(y + imageYOffset) * colourWidth + imageXOffset + x];

                    // Calculate pixel values
                    half depthRange = (half)(maximumSandDepth - minimumSandDepth);
                    half pixelValue = (half)(maximumSandDepth - depth);

                    //if (ir < irThreshold)
                    //{
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

                    _depthMapArray[y * _height + x] = val;
                    //}
                }
            }

        }
    }
}