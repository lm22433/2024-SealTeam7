using UnityEngine;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Game;
using Emgu.CV;  // need to install Emgu.CV on NuGet and Emgu.CV.runtime.windows if on windows
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Microsoft.Azure.Kinect.Sensor;
using Python;
using Debug = UnityEngine.Debug;

namespace Map
{
    public class KinectAPI
    {
        //Internal Variables
        private readonly Device _kinect;
        private readonly Transformation _transformation;
        private Image _transformedDepthImage;
        private float[] _heightMap;
        
        /*
         * This replaces _tempHeightMap. It's an Image (from EmguCV, C# bindings for OpenCV).
         * Get a pixel with:
         * float pixel = _tmpImage.Data[y, x, 0]
         * Set a pixel with:
         * _tmpImage.Data[y, x, 0] = 123f
         */
        private Image<Gray, float> _rawHeightImage;
        private Image<Gray, float> _maskedHeightImage;
        private Image<Gray, float> _heightMask;
        private Image<Gray, float> _tmpImage;
        
        private readonly Mat _dilationKernel;
        private readonly System.Drawing.Point _defaultAnchor;
        private readonly MCvScalar _scalarOne;
        private readonly float _heightScale;
        private readonly float _lerpFactor;
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
        private Task _getCaptureTask;

        // public for gizmos
        public Rect? BboxLeft = null;
        public Rect? BboxRight = null;
        public HandLandmarks HandLandmarks;
        public Image<Gray, float> RawHeightImage => _rawHeightImage;

        public KinectAPI(float heightScale, float lerpFactor, int minimumSandDepth, int maximumSandDepth, 
                int irThreshold, float similarityThreshold, int width, int height, int xOffsetStart, int xOffsetEnd, int yOffsetStart, int yOffsetEnd, ref float[] heightMap, int kernelSize, float gaussianStrength)
        {
            _heightScale = heightScale;
            _lerpFactor = lerpFactor;
            _minimumSandDepth = minimumSandDepth;
            _maximumSandDepth = maximumSandDepth;
            _width = width;
            _height = height;
            _xOffsetStart = xOffsetStart;
            _xOffsetEnd = xOffsetEnd;
            _yOffsetStart = yOffsetStart;
            _yOffsetEnd = yOffsetEnd;
            _heightMap = heightMap;
            
            _rawHeightImage = new Image<Gray, float>(_width + 1, _height + 1);
            _maskedHeightImage = new Image<Gray, float>(_width + 1, _height + 1);
            _heightMask = new Image<Gray, float>(_width + 1, _height + 1);
            _tmpImage = new Image<Gray, float>(_width + 1, _height + 1);
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
            _transformedDepthImage = new Image(ImageFormat.Depth16, _colourWidth, _colourHeight,
                _colourWidth * sizeof(UInt16));

            _running = true;
            _getCaptureTask = Task.Run(GetCaptureTask);
        }
        
        public void StopKinect()
        {
            _running = false;
            _getCaptureTask.Wait();
            _kinect.StopCameras();
            _kinect.Dispose();
            _transformedDepthImage.Dispose();
            _rawHeightImage.Dispose();
            _maskedHeightImage.Dispose();
            _heightMask.Dispose();
            _tmpImage.Dispose();
            _dilationKernel.Dispose();
        }
        
        private void GetCaptureTask()
        {
            PythonManager2.Initialize();
            
            while (_running)
            {
                //if (!GameManager.GetInstance().IsGameActive()) continue;
                
                try
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    using Capture capture = _kinect.GetCapture();
                    stopwatch.Stop();
                    Debug.Log($"Kinect.GetCapture: {stopwatch.ElapsedMilliseconds} ms");
                    
                    stopwatch.Restart();
                    var hl = PythonManager2.ProcessFrame(capture.Color);
                    stopwatch.Stop();
                    Debug.Log($"PythonManager2.ProcessFrame: {stopwatch.ElapsedMilliseconds} ms");
                    
                    _transformation.DepthImageToColorCamera(capture, _transformedDepthImage);
                    UpdateHandLandmarks(hl,  // Saves adjusted hand landmarks to HandLandmarks
                        leftHandDepth:hl.Left == null ? null : _transformedDepthImage.GetPixel<ushort>(
                            (int)hl.Left[0].z, 1920 - (int)hl.Left[0].x),  //  
                        rightHandDepth:hl.Right == null ? null : _transformedDepthImage.GetPixel<ushort>(
                            (int)hl.Right[0].z, 1920 - (int)hl.Right[0].x));
                    
                    stopwatch.Restart();
                    UpdateHeightMap(capture, HandLandmarks);
                    stopwatch.Stop();
                    Debug.Log($"UpdateHeightMap: {stopwatch.ElapsedMilliseconds} ms");
                    
                } catch (Exception e) {
                    Debug.Log(e);
                }
            }
            
            PythonManager2.Dispose();
        }

        private void UpdateHeightMap(Capture capture, HandLandmarks handLandmarks)
        {
            // Raw depth from kinect
            Span<ushort> depthBuffer = _transformedDepthImage.GetPixels<ushort>().Span;

            // Create a new image with data from the depth and colour image
            for (int y = 0; y < _height + 1; y++)
            {
                for (int x = 0; x < _width + 1; x++)
                {
                    var depth = depthBuffer[(y + _yOffsetStart) * _colourWidth + _xOffsetStart + (_width - x)];

                    var depthRange = (float)(_maximumSandDepth - _minimumSandDepth);
                    // Max depth is the lowest height, so this is the height normalised to [0, 1]
                    var height = (_maximumSandDepth - depth) / depthRange;
                    _rawHeightImage.Data[y, x, 0] = height;
                    
                    // temp for testing
                    if (depth == 0)
                    {
                        _rawHeightImage.Data[y, x, 0] = 0f;
                    }

                    // depth == 0 means kinect wasn't able to get a depth for that pixel
                    // hand masking threshold is now just _minimumSandDepth
                    if (depth == 0 || height >= 1f || height < 0f)
                    { 
                        // Mask the pixel
                        _tmpImage.Data[y, x, 0] = 1f;
                    }
                    else
                    {
                        // Don't mask the pixel (modulo dilation)
                        _tmpImage.Data[y, x, 0] = 0f;
                    }
                }
            }
            
            // // Dilate the mask (extend it slightly along its borders)
            // CvInvoke.Dilate(_tmpImage, _heightMask, _dilationKernel, _defaultAnchor, iterations: 1, 
            //     BorderType.Default, _scalarOne);
            
            // Also mask using hand landmarks
            // const float padding = 20f;
            // var bboxLeft = new Rect();
            // var bboxRight = new Rect();
            // if (handLandmarks.Left != null)
            // {
            //     bboxLeft.xMin = handLandmarks.Left.Min(p => p.x) - _xOffsetStart - padding;
            //     bboxLeft.xMax = handLandmarks.Left.Max(p => p.x) - _xOffsetStart + padding;
            //     bboxLeft.yMin = handLandmarks.Left.Min(p => p.z) - _yOffsetStart - padding;
            //     bboxLeft.yMax = handLandmarks.Left.Max(p => p.z) - _yOffsetStart + padding;
            //     // Debug.Log($"Left hand bbox: {bboxLeft}");
            // }
            // if (handLandmarks.Right != null)
            // {
            //     bboxRight.xMin = handLandmarks.Right.Min(p => p.x) - _xOffsetStart - padding;
            //     bboxRight.xMax = handLandmarks.Right.Max(p => p.x) - _xOffsetStart + padding;
            //     bboxRight.yMin = handLandmarks.Right.Min(p => p.z) - _yOffsetStart - padding;
            //     bboxRight.yMax = handLandmarks.Right.Max(p => p.z) - _yOffsetStart + padding;
            //     // Debug.Log($"Right hand bbox: {bboxRight}");
            // }
            // BboxLeft = bboxLeft;
            // BboxRight = bboxRight;
            // var vec2 = new Vector2();
            // for (int y = 0; y < _height + 1; y++)
            // {
            //     for (int x = 0; x < _width + 1; x++)
            //     {
            //         vec2.Set(x, y);
            //         if ((handLandmarks.Left != null && bboxLeft.Contains(vec2)) || 
            //             (handLandmarks.Right != null && bboxRight.Contains(vec2)))
            //         {
            //             _heightMask.Data[y, x, 0] = 1f;
            //         }
            //         else
            //         {
            //             _heightMask.Data[y, x, 0] = 0f; //temp
            //         }
            //     }
            // }
            
            // Update the heights, only in the non-masked part
            for (int y = 0; y < _height + 1; y++)
            {
                for (int x = 0; x < _width + 1; x++)
                {
                    if (_heightMask.Data[y, x, 0] == 0f)  // if pixel is not part of the hand mask
                    {
                        _maskedHeightImage.Data[y, x, 0] = _rawHeightImage.Data[y, x, 0];
                    }
                    // Otherwise height is kept the same for that pixel
                }
            }
            
            // Gaussian blur
            // CvInvoke.GaussianBlur(_maskedHeightImage, _tmpImage, new System.Drawing.Size(31, 31), 15);

            // Write new height data to _heightMap
            for (int y = 0; y < _height + 1; y++)
            {
                for (int x = 0; x < _width + 1; x++)
                {
                    _heightMap[y * (_width + 1) + x] = _maskedHeightImage.Data[y, x, 0] * _heightScale;
                    // _heightMap[y * (_width + 1) + x] = _heightMask.Data[y, x, 0] * 50f;
                }
            }
        }

        private void UpdateHandLandmarks(HandLandmarks handLandmarks, float? leftHandDepth, float? rightHandDepth)
        {
            // Debug.Log($"Hand landmarks before: {handLandmarks}");
            var depthRange = _maximumSandDepth - _minimumSandDepth;
            var offsetLeft = new Vector3();
            var offsetRight = new Vector3();
            const float wristYOffset = 0f;
            if (leftHandDepth.HasValue)
            {
                offsetLeft = new Vector3(PythonManager2.FlipX ? -(1920 - _xOffsetEnd) : -_xOffsetStart,
                    (_maximumSandDepth - leftHandDepth.Value) / depthRange * _heightScale + wristYOffset,
                    -_yOffsetStart);
            }
            if (rightHandDepth.HasValue)
            {
                offsetRight = new Vector3(PythonManager2.FlipX ? -(1920 - _xOffsetEnd) : -_xOffsetStart,
                    (_maximumSandDepth - rightHandDepth.Value) / depthRange * _heightScale + wristYOffset,
                    -_yOffsetStart);
            }

            const float handYScaling = 3f;
            HandLandmarks = new HandLandmarks
            {
                Left = handLandmarks.Left?.Select(p => 
                    new Vector3(p.x + offsetLeft.x, p.y*handYScaling + offsetLeft.y, p.z + offsetLeft.z)).ToArray(),
                Right = handLandmarks.Right?.Select(p =>
                    new Vector3(p.x + offsetRight.x, p.y*handYScaling + offsetRight.y, p.z + offsetRight.z)).ToArray()
            };
            // Debug.Log($"Hand landmarks after: {HandLandmarks}");
        }
    }
}