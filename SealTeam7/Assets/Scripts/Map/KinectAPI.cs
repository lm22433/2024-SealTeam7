using UnityEngine;
using System;
using System.Threading.Tasks;
using FishNet;
using Microsoft.Azure.Kinect.Sensor;
using FishNet.Object;
using FishNet.Connection;
using Map;
using FishNet.Transporting;
using Unity.Mathematics;
using UnityEngine.Serialization;

namespace Map
{
    public class KinectAPI : NetworkBehaviour
    {

        [Header("Depth Calibrations")] [SerializeField, Range(300f, 2000f)]
        private ushort minimumSandDepth;

        [SerializeField, Range(600f, 2000f)] private ushort maximumSandDepth;

        [Header("IR Calibrations")] [SerializeField, Range(0, 255f)]
        private int irThreshold;

        [Header("Similarity Threshold")] [SerializeField, Range(0f, 10f)]
        private float _similarityThresholdMin;
        [SerializeField, Range(0f, 500f)]
        private float _similarityThresholdMax;
        [SerializeField, Range(0f, 1f)]
        private float _similarityThresholdAdjustor;

        //Internal Variables
        private Device _kinect;
        private Transformation _transformation;
        [SerializeField] private MapGenerator mapGenerator;

        private int _colourWidth;
        private int _colourHeight;

        private half[] _depthMapArray;

        [Header("Position Calibrations")]
        [SerializeField] private int _width;
        [SerializeField] private int _length;
        [SerializeField] private float _maxHeight;
        [SerializeField, Range(0f, 1920f)] private int _xOffsetStart;
        [SerializeField, Range(0f, 1920f)] private int _xOffsetEnd;
        [SerializeField, Range(0f, 1080f)] private int _yOffsetStart;
        [SerializeField, Range(0f, 1080f)] private int _yOffsetEnd;

        [SerializeField] public bool isKinectPresent;

        [SerializeField] Texture2D texture;
        private bool _running;

        public void Start()
        {
            if (!IsServerInitialized || !isKinectPresent) return;

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

            this._colourWidth = this._kinect.GetCalibration().ColorCameraCalibration.ResolutionWidth;
            this._colourHeight = this._kinect.GetCalibration().ColorCameraCalibration.ResolutionHeight;

            StartKinect();
        }

        private void StartKinect()
        {
            _depthMapArray = new half[_width * _length];
            texture = new Texture2D(_width, _length);

            _running = true;
            Task.Run(GetCaptureAsync);
        }
        
        private void OnApplicationQuit()
        {
            if (_kinect != null) {
                _running = false;
                _kinect.StopCameras();
                _kinect.Dispose();
            }
        }

        public void RequestTexture(ushort lod, ushort chunkSize, int x, int z) {
            RequestChunkTextureServerRpc(InstanceFinder.ClientManager.Connection, lod, chunkSize, x, z); 
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestChunkTextureServerRpc(NetworkConnection conn, ushort lod, ushort chunkSize, int x, int z)
        {
            half[] depths = GetChunkTexture(lod, chunkSize, x, z);

            SendChunkTextureTargetRpc(conn, depths, x, z, lod);
        }

        public half GetHeight(int xPos, int zPos)
        {
            return _depthMapArray[zPos * _width + xPos];
        }

        [TargetRpc]
        private void SendChunkTextureTargetRpc(NetworkConnection conn, half[] depths, int x, int z, ushort lod)
        {
            mapGenerator.GetChunk(x, z).SetHeights(depths, lod);
        }
        
        private half[] GetChunkTexture(ushort lod, ushort chunkSize, int chunkX, int chunkZ)
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
            int zChunkOffset = chunkZ * (chunkSize - 1);
            int xChunkOffset = chunkX * (chunkSize - 1);
            
            var depth = new half[resolution * resolution];
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    depth[z * resolution + x] = _depthMapArray[(lodFactor * z + zChunkOffset) * _width + xChunkOffset + lodFactor * x];
                }
            }

            return depth;
        }   

        [SerializeField] bool takeSnapshot = false;
        private void Update() {
            if (takeSnapshot) {

                Color32[] col = new Color32[_depthMapArray.Length];
                for(int i = 0; i < _depthMapArray.Length; i++) {
                    col[i] = new Color32(Convert.ToByte(_depthMapArray[i] / _maxHeight * 255), 0, 0, Convert.ToByte(255));
                }

                texture.SetPixels32(col);
                texture.Apply();

                takeSnapshot = false;
            }
        }

        private async Task GetCaptureAsync()
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
            _transformation.DepthImageToColorCamera(capture, transformedDepth);

            // Create Depth Buffer
            Span<ushort> depthBuffer = transformedDepth.GetPixels<ushort>().Span;

            // Create a new image with data from the depth and colour image
            for (int y = 0; y < _length; y++)
            {
                for (int x = 0; x < _width; x++)
                {

                    var depth = depthBuffer[(y + _yOffsetStart) * _colourWidth + _xOffsetStart + x];


                    // Calculate pixel values
                    half depthRange = (half)(maximumSandDepth - minimumSandDepth);
                    half pixelValue = (half)(maximumSandDepth - depth);

                    half val;
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

                    var adj_val = (half) (val * _maxHeight);
                    var prev_val = _depthMapArray[y * _width + x];
                    if (adj_val < prev_val - _similarityThresholdMin ||
                        adj_val > prev_val + _similarityThresholdMin) {

                            if (adj_val < prev_val + _similarityThresholdMax && adj_val > prev_val - _similarityThresholdMax) {
                                _depthMapArray[y * _width + x] = adj_val;
                            } else {
                                _depthMapArray[y * _width + x] = (half) Mathf.Lerp(prev_val, adj_val, _similarityThresholdAdjustor);
                            }
                    } else {
                        _depthMapArray[y * _width + x] = (half) Mathf.Lerp(prev_val, adj_val, _similarityThresholdAdjustor);
                    }
                }
            }

        }
    }
}