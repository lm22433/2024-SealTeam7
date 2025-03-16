using UnityEngine;

namespace Enemies.FunkyPhysics
{
    public class ParaPhysics : BasePhysics
    {
        [SerializeField] private float newGrav;
        private Vector3 newGravity;
        [SerializeField] private GameObject parachute;

        protected override void Start()
        {
            base.Start();
            Rb.useGravity = false;
            newGravity = new Vector3(0, -newGrav, 0);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            
            Rb.AddForce(newGravity, ForceMode.Acceleration);
            
            if (Grounded) parachute.SetActive(false);
        }
    }
}