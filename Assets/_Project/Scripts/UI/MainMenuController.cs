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

        [Header("Progress Summary")]
        [SerializeField] private TMP_Text progressLevelText;
        [SerializeField] private TMP_Text progressStarsText;
        [SerializeField] private Image progressBarFill;

        [Header("Settings Modal")]
        [SerializeField] private SettingsModal settingsModal;

        private void Awake()
        {
            if (settingsModal != null)
            {
                settingsModal.OnProgressReset += RefreshUI;
            }
        }

        private void Start()
        {
            RefreshUI();
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
                labelPosition.y = isNewPlayer ? 0f : 19f;
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
            var continueLevel = PlayerProgress.GetContinueLevel();
            LevelSession.SelectedLevel = continueLevel;
            SceneManager.LoadScene("Gameplay");
        }

        public void OpenLevelMap()
        {
            SceneManager.LoadScene("LevelMap");
        }

        public void OpenSettings()
        {
            if (settingsModal != null)
            {
                settingsModal.Show();
            }
        }
    }
}
