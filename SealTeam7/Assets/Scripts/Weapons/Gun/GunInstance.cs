using UnityEngine;

namespace Weapons
{
    public class GunInstance : MonoBehaviour
    {
        [Header("Effects")]
        public ParticleSystem muzzleFlash;
        public Transform muzzlePoint;
        
        [Header("Audio")]
        public AudioSource gunAudioSource;
        public AudioClip gunShotSound;
        public AudioClip reloadSound;
        public AudioClip emptyMagazineSound;

        private Camera _mainCamera;
        
        public void Initialize()
        {
            _mainCamera = Camera.main;
        }

        public void PlayMuzzleFlash()
        {
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }
        }

        public void PlayShootSound()
        {
            if (gunShotSound != null && gunAudioSource != null)
            {
                gunAudioSource.PlayOneShot(gunShotSound);
            }
        }

        public void PlayReloadSound()
        {
            if (reloadSound != null && gunAudioSource != null)
            {
                gunAudioSource.PlayOneShot(reloadSound);
            }
        }

        public void PlayEmptySound()
        {
            if (emptyMagazineSound != null && gunAudioSource != null)
            {
                gunAudioSource.PlayOneShot(emptyMagazineSound);
            }
        }

        public Vector3 GetMuzzlePosition()
        {
            return muzzlePoint != null ? muzzlePoint.position : transform.position;
        }

        public Ray GetFireRay()
        {
            return _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        }
    }
}