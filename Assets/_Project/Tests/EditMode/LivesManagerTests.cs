using ArrowMaze.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace ArrowMaze.Tests
{
    public sealed class LivesManagerTests
    {
        [Test]
        public void LoseLife_DecrementsExactlyOnceAndEndsAtZero()
        {
            var gameObject = new GameObject("LivesManager Test");
            try
            {
                var lives = gameObject.AddComponent<LivesManager>();
                var gameOverCount = 0;
                lives.OnGameOver += () => gameOverCount++;

                lives.ResetLives();
                lives.LoseLife();
                Assert.That(lives.CurrentLives, Is.EqualTo(2));
                Assert.That(gameOverCount, Is.Zero);

                lives.LoseLife();
                lives.LoseLife();
                Assert.That(lives.CurrentLives, Is.Zero);
                Assert.That(gameOverCount, Is.EqualTo(1));

                lives.LoseLife();
                Assert.That(lives.CurrentLives, Is.Zero);
                Assert.That(gameOverCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
