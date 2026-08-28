using System;
using System.Collections.Generic;

namespace ArrowMaze.Core
{
    /// <summary>
    /// Evaluates legality of car taps against the board state.
    /// Each car follows a predetermined route through the road network from its starting
    /// cell to a perimeter exit. A tap is legal if all subsequent cells along its route
    /// are currently unobstructed (cleared of other cars) and exit through an active gate.
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

            var route = level.GetRoute(coordinate);
            if (route == null || route.Count == 0)
            {
                return false;
            }

            // Check each subsequent cell along the car's predetermined route
            for (var i = 1; i < route.Count; i++)
            {
                var step = route[i];
                if (!level.IsInBounds(step))
                {
                    return false;
                }

                if (!cleared[step.Row, step.Column])
                {
                    return false;
                }
            }

            // Verify that the final cell in the route exits through an active road gate
            var lastCell = route[route.Count - 1];
            var exitDirection = level.GetExitDirection(coordinate);
            return level.GetRoadTopology().HasExitGate(lastCell, exitDirection);
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

        public static ArrowDirection GetStepDirection(GridCoordinate from, GridCoordinate to)
        {
            if (to.Row == from.Row - 1 && to.Column == from.Column) return ArrowDirection.Up;
            if (to.Row == from.Row && to.Column == from.Column + 1) return ArrowDirection.Right;
            if (to.Row == from.Row + 1 && to.Column == from.Column) return ArrowDirection.Down;
            if (to.Row == from.Row && to.Column == from.Column - 1) return ArrowDirection.Left;
            throw new ArgumentException($"Coordinates {from} and {to} are not adjacent.");
        }

        public static ArrowDirection Opposite(ArrowDirection direction)
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
