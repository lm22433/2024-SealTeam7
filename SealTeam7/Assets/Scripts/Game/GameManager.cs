using System;
using Effects;
using Enemies.Utils;
using TMPro;
using UI;
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
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private GameObject healthBar;
        [SerializeField] private GameObject tooltipPrefab;
        [SerializeField] private Transform tooltipContainer;

        [Header("Sound Options")]
        [SerializeField] private AK.Wwise.Event celebrationFanfare;

        private static GameManager _instance;

        private bool _gameActive;
        private float _timer;
        private float _timeSurvived;
		private int _totalKills;
        private int _score;
        private int _health;
        private bool _sandboxMode;
        private bool _endlessMode;
        private bool _handTracking;

        private Difficulty _difficulty;
        
        private float _lastSurvivalBonusTime;
        private float _lastDamageTime;
        private bool _isGameOver = false; 
        
        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);

            _health = maxHealth;
            
            Application.targetFrameRate = 30;
        }

        private void Update()
        {
            if (!_gameActive) return;
            
            _timer -= Time.deltaTime;
            _timeSurvived += Time.deltaTime;
            
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
            scoreText.SetText($"{_score}");

            int wave = EnemyManager.GetInstance().GetWave();
            waveText.SetText(wave < 10 ? $"0{wave}" : $"{wave}");

            int minutes = (int) _timer / 60;
            int seconds = (int) _timer % 60;
            string secondsStr = (seconds < 10) ? $"0{seconds}" : $"{seconds}";
            timerText.SetText($"{minutes}:{secondsStr}");

            healthBar.transform.localScale = new Vector3(
                (float) _health / maxHealth, 
                healthBar.transform.localScale.y, 
                healthBar.transform.localScale.z);
        }
        
        public void DisplayTooltip(string text, float duration)
        {
            var toolTip = Instantiate(tooltipPrefab, tooltipContainer);
            toolTip.GetComponentInChildren<TMP_Text>().text = text;
            Destroy(toolTip, duration);
        }

        public void StartGame()
        {
            _isGameOver = false;
            EnemyPool.GetInstance().ClearPool();
            EnemyManager.GetInstance().SetDifficulty(_difficulty);
            
            _gameActive = true;
            _timer = gameDuration;
            _score = 0;
            _health = maxHealth;

            _lastSurvivalBonusTime = Time.time;

            OpeningManager.GetInstance().StartOpening();
            EnemyManager.GetInstance().StartSpawning();
            
            Debug.Log("Game started!");
            Debug.Log("Difficulty: " + _difficulty.difficultyName);
            Debug.Log("Game Duration: " + gameDuration);
            Debug.Log("Health: " + _health);
            Debug.Log("Sandbox Mode: " + _sandboxMode);
            Debug.Log("Endless Mode: " + _endlessMode);
            Debug.Log("Hand Tracking: " + _handTracking);
        }
        
        private void EndGame()
        {
            if (!_gameActive) throw new Exception("Game has not started yet, how can it end dummy?");

            if (_isGameOver) return;
            
            celebrationFanfare.Post(gameObject);

            int completionBonus = (_health / maxHealth) * completionBonusScore;
            _score += completionBonus;
            Debug.Log($"Completion Bonus! +{completionBonus} points");
            
            _gameActive = false;
            _isGameOver = true;
            Debug.Log($"Game Over! Score: {_score} Total Kills: {_totalKills}");

            MenuManager.GetInstance().TriggerGameOverMenu(
                false,
                _score,
                EnemyManager.GetInstance().GetEnemiesKilled(),
                EnemyManager.GetInstance().GetWave(),
                _timeSurvived,
                maxHealth - _health,
            EnemyManager.GetInstance().GetEnemiesKilledDetailed()
            );
            EnemyPool.GetInstance().ClearPool();
        }

        private void Die()
        {
            if (!_gameActive) throw new Exception("Game has not started yet, how have you died dummy!");
            
            _gameActive = false;
            Debug.Log($"You died! Score: {_score} Total Kills: {_totalKills}");

            _isGameOver = true;

            MenuManager.GetInstance().TriggerGameOverMenu(
                true,
                _score,
                EnemyManager.GetInstance().GetEnemiesKilled(),
                EnemyManager.GetInstance().GetWave(),
                _timeSurvived,
                maxHealth - _health,
                EnemyManager.GetInstance().GetEnemiesKilledDetailed()
            );
            EnemyPool.GetInstance().ClearPool();
        }

        public void ApplyWaveClearedEarlyBonus()
        {
            if (!_gameActive) throw new Exception("Game has not started yet, how can you clear a wave dummy?");

            _score += waveClearedEarlyBonusScore;
            Debug.Log($"Wave cleared early! +{waveClearedEarlyBonusScore} points");
        }
        
        public void TakeDamage(int damage)
        {
            if (!_gameActive) throw new Exception("Game has not started yet, how can you take damage dummy?");
            
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
            if (!_gameActive) throw new Exception("Game has not started yet, how have you killed something dummy?");
            
            int points = Mathf.RoundToInt(basePoints * multiplier);
            _score += points;
            _totalKills++;
            
            Debug.Log($"Killed something! +{points} points");
        }

        public void SetGameActive(bool isGameActive) => _gameActive = isGameActive;
        public void SetDifficulty(Difficulty difficulty) => _difficulty = difficulty;
        public void SetGameDuration(int time) => gameDuration = time;
        public void SetSandboxMode(bool isSandboxMode) => _sandboxMode = isSandboxMode;
        public void SetEndlessMode(bool isEndlessMode) => _endlessMode = isEndlessMode;
        public void SetHandTracking(bool isHandTracking) => _handTracking = isHandTracking; 
        
        public static GameManager GetInstance() => _instance;
        public bool IsGameActive() => _gameActive;
        public float GetTimer() => _timer;
        public int GetScore() => _score;
        public bool IsSandboxMode() => _sandboxMode;
        public bool IsEndlessMode() => _endlessMode;
        public bool IsHandTracking() => _handTracking;
    }
}
