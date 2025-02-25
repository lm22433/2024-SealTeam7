using Game;
using UnityEngine;

namespace Enemies.FunkyPhysics
{
    public class SoldierPhysics : BasePhysics
    {
        protected int _score = 10;
        protected GameManager _gameManager;

        protected override void DeathAffect(EnemyManager _enemyManager)
        {
            int curScore = _gameManager.GetScore();
            Debug.Log("Score stuff would go here");
        }
    }
}