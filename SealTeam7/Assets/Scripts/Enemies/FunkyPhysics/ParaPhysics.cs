using UnityEngine;

namespace Enemies.FunkyPhysics
{
    public class ParaPhysics : BasePhysics
    {
        [SerializeField] private float newGrav;
        [SerializeField] private GameObject parachute;
        private Vector3 _newGravity;
        private bool _falling;

        protected override void Start()
        {
            base.Start();
            Rb.useGravity = false;
            _newGravity = new Vector3(0, -newGrav, 0);
            _falling = true;
        }

        protected override void EnemyFixedUpdate()
        {
            if (!Self.Grounded && _falling) Rb.AddForce(_newGravity, ForceMode.Acceleration);
            else if (_falling)
            {
                _falling = false;
                parachute.SetActive(false);
                Rb.useGravity = true;
            }
        }
    }
}