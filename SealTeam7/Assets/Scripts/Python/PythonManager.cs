using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Threading;
using K4AdotNet.Sensor;
using Microsoft.Win32.SafeHandles;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Python
{
    public static class PythonManager
    {
        public static bool FlipX { get; set; } = true;
        public static bool FlipHandedness { get; set; } = false;
        public static HandLandmarks HandLandmarks => _handLandmarks;
        public static Gestures Gestures => _gestures;
        public static bool IsInitialized { get; private set; } = false;
        
        private const int PythonImageWidth = 1920;
        private const int PythonImageHeight = 1080;

        private static IPC _ipc;
        private static HandLandmarks _handLandmarks;  // Sometimes has a reference to _left/rightHandLandmarks
        private static Vector3[] _leftHandLandmarks;
        private static Vector3[] _rightHandLandmarks;
        private static Vector3[] _handLandmarksBuffer;  // Temporary buffer for reading hand landmarks
        private static Gestures _gestures;

        public static bool Initialize()
        {
            try {
                if (Application.platform == RuntimePlatform.WindowsEditor ||
                    Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    _ipc = new WindowsIPC();
                }
                else
                {
                    _ipc = new LinuxIPC();
                }
                _handLandmarks = new HandLandmarks();
                _leftHandLandmarks = new Vector3[21];
                _rightHandLandmarks = new Vector3[21];
                _handLandmarksBuffer = new Vector3[21];
                _gestures = new Gestures();
                IsInitialized = true;
                return true;
            }
            catch (Exception ex) {
                Debug.LogError($"Error initialising PythonManager: {ex.GetType().Name}: {ex.Message}");
                Debug.LogError($"{ex.StackTrace}");
                return false;
            }
        }

        public static unsafe HandLandmarks ProcessFrame(Image colourImage)
        {
            if (!IsInitialized) {
                Debug.LogWarning("Cannot process frame: PythonManager not initialized");
                return _handLandmarks;
            }

            // var kinectImage = colourImage.Memory.Span;
            byte[] tempBuffer = new byte[colourImage.SizeBytes];
            colourImage.CopyTo(tempBuffer);
            
            // Write the colour image to the memory mapped file as RGB
            // Debug.Log($"kinect image length: {kinectImage.Length}, colour image buffer size: {_colourImageBufferStream.Capacity}");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            byte* destPtr = _ipc.AcquireColourImagePtr();
            fixed (byte* srcPtr = tempBuffer)
            {
                byte* src = srcPtr;
                for (int i = 0; i < tempBuffer.Length; i += 4)
                {
                    *destPtr++ = *(src + 2);  // R
                    // Debug.Log($"destPtr-1: {*(destPtr-1)}");
                    *destPtr++ = *(src + 1);  // G
                    *destPtr++ = *src;        // B
                    src += 4;              // Skip to next pixel (including alpha)
                }
            }
            
            _ipc.ReleaseColourImagePtr();
            stopwatch.Stop();
            Debug.Log($"Writing colour image to memory mapped file: {stopwatch.ElapsedMilliseconds} ms");

            stopwatch.Restart();
            _ipc.SetReady();
            // Python processes the image and writes the hand landmarks to the memory mapped file
            _ipc.WaitDone();
            stopwatch.Stop();
            Debug.Log($"Waiting for Python to finish processing: {stopwatch.ElapsedMilliseconds} ms");
            
            // Read the hand landmarks from the memory mapped file
            stopwatch.Restart();
            _ipc.ReadHandLandmarksArray(0, _handLandmarksBuffer);
            if (_handLandmarksBuffer[0].x == 0f)
            {
                _handLandmarks.Left = null;
            }
            else
            {
                for (var i = 0; i < 21; i++)
                {
                    _leftHandLandmarks[i].x =
                        FlipX ? PythonImageWidth - _handLandmarksBuffer[i].x : _handLandmarksBuffer[i].x;
                    _leftHandLandmarks[i].y = _handLandmarksBuffer[i].y;
                    _leftHandLandmarks[i].z = _handLandmarksBuffer[i].z;
                }
                _handLandmarks.Left = _leftHandLandmarks;
            }
            _ipc.ReadHandLandmarksArray(21*3*4, _handLandmarksBuffer);
            if (_handLandmarksBuffer[0].x == 0f)
            {
                _handLandmarks.Right = null;
            }
            else
            {
                for (var i = 0; i < 21; i++)
                {
                    _rightHandLandmarks[i].x =
                        FlipX ? PythonImageWidth - _handLandmarksBuffer[i].x : _handLandmarksBuffer[i].x;
                    _rightHandLandmarks[i].y = _handLandmarksBuffer[i].y;
                    _rightHandLandmarks[i].z = _handLandmarksBuffer[i].z;
                }
                _handLandmarks.Right = _rightHandLandmarks;
            }

            stopwatch.Stop();
            Debug.Log($"Reading hand landmarks and gestures from memory mapped file: {stopwatch.ElapsedMilliseconds} ms");

            return _handLandmarks;
        }
        
        public static void Dispose()
        {
            if (!IsInitialized) {
                Debug.LogWarning("Cannot dispose of PythonManager: not initialized");
                return;
            }

            _ipc.Dispose();
            Debug.Log("Disposed of memory mapped files and events");
            IsInitialized = false;
        }
    }
}