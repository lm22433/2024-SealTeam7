using Player;
using UnityEngine;

namespace Enemies.Tower
{
    public class Tower : Enemy
    {
        [SerializeField] private float lookSpeed;
        private Vector3 _target;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _target = transform.forward;
        }

        protected override void Attack(Collider hit)
        {
            _target = hit.transform.position;
            var playerMgr = hit.GetComponentInParent<PlayerManager>();
            DealDamageRpc(playerMgr.Owner, playerMgr, damage);
        }

        protected override void ServerUpdate()
        {
            base.ServerUpdate();
            
            _target.y = transform.position.y;
            transform.LookAt(Vector3.Lerp(transform.position + transform.forward, _target, lookSpeed * Time.deltaTime));
        }
    }
}