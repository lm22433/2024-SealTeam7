using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoveCamera : MonoBehaviour
{

    [SerializeField] private Transform cameraPosition;

    // Update is called once per frame
    private void Update()
    {
        transform.position = new Vector3(cameraPosition.position.x,
                                        cameraPosition.position.y + 0.9f * cameraPosition.localScale.y,
                                        cameraPosition.position.z);
    }
}
