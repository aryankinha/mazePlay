using System;
using System.Collections;
using ArrowMaze.Core;
using ArrowMaze.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowMaze.UI
{
    public sealed class GameplayHUD : MonoBehaviour
    {
        private static readonly Color HeartFullColor = new Color32(255, 45, 75, 255);
        private static readonly Color HeartEmptyColor = new Color32(100, 115, 135, 255);

        [Header("Header Elements")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text carsRemainingText;
        [SerializeField] private TMP_Text difficultyText;
        [SerializeField] private Image[] heartIcons;
        [SerializeField] private Button backButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button restartButton;

        [Header("Bottom Controls")]
        [SerializeField] private Button hintButton;
        [SerializeField] private TMP_Text hintCountText;
        [SerializeField] private Button undoButton;

        [Header("Result Popup")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TMP_Text popupTitle;
        [SerializeField] private TMP_Text popupMessage;
        [SerializeField] private Button popupRestartButton;

        private LivesManager livesManager;
        private PathValidator pathValidator;
        private Action restartAction;
        private Action undoAction;
        private Action hintAction;

        private Coroutine[] heartFeedback;
        private Coroutine popupRevealRoutine;
        private CanvasGroup popupCanvasGroup;
        private RectTransform popupCard;
        private int displayedLives = -1;
        private int remainingHints = 2;

        public void Bind(
            LivesManager livesMgr,
            PathValidator validator,
            Action onRestart,
            Action onUndo = null,
            Action onHint = null,
            int levelNumber = 23)
        {
            Unbind();

            livesManager = livesMgr;
            pathValidator = validator;
            restartAction = onRestart;
            undoAction = onUndo;
            hintAction = onHint;

            if (titleText != null)
            {
                titleText.text = "Tap Away Cars";
                titleText.color = new Color32(30, 41, 59, 255);
            }

            if (levelText != null)
            {
                levelText.text = $"Level {levelNumber}";
                levelText.color = new Color32(71, 85, 105, 255);
            }

            if (difficultyText != null)
            {
                difficultyText.text = "Normal";
            }

            if (livesManager != null)
            {
                livesManager.OnLivesChanged += UpdateLives;
                UpdateLives(livesManager.CurrentLives);
            }

            if (pathValidator != null)
            {
                pathValidator.OnCorrectTap += _ => UpdateCarsCount();
                pathValidator.OnUndo += _ => UpdateCarsCount();
                UpdateCarsCount();
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartPressed);
            }

            if (popupRestartButton != null)
            {
                popupRestartButton.onClick.AddListener(RestartPressed);
            }

            if (undoButton != null)
            {
                undoButton.onClick.AddListener(UndoPressed);
            }

            if (hintButton != null)
            {
                hintButton.onClick.AddListener(HintPressed);
                UpdateHintCountUI();
            }

            HidePopup();
        }

        public void UpdateCarsCount()
        {
            if (carsRemainingText != null && pathValidator != null)
            {
                carsRemainingText.text = $"{pathValidator.RemainingCars}";
            }
        }

        public void ShowLevelComplete()
        {
            ShowPopup("Level Complete!", "All cars escaped smoothly.");
        }

        public void ShowGameOver()
        {
            ShowPopup("Out of Hearts", "Tap in the right order to avoid collisions.");
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void RestartPressed()
        {
            restartAction?.Invoke();
        }

        private void UndoPressed()
        {
            undoAction?.Invoke();
            UpdateCarsCount();
        }

        private void HintPressed()
        {
            if (remainingHints > 0)
            {
                remainingHints--;
                UpdateHintCountUI();
                hintAction?.Invoke();
            }
        }

        private void UpdateHintCountUI()
        {
            if (hintCountText != null)
            {
                hintCountText.text = $"{remainingHints}";
            }
        }

        private void UpdateLives(int currentLives)
        {
            if (heartIcons == null)
            {
                return;
            }

            if (heartFeedback == null || heartFeedback.Length != heartIcons.Length)
            {
                heartFeedback = new Coroutine[heartIcons.Length];
            }

            var lifeWasLost = displayedLives >= 0 && currentLives < displayedLives;

            for (var index = 0; index < heartIcons.Length; index++)
            {
                if (heartIcons[index] == null)
                {
                    continue;
                }

                var isFull = index < currentLives;
                var heartSprite = TileVisualFactory.GetHeartSprite(isFull);
                if (heartSprite != null)
                {
                    heartIcons[index].sprite = heartSprite;
                    heartIcons[index].color = Color.white;
                }
                else
                {
                    heartIcons[index].color = isFull ? HeartFullColor : HeartEmptyColor;
                }

                if (lifeWasLost && index == currentLives)
                {
                    if (heartFeedback[index] != null)
                    {
                        StopCoroutine(heartFeedback[index]);
                    }

                    heartFeedback[index] = StartCoroutine(HeartLossRoutine(heartIcons[index]));
                }
                else if (!lifeWasLost)
                {
                    heartIcons[index].rectTransform.localScale = Vector3.one;
                }
            }

            displayedLives = currentLives;
        }

        private void ShowPopup(string title, string message)
        {
            if (popupTitle != null)
            {
                popupTitle.text = title;
            }

            if (popupMessage != null)
            {
                popupMessage.text = message;
            }

            if (popupRoot != null)
            {
                popupRoot.SetActive(true);
                EnsurePopupAnimationTargets();
                if (popupRevealRoutine != null)
                {
                    StopCoroutine(popupRevealRoutine);
                }

                popupRevealRoutine = StartCoroutine(PopupRevealRoutine());
            }
        }

        private void HidePopup()
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }

            if (popupRevealRoutine != null)
            {
                StopCoroutine(popupRevealRoutine);
                popupRevealRoutine = null;
            }
        }

        private IEnumerator HeartLossRoutine(Image heart)
        {
            const float duration = 0.22f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var pulse = 1f + (Mathf.Sin(progress * Mathf.PI) * 0.28f);
                heart.rectTransform.localScale = Vector3.one * pulse;
                yield return null;
            }

            heart.rectTransform.localScale = Vector3.one;
        }

        private void EnsurePopupAnimationTargets()
        {
            if (popupCanvasGroup == null)
            {
                popupCanvasGroup = popupRoot.GetComponent<CanvasGroup>();
                if (popupCanvasGroup == null)
                {
                    popupCanvasGroup = popupRoot.AddComponent<CanvasGroup>();
                }
            }

            if (popupCard == null)
            {
                popupCard = popupRoot.transform.Find("Popup Card") as RectTransform;
            }
        }

        private IEnumerator PopupRevealRoutine()
        {
            const float duration = 0.24f;
            var elapsed = 0f;
            popupCanvasGroup.alpha = 0f;
            if (popupCard != null)
            {
                popupCard.localScale = Vector3.one * 0.82f;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                popupCanvasGroup.alpha = progress;
                if (popupCard != null)
                {
                    popupCard.localScale = Vector3.Lerp(Vector3.one * 0.82f, Vector3.one, progress);
                }

                yield return null;
            }

            popupCanvasGroup.alpha = 1f;
            if (popupCard != null)
            {
                popupCard.localScale = Vector3.one;
            }

            popupRevealRoutine = null;
        }

        private void Unbind()
        {
            if (livesManager != null)
            {
                livesManager.OnLivesChanged -= UpdateLives;
                livesManager = null;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartPressed);
            }

            if (popupRestartButton != null)
            {
                popupRestartButton.onClick.RemoveListener(RestartPressed);
            }

            if (undoButton != null)
            {
                undoButton.onClick.RemoveListener(UndoPressed);
            }

            if (hintButton != null)
            {
                hintButton.onClick.RemoveListener(HintPressed);
            }

            restartAction = null;
            undoAction = null;
            hintAction = null;
        }
    }
}
