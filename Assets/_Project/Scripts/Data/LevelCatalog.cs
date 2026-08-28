using System;
using System.Collections.Generic;
using ArrowMaze.Core;

namespace ArrowMaze.Data
{
    public enum LevelKind { Tutorial, Authored, Procedural, Challenge }

    public sealed class LevelDefinition
    {
        public int Id { get; }
        public string Difficulty { get; }
        public string TeachingGoal { get; }
        public LevelKind Kind { get; }
        public int Rows { get; }
        public int Columns { get; }
        public int Seed { get; }
        public float TrapDensity { get; }
        public int BranchingFactor { get; }
        public float CarDensity { get; }

        public LevelDefinition(int id, string difficulty, string teachingGoal, LevelKind kind, int rows, int columns, int seed, float trapDensity, int branchingFactor, float carDensity)
        {
            Id = id; Difficulty = difficulty; TeachingGoal = teachingGoal; Kind = kind;
            Rows = rows; Columns = columns; Seed = seed; TrapDensity = trapDensity;
            BranchingFactor = branchingFactor; CarDensity = carDensity;
        }

        public MazeLevel BuildLevel()
        {
            var authoredRoutes = LevelCatalog.GetAuthoredRoutes(Id, out var exitDirections);
            if (authoredRoutes != null)
            {
                return MazeLevel.FromRoutes(
                    Rows,
                    Columns,
                    authoredRoutes,
                    exitDirections,
                    seed: Seed);
            }

            return MazeGenerator.Generate(new MazeGenerationSettings(
                Rows,
                Columns,
                Seed,
                TrapDensity,
                BranchingFactor,
                carDensity: CarDensity,
                minimumInterlinkFraction: 0.20f,
                minimumJunctionCount: 1));
        }
    }

    /// <summary>Catalog entries are data, leaving the gameplay engine unaware of menu progression.</summary>
    public static class LevelCatalog
    {
        public const int HighestCatalogLevel = 23;

        private static readonly LevelDefinition[] Definitions =
        {
            new LevelDefinition(1, "Tutorial", "Tap a car and let it leave.", LevelKind.Tutorial, 1, 1, 101, 0f, 1, 1f),
            new LevelDefinition(2, "Tutorial", "Two cars, two easy exits.", LevelKind.Tutorial, 1, 2, 102, 0f, 2, 1f),
            new LevelDefinition(3, "Tutorial", "Clear the open lane first.", LevelKind.Tutorial, 1, 3, 103, 0f, 1, 1f),
            new LevelDefinition(4, "Easy", "Find the exits around the board.", LevelKind.Authored, 2, 2, 104, 0f, 2, 1f),
            new LevelDefinition(5, "Easy", "Lanes now cross - mind the traffic.", LevelKind.Authored, 3, 3, 105, .08f, 2, .75f),
            new LevelDefinition(6, "Easy", "Crossing paths block each other.", LevelKind.Authored, 3, 4, 106, .12f, 2, .75f),
            new LevelDefinition(7, "Easy", "Read crossings before you tap.", LevelKind.Authored, 4, 4, 107, .14f, 2, .55f),
            new LevelDefinition(8, "Easy", "Intersections decide the order.", LevelKind.Authored, 4, 5, 108, .16f, 2, .50f),
            new LevelDefinition(9, "Easy", "Busy crossroads ahead.", LevelKind.Authored, 5, 5, 109, .18f, 2, .50f),
            new LevelDefinition(10, "Challenge", "The busiest junction yet.", LevelKind.Challenge, 5, 6, 110, .20f, 2, .50f),
            new LevelDefinition(11, "Normal", "", LevelKind.Procedural, 6, 7, 111, .18f, 2, .45f),
            new LevelDefinition(12, "Normal", "", LevelKind.Procedural, 6, 7, 112, .20f, 2, .45f),
            new LevelDefinition(13, "Normal", "", LevelKind.Procedural, 6, 8, 113, .20f, 2, .45f),
            new LevelDefinition(14, "Normal", "", LevelKind.Procedural, 6, 8, 114, .22f, 2, .45f),
            new LevelDefinition(15, "Normal", "", LevelKind.Procedural, 6, 8, 115, .22f, 2, .45f),
            new LevelDefinition(16, "Normal", "", LevelKind.Procedural, 6, 8, 116, .24f, 2, .45f),
            new LevelDefinition(17, "Normal", "", LevelKind.Procedural, 6, 8, 117, .24f, 2, .45f),
            new LevelDefinition(18, "Normal", "", LevelKind.Procedural, 6, 8, 118, .24f, 2, .45f),
            new LevelDefinition(19, "Normal", "", LevelKind.Procedural, 6, 8, 119, .25f, 2, .45f),
            new LevelDefinition(20, "Normal", "", LevelKind.Procedural, 6, 8, 120, .25f, 2, .45f),
            new LevelDefinition(21, "Hard", "", LevelKind.Procedural, 6, 8, 121, .28f, 2, .45f),
            new LevelDefinition(22, "Hard", "", LevelKind.Procedural, 6, 8, 122, .28f, 2, .45f),
            new LevelDefinition(23, "Hard", "Development showcase", LevelKind.Challenge, 6, 8, 260816, .15f, 2, .45f)
        };

        public static LevelDefinition Get(int levelId)
        {
            if (levelId < 1 || levelId > Definitions.Length) throw new ArgumentOutOfRangeException(nameof(levelId));
            return Definitions[levelId - 1];
        }

        internal static Dictionary<GridCoordinate, IReadOnlyList<GridCoordinate>> GetAuthoredRoutes(
            int levelId,
            out Dictionary<GridCoordinate, ArrowDirection> exitDirections)
        {
            exitDirections = new Dictionary<GridCoordinate, ArrowDirection>();
            var routes = new Dictionary<GridCoordinate, IReadOnlyList<GridCoordinate>>();

            switch (levelId)
            {
                case 1:
                    routes[new GridCoordinate(0, 0)] = new[] { new GridCoordinate(0, 0) };
                    exitDirections[new GridCoordinate(0, 0)] = ArrowDirection.Left;
                    return routes;

                case 2:
                    routes[new GridCoordinate(0, 0)] = new[] { new GridCoordinate(0, 0) };
                    exitDirections[new GridCoordinate(0, 0)] = ArrowDirection.Left;
                    routes[new GridCoordinate(0, 1)] = new[] { new GridCoordinate(0, 1) };
                    exitDirections[new GridCoordinate(0, 1)] = ArrowDirection.Right;
                    return routes;

                case 3:
                    routes[new GridCoordinate(0, 0)] = new[] { new GridCoordinate(0, 0), new GridCoordinate(0, 1), new GridCoordinate(0, 2) };
                    exitDirections[new GridCoordinate(0, 0)] = ArrowDirection.Right;
                    routes[new GridCoordinate(0, 1)] = new[] { new GridCoordinate(0, 1), new GridCoordinate(0, 2) };
                    exitDirections[new GridCoordinate(0, 1)] = ArrowDirection.Right;
                    routes[new GridCoordinate(0, 2)] = new[] { new GridCoordinate(0, 2) };
                    exitDirections[new GridCoordinate(0, 2)] = ArrowDirection.Right;
                    return routes;

                case 4:
                    // 2x2 with bent routes sharing exit corridors
                    routes[new GridCoordinate(0, 0)] = new[] { new GridCoordinate(0, 0) };
                    exitDirections[new GridCoordinate(0, 0)] = ArrowDirection.Up;

                    routes[new GridCoordinate(0, 1)] = new[] { new GridCoordinate(0, 1), new GridCoordinate(0, 0) };
                    exitDirections[new GridCoordinate(0, 1)] = ArrowDirection.Up;

                    routes[new GridCoordinate(1, 0)] = new[] { new GridCoordinate(1, 0), new GridCoordinate(0, 0) };
                    exitDirections[new GridCoordinate(1, 0)] = ArrowDirection.Up;

                    routes[new GridCoordinate(1, 1)] = new[] { new GridCoordinate(1, 1), new GridCoordinate(1, 0), new GridCoordinate(0, 0) };
                    exitDirections[new GridCoordinate(1, 1)] = ArrowDirection.Up;
                    return routes;

                case 5:
                    // 3x3 with shared crossing corridors and turns
                    routes[new GridCoordinate(0, 2)] = new[] { new GridCoordinate(0, 2) };
                    exitDirections[new GridCoordinate(0, 2)] = ArrowDirection.Up;

                    routes[new GridCoordinate(2, 0)] = new[] { new GridCoordinate(2, 0) };
                    exitDirections[new GridCoordinate(2, 0)] = ArrowDirection.Left;

                    routes[new GridCoordinate(0, 1)] = new[] { new GridCoordinate(0, 1), new GridCoordinate(0, 2) };
                    exitDirections[new GridCoordinate(0, 1)] = ArrowDirection.Up;

                    routes[new GridCoordinate(0, 0)] = new[] { new GridCoordinate(0, 0), new GridCoordinate(0, 1), new GridCoordinate(0, 2) };
                    exitDirections[new GridCoordinate(0, 0)] = ArrowDirection.Up;

                    routes[new GridCoordinate(1, 1)] = new[] { new GridCoordinate(1, 1), new GridCoordinate(0, 1), new GridCoordinate(0, 2) };
                    exitDirections[new GridCoordinate(1, 1)] = ArrowDirection.Up;

                    routes[new GridCoordinate(1, 2)] = new[] { new GridCoordinate(1, 2), new GridCoordinate(0, 2) };
                    exitDirections[new GridCoordinate(1, 2)] = ArrowDirection.Up;

                    routes[new GridCoordinate(2, 1)] = new[] { new GridCoordinate(2, 1), new GridCoordinate(2, 0) };
                    exitDirections[new GridCoordinate(2, 1)] = ArrowDirection.Left;

                    routes[new GridCoordinate(2, 2)] = new[] { new GridCoordinate(2, 2), new GridCoordinate(2, 1), new GridCoordinate(2, 0) };
                    exitDirections[new GridCoordinate(2, 2)] = ArrowDirection.Left;

                    routes[new GridCoordinate(1, 0)] = new[] { new GridCoordinate(1, 0), new GridCoordinate(2, 0) };
                    exitDirections[new GridCoordinate(1, 0)] = ArrowDirection.Left;
                    return routes;

                case 6:
                    // 3x4 with bent routes, shared corridors, and T-junctions
                    routes[new GridCoordinate(0, 3)] = new[] { new GridCoordinate(0, 3) };
                    exitDirections[new GridCoordinate(0, 3)] = ArrowDirection.Up;

                    routes[new GridCoordinate(2, 0)] = new[] { new GridCoordinate(2, 0) };
                    exitDirections[new GridCoordinate(2, 0)] = ArrowDirection.Left;

                    routes[new GridCoordinate(0, 2)] = new[] { new GridCoordinate(0, 2), new GridCoordinate(0, 3) };
                    exitDirections[new GridCoordinate(0, 2)] = ArrowDirection.Up;

                    routes[new GridCoordinate(0, 1)] = new[] { new GridCoordinate(0, 1), new GridCoordinate(0, 2), new GridCoordinate(0, 3) };
                    exitDirections[new GridCoordinate(0, 1)] = ArrowDirection.Up;

                    routes[new GridCoordinate(0, 0)] = new[] { new GridCoordinate(0, 0), new GridCoordinate(0, 1), new GridCoordinate(0, 2), new GridCoordinate(0, 3) };
                    exitDirections[new GridCoordinate(0, 0)] = ArrowDirection.Up;

                    routes[new GridCoordinate(1, 2)] = new[] { new GridCoordinate(1, 2), new GridCoordinate(0, 2), new GridCoordinate(0, 3) };
                    exitDirections[new GridCoordinate(1, 2)] = ArrowDirection.Up;

                    routes[new GridCoordinate(1, 3)] = new[] { new GridCoordinate(1, 3), new GridCoordinate(0, 3) };
                    exitDirections[new GridCoordinate(1, 3)] = ArrowDirection.Up;

                    routes[new GridCoordinate(2, 1)] = new[] { new GridCoordinate(2, 1), new GridCoordinate(2, 0) };
                    exitDirections[new GridCoordinate(2, 1)] = ArrowDirection.Left;

                    routes[new GridCoordinate(2, 2)] = new[] { new GridCoordinate(2, 2), new GridCoordinate(2, 1), new GridCoordinate(2, 0) };
                    exitDirections[new GridCoordinate(2, 2)] = ArrowDirection.Left;

                    routes[new GridCoordinate(2, 3)] = new[] { new GridCoordinate(2, 3), new GridCoordinate(2, 2), new GridCoordinate(2, 1), new GridCoordinate(2, 0) };
                    exitDirections[new GridCoordinate(2, 3)] = ArrowDirection.Left;

                    routes[new GridCoordinate(1, 1)] = new[] { new GridCoordinate(1, 1), new GridCoordinate(2, 1), new GridCoordinate(2, 0) };
                    exitDirections[new GridCoordinate(1, 1)] = ArrowDirection.Left;

                    routes[new GridCoordinate(1, 0)] = new[] { new GridCoordinate(1, 0), new GridCoordinate(2, 0) };
                    exitDirections[new GridCoordinate(1, 0)] = ArrowDirection.Left;
                    return routes;

                case 7:
                    // 4x4 with bent routes, crossroads and arterial avenues
                    routes[new GridCoordinate(0, 3)] = new[] { new GridCoordinate(0, 3) };
                    exitDirections[new GridCoordinate(0, 3)] = ArrowDirection.Up;

                    routes[new GridCoordinate(3, 0)] = new[] { new GridCoordinate(3, 0) };
                    exitDirections[new GridCoordinate(3, 0)] = ArrowDirection.Left;

                    routes[new GridCoordinate(0, 2)] = new[] { new GridCoordinate(0, 2), new GridCoordinate(0, 3) };
                    exitDirections[new GridCoordinate(0, 2)] = ArrowDirection.Up;

                    routes[new GridCoordinate(0, 1)] = new[] { new GridCoordinate(0, 1), new GridCoordinate(0, 2), new GridCoordinate(0, 3) };
                    exitDirections[new GridCoordinate(0, 1)] = ArrowDirection.Up;

                    routes[new GridCoordinate(1, 1)] = new[] { new GridCoordinate(1, 1), new GridCoordinate(0, 1), new GridCoordinate(0, 2), new GridCoordinate(0, 3) };
                    exitDirections[new GridCoordinate(1, 1)] = ArrowDirection.Up;

                    routes[new GridCoordinate(3, 1)] = new[] { new GridCoordinate(3, 1), new GridCoordinate(3, 0) };
                    exitDirections[new GridCoordinate(3, 1)] = ArrowDirection.Left;

                    routes[new GridCoordinate(3, 2)] = new[] { new GridCoordinate(3, 2), new GridCoordinate(3, 1), new GridCoordinate(3, 0) };
                    exitDirections[new GridCoordinate(3, 2)] = ArrowDirection.Left;

                    routes[new GridCoordinate(2, 1)] = new[] { new GridCoordinate(2, 1), new GridCoordinate(3, 1), new GridCoordinate(3, 0) };
                    exitDirections[new GridCoordinate(2, 1)] = ArrowDirection.Left;

                    routes[new GridCoordinate(2, 2)] = new[] { new GridCoordinate(2, 2), new GridCoordinate(2, 1), new GridCoordinate(3, 1), new GridCoordinate(3, 0) };
                    exitDirections[new GridCoordinate(2, 2)] = ArrowDirection.Left;
                    return routes;

                case 8:
                    // 4x5 with bent routes and shared arterial corridors
                    routes[new GridCoordinate(0, 4)] = new[] { new GridCoordinate(0, 4) };
                    exitDirections[new GridCoordinate(0, 4)] = ArrowDirection.Right;

                    routes[new GridCoordinate(0, 3)] = new[] { new GridCoordinate(0, 3), new GridCoordinate(0, 4) };
                    exitDirections[new GridCoordinate(0, 3)] = ArrowDirection.Right;

                    routes[new GridCoordinate(0, 2)] = new[] { new GridCoordinate(0, 2), new GridCoordinate(0, 3), new GridCoordinate(0, 4) };
                    exitDirections[new GridCoordinate(0, 2)] = ArrowDirection.Right;

                    routes[new GridCoordinate(0, 1)] = new[] { new GridCoordinate(0, 1), new GridCoordinate(0, 2), new GridCoordinate(0, 3), new GridCoordinate(0, 4) };
                    exitDirections[new GridCoordinate(0, 1)] = ArrowDirection.Right;

                    routes[new GridCoordinate(1, 2)] = new[] { new GridCoordinate(1, 2), new GridCoordinate(0, 2), new GridCoordinate(0, 3), new GridCoordinate(0, 4) };
                    exitDirections[new GridCoordinate(1, 2)] = ArrowDirection.Right;

                    routes[new GridCoordinate(3, 0)] = new[] { new GridCoordinate(3, 0) };
                    exitDirections[new GridCoordinate(3, 0)] = ArrowDirection.Left;

                    routes[new GridCoordinate(3, 1)] = new[] { new GridCoordinate(3, 1), new GridCoordinate(3, 0) };
                    exitDirections[new GridCoordinate(3, 1)] = ArrowDirection.Left;

                    routes[new GridCoordinate(3, 2)] = new[] { new GridCoordinate(3, 2), new GridCoordinate(3, 1), new GridCoordinate(3, 0) };
                    exitDirections[new GridCoordinate(3, 2)] = ArrowDirection.Left;

                    routes[new GridCoordinate(3, 3)] = new[] { new GridCoordinate(3, 3), new GridCoordinate(3, 2), new GridCoordinate(3, 1), new GridCoordinate(3, 0) };
                    exitDirections[new GridCoordinate(3, 3)] = ArrowDirection.Left;

                    routes[new GridCoordinate(2, 2)] = new[] { new GridCoordinate(2, 2), new GridCoordinate(3, 2), new GridCoordinate(3, 1), new GridCoordinate(3, 0) };
                    exitDirections[new GridCoordinate(2, 2)] = ArrowDirection.Left;
                    return routes;

                case 9:
                    // 5x5 with dense network, shared multi-cell segments, and junctions
                    routes[new GridCoordinate(0, 4)] = new[] { new GridCoordinate(0, 4) };
                    exitDirections[new GridCoordinate(0, 4)] = ArrowDirection.Up;

                    routes[new GridCoordinate(0, 3)] = new[] { new GridCoordinate(0, 3), new GridCoordinate(0, 4) };
                    exitDirections[new GridCoordinate(0, 3)] = ArrowDirection.Up;

                    routes[new GridCoordinate(0, 2)] = new[] { new GridCoordinate(0, 2), new GridCoordinate(0, 3), new GridCoordinate(0, 4) };
                    exitDirections[new GridCoordinate(0, 2)] = ArrowDirection.Up;

                    routes[new GridCoordinate(0, 1)] = new[] { new GridCoordinate(0, 1), new GridCoordinate(0, 2), new GridCoordinate(0, 3), new GridCoordinate(0, 4) };
                    exitDirections[new GridCoordinate(0, 1)] = ArrowDirection.Up;

                    routes[new GridCoordinate(1, 2)] = new[] { new GridCoordinate(1, 2), new GridCoordinate(0, 2), new GridCoordinate(0, 3), new GridCoordinate(0, 4) };
                    exitDirections[new GridCoordinate(1, 2)] = ArrowDirection.Up;

                    routes[new GridCoordinate(2, 2)] = new[] { new GridCoordinate(2, 2), new GridCoordinate(1, 2), new GridCoordinate(0, 2), new GridCoordinate(0, 3), new GridCoordinate(0, 4) };
                    exitDirections[new GridCoordinate(2, 2)] = ArrowDirection.Up;

                    routes[new GridCoordinate(4, 0)] = new[] { new GridCoordinate(4, 0) };
                    exitDirections[new GridCoordinate(4, 0)] = ArrowDirection.Left;

                    routes[new GridCoordinate(4, 1)] = new[] { new GridCoordinate(4, 1), new GridCoordinate(4, 0) };
                    exitDirections[new GridCoordinate(4, 1)] = ArrowDirection.Left;

                    routes[new GridCoordinate(4, 2)] = new[] { new GridCoordinate(4, 2), new GridCoordinate(4, 1), new GridCoordinate(4, 0) };
                    exitDirections[new GridCoordinate(4, 2)] = ArrowDirection.Left;

                    routes[new GridCoordinate(4, 3)] = new[] { new GridCoordinate(4, 3), new GridCoordinate(4, 2), new GridCoordinate(4, 1), new GridCoordinate(4, 0) };
                    exitDirections[new GridCoordinate(4, 3)] = ArrowDirection.Left;

                    routes[new GridCoordinate(3, 2)] = new[] { new GridCoordinate(3, 2), new GridCoordinate(4, 2), new GridCoordinate(4, 1), new GridCoordinate(4, 0) };
                    exitDirections[new GridCoordinate(3, 2)] = ArrowDirection.Left;

                    routes[new GridCoordinate(2, 3)] = new[] { new GridCoordinate(2, 3), new GridCoordinate(3, 3), new GridCoordinate(4, 3), new GridCoordinate(4, 2), new GridCoordinate(4, 1), new GridCoordinate(4, 0) };
                    exitDirections[new GridCoordinate(2, 3)] = ArrowDirection.Left;
                    return routes;

                case 10:
                    // 5x6 Challenge with dense interconnected multi-segment corridors
                    routes[new GridCoordinate(0, 5)] = new[] { new GridCoordinate(0, 5) };
                    exitDirections[new GridCoordinate(0, 5)] = ArrowDirection.Right;

                    routes[new GridCoordinate(0, 4)] = new[] { new GridCoordinate(0, 4), new GridCoordinate(0, 5) };
                    exitDirections[new GridCoordinate(0, 4)] = ArrowDirection.Right;

                    routes[new GridCoordinate(0, 3)] = new[] { new GridCoordinate(0, 3), new GridCoordinate(0, 4), new GridCoordinate(0, 5) };
                    exitDirections[new GridCoordinate(0, 3)] = ArrowDirection.Right;

                    routes[new GridCoordinate(0, 2)] = new[] { new GridCoordinate(0, 2), new GridCoordinate(0, 3), new GridCoordinate(0, 4), new GridCoordinate(0, 5) };
                    exitDirections[new GridCoordinate(0, 2)] = ArrowDirection.Right;

                    routes[new GridCoordinate(0, 1)] = new[] { new GridCoordinate(0, 1), new GridCoordinate(0, 2), new GridCoordinate(0, 3), new GridCoordinate(0, 4), new GridCoordinate(0, 5) };
                    exitDirections[new GridCoordinate(0, 1)] = ArrowDirection.Right;

                    routes[new GridCoordinate(1, 3)] = new[] { new GridCoordinate(1, 3), new GridCoordinate(0, 3), new GridCoordinate(0, 4), new GridCoordinate(0, 5) };
                    exitDirections[new GridCoordinate(1, 3)] = ArrowDirection.Right;

                    routes[new GridCoordinate(2, 3)] = new[] { new GridCoordinate(2, 3), new GridCoordinate(1, 3), new GridCoordinate(0, 3), new GridCoordinate(0, 4), new GridCoordinate(0, 5) };
                    exitDirections[new GridCoordinate(2, 3)] = ArrowDirection.Right;

                    routes[new GridCoordinate(4, 0)] = new[] { new GridCoordinate(4, 0) };
                    exitDirections[new GridCoordinate(4, 0)] = ArrowDirection.Left;

                    routes[new GridCoordinate(4, 1)] = new[] { new GridCoordinate(4, 1), new GridCoordinate(4, 0) };
                    exitDirections[new GridCoordinate(4, 1)] = ArrowDirection.Left;

                    routes[new GridCoordinate(4, 2)] = new[] { new GridCoordinate(4, 2), new GridCoordinate(4, 1), new GridCoordinate(4, 0) };
                    exitDirections[new GridCoordinate(4, 2)] = ArrowDirection.Left;

                    routes[new GridCoordinate(4, 3)] = new[] { new GridCoordinate(4, 3), new GridCoordinate(4, 2), new GridCoordinate(4, 1), new GridCoordinate(4, 0) };
                    exitDirections[new GridCoordinate(4, 3)] = ArrowDirection.Left;

                    routes[new GridCoordinate(4, 4)] = new[] { new GridCoordinate(4, 4), new GridCoordinate(4, 3), new GridCoordinate(4, 2), new GridCoordinate(4, 1), new GridCoordinate(4, 0) };
                    exitDirections[new GridCoordinate(4, 4)] = ArrowDirection.Left;

                    routes[new GridCoordinate(3, 2)] = new[] { new GridCoordinate(3, 2), new GridCoordinate(4, 2), new GridCoordinate(4, 1), new GridCoordinate(4, 0) };
                    exitDirections[new GridCoordinate(3, 2)] = ArrowDirection.Left;

                    routes[new GridCoordinate(2, 2)] = new[] { new GridCoordinate(2, 2), new GridCoordinate(3, 2), new GridCoordinate(4, 2), new GridCoordinate(4, 1), new GridCoordinate(4, 0) };
                    exitDirections[new GridCoordinate(2, 2)] = ArrowDirection.Left;

                    routes[new GridCoordinate(3, 4)] = new[] { new GridCoordinate(3, 4), new GridCoordinate(4, 4), new GridCoordinate(4, 3), new GridCoordinate(4, 2), new GridCoordinate(4, 1), new GridCoordinate(4, 0) };
                    exitDirections[new GridCoordinate(3, 4)] = ArrowDirection.Left;
                    return routes;

                default:
                    return null;
            }
        }

        internal static ArrowDirection[,] GetAuthoredDirections(int levelId)
        {
            return null;
        }

        internal static bool[,] FilledCars(ArrowDirection[,] directions)
        {
            if (directions == null) return null;
            var cars = new bool[directions.GetLength(0), directions.GetLength(1)];
            for (var row = 0; row < cars.GetLength(0); row++)
            for (var column = 0; column < cars.GetLength(1); column++) cars[row, column] = true;
            return cars;
        }
    }
}
