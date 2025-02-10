using UnityEngine;

public class SandWaveRider : MonoBehaviour
{
    [SerializeField] LayerMask layerMask;
    [SerializeField] float riderSpeed;
    [SerializeField] float maximumUpPull = 200f;
    Rigidbody body;

    private void Awake() {
        body = transform.parent.GetComponent<Rigidbody>();
    }
    void FixedUpdate()
    {
        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position, Vector3.down, out hit, maximumUpPull, layerMask))
        { 
            Debug.DrawRay(transform.position, Vector3.down * hit.distance, Color.yellow); 
            body.AddForce(new Vector3 (0, hit.distance / maximumUpPull * riderSpeed, 0));
        }

    }
}
