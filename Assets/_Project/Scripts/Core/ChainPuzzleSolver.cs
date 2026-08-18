using System;
using System.Collections.Generic;

namespace ArrowMaze.Core
{
    public sealed class ChainPuzzleSolveResult
    {
        internal ChainPuzzleSolveResult(bool isSolved, IReadOnlyList<GridCoordinate> clearOrder, int statesExplored, bool hitSearchLimit)
        {
            IsSolved = isSolved;
            ClearOrder = clearOrder;
            StatesExplored = statesExplored;
            HitSearchLimit = hitSearchLimit;
        }

        public bool IsSolved { get; }
        public IReadOnlyList<GridCoordinate> ClearOrder { get; }
        public int StatesExplored { get; }
        public bool HitSearchLimit { get; }
    }

    /// <summary>
    /// Independent backtracking search used as generation's ground-truth
    /// acceptance gate. It never accepts a board when the search limit is hit.
    /// </summary>
    public static class ChainPuzzleSolver
    {
        public static ChainPuzzleSolveResult TrySolve(MazeLevel level, int maxStates = 250000)
        {
            return TrySolve(level, Array.Empty<GridCoordinate>(), maxStates);
        }

        public static ChainPuzzleSolveResult TrySolve(
            MazeLevel level,
            IReadOnlyList<GridCoordinate> requiredPrefix,
            int maxStates = 250000)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (maxStates < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStates));
            }

            var cleared = level.CreateInitialClearedState();
            var clearOrder = new List<GridCoordinate>(level.CarCount);
            if (requiredPrefix == null || !MazeGenerator.IsLegalTapSequence(level, requiredPrefix))
            {
                return new ChainPuzzleSolveResult(false, clearOrder.AsReadOnly(), 0, false);
            }

            for (var index = 0; index < requiredPrefix.Count; index++)
            {
                var coordinate = requiredPrefix[index];
                cleared[coordinate.Row, coordinate.Column] = true;
                clearOrder.Add(coordinate);
            }

            var deadStates = new HashSet<string>();
            var statesExplored = 0;
            var hitSearchLimit = false;
            var isSolved = Search(
                level,
                cleared,
                clearOrder.Count,
                maxStates,
                clearOrder,
                deadStates,
                ref statesExplored,
                ref hitSearchLimit);
            return new ChainPuzzleSolveResult(isSolved, clearOrder.AsReadOnly(), statesExplored, hitSearchLimit);
        }

        private static bool Search(
            MazeLevel level,
            bool[,] cleared,
            int clearedCount,
            int maxStates,
            List<GridCoordinate> clearOrder,
            HashSet<string> deadStates,
            ref int statesExplored,
            ref bool hitSearchLimit)
        {
            if (clearedCount == level.CarCount)
            {
                return true;
            }

            if (statesExplored++ >= maxStates)
            {
                hitSearchLimit = true;
                return false;
            }

            var stateKey = BuildStateKey(level, cleared);
            if (deadStates.Contains(stateKey))
            {
                return false;
            }

            var legalTaps = StraightLineLegality.GetLegalTaps(level, cleared);
            for (var index = 0; index < legalTaps.Count; index++)
            {
                var coordinate = legalTaps[index];
                cleared[coordinate.Row, coordinate.Column] = true;
                clearOrder.Add(coordinate);

                if (Search(
                        level,
                        cleared,
                        clearedCount + 1,
                        maxStates,
                        clearOrder,
                        deadStates,
                        ref statesExplored,
                        ref hitSearchLimit))
                {
                    return true;
                }

                clearOrder.RemoveAt(clearOrder.Count - 1);
                cleared[coordinate.Row, coordinate.Column] = false;
                if (hitSearchLimit)
                {
                    return false;
                }
            }

            deadStates.Add(stateKey);
            return false;
        }

        private static string BuildStateKey(MazeLevel level, bool[,] cleared)
        {
            var chars = new char[level.Rows * level.Columns];
            var index = 0;
            for (var row = 0; row < level.Rows; row++)
            {
                for (var column = 0; column < level.Columns; column++)
                {
                    chars[index++] = cleared[row, column] ? '1' : '0';
                }
            }

            return new string(chars);
        }
    }
}
