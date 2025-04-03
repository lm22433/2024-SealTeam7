using UnityEngine;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Emgu.CV;  // need to install Emgu.CV on NuGet and Emgu.CV.runtime.windows if on windows
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Game;
using K4AdotNet.Sensor;
using Python;
using Debug = UnityEngine.Debug;
using Unity.Mathematics;

namespace Map
{
    public class KinectAPI
    {
        //Internal Variables
        private readonly Device _kinect;
        private readonly Transformation _transformation;
        private Image _transformedDepthImage;
        private float[,] _heightMap;
        private float[,] _gradientMap;

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
        private readonly Point _defaultAnchor;
        private readonly MCvScalar _scalarOne;
        private readonly float _heightScale;
        private readonly float _minLerpFactor;
        private readonly float _maxLerpFactor;
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
        private readonly Size _gaussianKernelSize;
        private readonly float _gaussianKernelSigma;
        private readonly float _handHeightScale;
        private readonly float _handHeightOffset;
        private Capture _capture;
        private Action _onHeightUpdate;

        private bool _running;
        private Task _getCaptureTask;
        private Task _asyncPythonTask;
        private int _leftHandAbsentCount = 0;
        private int _rightHandAbsentCount = 0;

        // public for gizmos
        public Rect? BboxLeft = null;
        public Rect? BboxRight = null;
        public Rect? BboxLeftWrist = null;
        public Rect? BboxRightWrist = null;
        public HandLandmarks HandLandmarks;
        public Image<Gray, float> RawHeightImage => _rawHeightImage;

        public KinectAPI(float heightScale, float minLerpFactor, float maxLerpFactor, int minimumSandDepth, int maximumSandDepth, 
                int width, int height, int xOffsetStart, int xOffsetEnd, int yOffsetStart, int yOffsetEnd, ref float[,] heightMap, 
                int gaussianKernelRadius, float gaussianKernelSigma, Action onHeightUpdate, float handHeightScale, float handHeightOffset)
        {
            _onHeightUpdate = onHeightUpdate;
            _heightScale = heightScale;
            _minLerpFactor = minLerpFactor;
            _maxLerpFactor = maxLerpFactor;
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
            _dilationKernel = Mat.Ones(50, 50, DepthType.Cv8U, 1);
            _defaultAnchor = new Point(-1, -1);
            _scalarOne = new MCvScalar(1f);
            _gaussianKernelSize = new Size(gaussianKernelRadius * 2 + 1, gaussianKernelRadius * 2 + 1);
            _gaussianKernelSigma = gaussianKernelSigma;
            _handHeightScale = handHeightScale;
            _handHeightOffset = handHeightOffset;
            
            if (minimumSandDepth > maximumSandDepth)
            {
                Debug.LogError("Minimum depth is greater than maximum depth");
            }

            _kinect = Device.Open();

            // Configure camera modes
            _kinect.StartCameras(new DeviceConfiguration
            {
                ColorFormat = ImageFormat.ColorBgra32,
                ColorResolution = ColorResolution.R1080p,
                DepthMode = DepthMode.NarrowViewUnbinned,
                SynchronizedImagesOnly = false,
                CameraFps = FrameRate.Thirty,
            });

            Debug.Log("AKDK Serial Number: " + _kinect.SerialNumber);
            
            // Initialize the transformation engine
            _kinect.GetCalibration(DepthMode.NarrowViewUnbinned, ColorResolution.R1080p, out var calibration);
            _transformation = calibration.CreateTransformation();
            _colourWidth = calibration.ColorCameraCalibration.ResolutionWidth;
            _colourHeight = calibration.ColorCameraCalibration.ResolutionHeight;
            _transformedDepthImage = new Image(ImageFormat.Depth16, _colourWidth, _colourHeight,
                _colourWidth * sizeof(UInt16));
            
            _running = true;
            _capture = _kinect.GetCapture();
            _getCaptureTask = Task.Run(GetCaptureTask);
            _asyncPythonTask = Task.Run(GetPythonAsync);
        }
        
        public Vector3[] GetHandPositions(int hand) => hand == 0 ? HandLandmarks.Right : HandLandmarks.Left;

        public void StopKinect()
        {
            _running = false;
            _getCaptureTask.Wait();
            _asyncPythonTask.Wait();
            _capture.Dispose();
            _kinect.StopCameras();
            _kinect.Dispose();
            _transformedDepthImage.Dispose();
            _rawHeightImage.Dispose();
            _maskedHeightImage.Dispose();
            _heightMask.Dispose();
            _tmpImage.Dispose();
            _dilationKernel.Dispose();
        }

        private void GetPythonAsync()
        {
            PythonManager.Initialize();
        
            if (PythonManager.IsInitialized) 
            {
                while (_running)
                {
                    try
                    {
                        var hl = PythonManager.ProcessFrame(_capture.ColorImage);
                        UpdateHandLandmarks(hl, _transformedDepthImage);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            
            if (PythonManager.IsInitialized) {
                PythonManager.Dispose();
            }
        }

        private void GetCaptureTask()
        {
            while (_running)
            {
                try
                {
                    _capture = _kinect.GetCapture();
                    _transformation.DepthImageToColorCamera(_capture.DepthImage, _transformedDepthImage);
                    UpdateHeightMap(_transformedDepthImage, HandLandmarks);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
            }
        }

        private void UpdateHeightMap(Image depthImage, HandLandmarks handLandmarks)
        {
            // Raw depth from kinect
            short[] tempBuffer = new short[depthImage.HeightPixels * depthImage.WidthPixels];
            depthImage.CopyTo(tempBuffer);
            Span<short> depthBuffer = tempBuffer.AsSpan();
            
            var depthRange = (float)(_maximumSandDepth - _minimumSandDepth);
            // Create a new image with data from the depth and colour image
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int y = 0; y < _height + 1; y++)
            {
                for (int x = 0; x < _width + 1; x++)
                {
                    var depth = depthBuffer[(y + _yOffsetStart) * _colourWidth + _xOffsetStart + (_width - x)];

                    // Max depth is the lowest height, so this is the height normalised to [0, 1]
                    var height = (_maximumSandDepth - depth) / depthRange;
                    _rawHeightImage.Data[y, x, 0] = height;
                    
                    // depth == 0 means kinect wasn't able to get a depth for that pixel
                    // hand masking threshold is now just _minimumSandDepth
                    if (depth == 0 || height >= 1f || height < 0f)
                    { 
                        // Mask the pixel
                        _tmpImage.Data[y, x, 0] = 1f;
                    }
                    else
                    {
                        // Don't mask the pixel (yet)
                        _tmpImage.Data[y, x, 0] = 0f;
                    }
                }
            }
            stopwatch.Stop();
            Console.WriteLine($"Loop 1: {stopwatch.ElapsedMilliseconds} ms");
            
            stopwatch.Restart();
            // Dilate the mask (extend it slightly along its borders)
            CvInvoke.Dilate(_tmpImage, _heightMask, _dilationKernel, _defaultAnchor, iterations: 1, 
                BorderType.Default, _scalarOne);
            stopwatch.Stop();
            Console.WriteLine($"Loop 1: {stopwatch.ElapsedMilliseconds} ms");
            
            // Also mask using hand landmarks
            const float paddingHand = 20f;
            const float paddingWrist = 100f;
            var bboxLeftHand = new Rect();
            var bboxRightHand = new Rect();
            var bboxLeftWrist = new Rect();
            var bboxRightWrist = new Rect();
            if (handLandmarks.Left != null)
            {
                bboxLeftHand.xMin = handLandmarks.Left.Min(p => p.x) - paddingHand;
                bboxLeftHand.xMax = handLandmarks.Left.Max(p => p.x) + paddingHand;
                bboxLeftHand.yMin = handLandmarks.Left.Min(p => p.z) - paddingHand;
                bboxLeftHand.yMax = handLandmarks.Left.Max(p => p.z) + paddingHand;
                var wristCentre = handLandmarks.Left[0] + (handLandmarks.Left[0] - handLandmarks.Left[9]);
                bboxLeftWrist.xMin = wristCentre.x - paddingWrist;
                bboxLeftWrist.xMax = wristCentre.x + paddingWrist;
                bboxLeftWrist.yMin = wristCentre.z - paddingWrist;
                bboxLeftWrist.yMax = wristCentre.z + paddingWrist;
                // Debug.Log($"Left hand bbox: {bboxLeft}");
            }
            if (handLandmarks.Right != null)
            {
                bboxRightHand.xMin = handLandmarks.Right.Min(p => p.x) - paddingHand;
                bboxRightHand.xMax = handLandmarks.Right.Max(p => p.x) + paddingHand;
                bboxRightHand.yMin = handLandmarks.Right.Min(p => p.z) - paddingHand;
                bboxRightHand.yMax = handLandmarks.Right.Max(p => p.z) + paddingHand;
                var wristCentre = handLandmarks.Right[0] + (handLandmarks.Right[0] - handLandmarks.Right[9]);
                bboxRightWrist.xMin = wristCentre.x - paddingWrist;
                bboxRightWrist.xMax = wristCentre.x + paddingWrist;
                bboxRightWrist.yMin = wristCentre.z - paddingWrist;
                bboxRightWrist.yMax = wristCentre.z + paddingWrist;
                // Debug.Log($"Right hand bbox: {bboxRight}");
            }
            BboxLeft = bboxLeftHand;
            BboxRight = bboxRightHand;
            BboxLeftWrist = bboxLeftWrist;
            BboxRightWrist = bboxRightWrist;
            

            stopwatch.Restart();
            Parallel.For(0, (_height + 1)*(_width + 1), i => { 
                int x = i % (_width + 1);
                int y = i / (_width + 1);
                var vec2 = new Vector2(x, y);

                // if pixel is not part of the arm mask or the hand/wrist mask
                if (_heightMask.Data[y, x, 0] == 0f && 
                    (handLandmarks.Left == null || (!bboxLeftHand.Contains(vec2) && !bboxLeftWrist.Contains(vec2))) && 
                    (handLandmarks.Right == null || (!bboxRightHand.Contains(vec2) && !bboxRightWrist.Contains(vec2))))  
                {
                    // Apply adaptive lerping
                    var currentHeight = _heightMap[y, x];
                    var newHeight = _rawHeightImage.Data[y, x, 0] * _heightScale;
                    var distance = Mathf.Abs(currentHeight - newHeight);
                    // Debug.Log("distance: " + distance);
                    // var lerpFactor = Mathf.Clamp(distance / 30f, _minLerpFactor, _maxLerpFactor);
                    var lerpFactor = distance < 17f ? _minLerpFactor : _maxLerpFactor;
                    // Debug.Log("Lerp factor: " + lerpFactor);
                    _maskedHeightImage.Data[y, x, 0] = Mathf.Lerp(currentHeight, newHeight, lerpFactor);
                }
            });
            stopwatch.Stop();
            Console.WriteLine($"Loop 2: {stopwatch.ElapsedMilliseconds} ms");
            
            stopwatch.Restart();
            // Gaussian blur
            CvInvoke.GaussianBlur(_maskedHeightImage, _tmpImage, _gaussianKernelSize, _gaussianKernelSigma);
            stopwatch.Stop();
            Console.WriteLine($"Gaussian Blur: {stopwatch.ElapsedMilliseconds} ms");

            stopwatch.Restart();
            // Write new height data to _heightMap
            Parallel.For(0, (_height + 1)*(_width + 1), i => { 
                int x = i % (_width + 1);
                int y = i / (_width + 1);
                _heightMap[y, x] = _tmpImage.Data[y, x, 0];
  
            });
            stopwatch.Stop();
            Console.WriteLine($"Loop 3: {stopwatch.ElapsedMilliseconds} ms");

            _onHeightUpdate();
        }

        private void UpdateHandLandmarks(HandLandmarks handLandmarks, Image depthImage)
        {
            // Raw depth from kinect
            short[] depthBuffer = new short[depthImage.HeightPixels * depthImage.WidthPixels];
            depthImage.CopyTo(depthBuffer);

            float? leftHandDepth = handLandmarks.Left == null
                ? null
                : depthBuffer[(int)handLandmarks.Left[0].z * depthImage.WidthPixels + 1920 - (int)handLandmarks.Left[0].x];
                float? rightHandDepth = handLandmarks.Right == null
                    ? null
                    : depthBuffer[
                        (int)handLandmarks.Right[0].z * depthImage.WidthPixels + 1920 - (int)handLandmarks.Right[0].x];
                
            var depthRange = _maximumSandDepth - _minimumSandDepth;
            
            var offsetLeft = new Vector3();
            var offsetRight = new Vector3();
            if (leftHandDepth.HasValue)
            {
                offsetLeft = new Vector3(PythonManager.FlipX ? -(1920 - _xOffsetEnd) : -_xOffsetStart,
                    (_maximumSandDepth - leftHandDepth.Value) * _handHeightScale + _handHeightOffset,
                    -_yOffsetStart);
            }
            if (rightHandDepth.HasValue)
            {
                offsetRight = new Vector3(PythonManager.FlipX ? -(1920 - _xOffsetEnd) : -_xOffsetStart,
                    (_maximumSandDepth - rightHandDepth.Value) *_handHeightScale + _handHeightOffset,
                    -_yOffsetStart);
            }
        
            const float handYScaling = 1.5f;
            HandLandmarks = new HandLandmarks
            {
                Left = handLandmarks.Left?.Select(p => 
                    new Vector3(p.x + offsetLeft.x, p.y*handYScaling + offsetLeft.y, p.z + offsetLeft.z)).ToArray(),
                Right = handLandmarks.Right?.Select(p =>
                    new Vector3(p.x + offsetRight.x, p.y*handYScaling + offsetRight.y, p.z + offsetRight.z)).ToArray()
            };
        }
        
    }
}