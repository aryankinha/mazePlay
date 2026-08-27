using System;
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
            var authored = LevelCatalog.GetAuthoredDirections(Id);
            if (authored != null)
            {
                // Authored layouts start as clean ring/spoke designs; the interlink
                // pass rotates cars into genuine crossings while preserving a proven
                // solution, keeping the teaching intent but producing a real maze.
                var baseLevel = MazeLevel.FromDirections(authored, LevelCatalog.FilledCars(authored));
                var solved = ChainPuzzleSolver.TrySolve(baseLevel);
                return MazeGenerator.RaiseInterlinking(
                    baseLevel,
                    solved.ClearOrder,
                    Id * 48271 + 11,
                    minimumSharedFraction: 0.55f,
                    minimumCrossFraction: 0.45f);
            }

            return MazeGenerator.Generate(new MazeGenerationSettings(
                Rows, Columns, Seed, TrapDensity, BranchingFactor, carDensity: CarDensity));
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
            new LevelDefinition(5, "Easy", "Lanes now cross - mind the traffic.", LevelKind.Authored, 3, 3, 105, .08f, 2, .5f),
            new LevelDefinition(6, "Easy", "Crossing paths block each other.", LevelKind.Authored, 3, 4, 106, .12f, 2, .5f),
            new LevelDefinition(7, "Easy", "Read crossings before you tap.", LevelKind.Authored, 4, 4, 107, .14f, 2, .48f),
            new LevelDefinition(8, "Easy", "Intersections decide the order.", LevelKind.Authored, 4, 5, 108, .16f, 2, .46f),
            new LevelDefinition(9, "Easy", "Busy crossroads ahead.", LevelKind.Authored, 5, 5, 109, .18f, 2, .48f),
            new LevelDefinition(10, "Challenge", "The busiest junction yet.", LevelKind.Challenge, 5, 6, 110, .20f, 2, .48f),
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

        internal static ArrowDirection[,] GetAuthoredDirections(int levelId)
        {
            switch (levelId)
            {
                case 1: return new[,] { { ArrowDirection.Left } };
                case 2: return new[,] { { ArrowDirection.Left, ArrowDirection.Right } };
                case 3: return new[,] { { ArrowDirection.Right, ArrowDirection.Right, ArrowDirection.Right } };
                case 4: return new[,] { { ArrowDirection.Up, ArrowDirection.Right }, { ArrowDirection.Down, ArrowDirection.Left } };
                case 5: return new[,] {
                    { ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up },
                    { ArrowDirection.Left, ArrowDirection.Up, ArrowDirection.Right },
                    { ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down } };
                case 6: return new[,] {
                    { ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up },
                    { ArrowDirection.Left, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Right },
                    { ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down } };
                case 7: return new[,] {
                    { ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up },
                    { ArrowDirection.Left, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Right },
                    { ArrowDirection.Left, ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Right },
                    { ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down } };
                case 8: return new[,] {
                    { ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up },
                    { ArrowDirection.Left, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Right },
                    { ArrowDirection.Left, ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Right },
                    { ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down } };
                case 9: return new[,] {
                    { ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up },
                    { ArrowDirection.Left, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Right },
                    { ArrowDirection.Left, ArrowDirection.Left, ArrowDirection.Up, ArrowDirection.Right, ArrowDirection.Right },
                    { ArrowDirection.Left, ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Right },
                    { ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down } };
                case 10: return new[,] {
                    { ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up },
                    { ArrowDirection.Left, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Right },
                    { ArrowDirection.Left, ArrowDirection.Left, ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Right, ArrowDirection.Right },
                    { ArrowDirection.Left, ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Right },
                    { ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down, ArrowDirection.Down } };
                default: return null;
            }
        }

        internal static bool[,] FilledCars(ArrowDirection[,] directions)
        {
            var cars = new bool[directions.GetLength(0), directions.GetLength(1)];
            for (var row = 0; row < cars.GetLength(0); row++)
            for (var column = 0; column < cars.GetLength(1); column++) cars[row, column] = true;
            return cars;
        }
    }
}
