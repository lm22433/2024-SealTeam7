using System;
using UnityEngine;


namespace Sound
{
    public enum SoundType
    {
        Music,
        SoundEffect,
    }
    
    public class SoundManager : MonoBehaviour
    {   
        [Header("Volume Settings")] 
        [SerializeField, Range(0f, 100f)] private float masterVolume = 100f;
        [SerializeField, Range(0f, 100f)] private float musicVolume = 100f;
        [SerializeField, Range(0f, 100f)] private float soundEffectVolume = 100f;
        
        [SerializeField] private AK.Wwise.RTPC soundEffectRTPC;
        [SerializeField] private AK.Wwise.RTPC musicRTPC;

        private static SoundManager _instance;
        private bool _isShuttingDown = false;
            
        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);

            musicRTPC.SetGlobalValue(masterVolume * musicVolume);
            soundEffectRTPC.SetGlobalValue(masterVolume * soundEffectVolume);
        }

        private void OnApplicationQuit() {
            _isShuttingDown = true;

        }

        public void SetMasterVolume(float volume) {
            if (volume >= 0 && volume <= 100) {
                masterVolume = volume;

                musicRTPC.SetGlobalValue(masterVolume * musicVolume);
                soundEffectRTPC.SetGlobalValue(masterVolume * soundEffectVolume);
            } 
        }
        
        public void SetSoundEffectVolume(float volume) {
            if (volume >= 0 && volume <= 100) {
                soundEffectVolume = volume;
                soundEffectRTPC.SetGlobalValue(masterVolume * soundEffectVolume);
            } 
        }

        public void SetMusicVolume(float volume) {
            if (volume >= 0 && volume <= 100) {
                musicVolume = volume;
                musicRTPC.SetGlobalValue(masterVolume * musicVolume);
            } 
        }

        public void PostSound(GameObject obj, AK.Wwise.Event evt, SoundType type, bool loop = false) {

            if (loop) {
                void SoundCallBack(object in_cookie, AkCallbackType in_type, object in_info){
                    if (!_isShuttingDown) {
                        evt.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, SoundCallBack);
                    }
                }

                evt.Post(obj, (uint)AkCallbackType.AK_EndOfEvent, SoundCallBack);
            } else {
                evt.Post(obj);
            }
        }

        public void StopSound(GameObject obj, AK.Wwise.Event evt, int fade = 0, AkCurveInterpolation interpolation = AkCurveInterpolation.AkCurveInterpolation_Constant) {
            evt.Stop(obj, fade, interpolation);
        }

    }
}
