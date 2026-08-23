using System.Collections.Generic;
using ArrowMaze.Data;
using ArrowMaze.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ArrowMaze.Editor
{
    /// <summary>Builds the interactive meta screens from native uGUI pieces.</summary>
    public static class MetaUIBuilder
    {
        private static readonly Color Navy = new Color32(20, 38, 78, 255);
        private static readonly Color NavyDeep = new Color32(14, 28, 60, 255);
        private static readonly Color Blue = new Color32(42, 127, 235, 255);
        private static readonly Color SkyPale = new Color32(222, 246, 255, 255);
        private static readonly Color Gold = new Color32(255, 190, 33, 255);
        private static readonly Color GoldDark = new Color32(222, 132, 15, 255);
        private static readonly Color Green = new Color32(74, 172, 95, 255);
        private static readonly Color TextDark = new Color32(21, 42, 79, 255);
        private static readonly Color TextMuted = new Color32(89, 111, 145, 255);
        private static readonly Color Disabled = new Color32(125, 139, 162, 255);

        [MenuItem("Tools/Rebuild All Meta Screens")]
        public static void RebuildAll()
        {
            RebuildMainMenu();
            RebuildLevelMap();
            Debug.Log("Tap Away Cars: rebuilt Main Menu and Level Map.");
        }

        [MenuItem("Tools/Rebuild Main Menu UI")]
        public static void RebuildMainMenu()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity", OpenSceneMode.Single);
            EnsureSceneCamera();
            var art = LoadArt();
            var canvas = PrepareCanvas("Main Menu Canvas");
            RemoveObjectNamed("Main Menu Systems");
            var safeArea = ClearAndGetSafeArea(canvas.transform);
            BuildMainMenu(safeArea, art, canvas.GetComponent<MainMenuController>());
            Save(scene, "Main Menu");
        }

        [MenuItem("Tools/Capture Screen Preview")]
        public static void CapturePreview()
        {
            var sceneName = EditorSceneManager.GetActiveScene().name;
            var captureDirectory = System.IO.Path.Combine(
                System.IO.Directory.GetParent(Application.dataPath).FullName,
                "Temp",
                "Captures");
            System.IO.Directory.CreateDirectory(captureDirectory);
            var path = System.IO.Path.Combine(captureDirectory, sceneName.ToLowerInvariant() + "_rendered.png");
            var rt = new RenderTexture(540, 960, 24);
            var canvas = Object.FindAnyObjectByType<Canvas>();
            var cam = Camera.main;
            if (canvas != null && cam != null)
            {
                var prevMode = canvas.renderMode;
                var prevCam = canvas.worldCamera;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(540, 960, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 540, 960), 0, 0);
                tex.Apply();
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                cam.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
                canvas.renderMode = prevMode;
                canvas.worldCamera = prevCam;
                Debug.Log("Captured screen preview to: " + path);
            }
        }

        [MenuItem("Tools/Rebuild Level Map UI")]
        public static void RebuildLevelMap()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/LevelMap.unity", OpenSceneMode.Single);
            EnsureSceneCamera();
            var art = LoadArt();
            var canvas = PrepareCanvas("Level Map Canvas");
            RemoveObjectNamed("Level Map Systems");
            var safeArea = ClearAndGetSafeArea(canvas.transform);
            BuildLevelMap(safeArea, art, canvas.GetComponent<LevelMapController>());
            Save(scene, "Level Map");
        }

        private static void BuildMainMenu(Transform safeArea, Art art, MainMenuController controller)
        {
            CreateMenuBackdrop(safeArea, art);
            var settings = CreateCircularButton(safeArea, "SettingsButton", new Vector2(-440f, 810f), 88f, art.ButtonCircle, art.Settings);

            CreateLogoText(safeArea, "TAP AWAY", new Vector2(0f, 675f), 56f, Color.white, NavyDeep, .15f);
            CreateLogoText(safeArea, "CARS", new Vector2(0f, 575f), 92f, Gold, GoldDark, .12f);
            CreatePill(safeArea, "Tagline", new Vector2(0f, 490f), new Vector2(580f, 52f), new Color(0.04f, 0.12f, 0.28f, 0.45f), art.Pill, false);
            CreateText(safeArea, "Tagline Text", "Clear the traffic. One car at a time.", new Vector2(0f, 490f), new Vector2(560f, 38f), 22f, Color.white, FontStyles.Bold);
            BuildDecorativeHero(safeArea, art);

            var continueButton = CreateActionButton(safeArea, "PlayContinueButton", new Vector2(0f, -175f), new Vector2(870f, 126f), Gold, GoldDark, art.Card, null, "CONTINUE", "LEVEL 1");
            var continueLabel = continueButton.transform.Find("Label").GetComponent<TMP_Text>();
            var continueSubtext = continueButton.transform.Find("Subtext").GetComponent<TMP_Text>();
            var mapButton = CreateActionButton(safeArea, "LevelMapButton", new Vector2(0f, -330f), new Vector2(870f, 114f), Blue, Navy, art.Card, null, "LEVEL MAP", null);

            var progress = CreateCard(safeArea, "Progress Card", new Vector2(0f, -560f), new Vector2(870f, 210f), Color.white, art.Card);
            CreateText(progress.transform, "Progress Label", "YOUR JOURNEY", new Vector2(-240f, 44f), new Vector2(330f, 28f), 16f, TextMuted, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            var progressLevel = CreateText(progress.transform, "Progress Level", "LEVEL 1 OF 23", new Vector2(-240f, 8f), new Vector2(330f, 38f), 26f, TextDark, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            CreateImage(progress.transform, "Progress Star Icon", art.StarFull, new Vector2(170f, 8f), new Vector2(30f, 30f));
            var progressStars = CreateText(progress.transform, "Progress Stars", "0 / 69 STARS", new Vector2(285f, 8f), new Vector2(200f, 36f), 24f, GoldDark, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            var barBackground = CreatePill(progress.transform, "Progress Bar Background", new Vector2(0f, -44f), new Vector2(750f, 26f), new Color32(220, 230, 242, 255), art.Card, false);
            var fill = CreatePill(barBackground.transform, "Progress Bar Fill", Vector2.zero, Vector2.zero, Blue, art.Card, false);
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var settingsModal = CreateSettingsModal(safeArea, art);

            if (controller == null) controller = safeArea.GetComponentInParent<MainMenuController>();
            if (controller == null) controller = safeArea.parent.gameObject.AddComponent<MainMenuController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("playContinueButton").objectReferenceValue = continueButton;
            serialized.FindProperty("playContinueLabel").objectReferenceValue = continueLabel;
            serialized.FindProperty("playContinueSubtext").objectReferenceValue = continueSubtext;
            serialized.FindProperty("levelMapButton").objectReferenceValue = mapButton;
            serialized.FindProperty("settingsButton").objectReferenceValue = settings;
            serialized.FindProperty("progressLevelText").objectReferenceValue = progressLevel;
            serialized.FindProperty("progressStarsText").objectReferenceValue = progressStars;
            serialized.FindProperty("progressBarFill").objectReferenceValue = fill;
            serialized.FindProperty("settingsModal").objectReferenceValue = settingsModal;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(continueButton.onClick, controller.PlayContinue);
            UnityEventTools.AddPersistentListener(mapButton.onClick, controller.OpenLevelMap);
            UnityEventTools.AddPersistentListener(settings.onClick, controller.OpenSettings);
            EditorUtility.SetDirty(continueButton);
            EditorUtility.SetDirty(mapButton);
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(controller);
        }

        private static void BuildLevelMap(Transform safeArea, Art art, LevelMapController controller)
        {
            CreateMapBackdrop(safeArea, art);
            var scrollRoot = new GameObject("Map Scroll View", typeof(RectTransform), typeof(ScrollRect));
            scrollRoot.transform.SetParent(safeArea, false);
            Stretch(scrollRoot.GetComponent<RectTransform>(), new Vector2(0f, 145f), new Vector2(0f, -175f));
            var scroll = scrollRoot.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.decelerationRate = .12f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollRoot.transform, false);
            Stretch(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            scroll.viewport = viewport.GetComponent<RectTransform>();
            var content = new GameObject("Road Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 0f);
            contentRect.pivot = new Vector2(.5f, 0f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 5280f);
            scroll.content = contentRect;

            var positions = new Vector2[LevelCatalog.HighestCatalogLevel];
            for (var index = 0; index < positions.Length; index++) positions[index] = new Vector2(Mathf.Sin(index * .88f + .35f) * 220f, 155f + index * 224f);
            var roadRoot = new GameObject("Winding Road", typeof(RectTransform));
            roadRoot.transform.SetParent(content.transform, false);
            Stretch(roadRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            for (var index = 0; index < positions.Length - 1; index++) CreateRoadSegment(roadRoot.transform, positions[index], positions[index + 1], art.Road);
            var nodes = new List<LevelNode>();
            for (var index = 0; index < positions.Length; index++) nodes.Add(CreateLevelNode(content.transform, index + 1, positions[index], art));

            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(safeArea, false);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, -24f);
            headerRect.sizeDelta = new Vector2(0f, 142f);
            var headerBackdrop = CreatePanel(header.transform, "Header Backdrop", new Vector2(0f, -70f), new Vector2(0f, 166f), new Color(SkyPale.r, SkyPale.g, SkyPale.b, .96f), null, false);
            headerBackdrop.rectTransform.anchorMin = new Vector2(0f, 1f);
            headerBackdrop.rectTransform.anchorMax = new Vector2(1f, 1f);
            headerBackdrop.rectTransform.pivot = new Vector2(.5f, 1f);
            headerBackdrop.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            var back = CreateCircularButton(header.transform, "BackButton", new Vector2(-430f, -58f), 92f, art.ButtonCircle, art.Back);
            CreateText(header.transform, "Title", "LEVEL MAP", new Vector2(0f, -47f), new Vector2(560f, 56f), 47f, TextDark, FontStyles.Bold);
            CreateText(header.transform, "Subtitle", "Follow the road. Clear the traffic.", new Vector2(0f, -94f), new Vector2(680f, 38f), 24f, TextMuted, FontStyles.Normal);
            var settings = CreateCircularButton(header.transform, "SettingsButton", new Vector2(430f, -58f), 92f, art.ButtonCircle, art.Settings);
            var footer = CreateActionButton(safeArea, "Back To Menu Button", new Vector2(0f, -840f), new Vector2(650f, 102f), Navy, NavyDeep, art.Card, null, "BACK TO MENU", null);
            var settingsModal = CreateSettingsModal(safeArea, art);

            if (controller == null) controller = safeArea.GetComponentInParent<LevelMapController>();
            if (controller == null) controller = safeArea.parent.gameObject.AddComponent<LevelMapController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("backButton").objectReferenceValue = back;
            serialized.FindProperty("footerBackButton").objectReferenceValue = footer;
            serialized.FindProperty("settingsButton").objectReferenceValue = settings;
            serialized.FindProperty("scrollRect").objectReferenceValue = scroll;
            serialized.FindProperty("mapContent").objectReferenceValue = contentRect;
            serialized.FindProperty("settingsModal").objectReferenceValue = settingsModal;
            var nodeArray = serialized.FindProperty("levelNodes");
            nodeArray.arraySize = nodes.Count;
            for (var index = 0; index < nodes.Count; index++)
            {
                nodeArray.GetArrayElementAtIndex(index).objectReferenceValue = nodes[index];
                var levelId = index + 1;
                var nodeButton = nodes[index].GetComponent<Button>();
                UnityEventTools.AddIntPersistentListener(nodeButton.onClick, controller.SelectLevel, levelId);
                EditorUtility.SetDirty(nodeButton);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(back.onClick, controller.ReturnToMainMenu);
            UnityEventTools.AddPersistentListener(footer.onClick, controller.ReturnToMainMenu);
            UnityEventTools.AddPersistentListener(settings.onClick, controller.OpenSettings);
            EditorUtility.SetDirty(back);
            EditorUtility.SetDirty(footer);
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(controller);
        }

        private static void CreateMenuBackdrop(Transform parent, Art art)
        {
            var sky = CreateFullScreenPanel(parent, "Sky", new Color32(44, 142, 241, 255));
            sky.transform.SetAsFirstSibling();
            var ground = CreatePanel(parent, "Roadside Shadow", Vector2.zero, new Vector2(0f, 720f), new Color(0.03f, 0.10f, 0.24f, 0.35f), null, false);
            ground.rectTransform.anchorMin = new Vector2(0f, 0f); ground.rectTransform.anchorMax = new Vector2(1f, 0f); ground.rectTransform.pivot = new Vector2(.5f, 0f); ground.rectTransform.anchoredPosition = Vector2.zero;
            CreateCloud(parent, new Vector2(-360f, 650f), .75f, art.ButtonCircle);
            CreateCloud(parent, new Vector2(360f, 610f), .65f, art.ButtonCircle);
            CreateSkyline(parent, new Vector2(0f, 90f), new Vector2(1000f, 200f), art.Pill, new Color(.1f, .25f, .5f, .20f));
            CreateTree(parent, new Vector2(-435f, 190f), .85f, art.ButtonCircle, art.Pill);
            CreateTree(parent, new Vector2(435f, 190f), .85f, art.ButtonCircle, art.Pill);
        }

        private static void CreateMapBackdrop(Transform parent, Art art)
        {
            var sky = CreateFullScreenPanel(parent, "Sky", SkyPale);
            sky.transform.SetAsFirstSibling();
            var upper = CreatePanel(parent, "Sky Tint", Vector2.zero, new Vector2(0f, 850f), new Color(.44f, .79f, 1f, .24f), null, false);
            upper.rectTransform.anchorMin = new Vector2(0f, 1f); upper.rectTransform.anchorMax = new Vector2(1f, 1f); upper.rectTransform.pivot = new Vector2(.5f, 1f); upper.rectTransform.anchoredPosition = Vector2.zero;
            CreateCloud(parent, new Vector2(-330f, 650f), .82f, art.ButtonCircle);
            CreateCloud(parent, new Vector2(310f, 520f), 1.05f, art.ButtonCircle);
            CreateSkyline(parent, new Vector2(0f, 305f), new Vector2(1080f, 265f), art.Pill, new Color(.3f, .52f, .75f, .18f));
            var water = CreatePanel(parent, "Water", Vector2.zero, new Vector2(0f, 1060f), new Color(.42f, .78f, .93f, .16f), null, false);
            water.rectTransform.anchorMin = new Vector2(0f, 0f); water.rectTransform.anchorMax = new Vector2(1f, 0f); water.rectTransform.pivot = new Vector2(.5f, 0f); water.rectTransform.anchoredPosition = Vector2.zero;
            CreateTree(parent, new Vector2(-420f, -125f), .76f, art.ButtonCircle, art.Pill);
            CreateTree(parent, new Vector2(426f, -450f), 1.04f, art.ButtonCircle, art.Pill);
            CreateTree(parent, new Vector2(-402f, -690f), .9f, art.ButtonCircle, art.Pill);
        }

        private static void BuildDecorativeHero(Transform parent, Art art)
        {
            var card = CreateCard(parent, "Traffic Hero", new Vector2(0f, 195f), new Vector2(870f, 390f), new Color(1f, 1f, 1f, .94f), art.Card);
            var shadow = CreatePanel(card.transform, "Road Shadow", new Vector2(0f, -5f), new Vector2(260f, 800f), new Color(0f, 0f, 0f, .20f), null, false); shadow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var road = CreatePanel(card.transform, "Decorative Highway", new Vector2(0f, 5f), new Vector2(790f, 250f), Color.white, art.HeroRoad, false); road.type = Image.Type.Simple;
            CreateImage(card.transform, "Exit Gate", art.Exit, new Vector2(295f, 95f), new Vector2(130f, 65f));
            CreateText(card.transform, "Hero Caption", "THE ROAD IS CLEARING", new Vector2(0f, 155f), new Vector2(620f, 30f), 17f, TextMuted, FontStyles.Bold);
            CreateImage(card.transform, "Hero Car Yellow", art.YellowCar, new Vector2(-235f, -8f), new Vector2(105f, 150f), 12f);
            CreateImage(card.transform, "Hero Car Blue", art.BlueCar, new Vector2(-75f, 35f), new Vector2(105f, 150f), -4f);
            CreateImage(card.transform, "Hero Car Purple", art.PurpleCar, new Vector2(225f, 28f), new Vector2(105f, 150f), -8f);
            var red = CreateImage(card.transform, "Hero Car Red", art.RedCar, new Vector2(75f, -12f), new Vector2(110f, 158f), 0f); red.transform.SetAsLastSibling();
        }

        private static LevelNode CreateLevelNode(Transform parent, int levelId, Vector2 position, Art art)
        {
            CreatePanel(parent, "Node Shadow " + levelId, position + new Vector2(0f, -11f), new Vector2(166f, 166f), new Color(0f, .08f, .18f, .30f), art.ButtonCircle, true);
            var node = new GameObject("Level Node " + levelId, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline), typeof(LevelNode));
            node.transform.SetParent(parent, false);
            var rect = node.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.5f, 0f); rect.anchorMax = new Vector2(.5f, 0f); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(156f, 156f);
            var image = node.GetComponent<Image>(); image.sprite = art.ButtonCircle; image.color = Color.white;
            var button = node.GetComponent<Button>(); button.targetGraphic = image;
            var outline = node.GetComponent<Outline>(); outline.effectColor = Gold; outline.effectDistance = new Vector2(4f, -4f);
            var glow = CreateImage(node.transform, "Current Glow", art.SelectionGlow, Vector2.zero, new Vector2(214f, 214f)); glow.transform.SetAsFirstSibling(); glow.SetActive(false);
            var number = CreateText(node.transform, "Level Number", levelId.ToString(), new Vector2(0f, 12f), new Vector2(130f, 62f), 43f, TextDark, FontStyles.Bold);
            var stars = new GameObject("Stars", typeof(RectTransform), typeof(HorizontalLayoutGroup)); stars.transform.SetParent(node.transform, false);
            var starsRect = stars.GetComponent<RectTransform>(); starsRect.anchorMin = new Vector2(.5f, 0f); starsRect.anchorMax = new Vector2(.5f, 0f); starsRect.anchoredPosition = new Vector2(0f, -34f); starsRect.sizeDelta = new Vector2(126f, 34f);
            var layout = stars.GetComponent<HorizontalLayoutGroup>(); layout.spacing = 7f; layout.childAlignment = TextAnchor.MiddleCenter; layout.childControlWidth = false; layout.childControlHeight = false;
            var starImages = new Image[3]; for (var index = 0; index < starImages.Length; index++) starImages[index] = CreateImage(stars.transform, "Star " + (index + 1), art.StarFull, Vector2.zero, new Vector2(29f, 29f)).GetComponent<Image>();
            var marker = CreateImage(node.transform, "Car Marker", art.YellowCar, new Vector2(0f, 115f), new Vector2(54f, 78f)); marker.SetActive(false);
            var badge = CreatePill(node.transform, "Current Badge", new Vector2(112f, -68f), new Vector2(146f, 42f), Navy, art.Pill, false); CreateText(badge.transform, "Text", "CURRENT", Vector2.zero, new Vector2(136f, 32f), 16f, Color.white, FontStyles.Bold); badge.gameObject.SetActive(false);
            var lockLabel = CreateText(node.transform, "Lock Label", "LOCKED", new Vector2(0f, -33f), new Vector2(120f, 30f), 17f, Color.white, FontStyles.Bold); lockLabel.gameObject.SetActive(false);
            var component = node.GetComponent<LevelNode>(); var serialized = new SerializedObject(component);
            serialized.FindProperty("button").objectReferenceValue = button; serialized.FindProperty("nodeBackground").objectReferenceValue = image; serialized.FindProperty("nodeOutline").objectReferenceValue = outline; serialized.FindProperty("levelNumberText").objectReferenceValue = number; serialized.FindProperty("carMarker").objectReferenceValue = marker; serialized.FindProperty("currentGlow").objectReferenceValue = glow; serialized.FindProperty("starsContainer").objectReferenceValue = stars; serialized.FindProperty("starFullSprite").objectReferenceValue = art.StarFull; serialized.FindProperty("starEmptySprite").objectReferenceValue = art.StarEmpty; serialized.FindProperty("currentBadge").objectReferenceValue = badge.gameObject; serialized.FindProperty("lockIcon").objectReferenceValue = lockLabel.gameObject;
            var starArray = serialized.FindProperty("starImages"); starArray.arraySize = starImages.Length; for (var index = 0; index < starImages.Length; index++) starArray.GetArrayElementAtIndex(index).objectReferenceValue = starImages[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return component;
        }

        private static void CreateRoadSegment(Transform parent, Vector2 from, Vector2 to, Sprite roadSprite)
        {
            var direction = to - from; var distance = direction.magnitude; var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f; var midpoint = (from + to) * .5f;
            var shadow = CreatePanel(parent, "Road Shadow", midpoint + new Vector2(4f, -4f), new Vector2(98f, distance + 22f), new Color(0f, .07f, .15f, .13f), null, false); shadow.rectTransform.anchorMin = new Vector2(.5f, 0f); shadow.rectTransform.anchorMax = new Vector2(.5f, 0f); shadow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            var road = CreatePanel(parent, "Road", midpoint, new Vector2(92f, distance + 20f), Color.white, roadSprite, false); road.rectTransform.anchorMin = new Vector2(.5f, 0f); road.rectTransform.anchorMax = new Vector2(.5f, 0f); road.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle); road.type = Image.Type.Tiled;
        }

        private static void CreateCloud(Transform parent, Vector2 position, float scale, Sprite circle)
        {
            var cloud = new GameObject("Cloud", typeof(RectTransform)); cloud.transform.SetParent(parent, false); cloud.GetComponent<RectTransform>().anchoredPosition = position;
            var color = new Color(1f, 1f, 1f, .56f);
            CreatePanel(cloud.transform, "Puff A", new Vector2(-62f * scale, 0f), new Vector2(120f, 120f) * scale, color, circle, true);
            CreatePanel(cloud.transform, "Puff B", new Vector2(5f * scale, 26f * scale), new Vector2(148f, 148f) * scale, color, circle, true);
            CreatePanel(cloud.transform, "Puff C", new Vector2(78f * scale, -2f), new Vector2(108f, 108f) * scale, color, circle, true);
        }

        private static void CreateSkyline(Transform parent, Vector2 position, Vector2 size, Sprite pill, Color color)
        {
            var skyline = new GameObject("Distant City", typeof(RectTransform)); skyline.transform.SetParent(parent, false); var rect = skyline.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size;
            var heights = new[] { 92f, 132f, 110f, 180f, 120f, 145f, 98f, 166f, 112f };
            for (var index = 0; index < heights.Length; index++) CreatePanel(skyline.transform, "Building " + index, new Vector2(-size.x * .44f + index * size.x * .11f, -size.y * .5f + heights[index] * .5f), new Vector2(72f, heights[index]), color, pill, false);
        }

        private static void CreateTree(Transform parent, Vector2 position, float scale, Sprite circle, Sprite pill)
        {
            var tree = new GameObject("Tree", typeof(RectTransform)); tree.transform.SetParent(parent, false); tree.GetComponent<RectTransform>().anchoredPosition = position;
            CreatePanel(tree.transform, "Trunk", new Vector2(0f, -60f * scale), new Vector2(22f, 86f) * scale, new Color32(114, 77, 44, 255), pill, false);
            CreatePanel(tree.transform, "Canopy A", new Vector2(-27f * scale, 10f), new Vector2(90f, 90f) * scale, Green, circle, true);
            CreatePanel(tree.transform, "Canopy B", new Vector2(22f * scale, 24f), new Vector2(110f, 110f) * scale, new Color32(55, 148, 77, 255), circle, true);
            CreatePanel(tree.transform, "Canopy C", new Vector2(0f, 72f * scale), new Vector2(82f, 82f) * scale, new Color32(79, 180, 87, 255), circle, true);
        }

        private static Button CreateActionButton(Transform parent, string name, Vector2 position, Vector2 size, Color color, Color shadowColor, Sprite pill, string icon, string label, string subtext)
        {
            CreatePill(parent, name + " Shadow", position + new Vector2(0f, -12f), size, shadowColor, pill, false);
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); root.transform.SetParent(parent, false); var rect = root.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = root.GetComponent<Image>(); image.sprite = pill; image.type = Image.Type.Sliced; image.color = color;
            var button = root.GetComponent<Button>(); button.targetGraphic = image; var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1f, 1f, 1f, .92f); colors.pressedColor = new Color(.82f, .88f, 1f, 1f); button.colors = colors;
            var hasIcon = !string.IsNullOrEmpty(icon);
            if (hasIcon) CreateText(root.transform, "Icon", icon, new Vector2(-size.x * .36f, 0f), new Vector2(80f, 64f), 47f, Color.white, FontStyles.Bold);
            var textPosition = hasIcon ? new Vector2(12f, 0f) : Vector2.zero;
            var textWidth = hasIcon ? size.x - 180f : size.x - 64f;
            CreateText(root.transform, "Label", label, string.IsNullOrEmpty(subtext) ? textPosition : textPosition + new Vector2(0f, 19f), new Vector2(textWidth, 48f), 37f, Color.white, FontStyles.Bold);
            if (!string.IsNullOrEmpty(subtext)) CreateText(root.transform, "Subtext", subtext, textPosition + new Vector2(0f, -26f), new Vector2(textWidth, 34f), 22f, new Color(1f, 1f, 1f, .85f), FontStyles.Bold);
            return button;
        }

        private static Button CreateCircularButton(Transform parent, string name, Vector2 position, float size, Sprite circle, Sprite icon)
        {
            CreatePanel(parent, name + " Shadow", position + new Vector2(0f, -8f), new Vector2(size, size), new Color(0f, .07f, .16f, .30f), circle, true);
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); root.transform.SetParent(parent, false); var rect = root.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(size, size);
            var image = root.GetComponent<Image>(); image.sprite = circle; image.color = Color.white; var button = root.GetComponent<Button>(); button.targetGraphic = image; CreateImage(root.transform, "Icon", icon, Vector2.zero, new Vector2(size * .47f, size * .47f)); return button;
        }

        private static Image CreateFullScreenPanel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image)); panel.transform.SetParent(parent, false); Stretch(panel.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero); var image = panel.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }

        private static Image CreateCard(Transform parent, string name, Vector2 position, Vector2 size, Color color, Sprite card)
        {
            CreatePanel(parent, name + " Shadow", position + new Vector2(0f, -12f), size, new Color(0f, .08f, .18f, .24f), card, false); return CreatePanel(parent, name, position, size, color, card, false);
        }

        private static Image CreatePill(Transform parent, string name, Vector2 position, Vector2 size, Color color, Sprite sprite, bool preserveAspect) => CreatePanel(parent, name, position, size, color, sprite, preserveAspect);

        private static Image CreatePanel(Transform parent, string name, Vector2 position, Vector2 size, Color color, Sprite sprite, bool preserveAspect)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image)); root.transform.SetParent(parent, false); var rect = root.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = root.GetComponent<Image>(); image.sprite = sprite; image.type = sprite != null && !preserveAspect ? Image.Type.Sliced : Image.Type.Simple; image.preserveAspect = preserveAspect; image.color = color; image.raycastTarget = false; return image;
        }

        private static GameObject CreateImage(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size, float rotation = 0f)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image)); root.transform.SetParent(parent, false); var rect = root.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size; rect.localRotation = Quaternion.Euler(0f, 0f, rotation); var image = root.GetComponent<Image>(); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false; return root;
        }

        private static TMP_Text CreateText(Transform parent, string name, string content, Vector2 position, Vector2 size, float fontSize, Color color, FontStyles style, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); root.transform.SetParent(parent, false); var rect = root.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size; var text = root.GetComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset; text.text = content; text.fontSize = fontSize; text.color = color; text.fontStyle = style; text.alignment = alignment; text.textWrappingMode = TextWrappingModes.NoWrap; text.raycastTarget = false; return text;
        }

        private static void CreateLogoText(Transform parent, string content, Vector2 position, float fontSize, Color fill, Color outline, float outlineWidth)
        {
            var shadow = CreateText(parent, content + " Shadow", content, position + new Vector2(0f, -11f), new Vector2(900f, 105f), fontSize, new Color(0f, .05f, .15f, .52f), FontStyles.Bold); shadow.outlineWidth = .10f; shadow.outlineColor = new Color(0f, .05f, .15f, .75f);
            var text = CreateText(parent, content, content, position, new Vector2(900f, 120f), fontSize, fill, FontStyles.Bold); text.outlineWidth = outlineWidth; text.outlineColor = outline;
        }

        private static SettingsModal CreateSettingsModal(Transform parent, Art art)
        {
            var root = new GameObject("Settings Modal", typeof(RectTransform), typeof(Image), typeof(SettingsModal)); root.transform.SetParent(parent, false); Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero); root.GetComponent<Image>().color = new Color(0f, .06f, .16f, .66f);
            var card = CreateCard(root.transform, "Card", Vector2.zero, new Vector2(720f, 500f), Color.white, art.Card).gameObject; CreateText(card.transform, "Title", "SETTINGS", new Vector2(0f, 160f), new Vector2(400f, 56f), 42f, TextDark, FontStyles.Bold);
            var close = CreateCircularButton(card.transform, "CloseButton", new Vector2(280f, 160f), 70f, art.ButtonCircle, art.Back); close.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            var haptics = CreateSettingRow(card.transform, "Haptics", new Vector2(0f, 40f), art.Pill);
            var reset = CreateActionButton(card.transform, "ResetButton", new Vector2(0f, -95f), new Vector2(470f, 84f), new Color32(235, 87, 87, 255), new Color32(176, 54, 54, 255), art.Pill, null, "RESET PROGRESS", null);
            var confirm = new GameObject("ResetConfirmDialog", typeof(RectTransform), typeof(Image)); confirm.transform.SetParent(root.transform, false); Stretch(confirm.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero); confirm.GetComponent<Image>().color = new Color(0f, .05f, .13f, .74f);
            var confirmCard = CreateCard(confirm.transform, "Confirm Card", Vector2.zero, new Vector2(660f, 410f), Color.white, art.Card).gameObject; CreateText(confirmCard.transform, "Title", "RESET ALL PROGRESS?", new Vector2(0f, 110f), new Vector2(600f, 48f), 31f, TextDark, FontStyles.Bold); CreateText(confirmCard.transform, "Message", "Your unlocked levels and stars will be removed.", new Vector2(0f, 38f), new Vector2(590f, 52f), 22f, TextMuted, FontStyles.Normal);
            var cancel = CreateActionButton(confirmCard.transform, "CancelButton", new Vector2(-150f, -110f), new Vector2(240f, 78f), new Color32(213, 223, 237, 255), Disabled, art.Pill, null, "CANCEL", null); var ok = CreateActionButton(confirmCard.transform, "OkButton", new Vector2(150f, -110f), new Vector2(240f, 78f), new Color32(235, 87, 87, 255), new Color32(176, 54, 54, 255), art.Pill, null, "RESET", null); confirm.SetActive(false);
            var modal = root.GetComponent<SettingsModal>(); var serialized = new SerializedObject(modal);
            serialized.FindProperty("modalCard").objectReferenceValue = card; serialized.FindProperty("closeButton").objectReferenceValue = close; serialized.FindProperty("hapticsToggleButton").objectReferenceValue = haptics.Button; serialized.FindProperty("hapticsToggleText").objectReferenceValue = haptics.Label; serialized.FindProperty("hapticsToggleImage").objectReferenceValue = haptics.Image; serialized.FindProperty("resetProgressButton").objectReferenceValue = reset; serialized.FindProperty("resetConfirmDialog").objectReferenceValue = confirm; serialized.FindProperty("resetConfirmCancelButton").objectReferenceValue = cancel; serialized.FindProperty("resetConfirmOkButton").objectReferenceValue = ok; serialized.ApplyModifiedPropertiesWithoutUndo(); root.SetActive(false); return modal;
        }

        private static ToggleRow CreateSettingRow(Transform parent, string title, Vector2 position, Sprite pill)
        {
            var row = new GameObject(title + " Row", typeof(RectTransform)); row.transform.SetParent(parent, false); var rect = row.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(560f, 70f); CreateText(row.transform, "Title", title, new Vector2(-110f, 0f), new Vector2(320f, 42f), 28f, TextDark, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            var toggle = CreatePill(row.transform, "Toggle", new Vector2(190f, 0f), new Vector2(130f, 54f), Blue, pill, false); toggle.type = Image.Type.Sliced; var button = toggle.gameObject.AddComponent<Button>(); button.targetGraphic = toggle; var label = CreateText(toggle.transform, "Label", "ON", Vector2.zero, new Vector2(100f, 36f), 22f, Color.white, FontStyles.Bold); return new ToggleRow(button, label, toggle);
        }

        private static GameObject PrepareCanvas(string canvasName)
        {
            var canvasObject = GameObject.Find(canvasName) ?? GameObject.Find("Canvas") ?? GameObject.Find("HUD"); if (canvasObject == null) canvasObject = new GameObject(canvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); canvasObject.name = canvasName;
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; canvas.targetDisplay = 0; var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080f, 1920f); scaler.matchWidthOrHeight = .5f; if (canvasObject.GetComponent<GraphicRaycaster>() == null) canvasObject.AddComponent<GraphicRaycaster>(); if (Object.FindAnyObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); return canvasObject;
        }

        private static Transform ClearAndGetSafeArea(Transform canvas)
        {
            var safe = canvas.Find("Safe Area"); if (safe == null) { var safeObject = new GameObject("Safe Area", typeof(RectTransform), typeof(SafeAreaFitter)); safeObject.transform.SetParent(canvas, false); safe = safeObject.transform; }
            var safeRect = safe.GetComponent<RectTransform>(); safeRect.anchorMin = Vector2.zero; safeRect.anchorMax = Vector2.one; safeRect.offsetMin = Vector2.zero; safeRect.offsetMax = Vector2.zero; for (var index = safe.childCount - 1; index >= 0; index--) Object.DestroyImmediate(safe.GetChild(index).gameObject); return safe;
        }

        private static void EnsureSceneCamera()
        {
            var cameraObject = GameObject.FindWithTag("MainCamera") ?? GameObject.Find("Main Camera"); if (cameraObject == null) { cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); cameraObject.tag = "MainCamera"; }
            var camera = cameraObject.GetComponent<Camera>(); camera.enabled = true; camera.orthographic = true; camera.orthographicSize = 7.7f; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = SkyPale; camera.targetDisplay = 0; cameraObject.transform.position = new Vector3(0f, 0f, -10f); EditorUtility.SetDirty(cameraObject);
        }

        private static void RemoveObjectNamed(string name) { var legacy = GameObject.Find(name); if (legacy != null) Object.DestroyImmediate(legacy); }
        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax; }
        private static void Save(UnityEngine.SceneManagement.Scene scene, string label) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); Debug.Log("Tap Away Cars: " + label + " saved."); }
        private static Art LoadArt() => new Art { ButtonCircle = LoadSprite("Assets/_Project/Sprites/UI/button_circle.png"), Pill = LoadSprite("Assets/_Project/Sprites/UI/badge_pill.png"), Card = LoadSprite("Assets/_Project/Sprites/UI/card_board_bg.png"), SelectionGlow = LoadSprite("Assets/_Project/Sprites/UI/selection_glow.png"), Settings = LoadSprite("Assets/_Project/Sprites/UI/icon_settings.png"), Back = LoadSprite("Assets/_Project/Sprites/UI/icon_back.png"), BlueCar = LoadSprite("Assets/_Project/Sprites/Cars/car_blue.png"), RedCar = LoadSprite("Assets/_Project/Sprites/Cars/car_red.png"), YellowCar = LoadSprite("Assets/_Project/Sprites/Cars/car_yellow.png"), PurpleCar = LoadSprite("Assets/_Project/Sprites/Cars/car_purple.png"), Road = LoadSprite("Assets/_Project/Sprites/Roads/road_straight_v.png"), HeroRoad = LoadSprite("Assets/_Project/Sprites/Roads/road_straight_h.png"), Exit = LoadSprite("Assets/_Project/Sprites/Props/exit_gate.png"), StarFull = LoadSprite("Assets/_Project/Sprites/UI/star_full.png"), StarEmpty = LoadSprite("Assets/_Project/Sprites/UI/star_empty.png") };
        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
        private struct ToggleRow { public readonly Button Button; public readonly TMP_Text Label; public readonly Image Image; public ToggleRow(Button button, TMP_Text label, Image image) { Button = button; Label = label; Image = image; } }
        private sealed class Art { public Sprite ButtonCircle; public Sprite Pill; public Sprite Card; public Sprite SelectionGlow; public Sprite Settings; public Sprite Back; public Sprite BlueCar; public Sprite RedCar; public Sprite YellowCar; public Sprite PurpleCar; public Sprite Road; public Sprite HeroRoad; public Sprite Exit; public Sprite StarFull; public Sprite StarEmpty; }
    }
}
