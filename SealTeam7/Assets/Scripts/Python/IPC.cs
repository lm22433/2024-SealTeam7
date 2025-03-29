using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using UnityEngine;

namespace Python
{
    public abstract unsafe class IPC : IDisposable
    {
        protected const string ColourImageFileName = "colour_image";
        protected const string HandLandmarksFileName = "hand_landmarks";
        protected const string GesturesFileName = "gestures";
        protected const string ReadyEventName = "SealTeam7ColourImageReady";
        protected const string DoneEventName = "SealTeam7HandLandmarksDone";

        public abstract byte* AcquireColourImagePtr();
        public abstract void ReleaseColourImagePtr();
        public abstract void ReadHandLandmarksArray(int start, Vector3[] handLandmarks);
        public abstract void SetReady();
        public abstract void WaitDone();
        public abstract void Dispose();
    }


    public unsafe class WindowsIPC : IPC
    {
        private readonly MemoryMappedFile _colourImageMemory;
        private readonly MemoryMappedFile _handLandmarksMemory;
        private readonly MemoryMappedFile _gesturesMemory;
        private readonly MemoryMappedViewAccessor _colourImageViewAccessor;
        private readonly SafeMemoryMappedViewHandle _colourImageViewHandle;
        private readonly MemoryMappedViewAccessor _handLandmarksViewAccessor;
        private readonly SafeMemoryMappedViewHandle _handLandmarksViewHandle;
        private readonly MemoryMappedViewAccessor _gesturesViewAccessor;
        private readonly SafeMemoryMappedViewHandle _gesturesViewHandle;
        private readonly EventWaitHandle _readyEvent;
        private readonly EventWaitHandle _doneEvent;
        
        public WindowsIPC()
        {
            _colourImageMemory = MemoryMappedFile.OpenExisting(ColourImageFileName, MemoryMappedFileRights.Write);
            _handLandmarksMemory = MemoryMappedFile.OpenExisting(HandLandmarksFileName, MemoryMappedFileRights.Read);
            _gesturesMemory = MemoryMappedFile.OpenExisting(GesturesFileName, MemoryMappedFileRights.Read);
            _colourImageViewAccessor = _colourImageMemory.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Write);
            _colourImageViewHandle = _colourImageViewAccessor.SafeMemoryMappedViewHandle;
            _handLandmarksViewAccessor = _handLandmarksMemory.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            _handLandmarksViewHandle = _handLandmarksViewAccessor.SafeMemoryMappedViewHandle;
            _gesturesViewAccessor = _gesturesMemory.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            _gesturesViewHandle = _gesturesViewAccessor.SafeMemoryMappedViewHandle;
            _readyEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ReadyEventName);
            _doneEvent = new EventWaitHandle(false, EventResetMode.AutoReset, DoneEventName);
        }

        public override byte* AcquireColourImagePtr()
        {
            byte* ptr = null;
            _colourImageViewHandle.AcquirePointer(ref ptr);
            return ptr;
        }
        
        public override void ReleaseColourImagePtr()
        {
            _colourImageViewHandle.ReleasePointer();
        }
        
        public override void ReadHandLandmarksArray(int start, Vector3[] handLandmarks)
        {
            _handLandmarksViewAccessor.ReadArray(start, handLandmarks, 0, 21);
        }
        
        public override void SetReady()
        {
            _readyEvent.Set();
        }
        
        public override void WaitDone()
        {
            _doneEvent.WaitOne();
        }
        
        public override void Dispose()
        {
            _colourImageViewHandle.Dispose();
            _colourImageViewAccessor.Dispose();
            _handLandmarksViewHandle.Dispose();
            _handLandmarksViewAccessor.Dispose();
            _gesturesViewHandle.Dispose();
            _gesturesViewAccessor.Dispose();
            _colourImageMemory.Dispose();
            _handLandmarksMemory.Dispose();
            _gesturesMemory.Dispose();
            _readyEvent.Dispose();
            _doneEvent.Dispose();
        }
    }

    public unsafe class LinuxIPC : IPC
    {
        private readonly int _colourImageShmId;
        private readonly int _handLandmarksShmId;
        private readonly int _gesturesShmId;
        private readonly void* _readySemId;
        private readonly void* _doneSemId;
        private byte* _colourImagePtr;
        private byte* _handLandmarksPtr;
        private byte* _gesturesPtr;

        public LinuxIPC()
        {
            // Open shared memory segments
            int colourImageShmFd = shm_open("/" + ColourImageFileName, 0x0002, 0644);  // read/write
            int handLandmarksShmFd = shm_open("/" + HandLandmarksFileName, 0x0000, 0644); // read only
            
            // Attach shared memory segments
            _colourImagePtr = (byte*)mmap(null, 1920*1080*3, 0x1 | 0x2, 0x1 | 0x20, colourImageShmFd, 0);
            _handLandmarksPtr = (byte*)mmap(null, 21*3*2*4, 0x1 | 0x2, 0x1 | 0x20, handLandmarksShmFd, 0);
            
            // Close file descriptors
            close(colourImageShmFd);
            close(handLandmarksShmFd);
            
            // Open semaphores
            _readySemId = sem_open("/" + ReadyEventName, 0x0000);
            _doneSemId = sem_open("/" + DoneEventName, 0x0000);
        }

        public override byte* AcquireColourImagePtr()
        {
            return _colourImagePtr;
        }

        public override void ReleaseColourImagePtr()
        {
            // No need to release pointer as it's managed by the class
        }

        public override void ReadHandLandmarksArray(int start, Vector3[] handLandmarks)
        {
            fixed (Vector3* destPtr = handLandmarks)
            {
                Buffer.MemoryCopy(_handLandmarksPtr + start, destPtr, handLandmarks.Length * sizeof(Vector3), handLandmarks.Length * sizeof(Vector3));
            }
        }

        public override void SetReady()
        {
            sem_post(_readySemId);
        }
        
        public override void WaitDone()
        {
            sem_wait(_doneSemId);
        }

        public override void Dispose()
        {
            // Detach shared memory segments
            munmap(_colourImagePtr, 1920 * 1080 * 3);
            munmap(_handLandmarksPtr, 21 * 3 * 2 * 4);
        }
        
        [DllImport("libc", SetLastError = true)]
        private static extern int shm_open(string name, int oflag, int mode);
        
        [DllImport("libc", SetLastError = true)]
        public static extern void* mmap(void* addr, uint length, int prot, int flags, int fd, uint offset);

        [DllImport("libc", SetLastError = true)]
        public static extern int close(int fd);

        [DllImport("libc", SetLastError = true)]
        public static extern int munmap(void* addr, uint len);

        [DllImport("libc", SetLastError = true)]
        public static extern void* sem_open(string name, int oflag);

        [DllImport("libc", SetLastError = true)]
        public static extern int sem_post(void* sem);
        
        [DllImport("libc", SetLastError = true)]
        public static extern int sem_wait(void* sem);
    }
}