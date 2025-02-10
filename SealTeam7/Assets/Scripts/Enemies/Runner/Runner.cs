using System.Linq;
using Player;
using UnityEngine;

namespace Enemies.Runner
{
    public class Runner : Enemy
    {
        [SerializeField] private float focusRange;
        [SerializeField] private float moveSpeed;
        private Collider[] _aggroResults;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _aggroResults = new Collider[12];
        }

        protected override void Attack(Collider hit)
        {
            var playerMgr = hit.GetComponentInParent<PlayerManager>();
            DealDamageRpc(playerMgr.Owner, playerMgr, damage);
        }

        private void MoveTowards(Collider player)
        {
            transform.LookAt(new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z));
            if (Vector3.Distance(transform.position, player.transform.position) > attackRange)
            {
                transform.Translate(transform.forward * (moveSpeed * Time.deltaTime), Space.World);                
            }
        }

        protected override void ServerUpdate()
        {
            base.ServerUpdate();

            Physics.OverlapSphereNonAlloc(transform.position, focusRange, _aggroResults, LayerMask.GetMask("Player"), QueryTriggerInteraction.Collide);
            Collider closestPlayer = _aggroResults.OrderBy(c => c ? (transform.position - c.transform.position).sqrMagnitude : float.MaxValue).First();
            if (closestPlayer) MoveTowards(closestPlayer);
        }
    }
}