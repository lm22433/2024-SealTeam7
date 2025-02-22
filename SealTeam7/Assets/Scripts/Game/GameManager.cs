using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game
{
    public class GameManager : MonoBehaviour
    {
        [Header("Game Settings")] 
        [SerializeField, Range(0f, 600f)] private float gameDuration = 10f;

        private static GameManager _instance;
        private bool _gameActive;
        private float _timer;
        private int _score;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);

            StartGame();
        }

        private void Update()
        {
            if (!_gameActive) return;
            
            _timer -= Time.deltaTime;   
            if (_timer <= 0) EndGame();
        }

        public void StartGame()
        {
            if (_gameActive) throw new Exception("Game has already started!");
            
            _gameActive = true;
            _timer = gameDuration;
            _score = 0;
        }
        
        private void EndGame()
        {
            if (!_gameActive) throw new Exception("Game has not started yet!");
            
            _gameActive = false;
        }
        
        public GameManager GetInstance() => _instance;
        public bool IsGameActive() => _gameActive;
        public float GetTimer() => _timer;
        public int GetScore() => _score;
    }
}
