using System;
using System.Collections;
using System.Linq;
using Player;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Enemies.Spawner
{
    public class Runner : Enemy
    {
        [SerializeField] private float aggroRange;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float idleMoveDistance;
        [SerializeField] private float idleTargetChangeDelay;
        private Collider[] _aggroResults;
        private Vector3 _idleTarget;

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            _aggroResults = new Collider[12];
            StartCoroutine(ChangeIdleTarget());
        }

        protected override void Attack(Collider hit)
        {
            var playerMgr = hit.GetComponentInParent<PlayerManager>();
            DealDamageRpc(playerMgr.Owner, playerMgr, damage);
        }

        private void MoveTowards(Vector3 target)
        {
            
            transform.LookAt(new Vector3(target.x, transform.position.y, target.z));
            if (Vector3.Distance(transform.position, target) > attackRange)
            {
                transform.Translate(transform.forward * (moveSpeed * Time.deltaTime), Space.World);
            }
        }

        private IEnumerator ChangeIdleTarget()
        {
            while (true)
            {
                yield return new WaitForSeconds(idleTargetChangeDelay);
                _idleTarget = transform.position + Random.onUnitSphere * idleMoveDistance;                
            }
        }

        protected override void ServerUpdate()
        {
            base.ServerUpdate();

            Array.Clear(_aggroResults, 0, _aggroResults.Length);
            Physics.OverlapSphereNonAlloc(transform.position, aggroRange, _aggroResults, LayerMask.GetMask("Player"), QueryTriggerInteraction.Collide);
            Collider closestPlayer = _aggroResults.OrderBy(c => c ? (transform.position - c.transform.position).sqrMagnitude : float.MaxValue).First();
            if (closestPlayer)
            {
                MoveTowards(closestPlayer.transform.position);
            }
            else
            {
                MoveTowards(_idleTarget);
            }
        }
    }
}