using ArrowMaze.Data;
using UnityEngine;

namespace ArrowMaze.Meta
{
    /// <summary>Transient selected level between menus and Gameplay; durable progress stays in PlayerProgress.</summary>
    public static class LevelSession
    {
        private const string SelectedLevelKey = "TapAwayCars.SelectedLevel";
        public static int SelectedLevel
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(SelectedLevelKey, PlayerProgress.GetContinueLevel()), 1, LevelCatalog.HighestCatalogLevel);
            set { PlayerPrefs.SetInt(SelectedLevelKey, Mathf.Clamp(value, 1, LevelCatalog.HighestCatalogLevel)); PlayerPrefs.Save(); }
        }
    }
}
