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
            wrongTaps = 0;

            var selectedLevel = useInspectorLevelForDevelopment ? inspectorLevelNumber : LevelSession.SelectedLevel;
            currentDefinition = LevelCatalog.Get(selectedLevel);
            var level = currentDefinition.BuildLevel();
            pathValidator = new PathValidator(level);

            gridManager.TileTapped += RegisterTap;
            pathValidator.OnCorrectTap += gridManager.PlayClearAnimation;
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
            livesManager.LoseLife();
        }

        private void HandleLevelWin()
        {
            if (levelFinished)
            {
                return;
            }

            levelFinished = true;
            gridManager.SetInputEnabled(false);
            var stars = wrongTaps == 0 ? 3 : wrongTaps <= 2 ? 2 : 1;
            PlayerProgress.CompleteLevel(currentDefinition.Id, stars);
            gameplayHud?.ShowLevelComplete(stars, currentDefinition.Id < LevelCatalog.HighestCatalogLevel);
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
            }

            if (livesManager != null)
            {
                livesManager.OnGameOver -= HandleLevelLose;
            }

            if (pathValidator == null)
            {
                return;
            }

            pathValidator.OnCorrectTap -= gridManager.PlayClearAnimation;
            pathValidator.OnIncorrectTap -= HandleIncorrectTap;
            pathValidator.OnLevelCompleted -= HandleLevelWin;
            pathValidator = null;
        }
    }
}
