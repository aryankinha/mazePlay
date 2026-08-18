using System;
using ArrowMaze.Core;
using ArrowMaze.UI;
using UnityEngine;

namespace ArrowMaze.Gameplay
{
    public sealed class LevelController : MonoBehaviour
    {
        private const int FixedRows = 6;
        private const int FixedColumns = 8;
        private const int FixedSeed = 260816;

        [SerializeField] private GridManager gridManager;
        [SerializeField] private LivesManager livesManager;
        [SerializeField] private GameplayHUD gameplayHud;
        [SerializeField, Range(0f, 1f)] private float trapDensity = 0.15f;
        [SerializeField, Min(1)] private int targetStartingBranchingFactor = 2;
        [SerializeField, Range(0.25f, 1f)] private float carDensity = 0.45f;
        [SerializeField] private int currentLevelNumber = 23;

        private PathValidator pathValidator;
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

            var settings = new MazeGenerationSettings(
                FixedRows,
                FixedColumns,
                FixedSeed,
                trapDensity,
                targetStartingBranchingFactor,
                carDensity: carDensity);

            var level = MazeGenerator.Generate(settings);
            pathValidator = new PathValidator(level);

            gridManager.TileTapped += RegisterTap;
            pathValidator.OnCorrectTap += gridManager.PlayClearAnimation;
            pathValidator.OnIncorrectTap += HandleIncorrectTap;
            pathValidator.OnLevelCompleted += HandleLevelWin;
            livesManager.OnGameOver += HandleLevelLose;

            livesManager.ResetLives();
            gameplayHud?.Bind(livesManager, pathValidator, RestartLevel, HandleUndo, HandleHint, currentLevelNumber);
            gridManager.BuildLevel(level);
            gridManager.SetInputEnabled(true);

            Debug.Log($"Tap Away Cars started: {FixedRows}x{FixedColumns}, seed {FixedSeed}, cars {pathValidator.TotalCars}.");
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
            gameplayHud?.ShowLevelComplete();
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
