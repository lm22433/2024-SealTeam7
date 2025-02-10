using UnityEngine;

public class SandWaveRider : MonoBehaviour
{
    [SerializeField] LayerMask layerMask;
    [SerializeField] float riderSpeed;
    [SerializeField] float gravityPoint = 1f;
    [SerializeField] float surfacePoint = 1f;
    Transform body;

    private void Awake() {
        body = transform.parent;
    }
    void FixedUpdate()
    {
        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position, Vector3.down, out hit, gravityPoint, layerMask))
        { 
            Debug.DrawRay(transform.position, Vector3.down * hit.distance, Color.yellow); 

            if (Physics.Raycast(transform.position, Vector3.down, out hit, surfacePoint, layerMask))
            { 
                Debug.DrawRay(transform.position, Vector3.down * hit.distance, Color.blue);
                body.Translate(new Vector3(0, Mathf.Lerp(0, hit.distance, hit.distance * riderSpeed), 0));
            }
            body.GetComponent<Rigidbody>().useGravity = false;
        } else {
             Debug.DrawRay(transform.position, Vector3.down * gravityPoint, Color.green); 
             body.GetComponent<Rigidbody>().useGravity = true;
        }

    }
}
