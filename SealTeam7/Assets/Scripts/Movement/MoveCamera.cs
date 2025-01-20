using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoveCamera : MonoBehaviour
{

    [SerializeField] private Transform cameraPosition;

    // Update is called once per frame
    private void Update()
    {
        transform.position = cameraPosition.position;
    }
}
