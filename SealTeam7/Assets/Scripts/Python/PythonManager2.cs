using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Azure.Kinect.Sensor;
using UnityEngine;

namespace Python
{
    public class PythonManager2
    {
        public static bool FlipX { get; set; } = true;
        public static bool FlipHandedness { get; set; } = false;
        public static HandLandmarks HandLandmarks => _handLandmarks;
        
        private const int PythonImageWidth = 1920;
        private const int PythonImageHeight = 1080;
        private const string ColourImageFileName = "colour_image.dat";
        private const string HandLandmarksFileName = "hand_landmarks.dat";
        private const string ReadyEventName = "SealTeam7ColourImageReady";
        private const string DoneEventName = "SealTeam7HandLandmarksDone";

        private static MemoryMappedFile _colourImageBuffer;
        private static MemoryMappedFile _handLandmarksBuffer;
        private static MemoryMappedViewAccessor _colourImageBufferAccessor;
        private static MemoryMappedViewAccessor _handLandmarksBufferAccessor;
        private static EventWaitHandle _readyEvent;
        private static EventWaitHandle _doneEvent;
        private static HandLandmarks _handLandmarks;
        private static Vector3[] _leftHandLandmarks;
        private static Vector3[] _rightHandLandmarks;

        public static void Initialize()
        {
            _colourImageBuffer = MemoryMappedFile.CreateFromFile("Assets/PythonServer/"+ColourImageFileName);
            _handLandmarksBuffer = MemoryMappedFile.CreateFromFile("Assets/PythonServer/"+HandLandmarksFileName);
            _colourImageBufferAccessor = _colourImageBuffer.CreateViewAccessor();
            _handLandmarksBufferAccessor = _handLandmarksBuffer.CreateViewAccessor();
            _readyEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ReadyEventName);
            _doneEvent = new EventWaitHandle(false, EventResetMode.AutoReset, DoneEventName);
        }

        public static HandLandmarks ProcessFrame(Image colourImage)
        {
            var kinectImage = colourImage.Memory.Span;
            
            // Write the colour image to the memory mapped file as RGB
            for (var i = 0; i < kinectImage.Length; i += 4)
            {
                var r = kinectImage[i + 2];
                var g = kinectImage[i + 1];
                var b = kinectImage[i];
                _colourImageBufferAccessor.Write(i, r);
                _colourImageBufferAccessor.Write(i + 1, g);
                _colourImageBufferAccessor.Write(i + 2, b);
            }

            _readyEvent.Set();
            // Python processes the image and writes the hand landmarks to the memory mapped file
            _doneEvent.WaitOne();
            
            // Read the hand landmarks from the memory mapped file
            _handLandmarksBufferAccessor.ReadArray(0, _leftHandLandmarks, 0, 21);
            _handLandmarksBufferAccessor.ReadArray(21 * 3 * 4, _rightHandLandmarks, 0, 21);
            _handLandmarks.Left = _leftHandLandmarks[0].x == 0f ? null : _leftHandLandmarks;
            _handLandmarks.Right = _rightHandLandmarks[0].x == 0f ? null : _rightHandLandmarks;

            return _handLandmarks;
        }
        
        public static void Dispose()
        {
            _colourImageBufferAccessor.Dispose();
            _handLandmarksBufferAccessor.Dispose();
            _colourImageBuffer.Dispose();
            _handLandmarksBuffer.Dispose();
            _readyEvent.Dispose();
            _doneEvent.Dispose();
        }
    }
}