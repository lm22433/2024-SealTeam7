using System;
using System.Collections.Generic;
using System.Linq;
using Enemies;
using Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class MenuManager : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private Difficulty[] difficulties;
        [SerializeField] private int currentDifficulty;
        [SerializeField, Range(0f, 600f)] private int maxGameDuration = 600;
        [SerializeField] private int currentDuration = 180;
        [SerializeField] private bool endlessMode = false;
        [SerializeField] private bool sandboxMode = false;
        [SerializeField] private bool handTracking = true;

        [Header("UI References")]
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject settingsMenu;
        [SerializeField] private GameObject playerHUD;
        [SerializeField] private GameObject gameOverMenu;

        [Header("Settings Menu References")]
        [SerializeField] private GameObject gameSettings;
        [SerializeField] private GameObject audioSettings;
        [SerializeField] private GameObject debugSettings;
        [SerializeField] private Button gameSettingsButton;
        [SerializeField] private Button audioSettingsButton;
        [SerializeField] private Button debugSettingsButton;
        [SerializeField] private Slider durationSlider;
        [SerializeField] private TMP_Text durationSliderText;
        [SerializeField] private TMP_Dropdown difficultyDropdown;
        [SerializeField] private Button endlessModeButton;
        [SerializeField] private Button sandboxModeButton;
        [SerializeField] private Button handTrackingButton;
        
        [Header("Game Over Menu Settings")]
        [SerializeField] private TMP_Text gameOverTitleText;
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text enemiesKilledText;
        [SerializeField] private TMP_Text wavesClearedText;
        [SerializeField] private TMP_Text timeSurvivedText;
        [SerializeField] private TMP_Text damageTakenText;
        [SerializeField] private TMP_Text regularEnemiesKilledText;
        [SerializeField] private TMP_Text specialEnemiesKilledText;
        [SerializeField] private TMP_Text heavyEnemiesKilledText;
        [SerializeField] private TMP_Text rangedEnemiesKilledText;
        [SerializeField] private TMP_Text spawnerEnemiesKilledText;

        [Header("Sprites")] 
        [SerializeField] private Sprite defaultButtonSprite;
        [SerializeField] private Sprite highlightedButtonSprite;
        [SerializeField] private Sprite enabledButtonSprite;
        [SerializeField] private Sprite disabledButtonSprite;

        [Header("Music Settings")]
        [SerializeField] private AK.Wwise.Event mainMenuMusic;
        [SerializeField] private AK.Wwise.Event introMusic;
        [SerializeField] private AK.Wwise.Event gameAmbience;

        private static MenuManager _instance;

        private bool _paused;
        private bool _isGameRunning = false;
        private bool _isGracefulShutdown = false;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);

            mainMenu.SetActive(true);
            settingsMenu.SetActive(false);
            playerHUD.SetActive(false);
            gameOverMenu.SetActive(false);
        }

        private void Start()
        {
            mainMenuMusic.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, MainMenuMusicCallback);
        }

        private void OnApplicationQuit()
        {
            _isGracefulShutdown = true;

            mainMenuMusic.Stop(gameObject);
            introMusic.Stop(gameObject);
            gameAmbience.Stop(gameObject);
        }

        void MainMenuMusicCallback(object in_cookie, AkCallbackType in_type, object in_info)
        {
            if (!_isGameRunning && !_isGracefulShutdown)
            {
                mainMenuMusic.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, MainMenuMusicCallback);
            }
        }

        void AmbienceMusicCallback(object in_cookie, AkCallbackType in_type, object in_info)
        {
            if (_isGameRunning && !_isGracefulShutdown)
            {
                gameAmbience.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, AmbienceMusicCallback);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown("escape"))
            {
                PauseGame();
            }

            if (Input.GetKeyDown("r"))
            {
                GameManager.GetInstance().StartGame();
            }
        }

        private void PauseGame()
        {
            _paused = !_paused;
            GameManager.GetInstance().SetGameActive(!_paused);
            Time.timeScale = (_paused) ? 0 : 1;

            playerHUD.SetActive(!_paused);
            if (_paused)
            {
                OnSettingButtonClicked();
            }
            else
            {
                settingsMenu.SetActive(false);
            }
        }

        private void InitialiseSettings()
        {
            List<string> difficultyNames = difficulties.Select(diff => diff.difficultyName).ToList();

            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(difficultyNames);
            difficultyDropdown.value = currentDifficulty;
            difficultyDropdown.RefreshShownValue();

            difficultyDropdown.onValueChanged.AddListener(_ => { currentDifficulty = difficultyDropdown.value; });

            durationSlider.maxValue = maxGameDuration;
            durationSlider.value = currentDuration;
            durationSlider.onValueChanged.AddListener(_ =>
            {
                currentDuration = (int)durationSlider.value;

                var seconds = (currentDuration % 60 < 10) ? $"0{currentDuration % 60}" : $"{currentDuration % 60}";
                durationSliderText.SetText($"{currentDuration / 60}:{seconds}");
            });
        }

        private static EnemyCategory GetEnemyCategoryFromEnemyType(EnemyType enemyType) =>
            enemyType switch
            {
                EnemyType.Soldier => EnemyCategory.Regular,
                EnemyType.Parachutist => EnemyCategory.Special,
                EnemyType.FastSoldier => EnemyCategory.Special,
                EnemyType.SniperSoldier => EnemyCategory.Ranged,
                EnemyType.LmgSoldier => EnemyCategory.Regular,
                EnemyType.RpgSoldier => EnemyCategory.Regular,
                EnemyType.Tank => EnemyCategory.Heavy,
                EnemyType.KamikazePlane => EnemyCategory.Special,
                EnemyType.Helicopter => EnemyCategory.Heavy,
                EnemyType.Chinook => EnemyCategory.Spawner,
                EnemyType.Mech => EnemyCategory.Heavy,
                _ => throw new ArgumentOutOfRangeException(nameof(enemyType), enemyType, null)
            };

        // TODO: Add a way to get time survived
        public void TriggerGameOverMenu(bool died, int finalScore, int enemiesKilled, int wavesCleared, float timeSurvived, int damageTaken, Dictionary<EnemyType, int> detailedEnemiesKilled)
        {
            mainMenu.SetActive(false);
            settingsMenu.SetActive(false);
            playerHUD.SetActive(false);
            gameOverMenu.SetActive(true);
            
            gameOverTitleText.SetText(died ? "YOU DIED!" : "GAME OVER!");
            
            finalScoreText.SetText($"{finalScore}");
            enemiesKilledText.SetText($"{enemiesKilled}");
            wavesClearedText.SetText($"{wavesCleared}");
            int minutes = (int) timeSurvived / 60;
            int seconds = (int) timeSurvived % 60;
            string secondsStr = (seconds < 10) ? $"0{seconds}" : $"{seconds}";
            timeSurvivedText.SetText($"{minutes}:{secondsStr}");
            damageTakenText.SetText($"{damageTaken}");
            
            Dictionary<EnemyCategory, int> categoryDict = Enum.GetValues(typeof(EnemyCategory))
                .Cast<EnemyCategory>()
                .ToDictionary(category => category, _ => 0);

            foreach (var group in detailedEnemiesKilled
                         .GroupBy(kvp => GetEnemyCategoryFromEnemyType(kvp.Key)))
            {
                categoryDict[group.Key] = group.Sum(kvp => kvp.Value);
            }
            
            regularEnemiesKilledText.SetText($"{categoryDict[EnemyCategory.Regular]}");
            specialEnemiesKilledText.SetText($"{categoryDict[EnemyCategory.Special]}");
            heavyEnemiesKilledText.SetText($"{categoryDict[EnemyCategory.Heavy]}");
            rangedEnemiesKilledText.SetText($"{categoryDict[EnemyCategory.Ranged]}");
            spawnerEnemiesKilledText.SetText($"{categoryDict[EnemyCategory.Spawner]}");
        }

        public void OnPlayButtonClicked()
        {
            mainMenuMusic.Stop(gameObject, 200, AkCurveInterpolation.AkCurveInterpolation_Exp1);
            introMusic.Post(gameObject);
            gameAmbience.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, AmbienceMusicCallback);

            GameManager.GetInstance().SetDifficulty(difficulties[currentDifficulty]);
            GameManager.GetInstance().SetGameDuration(currentDuration);
            GameManager.GetInstance().SetEndlessMode(endlessMode);
            GameManager.GetInstance().SetSandboxMode(sandboxMode);
            GameManager.GetInstance().SetHandTracking(handTracking);

            GameManager.GetInstance().StartGame();

            mainMenu.SetActive(false);
            settingsMenu.SetActive(false);
            playerHUD.SetActive(true);
            gameOverMenu.SetActive(false);
            
            _isGameRunning = true;
        }

        public void OnSettingButtonClicked()
        {
            mainMenu.SetActive(false);
            settingsMenu.SetActive(true);
            OnGameSettingsButtonClicked();

            InitialiseSettings();
        }

        public void OnBackButtonClicked()
        {
            if (_isGameRunning)
            {
                PauseGame();
            }
            else
            {
                mainMenu.SetActive(true);
                settingsMenu.SetActive(false);
            }
        }

        public void OnGameSettingsButtonClicked()
        {
            gameSettings.SetActive(true);
            audioSettings.SetActive(false);
            debugSettings.SetActive(false);

            gameSettingsButton.image.sprite = highlightedButtonSprite;
            audioSettingsButton.image.sprite = defaultButtonSprite;
            debugSettingsButton.image.sprite = defaultButtonSprite;

            endlessModeButton.image.sprite = endlessMode ? enabledButtonSprite : disabledButtonSprite;
            sandboxModeButton.image.sprite = sandboxMode ? enabledButtonSprite : disabledButtonSprite;
            handTrackingButton.image.sprite = handTracking ? enabledButtonSprite : disabledButtonSprite;
        }

        public void OnAudioSettingsButtonClicked()
        {
            gameSettings.SetActive(false);
            audioSettings.SetActive(true);
            debugSettings.SetActive(false);

            gameSettingsButton.image.sprite = defaultButtonSprite;
            audioSettingsButton.image.sprite = highlightedButtonSprite;
            debugSettingsButton.image.sprite = defaultButtonSprite;
        }

        public void OnDebugSettingsButtonClicked()
        {
            gameSettings.SetActive(false);
            audioSettings.SetActive(false);
            debugSettings.SetActive(true);

            gameSettingsButton.image.sprite = defaultButtonSprite;
            audioSettingsButton.image.sprite = defaultButtonSprite;
            debugSettingsButton.image.sprite = highlightedButtonSprite;
        }

        public void OnEndlessModeButtonClicked()
        {
            endlessMode = !endlessMode;
            endlessModeButton.image.sprite = endlessMode ? enabledButtonSprite : disabledButtonSprite;
        }

        public void OnSandboxModeButtonClicked()
        {
            sandboxMode = !sandboxMode;
            sandboxModeButton.image.sprite = sandboxMode ? enabledButtonSprite : disabledButtonSprite;
        }

        public void OnHandTrackingButtonClicked()
        {
            handTracking = !handTracking;
            handTrackingButton.image.sprite = handTracking ? enabledButtonSprite : disabledButtonSprite;
        }

        public void OnExitButtonClicked() => Application.Quit();

        public static MenuManager GetInstance() => _instance;
    }
}
