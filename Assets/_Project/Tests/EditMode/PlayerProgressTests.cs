using ArrowMaze.Data;
using ArrowMaze.Core;
using ArrowMaze.Meta;
using NUnit.Framework;

namespace ArrowMaze.Tests
{
    public sealed class PlayerProgressTests
    {
        private const string SaveKey = "TapAwayCars.PlayerProgress.v1";
        private const string SelectedLevelKey = "TapAwayCars.SelectedLevel";
        private const string TutorialKey = "TapAwayCars.TutorialCompleted";
        private const string HapticsKey = "TapAwayCars.HapticsEnabled";
        private bool hadSave;
        private string previousSave;
        private PreferenceSnapshot selectedLevel;
        private PreferenceSnapshot tutorial;
        private PreferenceSnapshot haptics;

        [SetUp]
        public void SetUp()
        {
            hadSave = UnityEngine.PlayerPrefs.HasKey(SaveKey);
            previousSave = hadSave ? UnityEngine.PlayerPrefs.GetString(SaveKey) : null;
            selectedLevel = CaptureInt(SelectedLevelKey);
            tutorial = CaptureInt(TutorialKey);
            haptics = CaptureInt(HapticsKey);
            PlayerProgress.ResetForDevelopment();
        }

        [TearDown]
        public void TearDown()
        {
            if (hadSave)
            {
                UnityEngine.PlayerPrefs.SetString(SaveKey, previousSave);
            }
            else
            {
                UnityEngine.PlayerPrefs.DeleteKey(SaveKey);
            }

            RestoreInt(SelectedLevelKey, selectedLevel);
            RestoreInt(TutorialKey, tutorial);
            RestoreInt(HapticsKey, haptics);
            UnityEngine.PlayerPrefs.Save();
            PlayerProgress.ReloadFromDiskForTesting();
        }

        private static PreferenceSnapshot CaptureInt(string key)
        {
            return new PreferenceSnapshot(UnityEngine.PlayerPrefs.HasKey(key), UnityEngine.PlayerPrefs.GetInt(key));
        }

        private static void RestoreInt(string key, PreferenceSnapshot snapshot)
        {
            if (snapshot.Exists)
            {
                UnityEngine.PlayerPrefs.SetInt(key, snapshot.Value);
            }
            else
            {
                UnityEngine.PlayerPrefs.DeleteKey(key);
            }
        }

        private readonly struct PreferenceSnapshot
        {
            public PreferenceSnapshot(bool exists, int value)
            {
                Exists = exists;
                Value = value;
            }

            public bool Exists { get; }
            public int Value { get; }
        }

        [Test]
        public void NewPlayer_OnlyLevelOneIsUnlocked()
        {
            Assert.That(PlayerProgress.IsUnlocked(1), Is.True);
            Assert.That(PlayerProgress.IsUnlocked(2), Is.False);
            Assert.That(PlayerProgress.GetContinueLevel(), Is.EqualTo(1));
        }

        [Test]
        public void CompletingLevel_RecordsBestStarsAndUnlocksOnlyTheNextLevel()
        {
            PlayerProgress.CompleteLevel(1, 2);
            PlayerProgress.CompleteLevel(1, 1);

            Assert.That(PlayerProgress.GetStars(1), Is.EqualTo(2));
            Assert.That(PlayerProgress.IsUnlocked(2), Is.True);
            Assert.That(PlayerProgress.IsUnlocked(3), Is.False);
            Assert.That(PlayerProgress.GetContinueLevel(), Is.EqualTo(2));
        }

        [Test]
        public void CatalogLevels_AreDeterministicAndLevelTwentyThreeRemainsAvailableForDevelopment()
        {
            var first = LevelCatalog.Get(10).BuildLevel();
            var second = LevelCatalog.Get(10).BuildLevel();

            Assert.That(first.Seed, Is.EqualTo(second.Seed));
            Assert.That(first.CarCount, Is.EqualTo(second.CarCount));
            Assert.That(LevelCatalog.Get(23).Id, Is.EqualTo(23));
        }

        [Test]
        public void FirstTenCatalogLayouts_AreFixedAndSolvable()
        {
            for (var levelId = 1; levelId <= 10; levelId++)
            {
                var level = LevelCatalog.Get(levelId).BuildLevel();
                Assert.That(ChainPuzzleSolver.TrySolve(level).IsSolved, Is.True, "Level " + levelId);
            }
        }

        [Test]
        public void HapticsSetting_PersistsCorrectly()
        {
            PlayerProgress.HapticsEnabled = false;

            Assert.That(PlayerProgress.HapticsEnabled, Is.False);

            PlayerProgress.HapticsEnabled = true;

            Assert.That(PlayerProgress.HapticsEnabled, Is.True);
        }

        [Test]
        public void ProgressAggregation_CalculatesTotalStarsAndCompletedLevels()
        {
            PlayerProgress.CompleteLevel(1, 3);
            PlayerProgress.CompleteLevel(2, 2);

            Assert.That(PlayerProgress.GetTotalStarsEarned(), Is.EqualTo(5));
            Assert.That(PlayerProgress.GetCompletedLevelsCount(), Is.EqualTo(2));

            PlayerProgress.ResetAllProgress();

            Assert.That(PlayerProgress.GetTotalStarsEarned(), Is.EqualTo(0));
            Assert.That(PlayerProgress.GetCompletedLevelsCount(), Is.EqualTo(0));
            Assert.That(PlayerProgress.IsUnlocked(1), Is.True);
            Assert.That(PlayerProgress.IsUnlocked(2), Is.False);
        }
    }
}
