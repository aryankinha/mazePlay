using ArrowMaze.Data;
using ArrowMaze.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArrowMaze.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Action Buttons")]
        [SerializeField] private Button playContinueButton;
        [SerializeField] private TMP_Text playContinueLabel;
        [SerializeField] private TMP_Text playContinueSubtext;
        [SerializeField] private Button levelMapButton;
        [SerializeField] private Button settingsButton;

        [Header("Active Quest Card")]
        [SerializeField] private TMP_Text questCardLevelText;
        [SerializeField] private TMP_Text questCardStarsText;
        [SerializeField] private Image questCardProgressBarFill;

        [Header("Bottom Progress Bar")]
        [SerializeField] private TMP_Text progressLevelText;
        [SerializeField] private TMP_Text progressStarsText;
        [SerializeField] private Image progressBarFill;

        [Header("Hero Showcase Animation")]
        [SerializeField] private RectTransform[] heroCars;

        [Header("Settings Modal")]
        [SerializeField] private SettingsModal settingsModal;

        private Vector2[] heroCarBasePositions;
        private AudioClip clickSfx;

        private void Awake()
        {
            if (settingsModal != null)
            {
                settingsModal.OnProgressReset += RefreshUI;
            }

            clickSfx = Resources.Load<AudioClip>("Audio/Feedback/click_002");

            if (heroCars != null && heroCars.Length > 0)
            {
                heroCarBasePositions = new Vector2[heroCars.Length];
                for (var i = 0; i < heroCars.Length; i++)
                {
                    if (heroCars[i] != null)
                    {
                        heroCarBasePositions[i] = heroCars[i].anchoredPosition;
                    }
                }
            }
        }

        private void Start()
        {
            RefreshUI();
        }

        private void Update()
        {
            if (heroCars == null || heroCarBasePositions == null)
            {
                return;
            }

            var time = Time.time;
            for (var i = 0; i < heroCars.Length; i++)
            {
                if (heroCars[i] != null && i < heroCarBasePositions.Length)
                {
                    var offset = Mathf.Sin(time * 2.0f + i * 1.2f) * 2.5f;
                    heroCars[i].anchoredPosition = heroCarBasePositions[i] + new Vector2(0f, offset);
                }
            }
        }

        private void OnDestroy()
        {
            if (settingsModal != null)
            {
                settingsModal.OnProgressReset -= RefreshUI;
            }
        }

        public void RefreshUI()
        {
            var continueLevel = PlayerProgress.GetContinueLevel();
            var totalStars = PlayerProgress.GetTotalStarsEarned();
            var completedCount = PlayerProgress.GetCompletedLevelsCount();

            var isNewPlayer = completedCount == 0 && continueLevel == 1;

            if (playContinueLabel != null)
            {
                playContinueLabel.text = isNewPlayer ? "PLAY" : "CONTINUE";
                var labelPosition = playContinueLabel.rectTransform.anchoredPosition;
                labelPosition.y = isNewPlayer ? 0f : 17f;
                playContinueLabel.rectTransform.anchoredPosition = labelPosition;
            }

            if (playContinueSubtext != null)
            {
                playContinueSubtext.gameObject.SetActive(!isNewPlayer);
                if (!isNewPlayer)
                {
                    playContinueSubtext.text = $"LEVEL {continueLevel}";
                }
            }

            // Quest Card
            if (questCardLevelText != null)
            {
                questCardLevelText.text = $"Level {continueLevel}";
            }

            if (questCardStarsText != null)
            {
                questCardStarsText.text = $"{totalStars} / {LevelCatalog.HighestCatalogLevel * 3} STARS";
            }

            if (questCardProgressBarFill != null)
            {
                var fill = Mathf.Clamp01((float)completedCount / LevelCatalog.HighestCatalogLevel);
                questCardProgressBarFill.fillAmount = fill;
                var rt = questCardProgressBarFill.rectTransform;
                if (rt != null)
                {
                    rt.anchorMax = new Vector2(fill, 1f);
                }
            }

            // Bottom Progress
            if (progressLevelText != null)
            {
                progressLevelText.text = $"LEVEL {continueLevel} OF {LevelCatalog.HighestCatalogLevel}";
            }

            if (progressStarsText != null)
            {
                progressStarsText.text = $"{totalStars} / {LevelCatalog.HighestCatalogLevel * 3} STARS";
            }

            if (progressBarFill != null)
            {
                var fill = Mathf.Clamp01((float)completedCount / LevelCatalog.HighestCatalogLevel);
                progressBarFill.fillAmount = fill;
                var rt = progressBarFill.rectTransform;
                if (rt != null)
                {
                    rt.anchorMax = new Vector2(fill, 1f);
                }
            }
        }

        public void PlayContinue()
        {
            PlayClickSound();
            var continueLevel = PlayerProgress.GetContinueLevel();
            LevelSession.SelectedLevel = continueLevel;
            SceneManager.LoadScene("Gameplay");
        }

        public void OpenLevelMap()
        {
            PlayClickSound();
            SceneManager.LoadScene("LevelMap");
        }

        public void OpenSettings()
        {
            PlayClickSound();
            if (settingsModal != null)
            {
                settingsModal.Show();
            }
        }

        private void PlayClickSound()
        {
            if (clickSfx != null && PlayerProgress.SoundEffectsEnabled)
            {
                AudioSource.PlayClipAtPoint(clickSfx, Vector3.zero);
            }
        }
    }
}
