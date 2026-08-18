using ArrowMaze.Data;
using ArrowMaze.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArrowMaze.UI
{
    public sealed class LevelMapController : MonoBehaviour
    {
        private void Awake()
        {
            Build();
        }

        private void Build()
        {
            var canvas = MenuUiBuilder.CreateCanvas("Level Map Canvas");
            MenuUiBuilder.Panel(canvas, "Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color32(247, 251, 255, 255));
            MenuUiBuilder.Text(canvas, "Title", "LEVEL MAP", new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0, -100), new Vector2(750, 80), 56f, MenuUiBuilder.Navy);
            MenuUiBuilder.Text(canvas, "Subtitle", "Follow the road. Clear the traffic.", new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0, -160), new Vector2(850, 48), 26f, new Color32(102, 120, 151, 255));

            var scrollRoot = MenuUiBuilder.Panel(canvas, "Map Viewport", new Vector2(.05f, .12f), new Vector2(.95f, .84f), Vector2.zero, Vector2.zero, Color.white);
            var mask = scrollRoot.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = true;
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
            var content = new GameObject("Road Content", typeof(RectTransform)); content.transform.SetParent(scrollRoot.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1); contentRect.pivot = new Vector2(.5f, 1); contentRect.sizeDelta = new Vector2(0, 4400);
            scroll.viewport = scrollRoot.rectTransform; scroll.content = contentRect; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.verticalNormalizedPosition = 1f;

            var current = PlayerProgress.GetContinueLevel();
            for (var level = 1; level <= LevelCatalog.HighestCatalogLevel; level++)
            {
                CreateRoadSegment(content.transform, level);
                CreateNode(content.transform, level, current);
            }

            var back = MenuUiBuilder.Button(canvas, "Back Button", "‹  MENU", new Vector2(.5f, .055f), Vector2.zero, new Vector2(330, 78), MenuUiBuilder.Navy);
            back.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
#if UNITY_EDITOR
            var dev = MenuUiBuilder.Button(canvas, "Dev Level 23", "DEV 23", new Vector2(.85f, .92f), Vector2.zero, new Vector2(150, 56), new Color32(129, 151, 184, 255));
            var devLabel = dev.GetComponentInChildren<TextMeshProUGUI>(); if (devLabel != null) devLabel.fontSize = 21f;
            dev.onClick.AddListener(() => OpenLevel(23));
#endif
        }

        private void CreateRoadSegment(Transform parent, int level)
        {
            if (level == LevelCatalog.HighestCatalogLevel) return;
            var y = -135f - ((level - 1) * 175f);
            var image = MenuUiBuilder.Panel(parent, "Road " + level, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(-7, y - 122), new Vector2(7, y - 53), MenuUiBuilder.PaleBlue);
            image.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(level * .8f) * 155f, y - 88f);
        }

        private void CreateNode(Transform parent, int levelId, int current)
        {
            var x = Mathf.Sin(levelId * .8f) * 155f;
            var y = -135f - ((levelId - 1) * 175f);
            var completed = PlayerProgress.GetStars(levelId) > 0;
            var unlocked = PlayerProgress.IsUnlocked(levelId);
            var isCurrent = levelId == current;
            var color = completed ? MenuUiBuilder.Green : isCurrent ? MenuUiBuilder.Blue : unlocked ? MenuUiBuilder.Navy : MenuUiBuilder.Muted;
            var label = completed ? "DONE\n" + PlayerProgress.GetStars(levelId) + "/3" : unlocked ? "LEVEL\n" + levelId : "LOCKED\n" + levelId;
            var button = MenuUiBuilder.Button(parent, "Level " + levelId, label, new Vector2(.5f, 1f), new Vector2(x, y), new Vector2(176, 126), color);
            var buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>(); if (buttonLabel != null) buttonLabel.fontSize = 29f;
            button.interactable = unlocked;
            if (unlocked)
            {
                var captured = levelId;
                button.onClick.AddListener(() => OpenLevel(captured));
            }
            if (isCurrent)
            {
                MenuUiBuilder.Text(parent, "Current " + levelId, "CURRENT", new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(x, y + 102), new Vector2(220, 35), 21f, MenuUiBuilder.Blue);
            }
        }

        private static void OpenLevel(int levelId)
        {
            LevelSession.SelectedLevel = levelId;
            SceneManager.LoadScene("Gameplay");
        }
    }
}
