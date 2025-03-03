using System;
using System.Collections.Generic;
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
        [SerializeField] private int currentDifficulty = 0;
        [SerializeField, Range(0f, 600f)] private int maxGameDuration = 600;
        [SerializeField] private int currentDuration = 180;

        [Header("UI containers")]
        [SerializeField] private GameObject _settingsMenu;
        [SerializeField] private GameObject _mainMenu;
        [SerializeField] private GameObject _scoreUI;
        [SerializeField] private TMP_Dropdown _difficultyDropdown;
        [SerializeField] private Slider _durationSlider;
        [SerializeField] private TMP_Text _durationSliderText;

        private bool pauseGame = false;

        private void Awake() {

            _mainMenu.SetActive(true);
            _settingsMenu.SetActive(false);
        }

        void Update()
        {
            if (Input.GetKeyDown("escape")) {
                pauseGame = !pauseGame;
                GameManager.GetInstance().GameActive = !pauseGame;
                Time.timeScale = (pauseGame) ? 0 : 1;
                
                if (pauseGame) {
                    onSettingButtonClicked();
                } else{
                    _settingsMenu.SetActive(false);
                }

            }

            if (Input.GetKeyDown("r")) {
                GameManager.GetInstance().StartGame();
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
                GameManager.GetInstance().SetDifficulty(difficulties[currentDifficulty].difficulties);
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

        public void onPlayButtonClicked() {
            GameManager.GetInstance().StartGame();

            _mainMenu.SetActive(false);
            _scoreUI.SetActive(true);

        }

        public void onSettingButtonClicked() {
            _mainMenu.SetActive(false);
            _settingsMenu.SetActive(true);

            InitialiseSettings();
        }

        public void offSettingButtonClicked() {
            _mainMenu.SetActive(true);
            _settingsMenu.SetActive(false);

        }

        public void onExitButtonClicked() {
            Application.Quit();
        }
    }
}
