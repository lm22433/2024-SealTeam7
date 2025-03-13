using System;
using System.Collections.Generic;
using AK.Wwise;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game
{

    [Serializable]
    public struct DifficultyProfile
    {
        public String name;
        public Difficulty[] difficulties;
    }

    public class MenuHandler : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private DifficultyProfile[] difficulties;
        [SerializeField] private int currentDifficulty;
        [SerializeField, Range(0f, 600f)] private int maxGameDuration = 600;
        [SerializeField] private int currentDuration = 180;

        [Header("UI containers")]
        [SerializeField] private GameObject _settingsMenu;
        [SerializeField] private GameObject _mainMenu;
        [SerializeField] private GameObject _scoreUI;
        [SerializeField] private TMP_Dropdown _difficultyDropdown;
        [SerializeField] private Slider _durationSlider;
        [SerializeField] private TMP_Text _durationSliderText;

        [Header("Music Settings")]
        [SerializeField] private AK.Wwise.Event mainMenuMusic;
        [SerializeField] private AK.Wwise.Event introMusic;
        [SerializeField] private AK.Wwise.Event gameAmbience;

        private bool _paused;
        private bool _isGameRunning = false;
        private bool _isGracefulShutdown = false;

        private void Awake() {

            _mainMenu.SetActive(true);
            _settingsMenu.SetActive(false);
        }

        private void Start() {
            mainMenuMusic.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, MainMenuMusicCallback);
        }

        private void OnApplicationQuit() {
            _isGracefulShutdown = true;

            mainMenuMusic.Stop(gameObject);
            introMusic.Stop(gameObject);
            gameAmbience.Stop(gameObject);
        }

        void MainMenuMusicCallback(object in_cookie, AkCallbackType in_type, object in_info){
            if (!_isGameRunning && !_isGracefulShutdown) {
                mainMenuMusic.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, MainMenuMusicCallback);
            }
        }

        void AmbienceMusicCallback(object in_cookie, AkCallbackType in_type, object in_info){
            if (_isGameRunning && !_isGracefulShutdown) {
                gameAmbience.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, AmbienceMusicCallback);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown("escape")) {
                PauseGame();
            }

            if (Input.GetKeyDown("r")) {
                GameManager.GetInstance().StartGame();
            }
        }

        private void PauseGame() {
            _paused = !_paused;
            GameManager.GetInstance().GameActive = !_paused;
            Time.timeScale = (_paused) ? 0 : 1;
            
            _scoreUI.SetActive(!_paused);
            if (_paused) {
                OnSettingButtonClicked();
            } else{
                _settingsMenu.SetActive(false);
            }
                
        }
        
        private void InitialiseSettings() {
            List<String> difficultyNames = new List<string>();

            foreach(DifficultyProfile diff in difficulties) {
                difficultyNames.Add(diff.name);
            }

            _difficultyDropdown.ClearOptions();

            _difficultyDropdown.AddOptions(difficultyNames);
            _difficultyDropdown.value = currentDifficulty;

            _difficultyDropdown.RefreshShownValue();

            _difficultyDropdown.onValueChanged.AddListener(evt =>
            {
                currentDifficulty = _difficultyDropdown.value;
            });

            _durationSlider.maxValue = maxGameDuration;
            _durationSlider.value = currentDuration;
            _durationSlider.onValueChanged.AddListener(evt =>
            {
                currentDuration = (int) _durationSlider.value;

                var seconds = (currentDuration % 60 < 10) ? $"0{(int) (currentDuration % 60)}" : $"{(int) (currentDuration % 60)}";
                _durationSliderText.SetText($"{(int) currentDuration / 60}:{seconds}");
                
                GameManager.GetInstance().SetGameDuration(currentDuration);
            });

        }

        public void OnPlayButtonClicked() {
            mainMenuMusic.Stop(gameObject, 200, AkCurveInterpolation.AkCurveInterpolation_Exp1);
            introMusic.Post(gameObject);
            gameAmbience.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, AmbienceMusicCallback);

            GameManager.GetInstance().SetDifficulty(difficulties[currentDifficulty].difficulties);
            GameManager.GetInstance().SetGameDuration(currentDuration);

            GameManager.GetInstance().StartGame();

            _mainMenu.SetActive(false);
            _scoreUI.SetActive(true);

            _isGameRunning = true;

        }

        public void OnSettingButtonClicked() {
            _mainMenu.SetActive(false);
            _settingsMenu.SetActive(true);

            InitialiseSettings();
        }

        public void OnBackButtonClicked() {
            if (_isGameRunning) {
                PauseGame();

            } else {
                _mainMenu.SetActive(true);
                _settingsMenu.SetActive(false);
            }

        }

        public void OnExitButtonClicked() {
            Application.Quit();
        }
    }
}
