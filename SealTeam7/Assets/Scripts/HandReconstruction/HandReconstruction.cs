using Map;
using UnityEngine;

public class HandReconstruction : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private GameObject[] bones; //Must be length 18 (palm - 4 fingers left to right - thumb all bottom to top)
    [SerializeField] private MapManager mapManager;
    [SerializeField] private GameObject hand;
    [SerializeField] private float _lerpFactor;
    [SerializeField] private float _thresholdDst;
    [SerializeField, Range(0,1)] private int _hand;
    [SerializeField] private Vector3[] positions;

    void Start()
    {
        positions = new Vector3[21];
    }

    float SignedAngleBetween(Vector3 a, Vector3 b, Vector3 n){
        // angle in [0,180]
        float angle = Vector3.Angle(a,b);
        float sign = Mathf.Sign(Vector3.Dot(n,Vector3.Cross(a,b)));

        // angle in [-179,180]
        float signed_angle = angle * sign;

        float angle360 =  (signed_angle + 180) % 360;

        return angle360;
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        //Get hand points Vector3[20]
        var tempPositions = mapManager.GetHandPositions(_hand); //GetPositions();
        var prevPos = positions[0];

        if (tempPositions != null) {   
            for(int i = 0; i < tempPositions.Length; i++) {
                positions[i] = Vector3.Lerp(positions[i], tempPositions[i], _lerpFactor);
            }
        } else {
            return;
        }


        Vector3 targetDir = positions[9] - positions[0];
        gameObject.transform.localPosition = positions[0];
        
        gameObject.transform.localRotation = Quaternion.Euler(gameObject.transform.localRotation.eulerAngles.x, Quaternion.LookRotation(targetDir.normalized, transform.up).eulerAngles.y, gameObject.transform.localRotation.eulerAngles.z);

        //targetDir = positions[17] - positions[5];
        //hand.transform.localRotation = Quaternion.LookRotation(targetDir.normalized, Vector3.right);

        targetDir = positions[9] - positions[0];
        bones[1].transform.localRotation = Quaternion.Euler(Quaternion.LookRotation(targetDir.normalized, transform.up).eulerAngles.x, bones[1].transform.rotation.y, bones[1].transform.rotation.z);
    }

     private void OnDrawGizmos()
    {
        return;
        Vector3 targetDir = positions[17] - positions[5];

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(positions[5], positions[5] + targetDir);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(positions[0], positions[0] + transform.right * 10);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(positions[0], 2);
    }
}
