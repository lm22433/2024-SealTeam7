using UnityEngine;
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Kinect.Sensor;
using Unity.Collections;

namespace Map
{
    public class KinectAPI
    {
        //Internal Variables
        private readonly Device _kinect;
        private readonly Transformation _transformation;
        private float[] _heightMap;
        private float[] _heightMapTemp;
        
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

        private int _kernelSize;
        private float _guassianStrength;
        private float[,] _kernel;

        public KinectAPI(float heightScale, float lerpFactor, int minimumSandDepth, int maximumSandDepth, 
                int irThreshold, float similarityThreshold, int width, int height, int xOffsetStart, int xOffsetEnd, int yOffsetStart, int yOffsetEnd, ref float[] heightMap, int kernelSize, float guassianStrength)
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

            _kernelSize = kernelSize;
            _guassianStrength = guassianStrength;
            _kernel = GaussianBlur(_kernelSize, _guassianStrength);
            
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

        public static float[,] GaussianBlur(int lenght, float weight)
        {
            float[,] kernel = new float[lenght, lenght];
            float kernelSum = 0;
            int foff = (lenght - 1) / 2;
            float distance;
            double constant = 1d / (2 * Math.PI * weight * weight);
            for (int y = -foff; y <= foff; y++)
            {
                for (int x = -foff; x <= foff; x++)
                {
                    distance = ((y * y) + (x * x)) / (2 * weight * weight);
                    kernel[y + foff, x + foff] = (float) (constant * Math.Exp(-distance));
                    kernelSum += kernel[y + foff, x + foff];
                }
            }
            for (int y = 0; y < lenght; y++)
            {
                for (int x = 0; x < lenght; x++)
                {
                    kernel[y, x] =  (float) (kernel[y, x] * 1d / kernelSum);
                }
            }
            return kernel;
        }

        public void Convolve(float[,] kernel)
        {
            int foff = (kernel.GetLength(0) - 1) / 2;
            int kcenter;
            int kpixel;
            for (int y = foff; y < _height - foff; y++)
            {
                for (int x = foff; x < _width - foff; x++)
                {

                    kcenter = y * _width + x;
                    for (int fy = -foff; fy <= foff; fy++)
                    {
                        for (int fx = -foff; fx <= foff; fx++)
                        {
                            kpixel = kcenter + fy * _width + fx;
                            _heightMap[kcenter] += _heightMapTemp[kpixel] * kernel[fy + foff, fx + foff];
                        }
                    }
                }
            }
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

                    //_heightMap[y * (_width + 1) + x] = Mathf.Lerp(_heightMap[y * (_width + 1) + x], _heightScale * val, _lerpFactor);
                    _heightMapTemp[y * (_width + 1) + x] = val;
                }
            }

            Convolve(_kernel);

        }
    }
}