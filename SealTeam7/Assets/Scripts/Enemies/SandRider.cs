using FishNet.Object;
using GameKit.Dependencies.Utilities;
using UnityEngine;
using Map;

namespace Enemies
{
    public abstract class SandRider : NetworkBehaviour
    {
        private KinectAPI _kinect;
        private NoiseGenerator _noiseGenerator;
        
        public override void OnStartServer()
        {
            base.OnStartServer();

            _kinect = FindFirstObjectByType<KinectAPI>();
            _noiseGenerator = FindFirstObjectByType<NoiseGenerator>();
        }

        private void Update()
        {
            if (!IsServerInitialized)
            {
                ClientUpdate();
            }
            else
            {
                ServerUpdate();
            }
        }
        
        protected abstract void ClientUpdate();

        protected virtual void ServerUpdate()
        {
            var x = (int) transform.position.x;
            var z = (int) transform.position.z;
        
            // sit on terrain
            transform.SetPosition(false,
                _kinect.isKinectPresent
                    ? new Vector3(transform.position.x, _kinect.GetHeight(x, z) + transform.lossyScale.y, transform.position.z)
                    : new Vector3(transform.position.x, _noiseGenerator.GetHeight(x, z) + transform.lossyScale.y, transform.position.z));
        }
    }
}