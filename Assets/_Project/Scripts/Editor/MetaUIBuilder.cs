using System.Collections.Generic;
using ArrowMaze.Data;
using ArrowMaze.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ArrowMaze.Editor
{
    public static class MetaUIBuilder
    {
        private static readonly Color BackgroundColor = new Color32(244, 247, 252, 255);
        private static readonly Color PrimaryNavy = new Color32(23, 35, 61, 255);
        private static readonly Color SubtitleGray = new Color32(102, 117, 143, 255);
        private static readonly Color AccentBlue = new Color32(47, 128, 237, 255);
        private static readonly Color AccentGold = new Color32(242, 201, 78, 255);
        private static readonly Color MutedGray = new Color32(226, 232, 240, 255);
        private static readonly Color AsphaltDark = new Color32(50, 56, 70, 255);

        [MenuItem("Tools/Rebuild All Meta Screens")]
        public static void RebuildAll()
        {
            RebuildMainMenu();
            RebuildLevelMap();
            Debug.Log("✅ [MetaUIBuilder] Successfully rebuilt all Meta screens!");
        }

        [MenuItem("Tools/Rebuild Main Menu UI")]
        public static void RebuildMainMenu()
        {
            var scenePath = "Assets/_Project/Scenes/MainMenu.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 1. Ensure Camera
            EnsureSceneCamera();

            // 2. Load Sprites
            var buttonCircle = LoadSprite("Assets/_Project/Sprites/UI/button_circle.png");
            var badgePill = LoadSprite("Assets/_Project/Sprites/UI/badge_pill.png");
            var cardBoardBg = LoadSprite("Assets/_Project/Sprites/UI/card_board_bg.png");
            var iconSettings = LoadSprite("Assets/_Project/Sprites/UI/icon_settings.png");
            var carYellow = LoadSprite("Assets/_Project/Sprites/Cars/car_yellow.png");
            var carRed = LoadSprite("Assets/_Project/Sprites/Cars/car_red.png");
            var carBlue = LoadSprite("Assets/_Project/Sprites/Cars/car_blue.png");
            var roadStraight = LoadSprite("Assets/_Project/Sprites/Roads/road_straight_v.png");
            var roadCorner = LoadSprite("Assets/_Project/Sprites/Roads/road_corner_0.png");
            var exitGate = LoadSprite("Assets/_Project/Sprites/Props/exit_gate.png");

            // 3. Ensure Canvas & Safe Area
            var (canvasObj, safeAreaObj) = EnsureCanvasAndSafeArea("Main Menu Canvas");

            // Clear legacy children except persistent controllers
            for (var i = safeAreaObj.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(safeAreaObj.transform.GetChild(i).gameObject);
            }

            // 4. Background
            CreateBackground(safeAreaObj.transform);

            // 5. Hero Section (Title + Subtitle + Mini Traffic Card)
            var heroObj = new GameObject("Hero Section", typeof(RectTransform));
            heroObj.transform.SetParent(safeAreaObj.transform, false);
            var heroRect = heroObj.GetComponent<RectTransform>();
            heroRect.anchorMin = new Vector2(0.5f, 1f);
            heroRect.anchorMax = new Vector2(0.5f, 1f);
            heroRect.pivot = new Vector2(0.5f, 1f);
            heroRect.anchoredPosition = new Vector2(0f, -40f);
            heroRect.sizeDelta = new Vector2(800f, 620f);

            var titleObj = CreateText(heroObj.transform, "Title", "TAP AWAY", new Vector2(0f, 230f), new Vector2(700f, 60f), 52f, PrimaryNavy, FontStyles.Bold);
            var titleAccent = CreateText(heroObj.transform, "Title Accent", "CARS", new Vector2(0f, 160f), new Vector2(700f, 80f), 76f, AccentBlue, FontStyles.Bold);
            var subtitle = CreateText(heroObj.transform, "Subtitle", "Clear the traffic. One car at a time.", new Vector2(0f, 95f), new Vector2(700f, 40f), 24f, SubtitleGray, FontStyles.Normal);

            // Mini Traffic Hero Card
            var trafficCard = new GameObject("Traffic Card", typeof(RectTransform), typeof(Image));
            trafficCard.transform.SetParent(heroObj.transform, false);
            var cardRect = trafficCard.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(0f, -110f);
            cardRect.sizeDelta = new Vector2(520f, 260f);
            var cardImg = trafficCard.GetComponent<Image>();
            cardImg.sprite = cardBoardBg;
            cardImg.type = Image.Type.Sliced;
            cardImg.color = Color.white;
            cardImg.raycastTarget = false;

            // Mini road & cars inside card
            CreateImage(trafficCard.transform, "Mini Road 1", roadStraight, new Vector2(-120f, 0f), new Vector2(100f, 180f));
            CreateImage(trafficCard.transform, "Mini Road 2", roadStraight, new Vector2(120f, 0f), new Vector2(100f, 180f));
            CreateImage(trafficCard.transform, "Exit Gate", exitGate, new Vector2(-120f, 100f), new Vector2(74f, 38f));
            CreateImage(trafficCard.transform, "Mini Car Yellow", carYellow, new Vector2(-120f, -20f), new Vector2(68f, 104f));
            CreateImage(trafficCard.transform, "Mini Car Red", carRed, new Vector2(120f, 20f), new Vector2(68f, 104f));
            CreateImage(trafficCard.transform, "Mini Car Blue", carBlue, new Vector2(0f, -50f), new Vector2(68f, 104f), 90f);

            // 6. Action Section (Play/Continue, Level Map, Settings)
            var actionObj = new GameObject("Action Section", typeof(RectTransform));
            actionObj.transform.SetParent(safeAreaObj.transform, false);
            var actionRect = actionObj.GetComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(0.5f, 0.5f);
            actionRect.anchorMax = new Vector2(0.5f, 0.5f);
            actionRect.pivot = new Vector2(0.5f, 0.5f);
            actionRect.anchoredPosition = new Vector2(0f, -150f);
            actionRect.sizeDelta = new Vector2(600f, 380f);

            // Play / Continue CTA
            var playBtnObj = new GameObject("PlayContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
            playBtnObj.transform.SetParent(actionObj.transform, false);
            var playRect = playBtnObj.GetComponent<RectTransform>();
            playRect.anchorMin = new Vector2(0.5f, 0.5f);
            playRect.anchorMax = new Vector2(0.5f, 0.5f);
            playRect.anchoredPosition = new Vector2(0f, 110f);
            playRect.sizeDelta = new Vector2(560f, 126f);
            var playImg = playBtnObj.GetComponent<Image>();
            playImg.sprite = badgePill;
            playImg.type = Image.Type.Sliced;
            playImg.color = AccentBlue;
            var playBtn = playBtnObj.GetComponent<Button>();
            playBtn.targetGraphic = playImg;

            var playLabel = CreateText(playBtnObj.transform, "Label", "CONTINUE", new Vector2(0f, 14f), new Vector2(500f, 48f), 38f, Color.white, FontStyles.Bold);
            var playSubtext = CreateText(playBtnObj.transform, "Subtext", "LEVEL 1", new Vector2(0f, -24f), new Vector2(500f, 32f), 22f, new Color32(210, 230, 255, 255), FontStyles.Bold);

            // Level Map Button
            var mapBtnObj = new GameObject("LevelMapButton", typeof(RectTransform), typeof(Image), typeof(Button));
            mapBtnObj.transform.SetParent(actionObj.transform, false);
            var mapRect = mapBtnObj.GetComponent<RectTransform>();
            mapRect.anchorMin = new Vector2(0.5f, 0.5f);
            mapRect.anchorMax = new Vector2(0.5f, 0.5f);
            mapRect.anchoredPosition = new Vector2(0f, -20f);
            mapRect.sizeDelta = new Vector2(560f, 106f);
            var mapImg = mapBtnObj.GetComponent<Image>();
            mapImg.sprite = badgePill;
            mapImg.type = Image.Type.Sliced;
            mapImg.color = Color.white;
            var mapBtn = mapBtnObj.GetComponent<Button>();
            mapBtn.targetGraphic = mapImg;

            var mapLabel = CreateText(mapBtnObj.transform, "Label", "LEVEL MAP", Vector2.zero, new Vector2(500f, 48f), 32f, PrimaryNavy, FontStyles.Bold);

            // Settings Button (Small circular icon)
            var settingsBtnObj = new GameObject("SettingsButton", typeof(RectTransform), typeof(Image), typeof(Button));
            settingsBtnObj.transform.SetParent(actionObj.transform, false);
            var settingsRect = settingsBtnObj.GetComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(0.5f, 0.5f);
            settingsRect.anchorMax = new Vector2(0.5f, 0.5f);
            settingsRect.anchoredPosition = new Vector2(0f, -120f);
            settingsRect.sizeDelta = new Vector2(96f, 96f);
            var settingsImg = settingsBtnObj.GetComponent<Image>();
            settingsImg.sprite = buttonCircle;
            settingsImg.color = Color.white;
            var settingsBtn = settingsBtnObj.GetComponent<Button>();
            settingsBtn.targetGraphic = settingsImg;

            var settingsIcon = CreateImage(settingsBtnObj.transform, "Icon", iconSettings, Vector2.zero, new Vector2(44f, 44f));
            settingsIcon.GetComponent<Image>().color = PrimaryNavy;

            // 7. Progress Summary Card (Bottom)
            var progressCard = new GameObject("ProgressCard", typeof(RectTransform), typeof(Image));
            progressCard.transform.SetParent(safeAreaObj.transform, false);
            var progRect = progressCard.GetComponent<RectTransform>();
            progRect.anchorMin = new Vector2(0.5f, 0f);
            progRect.anchorMax = new Vector2(0.5f, 0f);
            progRect.pivot = new Vector2(0.5f, 0f);
            progRect.anchoredPosition = new Vector2(0f, 60f);
            progRect.sizeDelta = new Vector2(620f, 150f);
            var progImg = progressCard.GetComponent<Image>();
            progImg.sprite = cardBoardBg;
            progImg.type = Image.Type.Sliced;
            progImg.color = Color.white;
            progImg.raycastTarget = false;

            var progLevelText = CreateText(progressCard.transform, "Progress Level", "LEVEL 1 OF 23", new Vector2(-120f, 26f), new Vector2(300f, 38f), 24f, PrimaryNavy, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            var progStarsText = CreateText(progressCard.transform, "Progress Stars", "0 / 69 STARS", new Vector2(140f, 26f), new Vector2(240f, 38f), 24f, AccentGold, FontStyles.Bold, TextAlignmentOptions.MidlineRight);

            // Progress bar
            var barBg = new GameObject("BarBg", typeof(RectTransform), typeof(Image));
            barBg.transform.SetParent(progressCard.transform, false);
            var barBgRect = barBg.GetComponent<RectTransform>();
            barBgRect.anchoredPosition = new Vector2(0f, -24f);
            barBgRect.sizeDelta = new Vector2(520f, 16f);
            var barBgImg = barBg.GetComponent<Image>();
            barBgImg.sprite = badgePill;
            barBgImg.type = Image.Type.Sliced;
            barBgImg.color = MutedGray;
            barBgImg.raycastTarget = false;

            var barFill = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(barBg.transform, false);
            var barFillRect = barFill.GetComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = Vector2.one;
            barFillRect.sizeDelta = Vector2.zero;
            var barFillImg = barFill.GetComponent<Image>();
            barFillImg.sprite = badgePill;
            barFillImg.type = Image.Type.Filled;
            barFillImg.fillMethod = Image.FillMethod.Horizontal;
            barFillImg.fillAmount = 0.05f;
            barFillImg.color = AccentBlue;
            barFillImg.raycastTarget = false;

            // 8. Settings Modal
            var settingsModal = CreateSettingsModal(safeAreaObj.transform, buttonCircle, badgePill, cardBoardBg);

            // 9. Wire MainMenuController
            var menuController = canvasObj.GetComponent<MainMenuController>();
            if (menuController == null) menuController = canvasObj.AddComponent<MainMenuController>();

            var ser = new SerializedObject(menuController);
            ser.FindProperty("playContinueButton").objectReferenceValue = playBtn;
            ser.FindProperty("playContinueLabel").objectReferenceValue = playLabel;
            ser.FindProperty("playContinueSubtext").objectReferenceValue = playSubtext;
            ser.FindProperty("levelMapButton").objectReferenceValue = mapBtn;
            ser.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
            ser.FindProperty("progressLevelText").objectReferenceValue = progLevelText;
            ser.FindProperty("progressStarsText").objectReferenceValue = progStarsText;
            ser.FindProperty("progressBarFill").objectReferenceValue = barFillImg;
            ser.FindProperty("settingsModal").objectReferenceValue = settingsModal;
            ser.ApplyModifiedProperties();
            EditorUtility.SetDirty(menuController);

            // Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("✅ [MetaUIBuilder] Successfully reconstructed MainMenu scene!");
        }

        [MenuItem("Tools/Rebuild Level Map UI")]
        public static void RebuildLevelMap()
        {
            var scenePath = "Assets/_Project/Scenes/LevelMap.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 1. Ensure Camera
            EnsureSceneCamera();

            // 2. Load Sprites
            var buttonCircle = LoadSprite("Assets/_Project/Sprites/UI/button_circle.png");
            var badgePill = LoadSprite("Assets/_Project/Sprites/UI/badge_pill.png");
            var cardBoardBg = LoadSprite("Assets/_Project/Sprites/UI/card_board_bg.png");
            var iconBack = LoadSprite("Assets/_Project/Sprites/UI/icon_back.png");
            var iconSettings = LoadSprite("Assets/_Project/Sprites/UI/icon_settings.png");
            var carYellow = LoadSprite("Assets/_Project/Sprites/Cars/car_yellow.png");
            var roadStraight = LoadSprite("Assets/_Project/Sprites/Roads/road_straight_v.png");
            var starFull = LoadSprite("Assets/_Project/Sprites/UI/star_full.png");
            var starEmpty = LoadSprite("Assets/_Project/Sprites/UI/star_empty.png");

            // 3. Ensure Canvas & Safe Area
            var (canvasObj, safeAreaObj) = EnsureCanvasAndSafeArea("Level Map Canvas");

            for (var i = safeAreaObj.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(safeAreaObj.transform.GetChild(i).gameObject);
            }

            // 4. Background
            CreateBackground(safeAreaObj.transform);

            // 5. Header (Back Button, Title Group, Settings Button)
            var headerObj = new GameObject("Header", typeof(RectTransform));
            headerObj.transform.SetParent(safeAreaObj.transform, false);
            var headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, -20f);
            headerRect.sizeDelta = new Vector2(0f, 130f);

            // Back Button
            var backBtnObj = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            backBtnObj.transform.SetParent(headerObj.transform, false);
            var backBtnRect = backBtnObj.GetComponent<RectTransform>();
            backBtnRect.anchorMin = new Vector2(0f, 0.5f);
            backBtnRect.anchorMax = new Vector2(0f, 0.5f);
            backBtnRect.pivot = new Vector2(0f, 0.5f);
            backBtnRect.anchoredPosition = new Vector2(44f, 0f);
            backBtnRect.sizeDelta = new Vector2(96f, 96f);
            var backBtnImg = backBtnObj.GetComponent<Image>();
            backBtnImg.sprite = buttonCircle;
            backBtnImg.color = Color.white;
            var backBtn = backBtnObj.GetComponent<Button>();
            backBtn.targetGraphic = backBtnImg;

            var backIcon = CreateImage(backBtnObj.transform, "Icon", iconBack, Vector2.zero, new Vector2(42f, 42f));
            backIcon.GetComponent<Image>().color = PrimaryNavy;

            // Title Group
            var titleGroup = new GameObject("TitleGroup", typeof(RectTransform));
            titleGroup.transform.SetParent(headerObj.transform, false);
            var tgRect = titleGroup.GetComponent<RectTransform>();
            tgRect.anchorMin = new Vector2(0.5f, 0.5f);
            tgRect.anchorMax = new Vector2(0.5f, 0.5f);
            tgRect.anchoredPosition = Vector2.zero;
            tgRect.sizeDelta = new Vector2(600f, 110f);

            CreateText(titleGroup.transform, "Title", "LEVEL MAP", new Vector2(0f, 18f), new Vector2(560f, 54f), 44f, PrimaryNavy, FontStyles.Bold);
            CreateText(titleGroup.transform, "Subtitle", "Follow the road. Clear the traffic.", new Vector2(0f, -24f), new Vector2(560f, 38f), 24f, SubtitleGray, FontStyles.Normal);

            // Settings Button
            var settingsBtnObj = new GameObject("SettingsButton", typeof(RectTransform), typeof(Image), typeof(Button));
            settingsBtnObj.transform.SetParent(headerObj.transform, false);
            var settingsBtnRect = settingsBtnObj.GetComponent<RectTransform>();
            settingsBtnRect.anchorMin = new Vector2(1f, 0.5f);
            settingsBtnRect.anchorMax = new Vector2(1f, 0.5f);
            settingsBtnRect.pivot = new Vector2(1f, 0.5f);
            settingsBtnRect.anchoredPosition = new Vector2(-44f, 0f);
            settingsBtnRect.sizeDelta = new Vector2(96f, 96f);
            var settingsBtnImg = settingsBtnObj.GetComponent<Image>();
            settingsBtnImg.sprite = buttonCircle;
            settingsBtnImg.color = Color.white;
            var settingsBtn = settingsBtnObj.GetComponent<Button>();
            settingsBtn.targetGraphic = settingsBtnImg;

            var settingsIcon = CreateImage(settingsBtnObj.transform, "Icon", iconSettings, Vector2.zero, new Vector2(42f, 42f));
            settingsIcon.GetComponent<Image>().color = PrimaryNavy;

            // 6. Map Scroll View
            var scrollRoot = new GameObject("Map Scroll View", typeof(RectTransform), typeof(ScrollRect));
            scrollRoot.transform.SetParent(safeAreaObj.transform, false);
            var scrollRect = scrollRoot.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(0f, 0f);
            scrollRect.offsetMax = new Vector2(0f, -160f);

            var scrollComp = scrollRoot.GetComponent<ScrollRect>();
            scrollComp.horizontal = false;
            scrollComp.vertical = true;
            scrollComp.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollRoot.transform, false);
            var vpRect = viewport.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            scrollComp.viewport = vpRect;

            var totalLevels = LevelCatalog.HighestCatalogLevel;
            var contentHeight = 220f + (totalLevels * 195f);

            var content = new GameObject("Road Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 0f);
            contentRect.pivot = new Vector2(0.5f, 0f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, contentHeight);
            scrollComp.content = contentRect;

            // 7. Generate Winding Road and Level Nodes (Level 1 at bottom, Level 23 at top)
            var nodePositions = new Vector2[totalLevels];
            for (var i = 0; i < totalLevels; i++)
            {
                var x = Mathf.Sin(i * 0.95f) * 220f;
                var y = 140f + (i * 195f);
                nodePositions[i] = new Vector2(x, y);
            }

            // Road segments connecting nodes
            var roadContainer = new GameObject("Road Tracks", typeof(RectTransform));
            roadContainer.transform.SetParent(content.transform, false);
            var roadContainerRect = roadContainer.GetComponent<RectTransform>();
            roadContainerRect.anchorMin = Vector2.zero;
            roadContainerRect.anchorMax = Vector2.one;
            roadContainerRect.sizeDelta = Vector2.zero;

            for (var i = 0; i < totalLevels - 1; i++)
            {
                var p1 = nodePositions[i];
                var p2 = nodePositions[i + 1];
                CreateRoadSegment(roadContainer.transform, p1, p2, roadStraight);
            }

            // Create Level Nodes
            var levelMapController = canvasObj.GetComponent<LevelMapController>();
            if (levelMapController == null) levelMapController = canvasObj.AddComponent<LevelMapController>();

            var nodeComponents = new List<LevelNode>();
            for (var i = 0; i < totalLevels; i++)
            {
                var levelId = i + 1;
                var pos = nodePositions[i];
                var node = CreateLevelNode(content.transform, levelId, pos, buttonCircle, badgePill, carYellow, starFull, starEmpty);
                nodeComponents.Add(node);
            }

            // 8. Settings Modal
            var settingsModal = CreateSettingsModal(safeAreaObj.transform, buttonCircle, badgePill, cardBoardBg);

            // 9. Wire LevelMapController
            var mapSer = new SerializedObject(levelMapController);
            mapSer.FindProperty("backButton").objectReferenceValue = backBtn;
            mapSer.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
            mapSer.FindProperty("scrollRect").objectReferenceValue = scrollComp;
            mapSer.FindProperty("mapContent").objectReferenceValue = contentRect;
            mapSer.FindProperty("settingsModal").objectReferenceValue = settingsModal;

            var nodesArray = mapSer.FindProperty("levelNodes");
            nodesArray.arraySize = nodeComponents.Count;
            for (var i = 0; i < nodeComponents.Count; i++)
            {
                nodesArray.GetArrayElementAtIndex(i).objectReferenceValue = nodeComponents[i];
            }
            mapSer.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelMapController);

            // Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("✅ [MetaUIBuilder] Successfully reconstructed LevelMap scene!");
        }

        private static void EnsureSceneCamera()
        {
            var camObj = GameObject.FindWithTag("MainCamera") ?? GameObject.Find("Main Camera");
            if (camObj == null)
            {
                camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camObj.tag = "MainCamera";
            }

            var cam = camObj.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7.7f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BackgroundColor;
            cam.depth = -1;
            camObj.transform.position = new Vector3(0f, 0f, -10f);
            EditorUtility.SetDirty(camObj);
        }

        private static (GameObject canvas, GameObject safeArea) EnsureCanvasAndSafeArea(string canvasName)
        {
            var canvasObj = GameObject.Find(canvasName) ?? GameObject.Find("Canvas") ?? GameObject.Find("HUD");
            if (canvasObj == null)
            {
                canvasObj = new GameObject(canvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            }
            canvasObj.name = canvasName;

            var canvas = canvasObj.GetComponent<Canvas>();
            var cam = (GameObject.FindWithTag("MainCamera") ?? GameObject.Find("Main Camera"))?.GetComponent<Camera>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;

            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            var safeAreaObj = canvasObj.transform.Find("Safe Area")?.gameObject;
            if (safeAreaObj == null)
            {
                safeAreaObj = new GameObject("Safe Area", typeof(RectTransform), typeof(SafeAreaFitter));
                safeAreaObj.transform.SetParent(canvasObj.transform, false);
            }

            var saRect = safeAreaObj.GetComponent<RectTransform>();
            saRect.anchorMin = Vector2.zero;
            saRect.anchorMax = Vector2.one;
            saRect.offsetMin = Vector2.zero;
            saRect.offsetMax = Vector2.zero;

            return (canvasObj, safeAreaObj);
        }

        private static void CreateBackground(Transform parent)
        {
            var bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(parent, false);
            var rect = bgObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = bgObj.GetComponent<Image>();
            img.color = BackgroundColor;
            img.raycastTarget = false;
            bgObj.transform.SetAsFirstSibling();
        }

        private static LevelNode CreateLevelNode(Transform parent, int levelId, Vector2 pos, Sprite buttonCircle, Sprite badgePill, Sprite carSprite, Sprite starFull, Sprite starEmpty)
        {
            var nodeObj = new GameObject($"Level Node {levelId}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LevelNode));
            nodeObj.transform.SetParent(parent, false);
            var rect = nodeObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(146f, 146f);

            var img = nodeObj.GetComponent<Image>();
            img.sprite = buttonCircle;
            img.color = Color.white;
            var btn = nodeObj.GetComponent<Button>();
            btn.targetGraphic = img;

            var levelNum = CreateText(nodeObj.transform, "Level Number", $"{levelId}", new Vector2(0f, 8f), new Vector2(130f, 48f), 34f, PrimaryNavy, FontStyles.Bold);

            // Star rating container (Images instead of TMP characters)
            var starsObj = new GameObject("Stars", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            starsObj.transform.SetParent(nodeObj.transform, false);
            var starsRect = starsObj.GetComponent<RectTransform>();
            starsRect.anchorMin = new Vector2(0.5f, 0f);
            starsRect.anchorMax = new Vector2(0.5f, 0f);
            starsRect.anchoredPosition = new Vector2(0f, -22f);
            starsRect.sizeDelta = new Vector2(120f, 32f);
            var hlg = starsObj.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            var s1 = CreateImage(starsObj.transform, "Star 1", starFull, Vector2.zero, new Vector2(26f, 26f)).GetComponent<Image>();
            var s2 = CreateImage(starsObj.transform, "Star 2", starFull, Vector2.zero, new Vector2(26f, 26f)).GetComponent<Image>();
            var s3 = CreateImage(starsObj.transform, "Star 3", starFull, Vector2.zero, new Vector2(26f, 26f)).GetComponent<Image>();

            // Car marker
            var carMarker = CreateImage(nodeObj.transform, "Car Marker", carSprite, new Vector2(0f, 82f), new Vector2(56f, 86f));
            carMarker.SetActive(false);

            // Current Badge Pill
            var badgeObj = new GameObject("Current Badge", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(nodeObj.transform, false);
            var bRect = badgeObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 0f);
            bRect.anchorMax = new Vector2(0.5f, 0f);
            bRect.anchoredPosition = new Vector2(0f, -24f);
            bRect.sizeDelta = new Vector2(120f, 32f);
            var bImg = badgeObj.GetComponent<Image>();
            bImg.sprite = badgePill;
            bImg.type = Image.Type.Sliced;
            bImg.color = AccentBlue;
            bImg.raycastTarget = false;
            CreateText(badgeObj.transform, "Text", "CURRENT", Vector2.zero, new Vector2(110f, 28f), 17f, Color.white, FontStyles.Bold);
            badgeObj.SetActive(false);

            // Lock Icon
            var lockObj = CreateText(nodeObj.transform, "Lock Icon", "LOCKED", new Vector2(0f, -22f), new Vector2(100f, 26f), 18f, SubtitleGray, FontStyles.Bold);
            lockObj.gameObject.SetActive(false);

            // Serialize LevelNode
            var levelNodeComp = nodeObj.GetComponent<LevelNode>();
            var ser = new SerializedObject(levelNodeComp);
            ser.FindProperty("button").objectReferenceValue = btn;
            ser.FindProperty("nodeBackground").objectReferenceValue = img;
            ser.FindProperty("levelNumberText").objectReferenceValue = levelNum;
            ser.FindProperty("carMarker").objectReferenceValue = carMarker;
            ser.FindProperty("starsContainer").objectReferenceValue = starsObj;
            ser.FindProperty("starFullSprite").objectReferenceValue = starFull;
            ser.FindProperty("starEmptySprite").objectReferenceValue = starEmpty;
            ser.FindProperty("currentBadge").objectReferenceValue = badgeObj;
            ser.FindProperty("lockIcon").objectReferenceValue = lockObj.gameObject;

            var starsProp = ser.FindProperty("starImages");
            starsProp.arraySize = 3;
            starsProp.GetArrayElementAtIndex(0).objectReferenceValue = s1;
            starsProp.GetArrayElementAtIndex(1).objectReferenceValue = s2;
            starsProp.GetArrayElementAtIndex(2).objectReferenceValue = s3;

            ser.ApplyModifiedProperties();
            return levelNodeComp;
        }

        private static void CreateRoadSegment(Transform parent, Vector2 p1, Vector2 p2, Sprite roadSprite)
        {
            var mid = (p1 + p2) * 0.5f;
            var dir = p2 - p1;
            var dist = dir.magnitude;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

            var roadObj = new GameObject("Road", typeof(RectTransform), typeof(Image));
            roadObj.transform.SetParent(parent, false);
            var rect = roadObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = mid;
            rect.sizeDelta = new Vector2(58f, dist);
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);

            var img = roadObj.GetComponent<Image>();
            img.sprite = roadSprite;
            img.type = Image.Type.Tiled;
            img.color = new Color32(200, 212, 228, 255);
            img.raycastTarget = false;
        }

        private static SettingsModal CreateSettingsModal(Transform parent, Sprite buttonCircle, Sprite badgePill, Sprite cardBoardBg)
        {
            var modalRoot = new GameObject("Settings Modal", typeof(RectTransform), typeof(Image), typeof(SettingsModal));
            modalRoot.transform.SetParent(parent, false);
            var modalRect = modalRoot.GetComponent<RectTransform>();
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.offsetMin = Vector2.zero;
            modalRect.offsetMax = Vector2.zero;
            var dimImg = modalRoot.GetComponent<Image>();
            dimImg.color = new Color(0.04f, 0.08f, 0.16f, 0.60f);

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(modalRoot.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(680f, 680f);
            var cardImg = card.GetComponent<Image>();
            cardImg.sprite = cardBoardBg;
            cardImg.type = Image.Type.Sliced;
            cardImg.color = Color.white;

            // Card Header
            CreateText(card.transform, "Title", "SETTINGS", new Vector2(0f, 260f), new Vector2(400f, 54f), 38f, PrimaryNavy, FontStyles.Bold);

            // Close button
            var closeBtnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnObj.transform.SetParent(card.transform, false);
            var closeRect = closeBtnObj.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-36f, -36f);
            closeRect.sizeDelta = new Vector2(76f, 76f);
            var closeImg = closeBtnObj.GetComponent<Image>();
            closeImg.sprite = buttonCircle;
            closeImg.color = MutedGray;
            var closeBtn = closeBtnObj.GetComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            CreateText(closeBtnObj.transform, "Icon", "✕", Vector2.zero, new Vector2(40f, 40f), 28f, PrimaryNavy, FontStyles.Bold);

            // Rows (Sound, Music, Haptics)
            var (soundBtn, soundText, soundImg) = CreateSettingRow(card.transform, "Sound Effects", new Vector2(0f, 140f), badgePill);
            var (musicBtn, musicText, musicImg) = CreateSettingRow(card.transform, "Music", new Vector2(0f, 45f), badgePill);
            var (hapticsBtn, hapticsText, hapticsImg) = CreateSettingRow(card.transform, "Haptics", new Vector2(0f, -50f), badgePill);

            // Reset Progress Button
            var resetBtnObj = new GameObject("ResetButton", typeof(RectTransform), typeof(Image), typeof(Button));
            resetBtnObj.transform.SetParent(card.transform, false);
            var resetRect = resetBtnObj.GetComponent<RectTransform>();
            resetRect.anchoredPosition = new Vector2(0f, -165f);
            resetRect.sizeDelta = new Vector2(440f, 84f);
            var resetImg = resetBtnObj.GetComponent<Image>();
            resetImg.sprite = badgePill;
            resetImg.type = Image.Type.Sliced;
            resetImg.color = new Color32(254, 235, 235, 255);
            var resetBtn = resetBtnObj.GetComponent<Button>();
            resetBtn.targetGraphic = resetImg;
            CreateText(resetBtnObj.transform, "Label", "Reset All Progress", Vector2.zero, new Vector2(400f, 40f), 24f, new Color32(235, 87, 87, 255), FontStyles.Bold);

            // Reset Confirmation Dialog (Sub-modal)
            var confirmObj = new GameObject("ResetConfirmDialog", typeof(RectTransform), typeof(Image));
            confirmObj.transform.SetParent(modalRoot.transform, false);
            var confirmRect = confirmObj.GetComponent<RectTransform>();
            confirmRect.anchorMin = Vector2.zero;
            confirmRect.anchorMax = Vector2.one;
            confirmRect.offsetMin = Vector2.zero;
            confirmRect.offsetMax = Vector2.zero;
            confirmObj.GetComponent<Image>().color = new Color(0.04f, 0.08f, 0.16f, 0.70f);

            var confCard = new GameObject("ConfirmCard", typeof(RectTransform), typeof(Image));
            confCard.transform.SetParent(confirmObj.transform, false);
            var confCardRect = confCard.GetComponent<RectTransform>();
            confCardRect.anchorMin = new Vector2(0.5f, 0.5f);
            confCardRect.anchorMax = new Vector2(0.5f, 0.5f);
            confCardRect.anchoredPosition = Vector2.zero;
            confCardRect.sizeDelta = new Vector2(620f, 400f);
            var confCardImg = confCard.GetComponent<Image>();
            confCardImg.sprite = cardBoardBg;
            confCardImg.type = Image.Type.Sliced;
            confCardImg.color = Color.white;

            CreateText(confCard.transform, "Title", "Reset All Progress?", new Vector2(0f, 110f), new Vector2(500f, 48f), 32f, PrimaryNavy, FontStyles.Bold);
            CreateText(confCard.transform, "Message", "This will reset all unlocked levels\nand stars. This cannot be undone.", new Vector2(0f, 30f), new Vector2(520f, 80f), 22f, SubtitleGray, FontStyles.Normal);

            // Cancel Button
            var cancelBtnObj = new GameObject("CancelButton", typeof(RectTransform), typeof(Image), typeof(Button));
            cancelBtnObj.transform.SetParent(confCard.transform, false);
            var cancelRect = cancelBtnObj.GetComponent<RectTransform>();
            cancelRect.anchoredPosition = new Vector2(-130f, -95f);
            cancelRect.sizeDelta = new Vector2(210f, 84f);
            var cancelImg = cancelBtnObj.GetComponent<Image>();
            cancelImg.sprite = badgePill;
            cancelImg.type = Image.Type.Sliced;
            cancelImg.color = MutedGray;
            var cancelBtn = cancelBtnObj.GetComponent<Button>();
            cancelBtn.targetGraphic = cancelImg;
            CreateText(cancelBtnObj.transform, "Label", "CANCEL", Vector2.zero, new Vector2(180f, 36f), 24f, PrimaryNavy, FontStyles.Bold);

            // Confirm Button
            var okBtnObj = new GameObject("OkButton", typeof(RectTransform), typeof(Image), typeof(Button));
            okBtnObj.transform.SetParent(confCard.transform, false);
            var okRect = okBtnObj.GetComponent<RectTransform>();
            okRect.anchoredPosition = new Vector2(130f, -95f);
            okRect.sizeDelta = new Vector2(210f, 84f);
            var okImg = okBtnObj.GetComponent<Image>();
            okImg.sprite = badgePill;
            okImg.type = Image.Type.Sliced;
            okImg.color = new Color32(235, 87, 87, 255);
            var okBtn = okBtnObj.GetComponent<Button>();
            okBtn.targetGraphic = okImg;
            CreateText(okBtnObj.transform, "Label", "RESET", Vector2.zero, new Vector2(180f, 36f), 24f, Color.white, FontStyles.Bold);

            confirmObj.SetActive(false);

            // Wire SettingsModal
            var modalComp = modalRoot.GetComponent<SettingsModal>();
            var ser = new SerializedObject(modalComp);
            ser.FindProperty("modalCard").objectReferenceValue = card;
            ser.FindProperty("closeButton").objectReferenceValue = closeBtn;
            ser.FindProperty("soundToggleButton").objectReferenceValue = soundBtn;
            ser.FindProperty("soundToggleText").objectReferenceValue = soundText;
            ser.FindProperty("soundToggleImage").objectReferenceValue = soundImg;
            ser.FindProperty("musicToggleButton").objectReferenceValue = musicBtn;
            ser.FindProperty("musicToggleText").objectReferenceValue = musicText;
            ser.FindProperty("musicToggleImage").objectReferenceValue = musicImg;
            ser.FindProperty("hapticsToggleButton").objectReferenceValue = hapticsBtn;
            ser.FindProperty("hapticsToggleText").objectReferenceValue = hapticsText;
            ser.FindProperty("hapticsToggleImage").objectReferenceValue = hapticsImg;
            ser.FindProperty("resetProgressButton").objectReferenceValue = resetBtn;
            ser.FindProperty("resetConfirmDialog").objectReferenceValue = confirmObj;
            ser.FindProperty("resetConfirmCancelButton").objectReferenceValue = cancelBtn;
            ser.FindProperty("resetConfirmOkButton").objectReferenceValue = okBtn;
            ser.ApplyModifiedProperties();

            modalRoot.SetActive(false);
            return modalComp;
        }

        private static (Button btn, TMP_Text label, Image img) CreateSettingRow(Transform parent, string title, Vector2 pos, Sprite badgePill)
        {
            var rowObj = new GameObject(title + " Row", typeof(RectTransform));
            rowObj.transform.SetParent(parent, false);
            var rect = rowObj.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(520f, 70f);

            CreateText(rowObj.transform, "Title", title, new Vector2(-90f, 0f), new Vector2(300f, 40f), 28f, PrimaryNavy, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

            var toggleObj = new GameObject("Toggle", typeof(RectTransform), typeof(Image), typeof(Button));
            toggleObj.transform.SetParent(rowObj.transform, false);
            var tRect = toggleObj.GetComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(170f, 0f);
            tRect.sizeDelta = new Vector2(130f, 54f);
            var tImg = toggleObj.GetComponent<Image>();
            tImg.sprite = badgePill;
            tImg.type = Image.Type.Sliced;
            tImg.color = AccentBlue;
            var tBtn = toggleObj.GetComponent<Button>();
            tBtn.targetGraphic = tImg;

            var tText = CreateText(toggleObj.transform, "Label", "ON", Vector2.zero, new Vector2(100f, 36f), 22f, Color.white, FontStyles.Bold);

            return (tBtn, tText, tImg);
        }

        private static GameObject CreateImage(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size, float rotation = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return go;
        }

        private static TMP_Text CreateText(Transform parent, string name, string content, Vector2 pos, Vector2 size, float fontSize, Color color, FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = style;
            text.alignment = align;
            text.raycastTarget = false;
            return text;
        }

        private static Sprite LoadSprite(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
    }
}
