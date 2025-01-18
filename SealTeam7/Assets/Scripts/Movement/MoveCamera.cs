using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoveCamera : MonoBehaviour
{

    [SerializeField] private Transform cameraPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    /*void Start()
    {
        
    }*/

    // Update is called once per frame
    private void Update()
    {
        transform.position = cameraPosition.position;
    }
}
