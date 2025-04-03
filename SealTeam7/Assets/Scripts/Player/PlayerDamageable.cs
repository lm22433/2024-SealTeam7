using Game;
using UnityEngine;

namespace Player
{
    public abstract class PlayerDamageable : MonoBehaviour
    {
        [Header("Armour Settings")] 
        [SerializeField, Range(0, 20)] protected int armour;

        public void TakeDamage(int damage)
        {
            int finalDamage = damage *  (1 / armour);
            GameManager.GetInstance().TakeDamage(finalDamage);
            Debug.Log($"{gameObject.name} took {finalDamage} damage (Original: {damage}, Armour: {armour})");
        }
    }
}