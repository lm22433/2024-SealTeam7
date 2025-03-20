using Player;
using UnityEngine;

namespace Enemies.Utils
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed;
        public Vector3 Target { get; set; }
        public PlayerDamageable ToDamage { get; set; }
        public int Damage { get; set; }
        
        private void Update()
        {
            transform.position = Vector3.MoveTowards(transform.position, Target, speed * Time.deltaTime);

            if (transform.position == Target)
            {
                ToDamage.TakeDamage(Damage);
                Destroy(gameObject);
            }
        }
    }
}