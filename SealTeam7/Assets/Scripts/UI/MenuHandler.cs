using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
        [Header("Difficulty Settings")]
        [SerializeField] private DifficultyProfile[] difficulties;
        [SerializeField] private int currentDifficulty = 0;

        [Header("UI containers")]
        [SerializeField] private GameObject _settingsMenu;
        [SerializeField] private GameObject _mainMenu;
        [SerializeField] private GameObject _scoreUI;
        [SerializeField] private TMP_Dropdown _difficultyDropdown;

        private void Awake() {
            GameManager.GetInstance().GameActive = false;

            _mainMenu.SetActive(true);
            _settingsMenu.SetActive(false);
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
    }
}
