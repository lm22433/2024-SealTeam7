using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using Emgu.CV;
using Microsoft.Azure.Kinect.Sensor;
using Microsoft.Win32.SafeHandles;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Python
{
    public class PythonManager2
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

        private static MemoryMappedFile _colourImageBuffer;
        private static MemoryMappedFile _handLandmarksBuffer;
        private static MemoryMappedViewAccessor _colourImageViewAccessor;
        private static SafeMemoryMappedViewHandle _colourImageViewHandle;
        private static MemoryMappedViewStream _handLandmarksBufferStream;
        private static EventWaitHandle _readyEvent;
        private static EventWaitHandle _doneEvent;
        private static HandLandmarks _handLandmarks;
        private static Vector3[] _leftHandLandmarks;
        private static Vector3[] _rightHandLandmarks;

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
                _colourImageBuffer = MemoryMappedFile.OpenExisting(ColourImageFileName, MemoryMappedFileRights.Write);
                _handLandmarksBuffer = MemoryMappedFile.OpenExisting(HandLandmarksFileName, MemoryMappedFileRights.Read);
                _colourImageViewAccessor = _colourImageBuffer.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Write);
                _colourImageViewHandle = _colourImageViewAccessor.SafeMemoryMappedViewHandle;
                _handLandmarksBufferStream = _handLandmarksBuffer.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
                _readyEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ReadyEventName);
                _doneEvent = new EventWaitHandle(false, EventResetMode.AutoReset, DoneEventName);
                _handLandmarks = new HandLandmarks();
                _leftHandLandmarks = new Vector3[21];
                _rightHandLandmarks = new Vector3[21];
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
            _handLandmarksBufferStream.SafeMemoryMappedViewHandle.ReadArray(0, _leftHandLandmarks, 0, 21);
            _handLandmarksBufferStream.SafeMemoryMappedViewHandle.ReadArray(21*3*4, _rightHandLandmarks, 0, 21);
            _handLandmarks.Left = _leftHandLandmarks[0].x == 0f ? null : _leftHandLandmarks;
            _handLandmarks.Right = _rightHandLandmarks[0].x == 0f ? null : _rightHandLandmarks;
            stopwatch.Stop();
            Debug.Log($"Reading hand landmarks from memory mapped file: {stopwatch.ElapsedMilliseconds} ms");

            return _handLandmarks;
        }
        
        public static void Dispose()
        {
            _colourImageViewHandle.Dispose();
            _colourImageViewAccessor.Dispose();
            _handLandmarksBufferStream.Dispose();
            _colourImageBuffer.Dispose();
            _handLandmarksBuffer.Dispose();
            _readyEvent.Dispose();
            _doneEvent.Dispose();
        }
    }
}