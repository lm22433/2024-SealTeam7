using Map;
using UnityEngine;

public class HandReconstruction : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private GameObject[] bones; //Must be length 18 (palm - 4 fingers left to right - thumb all bottom to top)
    [SerializeField] private MapManager mapManager;
    [SerializeField] private GameObject hand;
    private Vector3[] positions;
    
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        //Get hand points Vector3[20]
        var tempPositions = mapManager.GetHandPositions(0); //GetPositions();

        if (tempPositions == null) {
            return;
        }

        positions = tempPositions;

        /*
            gameobject position to position of 0
            0 to 12 vector
            cosine angle with forward
            rotation for gameobject around y

            vector 5 to 17 
            cosine angle between left vector
            rotation around z for bone 0

            vector 0 to 9
            cosine angle between up vector
            rotaition around x for bone 1

            vector 5 to 6 
            rotation bone 2
            vector 6 to 7 
            rotation bone 3
            vector 7 to 8 
            rotation bone 4


            ...

            1 to 2 
            rotation bone 16
            2 to 3 
            rotation bone 17
            3 to 4 
            rotation bone 18

        */

        gameObject.transform.position = positions[0];
        
        Vector3 targetDir = positions[17] - positions[5];
        hand.transform.localRotation = Quaternion.Euler(0, 0, Vector3.SignedAngle(transform.right, targetDir, Vector3.forward));

        //targetDir = positions[9] - positions[0];
        //bones[1].transform.localRotation = Quaternion.Euler(Vector3.SignedAngle(transform.forward, targetDir, Vector3.forward), bones[1].transform.rotation.y, bones[1].transform.rotation.z);

    }

     private void OnDrawGizmos()
    {

        if (positions == null) {
            return;
        }

        Vector3 targetDir = positions[17] - positions[5];

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(positions[5], positions[5] + targetDir);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(positions[0], positions[0] + transform.right * 10);

        Gizmos.color = Color.yellow;
        foreach(Vector3 pos in positions) {
            Gizmos.DrawSphere(pos, 2);
        }
    }
}
