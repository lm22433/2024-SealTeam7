using Enemies.Utils;
using Player;
using UnityEngine;

namespace Enemies
{
    public class Chinook : Aircraft
    {

        [SerializeField] private AK.Wwise.Event helicopterSound;
        [SerializeField] private EnemyData[] spawnableEnemies;
        [SerializeField] private int spawnCount;
        private bool _isGracefulShutdown;

        protected override void Attack(PlayerDamageable toDamage)
        {
            base.Attack(toDamage);
            EnemyManager.GetInstance().SpawnerSpawn(transform.position, spawnableEnemies[Random.Range(0, spawnableEnemies.Length)], spawnCount);
        }

        public override void Init()
        {
            base.Init();
            helicopterSound.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, SoundEffectCallback);
        }

        private void OnDestroy()
        {
            _isGracefulShutdown = true;
            helicopterSound.Stop(gameObject);
        }

        void SoundEffectCallback(object in_cookie, AkCallbackType in_type, object in_info)
        {
            if (!_isGracefulShutdown)
            {
                helicopterSound.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, SoundEffectCallback);
            }
        }
    }
}