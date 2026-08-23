using System;
using ArrowMaze.Core;
using ArrowMaze.Data;
using ArrowMaze.Meta;
using ArrowMaze.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArrowMaze.Gameplay
{
    public sealed class LevelController : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private LivesManager livesManager;
        [SerializeField] private GameplayHUD gameplayHud;
        [SerializeField] private bool useInspectorLevelForDevelopment;
        [SerializeField, Min(1)] private int inspectorLevelNumber = 23;

        private PathValidator pathValidator;
        private LevelDefinition currentDefinition;
        private int wrongTaps;
        private bool levelFinished;
        private bool awaitingFinalExit;
        private GridCoordinate finalCarCoordinate;
        private int pendingStars;

        public event Action OnLevelStart;
        public event Action OnLevelWin;
        public event Action OnLevelLose;

        private void Awake()
        {
            gridManager = gridManager != null ? gridManager : FindAnyObjectByType<GridManager>();
            livesManager = livesManager != null ? livesManager : FindAnyObjectByType<LivesManager>();
            gameplayHud = gameplayHud != null ? gameplayHud : FindAnyObjectByType<GameplayHUD>();

            if (gridManager == null || livesManager == null)
            {
                throw new InvalidOperationException("LevelController requires GridManager and LivesManager.");
            }
        }

        private void Start()
        {
            StartLevel();
        }

        private void OnDestroy()
        {
            UnwireSystems();
        }

        public void RestartLevel()
        {
            StartLevel();
        }

        public void ReturnToLevelMap()
        {
            SceneManager.LoadScene("LevelMap");
        }

        public void PlayNextLevel()
        {
            var next = Mathf.Min(currentDefinition.Id + 1, LevelCatalog.HighestCatalogLevel);
            LevelSession.SelectedLevel = next;
            SceneManager.LoadScene("Gameplay");
        }

        public void HandleUndo()
        {
            if (levelFinished || pathValidator == null)
            {
                return;
            }

            if (pathValidator.TryUndo(out var restoredCoord))
            {
                gridManager.RestoreCar(restoredCoord);
            }
        }

        public void HandleHint()
        {
            if (levelFinished || pathValidator == null)
            {
                return;
            }

            var hintCoord = pathValidator.GetHint();
            if (hintCoord.HasValue)
            {
                gridManager.ShowHint(hintCoord.Value);
            }
        }

        private void StartLevel()
        {
            UnwireSystems();
            levelFinished = false;
            awaitingFinalExit = false;
            pendingStars = 0;
            wrongTaps = 0;

            var selectedLevel = useInspectorLevelForDevelopment ? inspectorLevelNumber : LevelSession.SelectedLevel;
            currentDefinition = LevelCatalog.Get(selectedLevel);
            var level = currentDefinition.BuildLevel();
            pathValidator = new PathValidator(level);

            gridManager.TileTapped += RegisterTap;
            gridManager.CarExitAnimationCompleted += HandleCarExitAnimationCompleted;
            pathValidator.OnCorrectTap += HandleCorrectTap;
            pathValidator.OnIncorrectTap += HandleIncorrectTap;
            pathValidator.OnLevelCompleted += HandleLevelWin;
            livesManager.OnGameOver += HandleLevelLose;

            livesManager.ResetLives();
            gameplayHud?.Bind(livesManager, pathValidator, RestartLevel, HandleUndo, HandleHint, currentDefinition.Id, currentDefinition.Difficulty, PlayNextLevel, ReturnToLevelMap);
            gridManager.BuildLevel(level);
            gridManager.SetInputEnabled(true);

            PlayerProgress.SetLastPlayed(currentDefinition.Id);
            Debug.Log($"Tap Away Cars started: Level {currentDefinition.Id}, {level.Rows}x{level.Columns}, seed {currentDefinition.Seed}, cars {pathValidator.TotalCars}.");
            OnLevelStart?.Invoke();
        }

        private void RegisterTap(GridCoordinate coordinate)
        {
            if (pathValidator == null)
            {
                return;
            }

            pathValidator.RegisterTap(coordinate);
        }

        private void HandleIncorrectTap(GridCoordinate coordinate)
        {
            wrongTaps++;
            gridManager.PlayWrongTapFeedback(coordinate);
            GameFeedback.PlayBlocked();
#if UNITY_IOS || UNITY_ANDROID
            if (PlayerProgress.HapticsEnabled)
            {
                Handheld.Vibrate();
            }
#endif
            livesManager.LoseLife();
        }

        private void HandleCorrectTap(GridCoordinate coordinate)
        {
            finalCarCoordinate = coordinate;
            GameFeedback.PlayCarMove();
            gridManager.PlayClearAnimation(coordinate);
        }

        private void HandleLevelWin()
        {
            if (levelFinished)
            {
                return;
            }

            levelFinished = true;
            awaitingFinalExit = true;
            gridManager.SetInputEnabled(false);
            pendingStars = wrongTaps == 0 ? 3 : wrongTaps <= 2 ? 2 : 1;
            // Persist the deterministic solve immediately; the presentation event waits
            // for the specific final car to finish its viewport-aware departure.
            PlayerProgress.CompleteLevel(currentDefinition.Id, pendingStars);
        }

        private void HandleCarExitAnimationCompleted(GridCoordinate coordinate)
        {
            if (!awaitingFinalExit || coordinate != finalCarCoordinate)
            {
                GameFeedback.PlayExit();
                return;
            }

            awaitingFinalExit = false;
            GameFeedback.PlaySuccess();
            gameplayHud?.ShowLevelComplete(pendingStars, currentDefinition.Id < LevelCatalog.HighestCatalogLevel);
            Debug.Log("Tap Away Cars level complete!");
            OnLevelWin?.Invoke();
        }

        private void HandleLevelLose()
        {
            if (levelFinished)
            {
                return;
            }

            levelFinished = true;
            gridManager.SetInputEnabled(false);
            gameplayHud?.ShowGameOver();
            Debug.Log("Tap Away Cars game over.");
            OnLevelLose?.Invoke();
        }

        private void UnwireSystems()
        {
            if (gridManager != null)
            {
                gridManager.TileTapped -= RegisterTap;
                gridManager.CarExitAnimationCompleted -= HandleCarExitAnimationCompleted;
            }

            if (livesManager != null)
            {
                livesManager.OnGameOver -= HandleLevelLose;
            }

            if (pathValidator == null)
            {
                return;
            }

            pathValidator.OnCorrectTap -= HandleCorrectTap;
            pathValidator.OnIncorrectTap -= HandleIncorrectTap;
            pathValidator.OnLevelCompleted -= HandleLevelWin;
            pathValidator = null;
        }
    }
}
