using FishNet.Object;
using UnityEngine;

namespace Enemies
{
    public abstract class SandRider : NetworkBehaviour
    {
        [SerializeField] private float riderSpeed = 0.1f;
        [SerializeField] private float lowPoint = -1.3f;
        [SerializeField] private float highPoint = 50f;
        [SerializeField] private float groundDeadZone = 1f;
        [SerializeField] private LayerMask sandRiderMask;

        private Vector3 _velocity;
        private bool _hasRb;

        public override void OnStartClient()
        {
            base.OnStartClient();
            Rigidbody rb;
            _hasRb = TryGetComponent<Rigidbody>(out rb);
        }

        public virtual void Update()
        {
            if (!IsServerInitialized || IsHostInitialized)
            {
                ClientUpdate();
            }
            if (IsServerInitialized || IsHostInitialized)
            {
                ServerUpdate();
            }
        }

        protected virtual void ClientUpdate()
        {
            RaycastHit hit;
            // Does the ray intersect any objects excluding the player layer
            if (Physics.Raycast(transform.position + Vector3.up * highPoint, Vector3.down, out hit, highPoint - lowPoint, sandRiderMask))
            { 
                Debug.DrawRay(transform.position + Vector3.up * highPoint, Vector3.down * hit.distance, Color.yellow);

                _velocity += Vector3.Lerp(Vector3.zero, Vector3.up * (highPoint - lowPoint - hit.distance),
                    (highPoint - lowPoint - hit.distance / (highPoint - lowPoint)) * riderSpeed * Time.deltaTime);
                
            } else {
                Debug.DrawRay(transform.position + Vector3.up * highPoint, Vector3.down * (highPoint - lowPoint), Color.green);
                if (!_hasRb) _velocity += Physics.gravity * Time.deltaTime;
            }
            
            // apply vertical translation
            transform.Translate(_velocity);
        }

        protected virtual void ServerUpdate() {}
    }
}