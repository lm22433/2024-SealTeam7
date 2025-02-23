using UnityEngine;
using System;
using System.Threading.Tasks;
using Emgu.CV;  // need to install Emgu.CV on NuGet and Emgu.CV.runtime.windows if on windows
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Microsoft.Azure.Kinect.Sensor;

namespace Map
{
    public class KinectAPI
    {
        //Internal Variables
        private readonly Device _kinect;
        private readonly Transformation _transformation;
        private float[] _heightMap;
        
        /*
         * This replaces _tempHeightMap. It's an Image (from EmguCV, C# bindings for OpenCV).
         * Get a pixel with:
         * float pixel = _tmpImage.Data[y, x, 0]
         * Set a pixel with:
         * _tmpImage.Data[y, x, 0] = 123f
         */
        private Image<Gray, float> _tmpImage1;
        private Image<Gray, float> _tmpImage2;
        private Image<Gray, float> _tmpImage3;
        
        private readonly Mat _dilationKernel;
        private readonly System.Drawing.Point _defaultAnchor;
        private readonly MCvScalar _scalarOne;
        private readonly float _heightScale;
        private readonly int _minimumSandDepth;
        private readonly int _maximumSandDepth;
        private readonly int _colourWidth;
        private readonly int _colourHeight;        
        private readonly int _width;
        private readonly int _height;
        private readonly int _xOffsetStart;
        private readonly int _xOffsetEnd;
        private readonly int _yOffsetStart;
        private readonly int _yOffsetEnd;

        private bool _running;

        public KinectAPI(float heightScale, float lerpFactor, int minimumSandDepth, int maximumSandDepth, 
                int irThreshold, float similarityThreshold, int width, int height, int xOffsetStart, int xOffsetEnd, int yOffsetStart, int yOffsetEnd, ref float[] heightMap, int kernelSize, float gaussianStrength)
        {
            _heightScale = heightScale;
            _minimumSandDepth = minimumSandDepth;
            _maximumSandDepth = maximumSandDepth;
            _width = width;
            _height = height;
            _xOffsetStart = xOffsetStart;
            _xOffsetEnd = xOffsetEnd;
            _yOffsetStart = yOffsetStart;
            _yOffsetEnd = yOffsetEnd;
            _heightMap = heightMap;
            
            _tmpImage1 = new Image<Gray, float>(_width + 1, _height + 1);
            _tmpImage2 = new Image<Gray, float>(_width + 1, _height + 1);
            _tmpImage3 = new Image<Gray, float>(_width + 1, _height + 1);
            _dilationKernel = Mat.Ones(100, 100, DepthType.Cv8U, 1);
            _defaultAnchor = new System.Drawing.Point(-1, -1);
            _scalarOne = new MCvScalar(1f);
            
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

            _colourWidth = _kinect.GetCalibration().ColorCameraCalibration.ResolutionWidth;
            _colourHeight = _kinect.GetCalibration().ColorCameraCalibration.ResolutionHeight;

            _running = true;
            Task.Run(GetCaptureAsync);
        }
        
        public void StopKinect()
        {
            _running = false;
            _kinect.StopCameras();
            _kinect.Dispose();
        }
        
        private async Task GetCaptureAsync()
        {
            while (_running)
            {
                try {
                    using Image transformedDepth = new Image(ImageFormat.Depth16, _colourWidth, _colourHeight, _colourWidth * sizeof(UInt16));
                    using Capture capture = await Task.Run(() => _kinect.GetCapture());
                    GetDepthTextureFromKinect(capture, transformedDepth);
                } catch (Exception e) {
                    Debug.Log(e);
                }
            }
        }

        private void GetDepthTextureFromKinect(Capture capture, Image transformedDepth)
        {
            // Transform the depth image to the colour camera perspective
            _transformation.DepthImageToColorCamera(capture, transformedDepth);

            // Create Depth Buffer
            Span<ushort> depthBuffer = transformedDepth.GetPixels<ushort>().Span;

            // Create a new image with data from the depth and colour image
            for (int y = 0; y < _height + 1; y++)
            {
                for (int x = 0; x < _width + 1; x++)
                {

                    var depth = depthBuffer[(y + _yOffsetStart) * _colourWidth + _xOffsetStart + x];

                    // Calculate pixel values
                    var depthRange = (float)(_maximumSandDepth - _minimumSandDepth);
                    var pixelValue = _maximumSandDepth - depth;

                    float val;
                    if (depth == 0 || depth >= _maximumSandDepth) // No depth image
                    {
                        val = 0.5f;
                    }
                    else if (depth < _minimumSandDepth)
                    {
                        val = 1;
                    }
                    else
                    {
                        val = pixelValue / depthRange;
                    }

                    _tmpImage1.Data[y, x, 0] = _heightScale * val;
                }
            }
            
            // At this point each pixel in _tmpImage1 is val * _heightScale
            // Generate a mask where pixels likely to be of a hand/arm are set to 1
            CvInvoke.Threshold(_tmpImage1, _tmpImage2, 0.8f*_heightScale, 1f, ThresholdType.Binary);
            
            // Dilate the mask (extend it slightly along its borders)
            CvInvoke.Dilate(_tmpImage2, _tmpImage3, _dilationKernel, _defaultAnchor, iterations: 1, 
                BorderType.Default, _scalarOne);

            // Write new height data to _heightMap
            for (int y = 0; y < _height + 1; y++)
            {
                for (int x = 0; x < _width + 1; x++)
                {
                    if (_tmpImage3.Data[y, x, 0] == 0f &&  // if pixel is not part of the hand mask
                        _tmpImage1.Data[y, x, 0] != 0.5f)  // if the Kinect was able to get a depth for that pixel
                    {
                        _heightMap[y * (_width + 1) + x] = _tmpImage1.Data[y, x, 0];
                    }
                }
            }
        }
    }
}