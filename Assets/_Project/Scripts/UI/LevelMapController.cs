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
        [SerializeField] private Button footerBackButton;
        [SerializeField] private Button settingsButton;

        [Header("Scroll Map")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform mapContent;
        [SerializeField] private List<LevelNode> levelNodes = new List<LevelNode>();

        [Header("Settings Modal")]
        [SerializeField] private SettingsModal settingsModal;

        private void Awake()
        {
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
                    levelNodes[i].Setup(levelId, currentLevel);
                }
            }
        }

        public void SelectLevel(int levelId)
        {
            if (!PlayerProgress.IsUnlocked(levelId))
            {
                return;
            }

            LevelSession.SelectedLevel = levelId;
            SceneManager.LoadScene("Gameplay");
        }

        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void OpenSettings()
        {
            if (settingsModal != null)
            {
                settingsModal.Show();
            }
        }

        private IEnumerator ScrollToCurrentLevelRoutine()
        {
            yield return null; // Wait for layout pass

            FocusCurrentLevel();
            yield return null; // Let ScrollRect apply its content position once.
            FocusCurrentLevel();
        }

        /// <summary>Centers the active progression node in the map viewport.</summary>
        public void FocusCurrentLevel()
        {
            if (scrollRect == null || levelNodes.Count == 0)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            var currentLevel = Mathf.Clamp(PlayerProgress.GetContinueLevel(), 1, levelNodes.Count);
            var currentNode = levelNodes[currentLevel - 1];
            var currentRect = currentNode != null ? currentNode.GetComponent<RectTransform>() : null;

            if (currentRect == null || mapContent == null || scrollRect.viewport == null)
            {
                return;
            }

            // The content is bottom-anchored, as is ScrollRect normalized position 0.
            // Center the actual node coordinate rather than estimating from the catalog range;
            // that keeps the current node visible if node spacing changes later.
            var scrollableHeight = Mathf.Max(1f, mapContent.rect.height - scrollRect.viewport.rect.height);
            var desiredBottom = currentRect.anchoredPosition.y - (scrollRect.viewport.rect.height * .5f);
            var targetNormalized = Mathf.Clamp01(desiredBottom / scrollableHeight);

            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = targetNormalized;
        }
    }
}
