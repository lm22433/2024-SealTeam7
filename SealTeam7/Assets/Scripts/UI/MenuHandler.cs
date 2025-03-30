using System.Collections.Generic;
using System.Linq;
using Game;
using Map;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    public class MenuHandler : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private Difficulty[] difficulties;
        [SerializeField] private int currentDifficulty;
        [SerializeField, Range(0f, 600f)] private int maxGameDuration = 600;
        [SerializeField] private int currentDuration = 180;

        [Header("UI Containers")]
        [SerializeField] private GameObject settingsMenu;
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject scoreUI;
        [SerializeField] private TMP_Dropdown difficultyDropdown;
        [SerializeField] private Slider durationSlider;
        [SerializeField] private TMP_Text durationSliderText;

        [Header("Music Settings")]
        // [SerializeField] private AK.Wwise.Event mainMenuMusic;
        // [SerializeField] private AK.Wwise.Event introMusic;
        // [SerializeField] private AK.Wwise.Event gameAmbience;

        private bool _paused;
        private bool _isGameRunning = false;
        private bool _isGracefulShutdown = false;

        private void Awake() {
            mainMenu.SetActive(true);
            settingsMenu.SetActive(false);
        }

        private void Start() {
            // mainMenuMusic.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, MainMenuMusicCallback);
        }

        private void OnApplicationQuit() {
            _isGracefulShutdown = true;

            // mainMenuMusic.Stop(gameObject);
            // introMusic.Stop(gameObject);
            // gameAmbience.Stop(gameObject);
        }

        // void MainMenuMusicCallback(object in_cookie, AkCallbackType in_type, object in_info){
        //     if (!_isGameRunning && !_isGracefulShutdown) {
        //         mainMenuMusic.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, MainMenuMusicCallback);
        //     }
        // }
        //
        // void AmbienceMusicCallback(object in_cookie, AkCallbackType in_type, object in_info){
        //     if (_isGameRunning && !_isGracefulShutdown) {
        //         gameAmbience.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, AmbienceMusicCallback);
        //     }
        // }

        void Update()
        {
            if (Input.GetKeyDown("escape")) {
                PauseGame();
            }

            if (Input.GetKeyDown("r")) {
                MapManager.GetInstance().Quit();
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        private void PauseGame() {
            _paused = !_paused;
            GameManager.GetInstance().GameActive = !_paused;
            Time.timeScale = (_paused) ? 0 : 1;
            
            scoreUI.SetActive(!_paused);
            if (_paused) {
                OnSettingButtonClicked();
            } else{
                settingsMenu.SetActive(false);
            }
                
        }
        
        private void InitialiseSettings() {
            List<string> difficultyNames = difficulties.Select(diff => diff.difficultyName).ToList();

            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(difficultyNames);
            difficultyDropdown.value = currentDifficulty;
            difficultyDropdown.RefreshShownValue();

            difficultyDropdown.onValueChanged.AddListener(_ =>
            {
                currentDifficulty = difficultyDropdown.value;
            });

            durationSlider.maxValue = maxGameDuration;
            durationSlider.value = currentDuration;
            durationSlider.onValueChanged.AddListener(_ =>
            {
                currentDuration = (int) durationSlider.value;

                var seconds = (currentDuration % 60 < 10) ? $"0{currentDuration % 60}" : $"{currentDuration % 60}";
                durationSliderText.SetText($"{currentDuration / 60}:{seconds}");
                
                GameManager.GetInstance().SetGameDuration(currentDuration);
            });

        }

        public void OnPlayButtonClicked() {
            // mainMenuMusic.Stop(gameObject, 200, AkCurveInterpolation.AkCurveInterpolation_Exp1);
            // introMusic.Post(gameObject);
            // gameAmbience.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, AmbienceMusicCallback);

            GameManager.GetInstance().SetDifficulty(difficulties[currentDifficulty]);
            GameManager.GetInstance().SetGameDuration(currentDuration);

            GameManager.GetInstance().StartGame();

            mainMenu.SetActive(false);
            scoreUI.SetActive(true);

            _isGameRunning = true;

        }

        public void OnSettingButtonClicked() {
            mainMenu.SetActive(false);
            settingsMenu.SetActive(true);

            InitialiseSettings();
        }

        public void OnBackButtonClicked() {
            if (_isGameRunning) {
                PauseGame();

            } else {
                mainMenu.SetActive(true);
                settingsMenu.SetActive(false);
            }

        }

        public void OnExitButtonClicked() {
            Application.Quit();
        }
    }
}
