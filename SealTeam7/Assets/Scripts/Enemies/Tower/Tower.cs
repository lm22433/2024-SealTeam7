using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using Player;
using UnityEngine;

namespace Enemies.Tower
{
    public class Tower : Enemy
    {
        [SerializeField] private float attackRange;
        [SerializeField] private float attackDelay;
        [SerializeField] private float lookSpeed;
        private float _timeSinceAttack;
        private Vector3 _target;

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            // align target in tower's xz plane
            _target = Vector3.ProjectOnPlane(transform.position, Vector3.up) + Vector3.up * transform.position.y;
        }
        
        public override void Attack(Collider hit)
        {
            if (_timeSinceAttack > attackDelay)
            {
                _target = Vector3.ProjectOnPlane(hit.transform.position, Vector3.up) + Vector3.up * transform.position.y;
                //attackEffect.Play();
                _timeSinceAttack = 0;
                var playerMgr = hit.GetComponent<PlayerManager>();
                DealDamageRPC(playerMgr.Owner, playerMgr, damage);
            }
        }

        [TargetRpc]
        public override void DealDamageRPC(NetworkConnection conn, PlayerManager playerMgr, float dmg)
        {
            playerMgr.TakeDamage(dmg);
        }
        
        public override void Update()
        {
            base.Update();
            
            transform.LookAt(Vector3.Lerp(transform.forward, _target, lookSpeed * Time.deltaTime));
            
            _timeSinceAttack += Time.deltaTime;

            Collider[] results = new Collider[12];
            Physics.OverlapSphereNonAlloc(transform.position, attackRange, results, LayerMask.GetMask("PlayerHolder"), QueryTriggerInteraction.Collide);
            // sort by distance from tower
            Collider closestPlayer = results.OrderBy(c => c ? (transform.position - c.transform.position).sqrMagnitude : float.MaxValue).First();
            if (closestPlayer) Attack(closestPlayer);
        }
    }
}