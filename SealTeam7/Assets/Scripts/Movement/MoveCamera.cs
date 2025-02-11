using UnityEngine;
using FishNet.Object;

namespace Movement
{
    public class MoveCamera : NetworkBehaviour
    {
        public override void OnStartClient()
        {
            if (IsOwner)
            {
                var mainCam = Camera.main;
                mainCam.transform.position = transform.position;
                mainCam.transform.rotation = transform.rotation;
                mainCam.transform.SetParent(transform);
            }
        }
    }
}
