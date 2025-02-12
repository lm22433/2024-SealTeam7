using FishNet.Object;
using UnityEngine;

namespace Enemies
{
    public abstract class SandRider : NetworkBehaviour
    {
        [SerializeField] private float riderSpeed = 10f;
        [SerializeField] private float lowPoint = -1.3f;
        [SerializeField] private float highPoint = 50f;
        [SerializeField] private float groundDeadZone = 1f;
        [SerializeField] private LayerMask sandRiderMask;

        private Vector3 _velocity;

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
            Debug.Log($"{name} riding");
            if (Physics.Raycast(transform.position + Vector3.up * highPoint, Vector3.down, out hit, highPoint - lowPoint, sandRiderMask))
            { 
                Debug.DrawRay(transform.position + Vector3.up * highPoint, Vector3.down * hit.distance, Color.yellow);

                _velocity += Vector3.Lerp(Vector3.zero, Vector3.up * (highPoint - lowPoint - hit.distance),
                    (highPoint - lowPoint - hit.distance / (highPoint - lowPoint)) * riderSpeed);
                
            } else {
                Debug.DrawRay(transform.position + Vector3.up * highPoint, Vector3.down * (highPoint - lowPoint), Color.green);
                _velocity += Physics.gravity * (0.0001f);
            }
            
            // apply vertical translation
            transform.Translate(_velocity * Time.deltaTime);
        }

        protected virtual void ServerUpdate() {}
    }
}