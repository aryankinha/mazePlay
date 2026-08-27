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
        private RoadTopology roadTopology;

        internal MazeLevel(
            ArrowDirection[,] directions,
            IReadOnlyList<GridCoordinate> constructionOrder,
            IReadOnlyList<GridCoordinate> trapCoordinates,
            int initialLegalTapCount,
            bool[,] hasCar = null,
            int seed = 0)
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
            Seed = seed;
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

            var carCount = 0;
            for (var row = 0; row < Rows; row++)
            {
                for (var column = 0; column < Columns; column++)
                {
                    if (this.hasCar[row, column])
                    {
                        carCount++;
                    }
                }
            }

            CarCount = carCount;
        }

        public int Rows { get; }
        public int Columns { get; }
        public int Seed { get; }
        public int InitialLegalTapCount { get; }
        public int CarCount { get; }
        public IReadOnlyList<GridCoordinate> ConstructionOrder => constructionOrder;
        public IReadOnlyList<GridCoordinate> TrapCoordinates => trapCoordinates;

        public static MazeLevel FromDirections(ArrowDirection[,] directions)
        {
            return new MazeLevel(directions, Array.Empty<GridCoordinate>(), Array.Empty<GridCoordinate>(), 0);
        }

        public static MazeLevel FromDirections(ArrowDirection[,] directions, bool[,] hasCars)
        {
            if (directions == null)
            {
                throw new ArgumentNullException(nameof(directions));
            }

            if (hasCars == null || hasCars.GetLength(0) != directions.GetLength(0) ||
                hasCars.GetLength(1) != directions.GetLength(1))
            {
                throw new ArgumentException("Car-state dimensions must match the directions grid.", nameof(hasCars));
            }

            return new MazeLevel(directions, Array.Empty<GridCoordinate>(), Array.Empty<GridCoordinate>(), 0, hasCars);
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

        /// <summary>
        /// Empty road cells are traversable from the start. Both the live game and
        /// the solver use this state, so generation cannot validate a different game.
        /// </summary>
        public bool[,] CreateInitialClearedState()
        {
            var cleared = new bool[Rows, Columns];
            for (var row = 0; row < Rows; row++)
            {
                for (var column = 0; column < Columns; column++)
                {
                    cleared[row, column] = !hasCar[row, column];
                }
            }

            return cleared;
        }

        public RoadTopology GetRoadTopology()
        {
            return roadTopology ?? (roadTopology = RoadTopology.Build(this));
        }

        internal ArrowDirection[,] CopyDirectionMatrix()
        {
            return (ArrowDirection[,])directions.Clone();
        }

        internal bool[,] CopyCarMatrix()
        {
            return (bool[,])hasCar.Clone();
        }

        private ReadOnlyCollection<GridCoordinate> CopyCoordinates(IReadOnlyList<GridCoordinate> source, string paramName)
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
            float carDensity = 0.50f,
            float minimumInterlinkFraction = 0.40f,
            float minimumCrossFraction = 0.15f)
        {
            Rows = rows;
            Columns = columns;
            Seed = seed;
            TrapDensity = trapDensity;
            TargetStartingBranchingFactor = targetStartingBranchingFactor;
            MaxGenerationAttempts = maxGenerationAttempts;
            SolverNodeLimit = solverNodeLimit;
            CarDensity = Math.Min(1f, Math.Max(0.25f, carDensity));
            MinimumInterlinkFraction = Math.Min(1f, Math.Max(0f, minimumInterlinkFraction));
            MinimumCrossFraction = Math.Min(1f, Math.Max(0f, minimumCrossFraction));
        }

        public int Rows { get; }
        public int Columns { get; }
        public int Seed { get; }
        public float TrapDensity { get; }
        public int TargetStartingBranchingFactor { get; }
        public int MaxGenerationAttempts { get; }
        public int SolverNodeLimit { get; }
        public float CarDensity { get; }

        /// <summary>
        /// Floor for the fraction of cars whose straight-line path shares at least one
        /// cell with another car's path. Boards below the floor are rejected, so no
        /// generated level can ship as a set of fully private lanes.
        /// </summary>
        public float MinimumInterlinkFraction { get; }

        /// <summary>
        /// Floor for the stricter crossing fraction - cars whose path crosses a
        /// perpendicularly oriented car's path. Keeps junctions mechanically real.
        /// </summary>
        public float MinimumCrossFraction { get; }
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

                var candidateLevel = new MazeLevel(directions, clearOrder, trapCoordinates, 0, hasCar, settings.Seed);
                var initialCleared = candidateLevel.CreateInitialClearedState();
                var initialLegalCount = CountLegalCars(candidateLevel, initialCleared);
                if (initialLegalCount < settings.TargetStartingBranchingFactor)
                {
                    continue;
                }

                var activeTrapCoordinates = GetInitialIllegalCars(candidateLevel, initialCleared, trapCoordinates);
                if (settings.TrapDensity > 0f && activeTrapCoordinates.Count == 0)
                {
                    continue;
                }

                var generated = new MazeLevel(
                    directions,
                    clearOrder,
                    activeTrapCoordinates,
                    initialLegalCount,
                    hasCar,
                    settings.Seed);

                var preSolve = ChainPuzzleSolver.TrySolve(generated, settings.SolverNodeLimit);
                if (!preSolve.IsSolved)
                {
                    continue;
                }

                // Push the board toward genuine maze topology (crossing paths) while
                // preserving the proven solution above; boards that still fall below
                // the interlink floors are rejected so private-lane layouts never ship.
                var raised = RaiseInterlinking(
                    generated,
                    preSolve.ClearOrder,
                    CombineSeed(settings.Seed, 31337),
                    settings.MinimumInterlinkFraction,
                    settings.MinimumCrossFraction);
                var postFractions = ComputePathFractions(raised);
                if (postFractions.shared < settings.MinimumInterlinkFraction ||
                    postFractions.cross < settings.MinimumCrossFraction)
                {
                    continue;
                }

                // When interlinking returned the board untouched its existing proof
                // stands; otherwise re-verify the shipped board independently.
                var solveResult = ReferenceEquals(raised, generated)
                    ? preSolve
                    : ChainPuzzleSolver.TrySolve(raised, settings.SolverNodeLimit);
                if (!solveResult.IsSolved)
                {
                    continue;
                }

                generated = raised;
                if (solveResult.IsSolved)
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
            return level != null && IsLegalTapSequence(level, tapOrder) && tapOrder.Count == level.CarCount;
        }

        /// <summary>
        /// Fraction of cars whose full straight-line path (start cell through exit)
        /// shares at least one cell with a different car's path. Purely geometric;
        /// independent of blocking or solvability.
        /// </summary>
        public static float ComputeSharedPathFraction(MazeLevel level)
        {
            return ComputePathFractions(level).shared;
        }

        /// <summary>
        /// Strict subset of sharing: cars whose path crosses a perpendicularly
        /// oriented car's path - genuine intersections rather than queues.
        /// </summary>
        public static float ComputeCrossPathFraction(MazeLevel level)
        {
            return ComputePathFractions(level).cross;
        }

        private static (float shared, float cross) ComputePathFractions(MazeLevel level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            var cars = new List<GridCoordinate>();
            for (var row = 0; row < level.Rows; row++)
            {
                for (var column = 0; column < level.Columns; column++)
                {
                    var coordinate = new GridCoordinate(row, column);
                    if (level.HasCar(coordinate))
                    {
                        cars.Add(coordinate);
                    }
                }
            }

            var total = cars.Count;
            if (total == 0)
            {
                return (1f, 1f);
            }

            var horizontal = new bool[total];
            var pathLengths = new int[total];
            var cells = new GridCoordinate[total, Math.Max(level.Rows, level.Columns) + 1];
            for (var index = 0; index < total; index++)
            {
                var direction = level.GetDirection(cars[index]);
                horizontal[index] = direction == ArrowDirection.Left || direction == ArrowDirection.Right;
                var current = cars[index];
                var length = 0;
                cells[index, length++] = current;
                while (true)
                {
                    current = StraightLineLegality.Move(current, direction);
                    if (!level.IsInBounds(current))
                    {
                        break;
                    }

                    cells[index, length++] = current;
                }

                pathLengths[index] = length;
            }

            var shared = 0;
            var crossed = 0;
            for (var a = 0; a < total; a++)
            {
                var touchesAnother = false;
                var crossesAnother = false;
                for (var b = 0; b < total && !(touchesAnother && crossesAnother); b++)
                {
                    if (a == b)
                    {
                        continue;
                    }

                    var touches = false;
                    for (var i = 0; i < pathLengths[a] && !touches; i++)
                    {
                        for (var j = 0; j < pathLengths[b]; j++)
                        {
                            if (cells[a, i] == cells[b, j])
                            {
                                touches = true;
                                break;
                            }
                        }
                    }

                    if (!touches)
                    {
                        continue;
                    }

                    touchesAnother = true;
                    if (horizontal[a] != horizontal[b])
                    {
                        crossesAnother = true;
                    }
                }

                if (touchesAnother)
                {
                    shared++;
                }

                if (crossesAnother)
                {
                    crossed++;
                }
            }

            return ((float)shared / total, (float)crossed / total);
        }

        /// <summary>
        /// Rewrites arrow directions so more car paths genuinely cross each other
        /// while provably preserving one known solution order (pass it in from the
        /// caller's own solve - this method never searches). Trap tiles keep their
        /// directions and must remain initially illegal, and the board always keeps
        /// at least two legal opening moves, so difficulty semantics survive.
        /// Deterministic for a given source level and seed. Best effort: returns the
        /// best board found even when targets are not fully reachable.
        /// </summary>
        public static MazeLevel RaiseInterlinking(
            MazeLevel source,
            IReadOnlyList<GridCoordinate> knownSolution,
            int seed,
            float minimumSharedFraction,
            float minimumCrossFraction,
            int maxAttempts = 900)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (knownSolution == null || knownSolution.Count != source.CarCount || source.CarCount < 2)
            {
                return source;
            }

            var traps = new HashSet<GridCoordinate>(source.TrapCoordinates);
            var rotatable = new List<GridCoordinate>();
            for (var row = 0; row < source.Rows; row++)
            {
                for (var column = 0; column < source.Columns; column++)
                {
                    var coordinate = new GridCoordinate(row, column);
                    if (source.HasCar(coordinate) && !traps.Contains(coordinate))
                    {
                        rotatable.Add(coordinate);
                    }
                }
            }

            if (rotatable.Count == 0)
            {
                return source;
            }

            var startFractions = ComputePathFractions(source);
            if (startFractions.shared >= minimumSharedFraction &&
                startFractions.cross >= minimumCrossFraction)
            {
                return source;
            }

            var minimumStartingMoves = Math.Min(source.InitialLegalTapCount, 2);
            var bestDirs = source.CopyDirectionMatrix();
            var bestCross = startFractions.cross;
            var bestShared = startFractions.shared;
            var random = new Random(seed);

            for (var attempt = 0;
                 attempt < maxAttempts && (bestCross < minimumCrossFraction || bestShared < minimumSharedFraction);
                 attempt++)
            {
                var trialDirs = (ArrowDirection[,])bestDirs.Clone();
                var coordinate = rotatable[random.Next(rotatable.Count)];
                var rotation = 1 + random.Next(3);
                trialDirs[coordinate.Row, coordinate.Column] =
                    (ArrowDirection)(((int)trialDirs[coordinate.Row, coordinate.Column] + rotation) % 4);

                var candidate = BuildLevelFrom(trialDirs, source);
                var initialCleared = candidate.CreateInitialClearedState();

                if (CountLegalCars(candidate, initialCleared) < minimumStartingMoves)
                {
                    continue;
                }

                var trapsPreserved = true;
                foreach (var trap in source.TrapCoordinates)
                {
                    if (StraightLineLegality.IsLegalTap(candidate, initialCleared, trap))
                    {
                        trapsPreserved = false;
                        break;
                    }
                }

                if (!trapsPreserved || !SolutionRemainsLegal(candidate, knownSolution))
                {
                    continue;
                }

                var fractions = ComputePathFractions(candidate);
                if (fractions.cross > bestCross + 0.0001f ||
                    (Math.Abs(fractions.cross - bestCross) <= 0.0001f && fractions.shared > bestShared + 0.0001f))
                {
                    bestCross = fractions.cross;
                    bestShared = fractions.shared;
                    bestDirs = trialDirs;
                }
            }

            var final = BuildLevelFrom(bestDirs, source);
            var finalCleared = final.CreateInitialClearedState();
            var finalLegalCount = CountLegalCars(final, finalCleared);
            return new MazeLevel(
                bestDirs,
                source.ConstructionOrder,
                source.TrapCoordinates,
                finalLegalCount,
                source.CopyCarMatrix(),
                source.Seed);
        }

        private static MazeLevel BuildLevelFrom(ArrowDirection[,] directions, MazeLevel source)
        {
            return new MazeLevel(
                directions,
                source.ConstructionOrder,
                source.TrapCoordinates,
                source.InitialLegalTapCount,
                source.CopyCarMatrix(),
                source.Seed);
        }

        private static bool SolutionRemainsLegal(MazeLevel level, IReadOnlyList<GridCoordinate> order)
        {
            var cleared = level.CreateInitialClearedState();
            foreach (var coordinate in order)
            {
                if (!StraightLineLegality.IsLegalTap(level, cleared, coordinate))
                {
                    return false;
                }

                cleared[coordinate.Row, coordinate.Column] = true;
            }

            return true;
        }

        public static bool IsLegalTapSequence(MazeLevel level, IReadOnlyList<GridCoordinate> tapOrder)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (tapOrder == null || tapOrder.Count > level.CarCount)
            {
                return false;
            }

            var cleared = level.CreateInitialClearedState();
            foreach (var coordinate in tapOrder)
            {
                if (!level.IsInBounds(coordinate) || !level.HasCar(coordinate) ||
                    !StraightLineLegality.IsLegalTap(level, cleared, coordinate))
                {
                    return false;
                }

                cleared[coordinate.Row, coordinate.Column] = true;
            }

            return true;
        }

        private static int CountLegalCars(MazeLevel level, bool[,] cleared)
        {
            var count = 0;
            foreach (var coordinate in StraightLineLegality.GetLegalTaps(level, cleared))
            {
                if (level.HasCar(coordinate))
                {
                    count++;
                }
            }

            return count;
        }

        private static List<GridCoordinate> GetInitialIllegalCars(
            MazeLevel level,
            bool[,] cleared,
            IReadOnlyList<GridCoordinate> candidateTraps)
        {
            var activeTraps = new List<GridCoordinate>();
            foreach (var coordinate in candidateTraps)
            {
                if (level.HasCar(coordinate) && !StraightLineLegality.IsLegalTap(level, cleared, coordinate))
                {
                    activeTraps.Add(coordinate);
                }
            }

            return activeTraps;
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
