using System;
using System.Collections.Generic;
using ArrowMaze.Core;
using ArrowMaze.Data;
using NUnit.Framework;

namespace ArrowMaze.Tests
{
    /// <summary>
    /// Regression coverage for the maze interlinking requirement: a legal board is
    /// only a maze if cars' straight-line routes actually share and cross cells.
    /// Levels whose paths form isolated private lanes must be rejected.
    /// </summary>
    public sealed class InterlinkTopologyTests
    {
        private const float MinSharedFraction = 0.40f;
        private const float MinCrossFraction = 0.15f;

        [Test]
        public void NonTutorialCatalogLevels_AreInterlinkedMazes_AndSolvable()
        {
            for (var levelId = 4; levelId <= LevelCatalog.HighestCatalogLevel; levelId++)
            {
                var definition = LevelCatalog.Get(levelId);
                var level = definition.BuildLevel();

                var solveResult = ChainPuzzleSolver.TrySolve(level);
                Assert.That(solveResult.IsSolved, Is.True, $"Level {levelId} is unsolvable.");
                Assert.That(solveResult.HitSearchLimit, Is.False, $"Level {levelId} exhausted the solver budget.");

                var fractions = ComputeInterlinkFractions(level);
                Assert.That(fractions.SharedFraction, Is.GreaterThanOrEqualTo(MinSharedFraction),
                    $"Level {levelId} shares too few path cells between cars ({fractions.SharedFraction:P1}).");
                Assert.That(fractions.CrossFraction, Is.GreaterThanOrEqualTo(MinCrossFraction),
                    $"Level {levelId} has almost no true path crossings ({fractions.CrossFraction:P1}); it reads as separate lanes.");
            }
        }

        [Test]
        public void FreshGeneratedLevels_AreInterlinkedMazes_AndSolvable()
        {
            for (var sample = 0; sample < 5; sample++)
            {
                var settings = new MazeGenerationSettings(
                    rows: 6,
                    columns: 8,
                    seed: 52000 + sample * 911,
                    trapDensity: 0.24f,
                    targetStartingBranchingFactor: 2,
                    carDensity: 0.45f);
                var level = MazeGenerator.Generate(settings);

                var solveResult = ChainPuzzleSolver.TrySolve(level);
                Assert.That(solveResult.IsSolved, Is.True, $"Sample {sample} is unsolvable.");
                Assert.That(solveResult.HitSearchLimit, Is.False, $"Sample {sample} exhausted the solver budget.");

                var fractions = ComputeInterlinkFractions(level);
                Assert.That(fractions.SharedFraction, Is.GreaterThanOrEqualTo(MinSharedFraction),
                    $"Sample {sample} shares too few path cells between cars ({fractions.SharedFraction:P1}).");
                Assert.That(fractions.CrossFraction, Is.GreaterThanOrEqualTo(MinCrossFraction),
                    $"Sample {sample} has almost no true path crossings ({fractions.CrossFraction:P1}).");
            }
        }

        internal static (float SharedFraction, float CrossFraction) ComputeInterlinkFractions(MazeLevel level)
        {
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

            if (cars.Count == 0)
            {
                return (1f, 1f);
            }

            var paths = new List<GridCoordinate>[cars.Count];
            var horizontal = new bool[cars.Count];
            for (var index = 0; index < cars.Count; index++)
            {
                paths[index] = TracePath(level, cars[index]);
                var direction = level.GetDirection(cars[index]);
                horizontal[index] = direction == ArrowDirection.Left || direction == ArrowDirection.Right;
            }

            var sharedCount = 0;
            var crossCount = 0;
            for (var a = 0; a < cars.Count; a++)
            {
                var sharesAny = false;
                var crossesAny = false;
                for (var b = 0; b < cars.Count; b++)
                {
                    if (a == b)
                    {
                        continue;
                    }

                    if (!PathsTouch(paths[a], paths[b]))
                    {
                        continue;
                    }

                    sharesAny = true;
                    if (horizontal[a] != horizontal[b])
                    {
                        crossesAny = true;
                    }
                }

                if (sharesAny)
                {
                    sharedCount++;
                }

                if (crossesAny)
                {
                    crossCount++;
                }
            }

            return ((float)sharedCount / cars.Count, (float)crossCount / cars.Count);
        }

        private static List<GridCoordinate> TracePath(MazeLevel level, GridCoordinate origin)
        {
            var direction = level.GetDirection(origin);
            var path = new List<GridCoordinate> { origin };
            var current = origin;
            while (true)
            {
                current = StraightLineLegality.Move(current, direction);
                if (!level.IsInBounds(current))
                {
                    return path;
                }

                path.Add(current);
            }
        }

        private static bool PathsTouch(List<GridCoordinate> a, List<GridCoordinate> b)
        {
            foreach (var cell in a)
            {
                for (var index = 0; index < b.Count; index++)
                {
                    if (b[index] == cell)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
