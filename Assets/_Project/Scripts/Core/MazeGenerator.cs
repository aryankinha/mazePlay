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
    /// Immutable data for one puzzle board.
    /// Each car has a starting coordinate and an assigned multi-segment route through the
    /// road network ending at a perimeter exit gate.
    /// </summary>
    public sealed class MazeLevel
    {
        private readonly ArrowDirection[,] directions;
        private readonly bool[,] hasCar;
        private readonly ReadOnlyCollection<GridCoordinate> constructionOrder;
        private readonly ReadOnlyCollection<GridCoordinate> trapCoordinates;
        private readonly Dictionary<GridCoordinate, ReadOnlyCollection<GridCoordinate>> routes;
        private readonly Dictionary<GridCoordinate, ArrowDirection> exitDirections;
        private RoadTopology roadTopology;

        public MazeLevel(
            ArrowDirection[,] directions,
            IReadOnlyList<GridCoordinate> constructionOrder,
            IReadOnlyList<GridCoordinate> trapCoordinates,
            int initialLegalTapCount,
            bool[,] hasCar = null,
            int seed = 0,
            IReadOnlyDictionary<GridCoordinate, IReadOnlyList<GridCoordinate>> customRoutes = null,
            IReadOnlyDictionary<GridCoordinate, ArrowDirection> customExitDirections = null)
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

            routes = new Dictionary<GridCoordinate, ReadOnlyCollection<GridCoordinate>>();
            exitDirections = new Dictionary<GridCoordinate, ArrowDirection>();

            // If custom routes are provided, register them
            if (customRoutes != null)
            {
                foreach (var kvp in customRoutes)
                {
                    var coord = kvp.Key;
                    var rList = new List<GridCoordinate>(kvp.Value);
                    routes[coord] = rList.AsReadOnly();

                    if (customExitDirections != null && customExitDirections.TryGetValue(coord, out var exitDir))
                    {
                        exitDirections[coord] = exitDir;
                    }
                    else if (rList.Count >= 2)
                    {
                        var last = rList[rList.Count - 1];
                        var prev = rList[rList.Count - 2];
                        exitDirections[coord] = StraightLineLegality.GetStepDirection(prev, last);
                    }
                    else
                    {
                        exitDirections[coord] = this.directions[coord.Row, coord.Column];
                    }

                    // Ensure initial facing direction matches the first step of the route
                    if (rList.Count >= 2)
                    {
                        this.directions[coord.Row, coord.Column] = StraightLineLegality.GetStepDirection(rList[0], rList[1]);
                    }
                    else
                    {
                        this.directions[coord.Row, coord.Column] = exitDirections[coord];
                    }
                }
            }

            // Fill missing routes for cars using straight-line escape
            for (var row = 0; row < Rows; row++)
            {
                for (var column = 0; column < Columns; column++)
                {
                    var coord = new GridCoordinate(row, column);
                    if (!this.hasCar[row, column] || routes.ContainsKey(coord))
                    {
                        continue;
                    }

                    var dir = this.directions[row, column];
                    var straightRoute = new List<GridCoordinate> { coord };
                    var current = coord;
                    while (true)
                    {
                        var next = StraightLineLegality.Move(current, dir);
                        if (!IsInBounds(next))
                        {
                            break;
                        }
                        straightRoute.Add(next);
                        current = next;
                    }

                    routes[coord] = straightRoute.AsReadOnly();
                    exitDirections[coord] = dir;
                }
            }
        }

        public int Rows { get; }
        public int Columns { get; }
        public int Seed { get; }
        public int InitialLegalTapCount { get; }
        public int CarCount { get; }
        public IReadOnlyList<GridCoordinate> ConstructionOrder => constructionOrder;
        public IReadOnlyList<GridCoordinate> TrapCoordinates => trapCoordinates;
        public IReadOnlyDictionary<GridCoordinate, ReadOnlyCollection<GridCoordinate>> Routes => routes;

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

        public static MazeLevel FromRoutes(
            int rows,
            int columns,
            IReadOnlyDictionary<GridCoordinate, IReadOnlyList<GridCoordinate>> routes,
            IReadOnlyDictionary<GridCoordinate, ArrowDirection> exitDirections = null,
            IReadOnlyList<GridCoordinate> constructionOrder = null,
            IReadOnlyList<GridCoordinate> trapCoordinates = null,
            int initialLegalTapCount = 0,
            int seed = 0)
        {
            var directions = new ArrowDirection[rows, columns];
            var hasCar = new bool[rows, columns];

            foreach (var kvp in routes)
            {
                var coord = kvp.Key;
                hasCar[coord.Row, coord.Column] = true;
                if (kvp.Value.Count >= 2)
                {
                    directions[coord.Row, coord.Column] = StraightLineLegality.GetStepDirection(kvp.Value[0], kvp.Value[1]);
                }
                else if (exitDirections != null && exitDirections.TryGetValue(coord, out var exitDir))
                {
                    directions[coord.Row, coord.Column] = exitDir;
                }
            }

            return new MazeLevel(
                directions,
                constructionOrder ?? Array.Empty<GridCoordinate>(),
                trapCoordinates ?? Array.Empty<GridCoordinate>(),
                initialLegalTapCount,
                hasCar,
                seed,
                routes,
                exitDirections);
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

        public IReadOnlyList<GridCoordinate> GetRoute(GridCoordinate coordinate)
        {
            EnsureInBounds(coordinate);
            if (routes.TryGetValue(coordinate, out var route))
            {
                return route;
            }
            return Array.Empty<GridCoordinate>();
        }

        public ArrowDirection GetExitDirection(GridCoordinate coordinate)
        {
            EnsureInBounds(coordinate);
            if (exitDirections.TryGetValue(coordinate, out var exitDir))
            {
                return exitDir;
            }
            return directions[coordinate.Row, coordinate.Column];
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
            float minimumInterlinkFraction = 0.35f,
            int minimumJunctionCount = 1)
        {
            Rows = rows;
            Columns = columns;
            Seed = seed;
            TrapDensity = trapDensity;
            TargetStartingBranchingFactor = targetStartingBranchingFactor;
            MaxGenerationAttempts = maxGenerationAttempts;
            SolverNodeLimit = solverNodeLimit;
            CarDensity = Math.Min(1f, Math.Max(0.20f, carDensity));
            MinimumInterlinkFraction = Math.Min(1f, Math.Max(0f, minimumInterlinkFraction));
            MinimumJunctionCount = Math.Max(0, minimumJunctionCount);
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
        /// Minimum fraction of adjacent road segments shared by 2 or more car routes.
        /// </summary>
        public float MinimumInterlinkFraction { get; }

        /// <summary>
        /// Minimum count of multi-route junctions across the board.
        /// </summary>
        public int MinimumJunctionCount { get; }
    }

    public readonly struct TopologyMetrics
    {
        public TopologyMetrics(float sharedSegmentFraction, int junctionCount, float carSharingFraction, int totalSegments, int sharedSegments)
        {
            SharedSegmentFraction = sharedSegmentFraction;
            JunctionCount = junctionCount;
            CarSharingFraction = carSharingFraction;
            TotalSegments = totalSegments;
            SharedSegments = sharedSegments;
        }

        public float SharedSegmentFraction { get; }
        public int JunctionCount { get; }
        public float CarSharingFraction { get; }
        public int TotalSegments { get; }
        public int SharedSegments { get; }
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

        public static bool Solve(MazeLevel level, IReadOnlyList<GridCoordinate> tapOrder)
        {
            return level != null && IsLegalTapSequence(level, tapOrder) && tapOrder.Count == level.CarCount;
        }

        public static bool IsLegalTapSequence(MazeLevel level, IReadOnlyList<GridCoordinate> tapOrder)
        {
            if (level == null || tapOrder == null)
            {
                return false;
            }

            var cleared = level.CreateInitialClearedState();
            var seen = new HashSet<GridCoordinate>();

            foreach (var coordinate in tapOrder)
            {
                if (!level.IsInBounds(coordinate) || !level.HasCar(coordinate) || !seen.Add(coordinate))
                {
                    return false;
                }

                if (!StraightLineLegality.IsLegalTap(level, cleared, coordinate))
                {
                    return false;
                }

                cleared[coordinate.Row, coordinate.Column] = true;
            }

            return true;
        }

        public static MazeLevel Generate(MazeGenerationSettings settings)
        {
            ValidateSettings(settings);

            for (var attempt = 0; attempt < settings.MaxGenerationAttempts; attempt++)
            {
                var random = new Random(CombineSeed(settings.Seed, attempt));

                // 1. Carve a rich connected road network (spanning tree + loops/braids)
                var network = CarveRoadNetwork(settings.Rows, settings.Columns, random);

                // 2. Select perimeter exit gates on the road network
                var exits = SelectExitGates(network, settings.Rows, settings.Columns, random);
                if (exits.Count == 0)
                {
                    continue;
                }

                // 3. Build reverse-constructed car routes ensuring 100% solvability and shared avenues
                if (!TryBuildReverseRoutes(settings, network, exits, random, out var level))
                {
                    continue;
                }

                // 4. Verify with independent solver
                var solveResult = ChainPuzzleSolver.TrySolve(level, settings.SolverNodeLimit);
                if (!solveResult.IsSolved)
                {
                    continue;
                }

                // 5. Evaluate topology metrics (segment overlap & junctions)
                var metrics = ComputeTopologyMetrics(level);
                if ((metrics.SharedSegmentFraction < settings.MinimumInterlinkFraction ||
                     metrics.JunctionCount < settings.MinimumJunctionCount) &&
                    attempt < settings.MaxGenerationAttempts - 1)
                {
                    continue;
                }

                return level;
            }

            throw new InvalidOperationException(
                $"Could not generate a solvable {settings.Rows}x{settings.Columns} maze after {settings.MaxGenerationAttempts} attempts.");
        }

        public static TopologyMetrics ComputeTopologyMetrics(MazeLevel level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            var segmentUsage = new Dictionary<string, int>();
            var cellUsage = new Dictionary<GridCoordinate, HashSet<GridCoordinate>>();
            var carSharesSegment = new HashSet<GridCoordinate>();
            var totalCars = level.CarCount;

            for (var r = 0; r < level.Rows; r++)
            {
                for (var c = 0; c < level.Columns; c++)
                {
                    var coord = new GridCoordinate(r, c);
                    if (!level.HasCar(coord))
                    {
                        continue;
                    }

                    var route = level.GetRoute(coord);
                    if (route == null || route.Count == 0)
                    {
                        continue;
                    }

                    for (var i = 0; i < route.Count; i++)
                    {
                        var cell = route[i];
                        if (!cellUsage.TryGetValue(cell, out var carsAtCell))
                        {
                            carsAtCell = new HashSet<GridCoordinate>();
                            cellUsage[cell] = carsAtCell;
                        }
                        carsAtCell.Add(coord);

                        if (i < route.Count - 1)
                        {
                            var next = route[i + 1];
                            var segKey = GetSegmentKey(cell, next);
                            segmentUsage[segKey] = segmentUsage.TryGetValue(segKey, out var count) ? count + 1 : 1;
                        }
                    }
                }
            }

            var totalSegments = segmentUsage.Count;
            var sharedSegments = 0;
            foreach (var kvp in segmentUsage)
            {
                if (kvp.Value >= 2)
                {
                    sharedSegments++;
                }
            }

            // Identify which cars share at least one segment
            for (var r = 0; r < level.Rows; r++)
            {
                for (var c = 0; c < level.Columns; c++)
                {
                    var coord = new GridCoordinate(r, c);
                    if (!level.HasCar(coord)) continue;
                    var route = level.GetRoute(coord);
                    for (var i = 0; i < route.Count - 1; i++)
                    {
                        var segKey = GetSegmentKey(route[i], route[i + 1]);
                        if (segmentUsage.TryGetValue(segKey, out var count) && count >= 2)
                        {
                            carSharesSegment.Add(coord);
                            break;
                        }
                    }
                }
            }

            var junctionCount = 0;
            foreach (var kvp in cellUsage)
            {
                if (kvp.Value.Count >= 3)
                {
                    junctionCount++;
                }
            }

            var sharedFraction = totalSegments > 0 ? (float)sharedSegments / totalSegments : 0f;
            var carSharingFraction = totalCars > 0 ? (float)carSharesSegment.Count / totalCars : 0f;

            return new TopologyMetrics(sharedFraction, junctionCount, carSharingFraction, totalSegments, sharedSegments);
        }

        private static string GetSegmentKey(GridCoordinate a, GridCoordinate b)
        {
            if (a.Row < b.Row || (a.Row == b.Row && a.Column <= b.Column))
            {
                return $"{a.Row},{a.Column}-{b.Row},{b.Column}";
            }
            return $"{b.Row},{b.Column}-{a.Row},{a.Column}";
        }

        private static RoadNetwork CarveRoadNetwork(int rows, int columns, Random random)
        {
            var network = new RoadNetwork(rows, columns);

            // Randomized DFS spanning tree to connect all cells
            var visited = new bool[rows, columns];
            var stack = new Stack<GridCoordinate>();
            var start = new GridCoordinate(random.Next(rows), random.Next(columns));
            visited[start.Row, start.Column] = true;
            stack.Push(start);

            while (stack.Count > 0)
            {
                var current = stack.Peek();
                var neighbors = GetUnvisitedNeighbors(current, rows, columns, visited);

                if (neighbors.Count > 0)
                {
                    var next = neighbors[random.Next(neighbors.Count)];
                    network.AddEdge(current, next);
                    visited[next.Row, next.Column] = true;
                    stack.Push(next);
                }
                else
                {
                    stack.Pop();
                }
            }

            // Add extra loop connections (braiding) to introduce alternate paths and junctions
            var loopChance = 0.35f;
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < columns; c++)
                {
                    var curr = new GridCoordinate(r, c);
                    if (c + 1 < columns && !network.HasEdge(curr, new GridCoordinate(r, c + 1)))
                    {
                        if (random.NextDouble() < loopChance)
                        {
                            network.AddEdge(curr, new GridCoordinate(r, c + 1));
                        }
                    }
                    if (r + 1 < rows && !network.HasEdge(curr, new GridCoordinate(r + 1, c)))
                    {
                        if (random.NextDouble() < loopChance)
                        {
                            network.AddEdge(curr, new GridCoordinate(r + 1, c));
                        }
                    }
                }
            }

            return network;
        }

        private static List<GridCoordinate> GetUnvisitedNeighbors(GridCoordinate c, int rows, int columns, bool[,] visited)
        {
            var list = new List<GridCoordinate>(4);
            if (c.Row > 0 && !visited[c.Row - 1, c.Column]) list.Add(new GridCoordinate(c.Row - 1, c.Column));
            if (c.Column + 1 < columns && !visited[c.Row, c.Column + 1]) list.Add(new GridCoordinate(c.Row, c.Column + 1));
            if (c.Row + 1 < rows && !visited[c.Row + 1, c.Column]) list.Add(new GridCoordinate(c.Row + 1, c.Column));
            if (c.Column > 0 && !visited[c.Row, c.Column - 1]) list.Add(new GridCoordinate(c.Row, c.Column - 1));
            return list;
        }

        private static List<RoadExit> SelectExitGates(RoadNetwork network, int rows, int columns, Random random)
        {
            var exits = new List<RoadExit>();

            // Top boundary
            for (var c = 0; c < columns; c++)
            {
                if (random.NextDouble() < 0.65 || (c == 0 || c == columns - 1))
                {
                    exits.Add(new RoadExit(new GridCoordinate(0, c), ArrowDirection.Up));
                }
            }
            // Bottom boundary
            for (var c = 0; c < columns; c++)
            {
                if (random.NextDouble() < 0.65 || (c == 0 || c == columns - 1))
                {
                    exits.Add(new RoadExit(new GridCoordinate(rows - 1, c), ArrowDirection.Down));
                }
            }
            // Left boundary
            for (var r = 0; r < rows; r++)
            {
                if (random.NextDouble() < 0.65 || (r == 0 || r == rows - 1))
                {
                    exits.Add(new RoadExit(new GridCoordinate(r, 0), ArrowDirection.Left));
                }
            }
            // Right boundary
            for (var r = 0; r < rows; r++)
            {
                if (random.NextDouble() < 0.65 || (r == 0 || r == rows - 1))
                {
                    exits.Add(new RoadExit(new GridCoordinate(r, columns - 1), ArrowDirection.Right));
                }
            }

            return exits;
        }

        private static bool TryBuildReverseRoutes(
            MazeGenerationSettings settings,
            RoadNetwork network,
            List<RoadExit> exits,
            Random random,
            out MazeLevel level)
        {
            level = null;
            var targetCars = Math.Max(2, (int)(settings.Rows * settings.Columns * settings.CarDensity));

            var routes = new Dictionary<GridCoordinate, IReadOnlyList<GridCoordinate>>();
            var exitDirs = new Dictionary<GridCoordinate, ArrowDirection>();
            var placedCars = new HashSet<GridCoordinate>();
            var reverseClearOrder = new List<GridCoordinate>();
            var edgeUsage = new Dictionary<string, int>();

            var allCells = new List<GridCoordinate>();
            for (var r = 0; r < settings.Rows; r++)
            {
                for (var c = 0; c < settings.Columns; c++)
                {
                    allCells.Add(new GridCoordinate(r, c));
                }
            }

            // Shuffle cells to randomize starting placement
            Shuffle(allCells, random);

            for (var step = 0; step < targetCars; step++)
            {
                var candidateFound = false;

                // Try to find a start cell and a valid route to an exit avoiding already placed cars (which clear after this car)
                foreach (var startCell in allCells)
                {
                    if (placedCars.Contains(startCell))
                    {
                        continue;
                    }

                    if (TryFindRouteToExit(startCell, network, exits, placedCars, edgeUsage, random, out var route, out var exitDir))
                    {
                        routes[startCell] = route;
                        exitDirs[startCell] = exitDir;
                        placedCars.Add(startCell);
                        reverseClearOrder.Add(startCell);

                        // Record edge usage to encourage shared arterial avenues
                        for (var i = 0; i < route.Count - 1; i++)
                        {
                            var key = GetSegmentKey(route[i], route[i + 1]);
                            edgeUsage[key] = edgeUsage.TryGetValue(key, out var u) ? u + 1 : 1;
                        }

                        candidateFound = true;
                        break;
                    }
                }

                if (!candidateFound)
                {
                    // Placed as many as possible
                    if (placedCars.Count >= Math.Max(2, targetCars * 0.70f))
                    {
                        break;
                    }
                    return false;
                }
            }

            if (placedCars.Count < 2)
            {
                return false;
            }

            // Construct solve order (C_1 to C_N)
            var clearOrder = new List<GridCoordinate>(reverseClearOrder);
            clearOrder.Reverse();

            var initialCleared = new bool[settings.Rows, settings.Columns];
            foreach (var cell in allCells)
            {
                if (!placedCars.Contains(cell))
                {
                    initialCleared[cell.Row, cell.Column] = true;
                }
            }

            var preliminaryLevel = MazeLevel.FromRoutes(
                settings.Rows,
                settings.Columns,
                routes,
                exitDirs,
                clearOrder,
                Array.Empty<GridCoordinate>(),
                settings.TargetStartingBranchingFactor,
                settings.Seed);

            // Identify trap coordinates (cars that are initially blocked and cannot be tapped right away)
            var trapCoordinates = new List<GridCoordinate>();
            foreach (var car in placedCars)
            {
                if (!StraightLineLegality.IsLegalTap(preliminaryLevel, initialCleared, car))
                {
                    trapCoordinates.Add(car);
                }
            }

            // Construct the final candidate level with traps registered
            level = MazeLevel.FromRoutes(
                settings.Rows,
                settings.Columns,
                routes,
                exitDirs,
                clearOrder,
                trapCoordinates,
                settings.TargetStartingBranchingFactor,
                settings.Seed);

            // Check initial legal tap count
            var legalTaps = StraightLineLegality.GetLegalTaps(level, initialCleared);
            if (legalTaps.Count < settings.TargetStartingBranchingFactor)
            {
                if (legalTaps.Count < 1)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryFindRouteToExit(
            GridCoordinate start,
            RoadNetwork network,
            List<RoadExit> exits,
            HashSet<GridCoordinate> blockedCells,
            Dictionary<string, int> edgeUsage,
            Random random,
            out List<GridCoordinate> bestRoute,
            out ArrowDirection bestExitDir)
        {
            bestRoute = null;
            bestExitDir = ArrowDirection.Up;

            // Dijkstra from start to any perimeter exit avoiding blockedCells
            var distances = new Dictionary<GridCoordinate, float>();
            var previous = new Dictionary<GridCoordinate, GridCoordinate>();
            var queue = new PriorityQueue<GridCoordinate, float>();

            distances[start] = 0f;
            queue.Enqueue(start, 0f);

            var reachableExits = new List<(RoadExit exit, float dist)>();

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentDist = distances[current];

                // Check if current is an exit gate cell
                foreach (var exit in exits)
                {
                    if (exit.Coordinate == current)
                    {
                        reachableExits.Add((exit, currentDist));
                    }
                }

                foreach (var next in network.GetNeighbors(current))
                {
                    if (blockedCells.Contains(next))
                    {
                        continue;
                    }

                    var segKey = GetSegmentKey(current, next);
                    var usage = edgeUsage.TryGetValue(segKey, out var u) ? u : 0;

                    // Lower cost for shared arterial segments (strongly encourages merging into busy lanes)
                    var edgeCost = 1.0f / (1f + usage * 4.0f);

                    var newDist = currentDist + edgeCost + (float)(random.NextDouble() * 0.1);

                    if (!distances.TryGetValue(next, out var oldDist) || newDist < oldDist)
                    {
                        distances[next] = newDist;
                        previous[next] = current;
                        queue.Enqueue(next, newDist);
                    }
                }
            }

            if (reachableExits.Count == 0)
            {
                return false;
            }

            // Pick the best reachable exit
            reachableExits.Sort((a, b) => a.dist.CompareTo(b.dist));
            var selectedExit = reachableExits[0].exit;

            // Reconstruct path
            var path = new List<GridCoordinate>();
            var curr = selectedExit.Coordinate;
            while (curr != start)
            {
                path.Add(curr);
                curr = previous[curr];
            }
            path.Add(start);
            path.Reverse();

            bestRoute = path;
            bestExitDir = selectedExit.Direction;
            return true;
        }

        private static void Shuffle<T>(List<T> list, Random random)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                var temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        private static void ValidateSettings(MazeGenerationSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (settings.Rows < 1 || settings.Columns < 1)
            {
                throw new ArgumentException("Grid dimensions must be at least 1x1.");
            }
        }

        private static int CombineSeed(int seed, int modifier)
        {
            unchecked
            {
                return (seed * 397) ^ modifier;
            }
        }

        private sealed class RoadNetwork
        {
            private readonly int rows;
            private readonly int columns;
            private readonly HashSet<string> edges = new HashSet<string>();
            private readonly List<GridCoordinate>[,] adjacency;

            public RoadNetwork(int rows, int columns)
            {
                this.rows = rows;
                this.columns = columns;
                adjacency = new List<GridCoordinate>[rows, columns];
                for (var r = 0; r < rows; r++)
                {
                    for (var c = 0; c < columns; c++)
                    {
                        adjacency[r, c] = new List<GridCoordinate>(4);
                    }
                }
            }

            public void AddEdge(GridCoordinate a, GridCoordinate b)
            {
                var key = GetSegmentKey(a, b);
                if (edges.Add(key))
                {
                    adjacency[a.Row, a.Column].Add(b);
                    adjacency[b.Row, b.Column].Add(a);
                }
            }

            public bool HasEdge(GridCoordinate a, GridCoordinate b)
            {
                return edges.Contains(GetSegmentKey(a, b));
            }

            public IReadOnlyList<GridCoordinate> GetNeighbors(GridCoordinate c)
            {
                return adjacency[c.Row, c.Column];
            }
        }

        private sealed class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
        {
            private readonly List<(TElement element, TPriority priority)> elements = new List<(TElement, TPriority)>();

            public int Count => elements.Count;

            public void Enqueue(TElement element, TPriority priority)
            {
                elements.Add((element, priority));
                var i = elements.Count - 1;
                while (i > 0)
                {
                    var parent = (i - 1) / 2;
                    if (elements[i].priority.CompareTo(elements[parent].priority) >= 0)
                    {
                        break;
                    }
                    var temp = elements[i];
                    elements[i] = elements[parent];
                    elements[parent] = temp;
                    i = parent;
                }
            }

            public TElement Dequeue()
            {
                var best = elements[0].element;
                var last = elements[elements.Count - 1];
                elements.RemoveAt(elements.Count - 1);

                if (elements.Count > 0)
                {
                    elements[0] = last;
                    var i = 0;
                    while (true)
                    {
                        var left = i * 2 + 1;
                        var right = i * 2 + 2;
                        var smallest = i;

                        if (left < elements.Count && elements[left].priority.CompareTo(elements[smallest].priority) < 0)
                        {
                            smallest = left;
                        }
                        if (right < elements.Count && elements[right].priority.CompareTo(elements[smallest].priority) < 0)
                        {
                            smallest = right;
                        }
                        if (smallest == i)
                        {
                            break;
                        }

                        var temp = elements[i];
                        elements[i] = elements[smallest];
                        elements[smallest] = temp;
                        i = smallest;
                    }
                }

                return best;
            }
        }
    }
}
