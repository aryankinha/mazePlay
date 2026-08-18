using System;
using System.Collections.Generic;

namespace ArrowMaze.Core
{
    /// <summary>
    /// Owns the live tap sequence, car tracking, undo history, and hinting for a level.
    /// </summary>
    public sealed class PathValidator
    {
        private readonly MazeLevel level;
        private readonly bool[,] cleared;
        private readonly Stack<GridCoordinate> tapHistory = new Stack<GridCoordinate>();
        private readonly int totalCars;
        private int clearedCount;

        public PathValidator(MazeLevel level)
        {
            this.level = level ?? throw new ArgumentNullException(nameof(level));
            cleared = level.CreateInitialClearedState();
            totalCars = level.CarCount;
        }

        public event Action<GridCoordinate> OnCorrectTap;
        public event Action<GridCoordinate> OnIncorrectTap;
        public event Action<GridCoordinate> OnUndo;
        public event Action OnLevelCompleted;

        public int TotalCars => totalCars;
        public int ClearedCount => clearedCount;
        public int RemainingCars => Math.Max(0, totalCars - clearedCount);
        public bool IsLevelComplete { get; private set; }

        public bool IsCleared(GridCoordinate coordinate)
        {
            return level.IsInBounds(coordinate) && cleared[coordinate.Row, coordinate.Column];
        }

        public bool IsLegalTap(GridCoordinate coordinate)
        {
            if (IsLevelComplete || !level.IsInBounds(coordinate) || !level.HasCar(coordinate))
            {
                return false;
            }

            return StraightLineLegality.IsLegalTap(level, cleared, coordinate);
        }

        public bool RegisterTap(GridCoordinate coordinate)
        {
            if (IsLevelComplete || !level.IsInBounds(coordinate) || !level.HasCar(coordinate))
            {
                return false;
            }

            if (!IsLegalTap(coordinate))
            {
                OnIncorrectTap?.Invoke(coordinate);
                return false;
            }

            cleared[coordinate.Row, coordinate.Column] = true;
            clearedCount++;
            tapHistory.Push(coordinate);

            OnCorrectTap?.Invoke(coordinate);

            if (clearedCount >= totalCars)
            {
                IsLevelComplete = true;
                OnLevelCompleted?.Invoke();
            }

            return true;
        }

        public bool TryUndo(out GridCoordinate restoredCoordinate)
        {
            if (tapHistory.Count == 0)
            {
                restoredCoordinate = default;
                return false;
            }

            restoredCoordinate = tapHistory.Pop();
            cleared[restoredCoordinate.Row, restoredCoordinate.Column] = false;
            clearedCount = Math.Max(0, clearedCount - 1);
            IsLevelComplete = false;

            OnUndo?.Invoke(restoredCoordinate);
            return true;
        }

        public GridCoordinate? GetHint()
        {
            if (IsLevelComplete)
            {
                return null;
            }

            var legalTaps = StraightLineLegality.GetLegalTaps(level, cleared);
            foreach (var candidate in legalTaps)
            {
                if (level.HasCar(candidate) && !cleared[candidate.Row, candidate.Column])
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
