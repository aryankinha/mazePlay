using System;
using System.Collections.Generic;
using ArrowMaze.Data;
using UnityEngine;

namespace ArrowMaze.Meta
{
    [Serializable]
    public sealed class LevelStarRecord
    {
        public int levelId;
        public int stars;
    }

    [Serializable]
    public sealed class PlayerProgressData
    {
        public int highestUnlockedLevel = 1;
        public int lastPlayedLevel = 1;
        public List<LevelStarRecord> levelStars = new List<LevelStarRecord>();
    }

    /// <summary>Small local save for the offline single-player journey.</summary>
    public static class PlayerProgress
    {
        private const string SaveKey = "TapAwayCars.PlayerProgress.v1";
        private static PlayerProgressData cached;

        public static PlayerProgressData Data => cached ?? (cached = LoadInternal());

        public static bool IsUnlocked(int levelId) => levelId > 0 && levelId <= Data.highestUnlockedLevel;

        public static int GetStars(int levelId)
        {
            var record = Data.levelStars.Find(entry => entry.levelId == levelId);
            return record != null ? record.stars : 0;
        }

        public static void SetLastPlayed(int levelId)
        {
            Data.lastPlayedLevel = Mathf.Clamp(levelId, 1, LevelCatalog.HighestCatalogLevel);
            Save();
        }

        public static int GetContinueLevel()
        {
            for (var levelId = 1; levelId <= Data.highestUnlockedLevel; levelId++)
            {
                if (GetStars(levelId) == 0)
                {
                    return levelId;
                }
            }

            return Mathf.Clamp(Data.highestUnlockedLevel, 1, LevelCatalog.HighestCatalogLevel);
        }

        public static void CompleteLevel(int levelId, int stars)
        {
            if (levelId < 1)
            {
                return;
            }

            stars = Mathf.Clamp(stars, 1, 3);
            var record = Data.levelStars.Find(entry => entry.levelId == levelId);
            if (record == null)
            {
                record = new LevelStarRecord { levelId = levelId };
                Data.levelStars.Add(record);
            }

            record.stars = Mathf.Max(record.stars, stars);
            Data.highestUnlockedLevel = Mathf.Clamp(
                Mathf.Max(Data.highestUnlockedLevel, levelId + 1),
                1,
                LevelCatalog.HighestCatalogLevel);
            Data.lastPlayedLevel = Mathf.Clamp(levelId + 1, 1, Data.highestUnlockedLevel);
            Save();
        }

        public static int GetTotalStarsEarned()
        {
            var total = 0;
            foreach (var record in Data.levelStars)
            {
                total += record.stars;
            }
            return total;
        }

        public static int GetCompletedLevelsCount()
        {
            var count = 0;
            foreach (var record in Data.levelStars)
            {
                if (record.stars > 0)
                {
                    count++;
                }
            }
            return count;
        }

        public static bool SoundEffectsEnabled
        {
            get => PlayerPrefs.GetInt("TapAwayCars.SoundEffectsEnabled", 1) == 1;
            set { PlayerPrefs.SetInt("TapAwayCars.SoundEffectsEnabled", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool HapticsEnabled
        {
            get => PlayerPrefs.GetInt("TapAwayCars.HapticsEnabled", 1) == 1;
            set { PlayerPrefs.SetInt("TapAwayCars.HapticsEnabled", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool TutorialCompleted
        {
            get => PlayerPrefs.GetInt("TapAwayCars.TutorialCompleted", 0) == 1;
            set { PlayerPrefs.SetInt("TapAwayCars.TutorialCompleted", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static void ResetAllProgress()
        {
            cached = new PlayerProgressData();
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey("TapAwayCars.TutorialCompleted");
            PlayerPrefs.DeleteKey("TapAwayCars.SelectedLevel");
            Save();
        }

        public static void ResetForDevelopment()
        {
            cached = new PlayerProgressData();
            Save();
        }

        public static void UnlockThroughForDevelopment(int levelId)
        {
            Data.highestUnlockedLevel = Mathf.Clamp(levelId, 1, LevelCatalog.HighestCatalogLevel);
            Save();
        }

        /// <summary>Used by EditMode fixtures after restoring a PlayerPrefs snapshot.</summary>
        public static void ReloadFromDiskForTesting()
        {
            cached = null;
        }

        private static PlayerProgressData LoadInternal()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                return new PlayerProgressData();
            }

            var loaded = JsonUtility.FromJson<PlayerProgressData>(PlayerPrefs.GetString(SaveKey));
            if (loaded == null)
            {
                return new PlayerProgressData();
            }

            loaded.highestUnlockedLevel = Mathf.Clamp(loaded.highestUnlockedLevel, 1, LevelCatalog.HighestCatalogLevel);
            loaded.lastPlayedLevel = Mathf.Clamp(loaded.lastPlayedLevel, 1, loaded.highestUnlockedLevel);
            loaded.levelStars = loaded.levelStars ?? new List<LevelStarRecord>();
            return loaded;
        }

        private static void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }
    }
}
