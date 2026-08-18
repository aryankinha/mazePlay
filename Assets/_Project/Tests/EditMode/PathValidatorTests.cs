using ArrowMaze.Core;
using NUnit.Framework;

namespace ArrowMaze.Tests
{
    public sealed class PathValidatorTests
    {
        [Test]
        public void BoundaryArrow_IsLegalWhenItsStraightPathExits()
        {
            var validator = new PathValidator(MazeLevel.FromDirections(new[,]
            {
                { ArrowDirection.Up, ArrowDirection.Left },
                { ArrowDirection.Up, ArrowDirection.Right }
            }));

            Assert.That(validator.IsLegalTap(new GridCoordinate(0, 0)), Is.True);
            Assert.That(validator.RegisterTap(new GridCoordinate(0, 0)), Is.True);
        }

        [Test]
        public void ActiveTileInStraightLine_BlocksUntilItIsCleared()
        {
            var validator = new PathValidator(MazeLevel.FromDirections(new[,]
            {
                { ArrowDirection.Right, ArrowDirection.Up },
                { ArrowDirection.Up, ArrowDirection.Up }
            }));

            var left = new GridCoordinate(0, 0);
            var middle = new GridCoordinate(0, 1);
            Assert.That(validator.IsLegalTap(left), Is.False);
            Assert.That(validator.RegisterTap(left), Is.False);

            Assert.That(validator.RegisterTap(middle), Is.True);
            Assert.That(validator.IsLegalTap(left), Is.True);
        }

        [Test]
        public void FullClear_RaisesCompletionOnlyAfterEveryTileIsCleared()
        {
            var validator = new PathValidator(MazeLevel.FromDirections(new[,]
            {
                { ArrowDirection.Up, ArrowDirection.Up },
                { ArrowDirection.Down, ArrowDirection.Down }
            }));
            var completed = false;
            validator.OnLevelCompleted += () => completed = true;

            Assert.That(validator.RegisterTap(new GridCoordinate(0, 0)), Is.True);
            Assert.That(validator.RegisterTap(new GridCoordinate(0, 1)), Is.True);
            Assert.That(validator.RegisterTap(new GridCoordinate(1, 0)), Is.True);
            Assert.That(completed, Is.False);
            Assert.That(validator.RegisterTap(new GridCoordinate(1, 1)), Is.True);
            Assert.That(completed, Is.True);
            Assert.That(validator.ClearedCount, Is.EqualTo(4));
        }

        [Test]
        public void EmptyRoads_DoNotBlockCarsAndSolverUsesTheSameBoardState()
        {
            var level = MazeLevel.FromDirections(
                new[,]
                {
                    { ArrowDirection.Right, ArrowDirection.Right, ArrowDirection.Right },
                    { ArrowDirection.Up, ArrowDirection.Up, ArrowDirection.Up }
                },
                new[,]
                {
                    { true, false, false },
                    { false, false, true }
                });
            var validator = new PathValidator(level);

            Assert.That(level.CarCount, Is.EqualTo(2));
            Assert.That(validator.IsLegalTap(new GridCoordinate(0, 0)), Is.True,
                "The empty middle road must be passable to the left car.");

            var solve = ChainPuzzleSolver.TrySolve(level);
            Assert.That(solve.IsSolved, Is.True);
            Assert.That(solve.ClearOrder.Count, Is.EqualTo(2));
        }
    }
}
