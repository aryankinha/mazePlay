using System;
using System.Collections.Generic;

namespace ArrowMaze.Core
{
    /// <summary>
    /// Pure rule implementation for the board. A tile travels straight in its
    /// own direction; active tiles block it and cleared cells are empty space.
    /// </summary>
    public static class StraightLineLegality
    {
        public static bool IsLegalTap(MazeLevel level, bool[,] cleared, GridCoordinate coordinate)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (cleared == null || cleared.GetLength(0) != level.Rows || cleared.GetLength(1) != level.Columns)
            {
                throw new ArgumentException("Cleared-state dimensions must match the level.", nameof(cleared));
            }

            if (!level.IsInBounds(coordinate) || cleared[coordinate.Row, coordinate.Column])
            {
                return false;
            }

            var direction = level.GetDirection(coordinate);
            var current = coordinate;
            while (true)
            {
                current = Move(current, direction);
                if (!level.IsInBounds(current))
                {
                    // The same topology used for rendering owns legal exits. A car
                    // cannot leave through a border where no physical gate exists.
                    return level.GetRoadTopology().HasExitGate(
                        Move(current, Opposite(direction)),
                        direction);
                }

                if (!cleared[current.Row, current.Column])
                {
                    return false;
                }
            }
        }

        public static IReadOnlyList<GridCoordinate> GetLegalTaps(MazeLevel level, bool[,] cleared)
        {
            var legalTaps = new List<GridCoordinate>();
            for (var row = 0; row < level.Rows; row++)
            {
                for (var column = 0; column < level.Columns; column++)
                {
                    var coordinate = new GridCoordinate(row, column);
                    if (IsLegalTap(level, cleared, coordinate))
                    {
                        legalTaps.Add(coordinate);
                    }
                }
            }

            return legalTaps;
        }

        public static GridCoordinate Move(GridCoordinate origin, ArrowDirection direction)
        {
            switch (direction)
            {
                case ArrowDirection.Up:
                    return new GridCoordinate(origin.Row - 1, origin.Column);
                case ArrowDirection.Right:
                    return new GridCoordinate(origin.Row, origin.Column + 1);
                case ArrowDirection.Down:
                    return new GridCoordinate(origin.Row + 1, origin.Column);
                case ArrowDirection.Left:
                    return new GridCoordinate(origin.Row, origin.Column - 1);
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        private static ArrowDirection Opposite(ArrowDirection direction)
        {
            switch (direction)
            {
                case ArrowDirection.Up: return ArrowDirection.Down;
                case ArrowDirection.Right: return ArrowDirection.Left;
                case ArrowDirection.Down: return ArrowDirection.Up;
                case ArrowDirection.Left: return ArrowDirection.Right;
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }
    }
}
