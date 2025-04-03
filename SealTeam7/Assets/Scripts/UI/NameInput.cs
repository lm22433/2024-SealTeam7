using System.Linq;
using Enemies.Utils;
using Game;
using Leaderboard;
using TMPro;
using UnityEngine;

namespace UI
{
    public class NameInput : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;

        public void SubmitResult()
        {
            GameManager gameManager = GameManager.GetInstance();
            EnemyManager enemyManager = EnemyManager.GetInstance();
            var gameResult = new GameResult(
                inputField.text,
                gameManager.GetDifficulty().difficultyName,
                gameManager.GetScore(),
                enemyManager.GetWave(),
                enemyManager.GetEnemiesKilled(),
                gameManager.GetDamageTaken(),
                (long) gameManager.GetTimeSurvived(),
                enemyManager.GetEnemiesKilledDetailed().ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => (long) kvp.Value
                )
            );
            FirebaseManager.GetInstance().SaveGameResult(gameResult);
        }
    }
}
