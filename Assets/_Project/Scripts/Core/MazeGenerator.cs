using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ArrowMaze.Core
{
    public enum ArrowDirection
    {
        Up,
        Right,
        Down,
        Left
    }

    /// <summary>
    /// Immutable grid position. Row 0 is the top row; column 0 is the left column.
    /// </summary>
    public readonly struct GridCoordinate : IEquatable<GridCoordinate>
    {
        public GridCoordinate(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public int Row { get; }
        public int Column { get; }

        public bool Equals(GridCoordinate other) => Row == other.Row && Column == other.Column;
        public override bool Equals(object obj) => obj is GridCoordinate other && Equals(other);
        public override int GetHashCode() => (Row * 397) ^ Column;
        public static bool operator ==(GridCoordinate left, GridCoordinate right) => left.Equals(right);
        public static bool operator !=(GridCoordinate left, GridCoordinate right) => !left.Equals(right);
        public override string ToString() => $"({Row}, {Column})";
    }

    /// <summary>
    /// Immutable data for one arrow/car board.
    /// </summary>
    public sealed class MazeLevel
    {
        private readonly ArrowDirection[,] directions;
        private readonly bool[,] hasCar;
        private readonly ReadOnlyCollection<GridCoordinate> constructionOrder;
        private readonly ReadOnlyCollection<GridCoordinate> trapCoordinates;

        internal MazeLevel(
            ArrowDirection[,] directions,
            IList<GridCoordinate> constructionOrder,
            IList<GridCoordinate> trapCoordinates,
            int initialLegalTapCount,
            bool[,] hasCar = null)
        {
            if (directions == null)
            {
                throw new ArgumentNullException(nameof(directions));
            }

            if (directions.GetLength(0) < 1 || directions.GetLength(1) < 1)
            {
                throw new ArgumentException("A maze needs at least one cell.", nameof(directions));
            }

            Rows = directions.GetLength(0);
            Columns = directions.GetLength(1);
            this.directions = (ArrowDirection[,])directions.Clone();
            this.constructionOrder = CopyCoordinates(constructionOrder, nameof(constructionOrder));
            this.trapCoordinates = CopyCoordinates(trapCoordinates, nameof(trapCoordinates));
            InitialLegalTapCount = initialLegalTapCount;

            if (hasCar != null)
            {
                this.hasCar = (bool[,])hasCar.Clone();
            }
            else
            {
                // Default: every cell has a car for standard 100% density boards
                this.hasCar = new bool[Rows, Columns];
                for (var r = 0; r < Rows; r++)
                {
                    for (var c = 0; c < Columns; c++)
                    {
                        this.hasCar[r, c] = true;
                    }
                }
            }
        }

        public int Rows { get; }
        public int Columns { get; }
        public int InitialLegalTapCount { get; }
        public IReadOnlyList<GridCoordinate> ConstructionOrder => constructionOrder;
        public IReadOnlyList<GridCoordinate> TrapCoordinates => trapCoordinates;

        public static MazeLevel FromDirections(ArrowDirection[,] directions)
        {
            return new MazeLevel(directions, Array.Empty<GridCoordinate>(), Array.Empty<GridCoordinate>(), 0);
        }

        public ArrowDirection GetDirection(GridCoordinate coordinate)
        {
            EnsureInBounds(coordinate);
            return directions[coordinate.Row, coordinate.Column];
        }

        public bool HasCar(GridCoordinate coordinate)
        {
            EnsureInBounds(coordinate);
            return hasCar != null && hasCar[coordinate.Row, coordinate.Column];
        }

        public bool IsInBounds(GridCoordinate coordinate)
        {
            return coordinate.Row >= 0 && coordinate.Row < Rows &&
                   coordinate.Column >= 0 && coordinate.Column < Columns;
        }

        private ReadOnlyCollection<GridCoordinate> CopyCoordinates(IList<GridCoordinate> source, string paramName)
        {
            var copied = new List<GridCoordinate>();
            if (source != null)
            {
                var seen = new HashSet<GridCoordinate>();
                for (var index = 0; index < source.Count; index++)
                {
                    var coordinate = source[index];
                    if (!IsInBounds(coordinate) || !seen.Add(coordinate))
                    {
                        throw new ArgumentException("Metadata coordinates must be unique and in bounds.", paramName);
                    }

                    copied.Add(coordinate);
                }
            }

            return copied.AsReadOnly();
        }

        private void EnsureInBounds(GridCoordinate coordinate)
        {
            if (!IsInBounds(coordinate))
            {
                throw new ArgumentOutOfRangeException(nameof(coordinate), coordinate, "Coordinate is outside this maze.");
            }
        }
    }

    public sealed class MazeGenerationSettings
    {
        public MazeGenerationSettings(
            int rows,
            int columns,
            int seed,
            float trapDensity = 0.15f,
            int targetStartingBranchingFactor = 2,
            int maxGenerationAttempts = 160,
            int solverNodeLimit = 250000,
            float carDensity = 0.50f)
        {
            Rows = rows;
            Columns = columns;
            Seed = seed;
            TrapDensity = trapDensity;
            TargetStartingBranchingFactor = targetStartingBranchingFactor;
            MaxGenerationAttempts = maxGenerationAttempts;
            SolverNodeLimit = solverNodeLimit;
            CarDensity = Mathf.Clamp(carDensity, 0.25f, 1f);
        }

        public int Rows { get; }
        public int Columns { get; }
        public int Seed { get; }
        public float TrapDensity { get; }
        public int TargetStartingBranchingFactor { get; }
        public int MaxGenerationAttempts { get; }
        public int SolverNodeLimit { get; }
        public float CarDensity { get; }
    }

    public static class MazeGenerator
    {
        private static readonly ArrowDirection[] AllDirections =
        {
            ArrowDirection.Up,
            ArrowDirection.Right,
            ArrowDirection.Down,
            ArrowDirection.Left
        };

        public static MazeLevel Generate(int rows, int columns, int seed)
        {
            return Generate(new MazeGenerationSettings(rows, columns, seed));
        }

        public static MazeLevel Generate(
            int rows,
            int columns,
            int seed,
            float trapDensity,
            int targetStartingBranchingFactor)
        {
            return Generate(new MazeGenerationSettings(
                rows,
                columns,
                seed,
                trapDensity,
                targetStartingBranchingFactor));
        }

        public static MazeLevel Generate(MazeGenerationSettings settings)
        {
            ValidateSettings(settings);
            for (var attempt = 0; attempt < settings.MaxGenerationAttempts; attempt++)
            {
                var random = new Random(CombineSeed(settings.Seed, attempt));
                if (!TryBuildClearOrder(settings, random, out var directions, out var clearOrder))
                {
                    continue;
                }

                if (!TryPlaceTraps(settings, directions, clearOrder, random, out var trapCoordinates))
                {
                    continue;
                }

                var provisional = new MazeLevel(directions, clearOrder, trapCoordinates, 0);
                var initialCleared = new bool[settings.Rows, settings.Columns];
                var initialLegalCount = StraightLineLegality.GetLegalTaps(provisional, initialCleared).Count;
                if (initialLegalCount != settings.TargetStartingBranchingFactor)
                {
                    continue;
                }

                if (settings.TrapDensity > 0f && trapCoordinates.Count == 0)
                {
                    continue;
                }

                // Determine car placement based on car density setting
                var hasCar = new bool[settings.Rows, settings.Columns];
                if (Math.Abs(settings.CarDensity - 1f) < 0.01f)
                {
                    for (var r = 0; r < settings.Rows; r++)
                    {
                        for (var c = 0; c < settings.Columns; c++)
                        {
                            hasCar[r, c] = true;
                        }
                    }
                }
                else
                {
                    // Pick cars along the clear order to ensure full solvability and clean road layout
                    var targetCarCount = Math.Max(4, (int)(settings.Rows * settings.Columns * settings.CarDensity));
                    var step = (double)clearOrder.Count / targetCarCount;
                    for (var i = 0; i < targetCarCount; i++)
                    {
                        var idx = Math.Min((int)(i * step), clearOrder.Count - 1);
                        var coord = clearOrder[idx];
                        hasCar[coord.Row, coord.Column] = true;
                    }
                }

                var generated = new MazeLevel(directions, clearOrder, trapCoordinates, initialLegalCount, hasCar);
                var solveResult = ChainPuzzleSolver.TrySolve(generated, settings.SolverNodeLimit);
                if (solveResult.IsSolved)
                {
                    return generated;
                }
            }

            throw new InvalidOperationException(
                $"Could not generate a solvable {settings.Rows}x{settings.Columns} maze after {settings.MaxGenerationAttempts} attempts.");
        }

        public static bool Solve(MazeLevel level, IReadOnlyList<GridCoordinate> tapOrder)
        {
            return IsLegalTapSequence(level, tapOrder) && tapOrder.Count == level.Rows * level.Columns;
        }

        public static bool IsLegalTapSequence(MazeLevel level, IReadOnlyList<GridCoordinate> tapOrder)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (tapOrder == null || tapOrder.Count > level.Rows * level.Columns)
            {
                return false;
            }

            var cleared = new bool[level.Rows, level.Columns];
            foreach (var coordinate in tapOrder)
            {
                if (!StraightLineLegality.IsLegalTap(level, cleared, coordinate))
                {
                    return false;
                }

                cleared[coordinate.Row, coordinate.Column] = true;
            }

            return true;
        }

        private static bool TryBuildClearOrder(
            MazeGenerationSettings settings,
            Random random,
            out ArrowDirection[,] directions,
            out List<GridCoordinate> clearOrder)
        {
            directions = new ArrowDirection[settings.Rows, settings.Columns];
            clearOrder = new List<GridCoordinate>(settings.Rows * settings.Columns);

            var remaining = new HashSet<GridCoordinate>();
            for (var row = 0; row < settings.Rows; row++)
            {
                for (var column = 0; column < settings.Columns; column++)
                {
                    remaining.Add(new GridCoordinate(row, column));
                }
            }

            while (remaining.Count > 0)
            {
                var candidateMoves = GetCandidatePlacementMoves(settings, remaining, directions);
                if (candidateMoves.Count == 0)
                {
                    return false;
                }

                var chosen = candidateMoves[random.Next(candidateMoves.Count)];
                directions[chosen.Coordinate.Row, chosen.Coordinate.Column] = chosen.Direction;
                remaining.Remove(chosen.Coordinate);
                clearOrder.Insert(0, chosen.Coordinate);
            }

            return true;
        }

        private static bool TryPlaceTraps(
            MazeGenerationSettings settings,
            ArrowDirection[,] directions,
            List<GridCoordinate> clearOrder,
            Random random,
            out List<GridCoordinate> trapCoordinates)
        {
            trapCoordinates = new List<GridCoordinate>();
            var targetTrapCount = (int)Math.Round(settings.Rows * settings.Columns * settings.TrapDensity);
            if (targetTrapCount <= 0)
            {
                return true;
            }

            var candidates = new List<GridCoordinate>();
            for (var index = 0; index < clearOrder.Count - settings.TargetStartingBranchingFactor; index++)
            {
                candidates.Add(clearOrder[index]);
            }

            Shuffle(candidates, random);

            foreach (var candidate in candidates)
            {
                if (trapCoordinates.Count >= targetTrapCount)
                {
                    break;
                }

                var originalDirection = directions[candidate.Row, candidate.Column];
                var candidateDirections = new List<ArrowDirection>(AllDirections);
                candidateDirections.Remove(originalDirection);
                Shuffle(candidateDirections, random);

                foreach (var trapDirection in candidateDirections)
                {
                    directions[candidate.Row, candidate.Column] = trapDirection;
                    var candidateLevel = new MazeLevel(directions, clearOrder, trapCoordinates, 0);
                    var solveResult = ChainPuzzleSolver.TrySolve(candidateLevel, settings.SolverNodeLimit);
                    if (solveResult.IsSolved)
                    {
                        trapCoordinates.Add(candidate);
                        break;
                    }

                    directions[candidate.Row, candidate.Column] = originalDirection;
                }
            }

            return true;
        }

        private static List<PlacementMove> GetCandidatePlacementMoves(
            MazeGenerationSettings settings,
            HashSet<GridCoordinate> remaining,
            ArrowDirection[,] directions)
        {
            var moves = new List<PlacementMove>();
            foreach (var coordinate in remaining)
            {
                foreach (var direction in AllDirections)
                {
                    if (IsPlacementValid(settings, coordinate, direction, remaining, directions))
                    {
                        moves.Add(new PlacementMove(coordinate, direction));
                    }
                }
            }

            return moves;
        }

        private static bool IsPlacementValid(
            MazeGenerationSettings settings,
            GridCoordinate coordinate,
            ArrowDirection direction,
            HashSet<GridCoordinate> remaining,
            ArrowDirection[,] directions)
        {
            var current = coordinate;
            while (true)
            {
                current = StraightLineLegality.Move(current, direction);
                if (current.Row < 0 || current.Row >= settings.Rows ||
                    current.Column < 0 || current.Column >= settings.Columns)
                {
                    return true;
                }

                if (remaining.Contains(current))
                {
                    return false;
                }
            }
        }

        private static void ValidateSettings(MazeGenerationSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (settings.Rows < 1 || settings.Columns < 1) throw new ArgumentException("Grid must have at least 1x1 cells.");
            if (settings.TrapDensity < 0f || settings.TrapDensity > 1f) throw new ArgumentOutOfRangeException(nameof(settings.TrapDensity));
            if (settings.TargetStartingBranchingFactor < 1) throw new ArgumentOutOfRangeException(nameof(settings.TargetStartingBranchingFactor));
            if (settings.MaxGenerationAttempts < 1) throw new ArgumentOutOfRangeException(nameof(settings.MaxGenerationAttempts));
            if (settings.SolverNodeLimit < 1) throw new ArgumentOutOfRangeException(nameof(settings.SolverNodeLimit));
        }

        private static int CombineSeed(int seed, int attempt)
        {
            unchecked
            {
                return (seed * 397) ^ (attempt * 104729);
            }
        }

        private static void Shuffle<T>(IList<T> list, Random random)
        {
            for (var index = list.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);
                var temp = list[index];
                list[index] = list[swapIndex];
                list[swapIndex] = temp;
            }
        }

        private readonly struct PlacementMove
        {
            public PlacementMove(GridCoordinate coordinate, ArrowDirection direction)
            {
                Coordinate = coordinate;
                Direction = direction;
            }

            public GridCoordinate Coordinate { get; }
            public ArrowDirection Direction { get; }
        }
    }
}
