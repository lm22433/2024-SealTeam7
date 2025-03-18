using Map;
using UnityEngine;

public class HandReconstruction : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private GameObject[] bones; //Must be length 18 (palm - 4 fingers right to left - thumb all bottom to top)
    [SerializeField] private MapManager mapManager;
    [SerializeField] private GameObject hand;
    [SerializeField] private float _lerpFactor;
    [SerializeField] private float _thresholdDst;
    [SerializeField, Range(0,1)] private int _hand;
    [SerializeField] private Vector3[] positions_offset;
    private Vector3[] positions;

    void Start()
    {
        positions = new Vector3[21];
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        //Get hand points Vector3[21]
        var tempPositions = mapManager.GetHandPositions(_hand); //GetPositions();

        if (tempPositions == null) {   
            Debug.Log("Null position");
            return;
        }

        for(int i = 0; i < tempPositions.Length; i++) {
            positions[i] = tempPositions[i];// + positions_offset[i];
        }

        //Hand direction about the Y axis
        Vector3 targetDir = positions[9] - positions[0];
        gameObject.transform.localPosition = positions[0];
        
        gameObject.transform.localRotation = Quaternion.Euler(gameObject.transform.localRotation.eulerAngles.x, Quaternion.LookRotation(targetDir.normalized, transform.up).eulerAngles.y, gameObject.transform.localRotation.eulerAngles.z);

        targetDir = positions[17] - positions[5];
        hand.transform.localRotation = Quaternion.Euler(
            hand.transform.localRotation.eulerAngles.x, 
            hand.transform.localRotation.eulerAngles.y,  
            Vector3.Angle(targetDir.normalized, transform.right) + 180
        );

        //Hand direction wrist to knuckle x axis
        RotateBoneToVector(1, 0, 9);
    
        //4 finger knuckles x axis
        RotateBoneToVector(2, 17, 18);
        RotateBoneToVector(5, 13, 14);
        RotateBoneToVector(8, 9, 10);
        RotateBoneToVector(11, 5, 6);
    
        //4 finger knuckles to mid finger x axis
        RotateBoneToVector(3, 18, 19);
        RotateBoneToVector(6, 14, 15);
        RotateBoneToVector(9, 10, 11);
        RotateBoneToVector(12, 6, 7);

        //4 finger mid to end x axis
        RotateBoneToVector(4, 19, 20);
        RotateBoneToVector(7, 15, 16);
        RotateBoneToVector(10, 11, 12);
        RotateBoneToVector(13, 7, 8);

        //Thumb X axis
        RotateBoneToVector(15, 1, 2);
        RotateBoneToVector(16, 2, 3);
        RotateBoneToVector(17, 3, 4);

    }


    private void RotateBoneToVector(int bone, int start, int end) {
        Vector3 targetDir = positions[end] - positions[start];
        bones[bone].transform.localRotation = Quaternion.Euler(
            Quaternion.LookRotation(targetDir.normalized, transform.up).eulerAngles.x, 
            bones[bone].transform.rotation.y, 
            bones[bone].transform.rotation.z //Quaternion.LookRotation(targetDir.normalized, transform.up).eulerAngles.z
        );
    }

    private void OnDrawGizmos()
    {
        Vector3 targetDir = positions[17] - positions[5];

        Gizmos.color = Color.black;
        Gizmos.DrawLine(positions[5], positions[5] + targetDir);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(positions[0], positions[0] + transform.right * 10);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(positions[0], 2);
    }
}
