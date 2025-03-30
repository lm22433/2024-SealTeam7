using UnityEngine;

namespace Enemies
{
    public class Helicopter : Aircraft
    {
        // [SerializeField] private AK.Wwise.Event helicopterSound;
        private bool _isGracefulShutdown;

        public override void Init()
        {
            base.Init();
           //  helicopterSound.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, SoundEffectCallback);
        }

        private void OnDestroy()
        {
            _isGracefulShutdown = true;
            // helicopterSound.Stop(gameObject);
        }

        // void SoundEffectCallback(object in_cookie, AkCallbackType in_type, object in_info)
        // {
        //     if (!_isGracefulShutdown)
        //     {
        //         helicopterSound.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, SoundEffectCallback);
        //     }
        // }
    }
}