using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game
{
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

        private static GameManager _instance;
        private bool _gameActive;
        private float _timer;
		private int _totalKills;
        private int _score;
        private int _health;
        
        private float _lastSurvivalBonusTime;
        private float _lastDamageTime;
        
        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);

            _health = maxHealth;

            StartGame();
        }

        private void Update()
        {
            if (!_gameActive) return;
            
            _timer -= Time.deltaTime;
            
            if (Time.time - _lastSurvivalBonusTime >= survivalBonusInterval)
            {
                _score += survivalBonusScore;
                _lastSurvivalBonusTime = Time.time;
                Debug.Log("Survival Bonus! +500 points");
            }
            
            if (_timer <= 0) EndGame();
        }

        public void StartGame()
        {
            if (_gameActive) throw new Exception("You can't start a game when one is already happening dummy!");
            
            _gameActive = true;
            _timer = gameDuration;
            _score = 0;

            _lastSurvivalBonusTime = Time.time;
            
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
            
            // Debug.Log($"Killed something! +{score} points");

			_totalKills++;
			_score += score;
        }
        
        public static GameManager GetInstance() => _instance;
        public bool IsGameActive() => _gameActive;
        public float GetTimer() => _timer;
        public int GetScore() => _score;
    }
}
