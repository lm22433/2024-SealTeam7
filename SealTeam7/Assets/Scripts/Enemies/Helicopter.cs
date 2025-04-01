using UnityEngine;
using Enemies.Utils;

namespace Enemies
{
    public class Helicopter : Aircraft
    {
        // [SerializeField] private AK.Wwise.Event helicopterSound;
        private bool _isGracefulShutdown;

        public override void Init()
        {
            base.Init();
            transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
            // helicopterSound.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, SoundEffectCallback);
        }

        private void OnDestroy()
        {
            _isGracefulShutdown = true;

            // helicopterSound.Stop(gameObject);
        }

        // void SoundEffectCallback(object in_cookie, AkCallbackType in_type, object in_info){
        //     if (!_isGracefulShutdown) {
        //         helicopterSound.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, SoundEffectCallback);
        //     }
        // }
        
        protected override float Heuristic(Node start, Node end)
        {
            return start.WorldPos.y > flyHeight - 10f ? 10000f : 0f;
        }
        
        protected override void EnemyUpdate()
        {
            switch (State)
            {
                case EnemyState.AttackCore:
                {
                    TargetPosition = new Vector3(TargetPosition.x, flyHeight, TargetPosition.z);
                    TargetRotation = Quaternion.Euler(transform.eulerAngles.x,
                        Quaternion.LookRotation(TargetPosition - transform.position).eulerAngles.y,
                        transform.eulerAngles.z);
                    break;
                }
                case EnemyState.AttackHands:
                {
                    TargetRotation = Quaternion.Euler(
                        transform.eulerAngles.x,
                        Quaternion.LookRotation(TargetPosition - transform.position).eulerAngles.y,
                        transform.eulerAngles.z);
                    break;
                }
                case EnemyState.Moving:
                {
                    TargetRotation = Quaternion.Euler(transform.eulerAngles.x, Quaternion.LookRotation((Path.Length > 0 ? Path[PathIndex] : TargetPosition) - transform.position).eulerAngles.y, transform.eulerAngles.z);
                    break;
                }
            }
        }

        protected override void EnemyFixedUpdate()
        {
            if (Rb.position.y > flyHeight) Rb.AddForce(Vector3.down, ForceMode.Impulse);
            if (Rb.position.y < flyHeight) Rb.AddForce(Vector3.up, ForceMode.Impulse);
        }
    }
}