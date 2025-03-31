using System;
using System.Collections.Generic;
using Effects;
using Enemies;
using Enemies.Utils;
using TMPro;
using UnityEngine;

namespace Game
{
    public class GameManager : MonoBehaviour
    {
        [Header("Game Settings")] 
        [SerializeField, Range(0f, 600f)] private float gameDuration = 600f;
        
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 1000;
        
        [Header("Score Settings")]
        [SerializeField] private int completionBonusScore = 500;
        [SerializeField] private float survivalBonusInterval = 30f;
        [SerializeField] private int survivalBonusScore = 250;
        [SerializeField] private int waveClearedEarlyBonusScore = 750;

        [Header("UI objects")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private GameObject healthBar;
        [SerializeField] private TMP_Text gameoverScoreText;
        [SerializeField] private TMP_Text gameoverText;
        [SerializeField] private GameObject tooltipPrefab;
        [SerializeField] private Transform tooltipContainer;

        [Header("Sound Options")]
        [SerializeField] private AK.Wwise.Event celebrationFanfare;

        private static GameManager _instance;
        
        public bool GameActive {get; set;}
        private float _timer;
		private int _totalKills;
        private int _score;
        private int _health;

        private Difficulty _difficulty;
        
        private float _lastSurvivalBonusTime;
        private float _lastDamageTime;
        private bool _isGameOver = false; 
        
        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);

            _health = maxHealth;
        }

        private void Update()
        {
            if (!GameActive) return;
            
            _timer -= Time.deltaTime;
            
            if (Time.time - _lastSurvivalBonusTime >= survivalBonusInterval)
            {
                _score += survivalBonusScore;
                _lastSurvivalBonusTime = Time.time;
                Debug.Log($"Survival Bonus! +{survivalBonusScore} points");
            }
            
            if (_timer <= 0) EndGame();

            UIUpdate();
        }

        private void UIUpdate()
        {
            scoreText.SetText($"Score: {_score}");
            var seconds = (_timer % 60 < 10) ? $"0{(int) (_timer % 60)}" : $"{(int) (_timer % 60)}";
            timerText.SetText($"{(int) _timer / 60}:{seconds}");

            healthBar.transform.localScale = new Vector3((float) _health / maxHealth, 1, 1);
        }

        public void DisplayEnemyTooltip(EnemyData newEnemy)
        {
            var toolTip = Instantiate(tooltipPrefab, tooltipContainer);
            toolTip.GetComponentInChildren<TMP_Text>().text = newEnemy.name + ": \n" + newEnemy.tooltipText;
            Destroy(toolTip, 5f);
        }

        public void StartGame()
        {
            _isGameOver = false;
            EnemyPool.GetInstance().ClearPool();
            EnemyManager.GetInstance().SetDifficulty(_difficulty);
            
            GameActive = true;
            _timer = gameDuration;
            _score = 0;
            _health = maxHealth;

            _lastSurvivalBonusTime = Time.time;

            EnemyManager.GetInstance().StartSpawning();

            gameoverScoreText.gameObject.transform.parent.gameObject.SetActive(false);
            
            Debug.Log("Game started!");
        }
        
        private void EndGame()
        {
            if (!GameActive) throw new Exception("Game has not started yet, how can it end dummy?");

            if (_isGameOver) return;
            
            celebrationFanfare.Post(gameObject);

            int completionBonus = (_health / maxHealth) * completionBonusScore;
            _score += completionBonus;
            Debug.Log($"Completion Bonus! +{completionBonus} points");
            
            GameActive = false;
            _isGameOver = true;
            Debug.Log($"Game Over! Score: {_score} Total Kills: {_totalKills}");

            gameoverText.SetText("Game over!");
            gameoverScoreText.SetText($"Score: {_score}");
            gameoverScoreText.gameObject.transform.parent.gameObject.SetActive(true);

            EnemyPool.GetInstance().ClearPool();
        }

        private void Die()
        {
            if (!GameActive) throw new Exception("Game has not started yet, how have you died dummy!");
            
            GameActive = false;
            Debug.Log($"You died! Score: {_score} Total Kills: {_totalKills}");

            _isGameOver = true;
            gameoverScoreText.SetText($"Score: {_score}");
            gameoverText.SetText("You died :(");
            gameoverScoreText.gameObject.transform.parent.gameObject.SetActive(true);

            EnemyPool.GetInstance().ClearPool();
        }

        public void ApplyWaveClearedEarlyBonus()
        {
            if (!GameActive) throw new Exception("Game has not started yet, how can you clear a wave dummy?");

            _score += waveClearedEarlyBonusScore;
            Debug.Log($"Wave cleared early! +{waveClearedEarlyBonusScore} points");
        }
        
        public void TakeDamage(int damage)
        {
            if (!GameActive) throw new Exception("Game has not started yet, how can you take damage dummy?");
            
            _health -= damage;
            DamageEffectManager.GetInstance().ScreenDamageEffect(damage / 10.0f);
            Debug.Log($"Ouch! Took {damage} damage!");
            
            if (_health <= 0)
            {
                _health = 0;
                Die();
            }
        }

        public void RegisterKill(int basePoints, float multiplier = 1.0f)
        {
            if (!GameActive) throw new Exception("Game has not started yet, how have you killed something dummy?");
            
            int points = Mathf.RoundToInt(basePoints * multiplier);
            _score += points;
            _totalKills++;
            
            Debug.Log($"Killed something! +{points} points");
        }

        public void SetDifficulty(Difficulty difficulty) => _difficulty = difficulty;
        public void SetGameDuration(int time) => gameDuration = time;
        
        public static GameManager GetInstance() => _instance;
        public bool IsGameActive() => GameActive;
        public float GetTimer() => _timer;
        public int GetScore() => _score;
    }
}
