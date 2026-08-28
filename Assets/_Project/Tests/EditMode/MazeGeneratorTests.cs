using ArrowMaze.Core;
using NUnit.Framework;

namespace ArrowMaze.Tests
{
    public sealed class MazeGeneratorTests
    {
        [Test]
        public void GeneratedMazes_AreSolvableAcrossOneHundredSeeds()
        {
            for (var seed = 0; seed < 100; seed++)
            {
                var level = MazeGenerator.Generate(new MazeGenerationSettings(
                    rows: 6,
                    columns: 8,
                    seed: seed,
                    trapDensity: 0.10f,
                    targetStartingBranchingFactor: 2));
                var solveResult = ChainPuzzleSolver.TrySolve(level);

                Assert.That(solveResult.IsSolved, Is.True, $"Seed {seed} produced an unsolvable grid.");
                Assert.That(solveResult.HitSearchLimit, Is.False, $"Seed {seed} exhausted the solver budget.");
                Assert.That(solveResult.ClearOrder.Count, Is.EqualTo(level.CarCount));
            }
        }

        [Test]
        public void GeneratedMaze_RespectsRequestedDimensionsAndBranchingTarget()
        {
            var level = MazeGenerator.Generate(new MazeGenerationSettings(
                rows: 6,
                columns: 8,
                seed: 12345,
                trapDensity: 0.10f,
                targetStartingBranchingFactor: 2));

            Assert.That(level.Rows, Is.EqualTo(6));
            Assert.That(level.Columns, Is.EqualTo(8));
            Assert.That(level.InitialLegalTapCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(level.ConstructionOrder.Count, Is.EqualTo(level.CarCount));
        }

        [Test]
        public void GeneratedMaze_WithTrapDensity_HasInitialIllegalTrapTiles()
        {
            var level = MazeGenerator.Generate(new MazeGenerationSettings(
                rows: 6,
                columns: 8,
                seed: 24680,
                trapDensity: 0.10f,
                targetStartingBranchingFactor: 2));
            var initialCleared = level.CreateInitialClearedState();

            Assert.That(level.TrapCoordinates.Count, Is.GreaterThan(0));
            foreach (var trap in level.TrapCoordinates)
            {
                Assert.That(level.HasCar(trap), Is.True);
                Assert.That(StraightLineLegality.IsLegalTap(level, initialCleared, trap), Is.False);
            }
        }

        [Test]
        public void SameSeed_AllowsTwoDifferentValidClearOrders()
        {
            var level = MazeGenerator.Generate(new MazeGenerationSettings(
                rows: 6,
                columns: 8,
                seed: 67890,
                trapDensity: 0.10f,
                targetStartingBranchingFactor: 2));
            var starts = StraightLineLegality.GetLegalTaps(level, level.CreateInitialClearedState());
            starts = new System.Collections.Generic.List<GridCoordinate>(starts).FindAll(level.HasCar);

            Assert.That(starts.Count, Is.GreaterThanOrEqualTo(2));
            var firstOrder = ChainPuzzleSolver.TrySolve(level, new[] { starts[0] });
            var secondOrder = ChainPuzzleSolver.TrySolve(level, new[] { starts[1] });

            Assert.That(firstOrder.IsSolved, Is.True);
            Assert.That(secondOrder.IsSolved, Is.True);
            Assert.That(firstOrder.ClearOrder[0], Is.Not.EqualTo(secondOrder.ClearOrder[0]));
        }
    }
}
