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
        [Range(0f, 1f)] public float durationPercentage;
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
        [SerializeField] private int completionBonusScore = 500;
        [SerializeField] private float survivalBonusInterval = 30f;
        [SerializeField] private int survivalBonusScore = 250;

        [Header("UI objects")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private GameObject healthBar;
        [SerializeField] private TMP_Text gameoverScoreText;
        [SerializeField] private TMP_Text gameoverText;

        private static GameManager _instance;
        public bool GameActive {get; set;}
        private float _timer;
		private int _totalKills;
        private int _score;
        private int _health;
        private Difficulty _difficulty;
        private Difficulty[] _difficulties;
        
        private float _lastSurvivalBonusTime;
        private float _lastDamageTime;
        private float _lastDifficultyIncrease;
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

            if (Time.time - _lastDifficultyIncrease >= _difficulty.durationPercentage * gameDuration)
            {
                if (_difficulty.index < _difficulties.Length - 1)
                {
                    _difficulty = _difficulties[_difficulty.index + 1];
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

        private void UIUpdate()
        {
            scoreText.SetText($"Score: {_score}");
            var seconds = (_timer % 60 < 10) ? $"0{(int) (_timer % 60)}" : $"{(int) (_timer % 60)}";
            timerText.SetText($"{(int) _timer / 60}:{seconds}");

            healthBar.transform.localScale = new Vector3((float) _health / maxHealth, 1, 1);
        }

        public void StartGame()
        {
            _isGameOver = false;
            EnemyManager.GetInstance().KillAllEnemies();
            _difficulty = _difficulties[0];
            EnemyManager.GetInstance().SetDifficulty(_difficulty);
            
            GameActive = true;
            _timer = gameDuration;
            _score = 0;
            _health = maxHealth;

            _lastSurvivalBonusTime = Time.time;
            _lastDifficultyIncrease = Time.time;

            gameoverScoreText.gameObject.transform.parent.gameObject.SetActive(false);
            
            Debug.Log("Game started!");
        }
        
        private void EndGame()
        {
            if (!GameActive) throw new Exception("Game has not started yet, how can it end dummy?");

            if (_isGameOver) return;
            
            int completionBonus = (_health / maxHealth) * completionBonusScore;
            _score += completionBonus;
            Debug.Log($"Completion Bonus! +{completionBonus} points");
            
            GameActive = false;
            _isGameOver = true;
            Debug.Log($"Game Over! Score: {_score} Total Kills: {_totalKills}");

            gameoverText.SetText("Game over!");
            gameoverScoreText.SetText($"Score: {_score}");
            gameoverScoreText.gameObject.transform.parent.gameObject.SetActive(true);

            EnemyManager.GetInstance().KillAllEnemies();
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

            EnemyManager.GetInstance().KillAllEnemies();
        }
        
        public void TakeDamage(int damage)
        {
            if (!GameActive) throw new Exception("Game has not started yet, how can you take damage dummy?");
            
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
            if (!GameActive) throw new Exception("Game has not started yet, how have you killed something dummy?");
            
            Debug.Log($"Killed something! +{score} points");

			_totalKills++;
			_score += score;
        }

        public void SetDifficulty(Difficulty[] difficulties) => _difficulties = difficulties;
        public void SetGameDuration(int time) => gameDuration = time;
        
        public static GameManager GetInstance() => _instance;
        public bool IsGameActive() => GameActive;
        public float GetTimer() => _timer;
        public int GetScore() => _score;
    }
}
