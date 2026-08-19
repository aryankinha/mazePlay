using System.Collections;
using System.Collections.Generic;
using ArrowMaze.Data;
using ArrowMaze.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArrowMaze.UI
{
    public sealed class LevelMapController : MonoBehaviour
    {
        [Header("Header Navigation")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button settingsButton;

        [Header("Scroll Map")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform mapContent;
        [SerializeField] private List<LevelNode> levelNodes = new List<LevelNode>();

        [Header("Settings Modal")]
        [SerializeField] private SettingsModal settingsModal;

        private void Awake()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(HandleBack);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(HandleSettings);
            }

            if (settingsModal != null)
            {
                settingsModal.OnProgressReset += RefreshMap;
            }
        }

        private void Start()
        {
            RefreshMap();
            StartCoroutine(ScrollToCurrentLevelRoutine());
        }

        private void OnDestroy()
        {
            if (settingsModal != null)
            {
                settingsModal.OnProgressReset -= RefreshMap;
            }
        }

        public void RegisterNode(LevelNode node)
        {
            if (node != null && !levelNodes.Contains(node))
            {
                levelNodes.Add(node);
            }
        }

        public void RefreshMap()
        {
            var currentLevel = PlayerProgress.GetContinueLevel();
            for (var i = 0; i < levelNodes.Count; i++)
            {
                var levelId = i + 1;
                if (levelNodes[i] != null)
                {
                    levelNodes[i].Setup(levelId, currentLevel, OpenLevel);
                }
            }
        }

        private void OpenLevel(int levelId)
        {
            LevelSession.SelectedLevel = levelId;
            SceneManager.LoadScene("Gameplay");
        }

        private void HandleBack()
        {
            SceneManager.LoadScene("MainMenu");
        }

        private void HandleSettings()
        {
            if (settingsModal != null)
            {
                settingsModal.Show();
            }
        }

        private IEnumerator ScrollToCurrentLevelRoutine()
        {
            yield return null; // Wait for layout pass

            if (scrollRect == null || levelNodes.Count == 0)
            {
                yield break;
            }

            var currentLevel = PlayerProgress.GetContinueLevel();
            // Levels are laid out from Level 1 at bottom (or top) to 23
            // Normalized position: 0 is bottom, 1 is top
            var targetNormalized = Mathf.Clamp01((float)(currentLevel - 1) / Mathf.Max(LevelCatalog.HighestCatalogLevel - 1, 1));
            
            // If Level 1 is at bottom (y near 0) and Level 23 is at top (y near max),
            // normalized 0 is bottom (Level 1), 1 is top (Level 23).
            // Let's smoothly scroll towards target
            var start = scrollRect.verticalNormalizedPosition;
            var elapsed = 0f;
            var duration = 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                scrollRect.verticalNormalizedPosition = Mathf.Lerp(start, targetNormalized, elapsed / duration);
                yield return null;
            }

            scrollRect.verticalNormalizedPosition = targetNormalized;
        }
    }
}
