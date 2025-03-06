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
            int finalDamage = Mathf.Max(damage - armour, 1);
            GameManager.GetInstance().TakeDamage(finalDamage);
            Debug.Log($"{gameObject.name} took {finalDamage} damage (Original: {damage}, Armour: {armour})");
        }
    }
}