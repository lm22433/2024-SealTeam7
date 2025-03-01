using System;
using UnityEngine;

namespace Camera
{
    public class CameraManager : MonoBehaviour
    {
        [Header("Camera References")]
        [SerializeField] private UnityEngine.Camera mainCamera;
        [SerializeField] private UnityEngine.Camera topologicalCamera;
        [SerializeField] private UnityEngine.Camera sideCamera1;
        [SerializeField] private UnityEngine.Camera sideCamera2;
        

        [Header("Camera Settings")] 
        [SerializeField, Range(0.0f, 1.0f)] private float sideCameraWidth = 0.25f;

        private void Start() => SetupCameraViewports();
        
        private void SetupCameraViewports()
        {
            mainCamera.rect = new Rect(
                sideCameraWidth,
                0,
                1 - sideCameraWidth,
                1
            );

            topologicalCamera.rect = new Rect(
                0,
                2 / 3f,
                sideCameraWidth,
                1 / 3f
            );

            sideCamera1.rect = new Rect(
                0,
                1 / 3f,
                sideCameraWidth,
                1 / 3f
            );
            
            sideCamera2.rect = new Rect(
                0,
                0,
                sideCameraWidth,
                1 / 3f
            );
        }
    }
}