using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;

public class MoveCamera : NetworkBehaviour
{

    [SerializeField] private Transform cameraPosition;

    public override void OnStartClient()
    {
        if (IsOwner) {
            var mainCam = FindFirstObjectByType<Camera>().gameObject.transform;
            mainCam.SetParent(transform);
            mainCam.position = transform.position;
            mainCam.rotation = transform.rotation;
        }
    }

    // Update is called once per frame
    private void Update()
    {
        transform.position = cameraPosition.position;
    }
}
