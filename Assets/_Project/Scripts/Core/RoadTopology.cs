using System;
using System.Collections.Generic;

namespace ArrowMaze.Core
{
    [Flags]
    public enum RoadConnections
    {
        None = 0,
        Up = 1,
        Right = 2,
        Down = 4,
        Left = 8
    }

    /// <summary>
    /// A physical exit is an opening at the edge of the last road cell, facing out.
    /// </summary>
    public readonly struct RoadExit : IEquatable<RoadExit>
    {
        public RoadExit(GridCoordinate coordinate, ArrowDirection direction)
        {
            Coordinate = coordinate;
            Direction = direction;
        }

        public GridCoordinate Coordinate { get; }
        public ArrowDirection Direction { get; }

        public bool Equals(RoadExit other) => Coordinate == other.Coordinate && Direction == other.Direction;
        public override bool Equals(object obj) => obj is RoadExit other && Equals(other);
        public override int GetHashCode() => (Coordinate.GetHashCode() * 397) ^ (int)Direction;
    }

    /// <summary>
    /// Derives the visible road network from the exact straight routes cars can use.
    /// This prevents artwork from suggesting turns or exits that gameplay cannot take.
    /// </summary>
    public sealed class RoadTopology
    {
        private readonly RoadConnections[,] connections;
        private readonly HashSet<RoadExit> exits;

        private RoadTopology(int rows, int columns)
        {
            connections = new RoadConnections[rows, columns];
            exits = new HashSet<RoadExit>();
        }

        public static RoadTopology Build(MazeLevel level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            var topology = new RoadTopology(level.Rows, level.Columns);
            for (var row = 0; row < level.Rows; row++)
            {
                for (var column = 0; column < level.Columns; column++)
                {
                    var source = new GridCoordinate(row, column);
                    if (level.HasCar(source))
                    {
                        topology.AddEscapeRoute(level, source, level.GetDirection(source));
                    }
                }
            }

            return topology;
        }

        public RoadConnections GetConnections(GridCoordinate coordinate)
        {
            return connections[coordinate.Row, coordinate.Column];
        }

        public bool HasExitGate(GridCoordinate coordinate, ArrowDirection direction)
        {
            return exits.Contains(new RoadExit(coordinate, direction));
        }

        private void AddEscapeRoute(MazeLevel level, GridCoordinate source, ArrowDirection direction)
        {
            var current = source;
            while (true)
            {
                connections[current.Row, current.Column] |= ToConnection(direction);
                var next = StraightLineLegality.Move(current, direction);
                if (!level.IsInBounds(next))
                {
                    exits.Add(new RoadExit(current, direction));
                    return;
                }

                connections[next.Row, next.Column] |= ToConnection(Opposite(direction));
                current = next;
            }
        }

        private static RoadConnections ToConnection(ArrowDirection direction)
        {
            switch (direction)
            {
                case ArrowDirection.Up: return RoadConnections.Up;
                case ArrowDirection.Right: return RoadConnections.Right;
                case ArrowDirection.Down: return RoadConnections.Down;
                case ArrowDirection.Left: return RoadConnections.Left;
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
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
