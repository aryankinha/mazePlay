using System;
using System.Collections.Generic;
using ArrowMaze.Core;
using ArrowMaze.Data;
using NUnit.Framework;

namespace ArrowMaze.Tests
{
    /// <summary>
    /// Regression coverage for the maze interlinking and bent-route requirement:
    /// Validates that cars navigate multi-segment bent routes through a connected road network,
    /// share extended road segments, pass through multi-route junctions, and are 100% solvable.
    /// </summary>
    public sealed class InterlinkTopologyTests
    {
        private const float MinSharedSegmentFraction = 0.20f;

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

                var metrics = MazeGenerator.ComputeTopologyMetrics(level);
                Assert.That(metrics.SharedSegmentFraction, Is.GreaterThanOrEqualTo(MinSharedSegmentFraction),
                    $"Level {levelId} has too few shared road segments ({metrics.SharedSegmentFraction:P1}).");
            }
        }

        [Test]
        public void FreshGeneratedLevels_AreInterlinkedMazes_AndSolvable()
        {
            for (var sample = 0; sample < 10; sample++)
            {
                var settings = new MazeGenerationSettings(
                    rows: 6,
                    columns: 8,
                    seed: 52000 + sample * 911,
                    trapDensity: 0.15f,
                    targetStartingBranchingFactor: 2,
                    carDensity: 0.45f,
                    minimumInterlinkFraction: 0.25f,
                    minimumJunctionCount: 1);
                var level = MazeGenerator.Generate(settings);

                var solveResult = ChainPuzzleSolver.TrySolve(level);
                Assert.That(solveResult.IsSolved, Is.True, $"Sample {sample} is unsolvable.");
                Assert.That(solveResult.HitSearchLimit, Is.False, $"Sample {sample} exhausted the solver budget.");

                var metrics = MazeGenerator.ComputeTopologyMetrics(level);
                Assert.That(metrics.SharedSegmentFraction, Is.GreaterThanOrEqualTo(MinSharedSegmentFraction),
                    $"Sample {sample} shares too few road segments ({metrics.SharedSegmentFraction:P1}).");
            }
        }

        [Test]
        public void CatalogLevel10_HasMultiSegmentCorridors_AndJunctions()
        {
            var level = LevelCatalog.Get(10).BuildLevel();
            var solveResult = ChainPuzzleSolver.TrySolve(level);
            Assert.That(solveResult.IsSolved, Is.True);

            var metrics = MazeGenerator.ComputeTopologyMetrics(level);
            Assert.That(metrics.SharedSegments, Is.GreaterThan(0));
            Assert.That(metrics.JunctionCount, Is.GreaterThan(0));
            Assert.That(metrics.CarSharingFraction, Is.GreaterThan(0.5f));
        }

        [Test]
        public void ExportDetailedTopologyDiagnostics()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== TOPOLOGY AUDIT: DETAILED METRICS PER LEVEL ===");

            foreach (var levelId in new[] { 5, 10, 23 })
            {
                var def = LevelCatalog.Get(levelId);
                var level = def.BuildLevel();
                var metrics = MazeGenerator.ComputeTopologyMetrics(level);
                var solved = ChainPuzzleSolver.TrySolve(level);

                sb.AppendLine($"\n--- LEVEL {levelId} ({def.Rows}x{def.Columns}, Kind={def.Kind}, Cars={level.CarCount}) ---");
                sb.AppendLine($"Solvable: {solved.IsSolved}, Steps: {solved.ClearOrder.Count}");
                sb.AppendLine($"Total Road Segments: {metrics.TotalSegments}");
                sb.AppendLine($"Shared Road Segments: {metrics.SharedSegments} ({metrics.SharedSegmentFraction:P1})");
                sb.AppendLine($"Junction Count: {metrics.JunctionCount}");
                sb.AppendLine($"Car Sharing Fraction: {metrics.CarSharingFraction:P1}");
                sb.AppendLine("Car Routes:");
                foreach (var kvp in level.Routes)
                {
                    var exitDir = level.GetExitDirection(kvp.Key);
                    var pathStr = string.Join(" -> ", kvp.Value);
                    sb.AppendLine($"  Car at {kvp.Key}: [{pathStr}] (Exit {exitDir})");
                }
            }

            System.IO.File.WriteAllText("Logs/topology-detailed-metrics.txt", sb.ToString());
            Assert.That(System.IO.File.Exists("Logs/topology-detailed-metrics.txt"), Is.True);
        }
    }
}
