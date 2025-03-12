using Game;
using Player;
using UnityEngine;

namespace Enemies.Utils
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed;
        public Vector3 TargetPosition { get; set; }
        public PlayerDamageable ToDamage { get; set; }
        public int Damage { get; set; }
        
        private void Update()
        {
            if (!GameManager.GetInstance().IsGameActive())
            {
                Destroy(gameObject);
                return;
            }
            
            transform.position = Vector3.MoveTowards(transform.position, TargetPosition, speed * Time.deltaTime);

            if (transform.position == TargetPosition)
            {
                ToDamage.TakeDamage(Damage);
                Destroy(gameObject);
            }
        }
    }
}