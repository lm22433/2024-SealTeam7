using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;

public class MoveCamera : NetworkBehaviour
{

    [SerializeField] private Transform cameraPosition;

    public override void OnStartClient()
    {
        if (base.IsOwner) {
            var mainCam = FindFirstObjectByType<Camera>().gameObject.transform;
            mainCam.SetParent(transform);
            mainCam.position = transform.position;
        }
    }

    // Update is called once per frame
    private void Update()
    {

        transform.position = cameraPosition.position;
    }
}
