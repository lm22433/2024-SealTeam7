using System;
using System.Collections;
using Map;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class OpeningManager : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Vector3 startPosition;
        [SerializeField] private Quaternion startRotation;
        [SerializeField] private Vector3 endPosition;
        [SerializeField] private Quaternion endRotation;
        [SerializeField] private float duration;

        [Header("Visual Settings")] 
        [SerializeField] private RawImage kinectFeed;
        [SerializeField] private float kinectFeedDuration;
        // [SerializeField] private float kinectFeedFadeDuration = 0.5f;
        [SerializeField] private float gameViewDuration;
        
        private static OpeningManager _instance;

        private byte[] _colourImage;
        private readonly object _lock = new();
        
        private Texture2D _kinectFeedTexture;
        private bool _isPlaying = false;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);
            
            kinectFeed.gameObject.SetActive(false);
            mainCamera.transform.position = startPosition;
            mainCamera.transform.rotation = startRotation;
        }

        private void Update()
        {
            if (!_isPlaying) return;

            if (_kinectFeedTexture == null)
            {
                _kinectFeedTexture = new Texture2D(1920, 1080, TextureFormat.BGRA32, false);
                kinectFeed.texture = _kinectFeedTexture;
            }

            lock (_lock)
            {
                if (_colourImage != null)
                {
                    _kinectFeedTexture.LoadRawTextureData(_colourImage);
                    _kinectFeedTexture.Apply();
                }
            }
        }

        public static OpeningManager GetInstance() => _instance;

        public void UpdateKinectFeed(K4AdotNet.Sensor.Image colourImage)
        {
            if (!_isPlaying || colourImage == null) return;
    
            int width = colourImage.WidthPixels;
            int height = colourImage.HeightPixels;
    
            lock (_lock)
            {
                if (_colourImage == null || _colourImage.Length != width * height * 4)
                    _colourImage = new byte[width * height * 4];

                colourImage.CopyTo(_colourImage);
            }
        }

        public void StartOpening()
        {
            if (_isPlaying) throw new Exception("The opening sequence is already playing!");
            _isPlaying = true;
            kinectFeed.gameObject.SetActive(true);
            StartCoroutine(PlayOpeningSequence());
        }
        
        private IEnumerator PlayOpeningSequence()
        {
            yield return new WaitForSeconds(kinectFeedDuration);
         
            // TODO: Fade
            kinectFeed.gameObject.SetActive(false);
            
            yield return new WaitForSeconds(gameViewDuration);
            
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                float easedT = CubicEase(t);

                mainCamera.transform.position = Vector3.Lerp(startPosition, endPosition, easedT);
                mainCamera.transform.rotation = Quaternion.Slerp(startRotation, endRotation, easedT);
                
                yield return null;
            }

            mainCamera.transform.position = endPosition;
            mainCamera.transform.rotation = endRotation;
            _isPlaying = false;
        }
        
        private static float CubicEase(float t)
        {
            return 1 - (1 - t) * (1 - t) * (1 - t);
        }
    }
}