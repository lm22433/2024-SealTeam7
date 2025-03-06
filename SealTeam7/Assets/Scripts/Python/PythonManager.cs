using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Threading;
using Microsoft.Azure.Kinect.Sensor;
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
        
        private const int PythonImageWidth = 1920;
        private const int PythonImageHeight = 1080;
        private const string ColourImageFileName = "colour_image";
        private const string HandLandmarksFileName = "hand_landmarks";
        private const string ReadyEventName = "SealTeam7ColourImageReady";
        private const string DoneEventName = "SealTeam7HandLandmarksDone";

        private static MemoryMappedFile _colourImageMemory;
        private static MemoryMappedFile _handLandmarksMemory;
        private static MemoryMappedViewAccessor _colourImageViewAccessor;
        private static SafeMemoryMappedViewHandle _colourImageViewHandle;
        private static MemoryMappedViewAccessor _handLandmarksViewAccessor;
        private static SafeMemoryMappedViewHandle _handLandmarksViewHandle;
        private static EventWaitHandle _readyEvent;
        private static EventWaitHandle _doneEvent;
        private static HandLandmarks _handLandmarks;  // Sometimes has a reference to _left/rightHandLandmarks
        private static Vector3[] _leftHandLandmarks;
        private static Vector3[] _rightHandLandmarks;
        private static Vector3[] _handLandmarksBuffer;  // Temporary buffer for reading hand landmarks

        public static void Initialize()
        {
            // string filePath = Path.Combine(Application.dataPath, "PythonServer", ColourImageFileName);
            // Debug.Log($"File exists: {File.Exists(filePath)}");
            // Debug.Log($"File size: {new FileInfo(filePath).Length} bytes");

            try {
                // // Try opening with just a FileStream first to see if basic access works
                // using (var fileStream = File.OpenRead(filePath))
                // {
                //     Debug.Log("Successfully opened file with FileStream");
                // }
    
                // Then try with MemoryMappedFile with size parameter set to 0
                _colourImageMemory = MemoryMappedFile.OpenExisting(ColourImageFileName, MemoryMappedFileRights.Write);
                _handLandmarksMemory = MemoryMappedFile.OpenExisting(HandLandmarksFileName, MemoryMappedFileRights.Read);
                _colourImageViewAccessor = _colourImageMemory.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Write);
                _colourImageViewHandle = _colourImageViewAccessor.SafeMemoryMappedViewHandle;
                _handLandmarksViewAccessor = _handLandmarksMemory.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                _handLandmarksViewHandle = _handLandmarksViewAccessor.SafeMemoryMappedViewHandle;
                _readyEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ReadyEventName);
                _doneEvent = new EventWaitHandle(false, EventResetMode.AutoReset, DoneEventName);
                _handLandmarks = new HandLandmarks();
                _leftHandLandmarks = new Vector3[21];
                _rightHandLandmarks = new Vector3[21];
                _handLandmarksBuffer = new Vector3[21];
            }
            catch (Exception ex) {
                Debug.LogError($"{ex.GetType().Name}: {ex.Message}");
                Debug.LogError($"{ex.StackTrace}");
            }
        }

        public static unsafe HandLandmarks ProcessFrame(Image colourImage)
        {
            var kinectImage = colourImage.Memory.Span;
            
            // Write the colour image to the memory mapped file as RGB
            // Debug.Log($"kinect image length: {kinectImage.Length}, colour image buffer size: {_colourImageBufferStream.Capacity}");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            byte* destPtr = null;
            _colourImageViewHandle.AcquirePointer(ref destPtr);
            fixed (byte* srcPtr = kinectImage)
            {
                byte* src = srcPtr;
                for (int i = 0; i < kinectImage.Length; i += 4)
                {
                    *destPtr++ = *(src + 2);  // R
                    *destPtr++ = *(src + 1);  // G
                    *destPtr++ = *src;        // B
                    src += 4;              // Skip to next pixel (including alpha)
                }
            }
            _colourImageViewHandle.ReleasePointer();
            stopwatch.Stop();
            Debug.Log($"Writing colour image to memory mapped file: {stopwatch.ElapsedMilliseconds} ms");

            stopwatch.Restart();
            _readyEvent.Set();
            // Python processes the image and writes the hand landmarks to the memory mapped file
            _doneEvent.WaitOne();
            stopwatch.Stop();
            Debug.Log($"Waiting for Python to finish processing: {stopwatch.ElapsedMilliseconds} ms");
            
            // Read the hand landmarks from the memory mapped file
            stopwatch.Restart();
            _handLandmarksViewHandle.ReadArray(0, _handLandmarksBuffer, 0, 21);
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
            _handLandmarksViewHandle.ReadArray(21*3*4, _handLandmarksBuffer, 0, 21);
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
            Debug.Log($"Reading hand landmarks from memory mapped file: {stopwatch.ElapsedMilliseconds} ms");

            return _handLandmarks;
        }
        
        public static void Dispose()
        {
            _colourImageViewHandle.Dispose();
            _colourImageViewAccessor.Dispose();
            _handLandmarksViewHandle.Dispose();
            _handLandmarksViewAccessor.Dispose();
            _colourImageMemory.Dispose();
            _handLandmarksMemory.Dispose();
            _readyEvent.Dispose();
            _doneEvent.Dispose();
        }
    }
}