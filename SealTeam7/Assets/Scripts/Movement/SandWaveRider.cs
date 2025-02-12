using UnityEngine;

namespace Movement
{
    public class SandWaveRider : MonoBehaviour
    {
        [SerializeField] LayerMask layerMask;
        [SerializeField] float riderSpeed;
        [SerializeField] float startPoint = 1f;
        [SerializeField] float rangeGap = 1f;
        [SerializeField] float deadGap = 1f;
        [SerializeField] private bool enemy;
    
        private Rigidbody _rb;
        private Transform _body;

        private void Awake() {
            _rb = GetComponentInParent<Rigidbody>();
            _body = transform.parent;
        }
        
        void Update()
        {
            RaycastHit hit;
            // Does the ray intersect any objects excluding the player layer
            if (Physics.Raycast(transform.position + new Vector3(0, startPoint, 0), Vector3.down, out hit, startPoint, layerMask))
            { 
                Debug.DrawRay(transform.position + new Vector3(0, startPoint, 0), Vector3.down * hit.distance, Color.yellow);
                
                if (enemy && hit.distance < deadGap)
                {
                    _rb.useGravity = false;
                }
                else if (enemy && hit.distance > deadGap)
                {
                    _rb.useGravity = true;
                }
                
                float diff = Mathf.Abs(hit.transform.position.y - transform.position.y) / (hit.transform.position.y + transform.position.y);

                _body.Translate(new Vector3(0, Mathf.Lerp(0, startPoint - hit.distance, (startPoint - hit.distance / startPoint) * riderSpeed), 0));


            } else {
                Debug.DrawRay(transform.position + new Vector3(0, startPoint, 0), Vector3.down * (startPoint + rangeGap / 2), Color.green); 
            }

        }
    }
}
