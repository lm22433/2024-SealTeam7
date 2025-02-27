using System;
using Enemies;
using TMPro;
using UnityEngine;

namespace Game
{
    [Serializable]
    public struct Difficulty
    {
        public int index;
        public float duration;
        public float spawnInterval;
        public EnemyData[] enemies;
    }
    
    public class GameManager : MonoBehaviour
    {
        [Header("Game Settings")] 
        [SerializeField, Range(0f, 600f)] private float gameDuration = 600f;
        
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 1000;
        
        [Header("Score Settings")]
        [SerializeField] private int completionBonusScore = 1000;
        [SerializeField] private float survivalBonusInterval = 30f;
        [SerializeField] private int survivalBonusScore = 500;
        
        [Header("Difficulty Settings")]
        [SerializeField] private Difficulty[] difficulties;

        [Header("UI objects")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timerText;

        private static GameManager _instance;
        private bool _gameActive;
        private float _timer;
		private int _totalKills;
        private int _score;
        private int _health;
        private Difficulty _difficulty;
        
        private float _lastSurvivalBonusTime;
        private float _lastDamageTime;
        private float _lastDifficultyIncrease;
        
        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);

            _difficulty = difficulties[0];
            _health = maxHealth;

            StartGame();
        }

        private void Update()
        {
            if (!_gameActive) return;
            
            _timer -= Time.deltaTime;

            if (Time.time - _lastDifficultyIncrease >= _difficulty.duration)
            {
                if (_difficulty.index < difficulties.Length - 1)
                {
                    _difficulty = difficulties[_difficulty.index + 1];
                    EnemyManager.GetInstance().SetDifficulty(_difficulty);
                    _lastDifficultyIncrease = Time.time;
                    Debug.Log($"Difficulty increased! Current difficulty: {_difficulty.index}");   
                }
            }
            
            if (Time.time - _lastSurvivalBonusTime >= survivalBonusInterval)
            {
                _score += survivalBonusScore;
                _lastSurvivalBonusTime = Time.time;
                Debug.Log("Survival Bonus! +500 points");
            }
            
            if (_timer <= 0) EndGame();

            UIUpdate();
        }

        private void UIUpdate() {
            scoreText.SetText($"Score: {_score}");
            timerText.SetText($"{(int) _timer / 60}:{(int) (_timer % 60)}");
        }

        public void StartGame()
        {
            if (_gameActive) throw new Exception("You can't start a game when one is already happening dummy!");
            
            _gameActive = true;
            _timer = gameDuration;
            _score = 0;

            _lastSurvivalBonusTime = Time.time;
            _lastDifficultyIncrease = Time.time;
            
            Debug.Log("Game started!");
        }
        
        private void EndGame()
        {
            if (!_gameActive) throw new Exception("Game has not started yet, how can it end dummy?");

            _score += completionBonusScore;
            Debug.Log("Completion Bonus! +1000 points");
            
            _gameActive = false;
            Debug.Log($"Game Over! Score: {_score} Total Kills: {_totalKills}");
        }

        private void Die()
        {
            if (!_gameActive) throw new Exception("Game has not started yet, how have you died dummy!");
            
            Debug.Log($"You died! Score: {_score} Total Kills: {_totalKills}");
        }
        
        public void TakeDamage(int damage)
        {
            if (!_gameActive) throw new Exception("Game has not started yet, how can you take damage dummy?");
            
            _health -= damage;
            Debug.Log($"Ouch! Took {damage} damage!");
            
            if (_health <= 0)
            {
                _health = 0;
                Die();
            }
        }

        public void RegisterKill(int score)
        {
            if (!_gameActive) throw new Exception("Game has not started yet, how have you killed something dummy?");
            
            Debug.Log($"Killed something! +{score} points");

			_totalKills++;
			_score += score;
        }
        
        public static GameManager GetInstance() => _instance;
        public bool IsGameActive() => _gameActive;
        public float GetTimer() => _timer;
        public int GetScore() => _score;
        public Difficulty GetInitialDifficulty() => difficulties[0];
    }
}
