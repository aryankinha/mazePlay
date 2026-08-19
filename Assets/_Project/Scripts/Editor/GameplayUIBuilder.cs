using ArrowMaze.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowMaze.Editor
{
    public static class GameplayUIBuilder
    {
        private static readonly Color BackgroundColor = new Color32(244, 247, 252, 255);
        private static readonly Color PrimaryNavy = new Color32(23, 35, 61, 255);
        private static readonly Color SubtitleGray = new Color32(102, 117, 143, 255);
        private static readonly Color AccentBlue = new Color32(47, 128, 237, 255);

        [MenuItem("Tools/Rebuild Gameplay UI")]
        public static void Rebuild()
        {
            var scenePath = "Assets/_Project/Scenes/Gameplay.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 1. Camera setup
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = BackgroundColor;
                EditorUtility.SetDirty(cam);
            }

            // 2. Load Sprites
            var buttonCircle = LoadSprite("Assets/_Project/Sprites/UI/button_circle.png");
            var badgePill = LoadSprite("Assets/_Project/Sprites/UI/badge_pill.png");
            var heartFull = LoadSprite("Assets/_Project/Sprites/UI/heart_full.png");
            var heartEmpty = LoadSprite("Assets/_Project/Sprites/UI/heart_empty.png");
            var iconBack = LoadSprite("Assets/_Project/Sprites/UI/icon_back.png");
            var iconSettings = LoadSprite("Assets/_Project/Sprites/UI/icon_settings.png");
            var iconCar = LoadSprite("Assets/_Project/Sprites/UI/icon_car_badge.png");
            var iconHint = LoadSprite("Assets/_Project/Sprites/UI/icon_hint.png");
            var iconUndo = LoadSprite("Assets/_Project/Sprites/UI/icon_undo.png");

            // 3. Find Canvas (HUD or Canvas)
            var canvasObj = GameObject.Find("HUD") ?? GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                var canvasComp = Object.FindAnyObjectByType<Canvas>();
                if (canvasComp != null)
                {
                    canvasObj = canvasComp.gameObject;
                }
            }

            if (canvasObj == null)
            {
                Debug.LogError("No Canvas or HUD found in Gameplay scene!");
                return;
            }

            var scaler = canvasObj.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasObj.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var safeAreaObj = canvasObj.transform.Find("Safe Area")?.gameObject;
            if (safeAreaObj == null)
            {
                safeAreaObj = new GameObject("Safe Area", typeof(RectTransform), typeof(SafeAreaFitter));
                safeAreaObj.transform.SetParent(canvasObj.transform, false);
            }

            var safeAreaRect = safeAreaObj.GetComponent<RectTransform>();
            safeAreaRect.anchorMin = Vector2.zero;
            safeAreaRect.anchorMax = Vector2.one;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;

            // Preserve Result Popup if present
            Transform resultPopup = canvasObj.transform.Find("Result Popup") ?? safeAreaObj.transform.Find("Result Popup");
            if (resultPopup != null)
            {
                resultPopup.SetParent(safeAreaObj.transform, false);
            }

            // Remove all other children to build a fresh, clean, responsive UI structure
            for (var i = safeAreaObj.transform.childCount - 1; i >= 0; i--)
            {
                var child = safeAreaObj.transform.GetChild(i);
                if (child != resultPopup)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            // 4. Background
            var bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(safeAreaObj.transform, false);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgObj.GetComponent<Image>();
            // The roads and cars are SpriteRenderers drawn by the camera. A Screen Space
            // Overlay Image always renders above them, even as the first UI sibling, so
            // this must remain transparent; the camera owns the actual background colour.
            bgImg.color = new Color(BackgroundColor.r, BackgroundColor.g, BackgroundColor.b, 0f);
            bgImg.raycastTarget = false;
            bgObj.transform.SetAsFirstSibling();

            // 5. Header Bar (Anchored Top: 0..1 x 1, height 140)
            var headerObj = new GameObject("Header", typeof(RectTransform));
            headerObj.transform.SetParent(safeAreaObj.transform, false);
            var headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, -20f);
            headerRect.sizeDelta = new Vector2(0f, 130f);

            // Back Button (Top-Left)
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

            var backIconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            backIconObj.transform.SetParent(backBtnObj.transform, false);
            var backIconRect = backIconObj.GetComponent<RectTransform>();
            backIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            backIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            backIconRect.anchoredPosition = Vector2.zero;
            backIconRect.sizeDelta = new Vector2(42f, 42f);
            var backIconImg = backIconObj.GetComponent<Image>();
            backIconImg.sprite = iconBack;
            backIconImg.color = PrimaryNavy;
            backIconImg.preserveAspect = true;
            backIconImg.raycastTarget = false;

            // Title Group (Centered in Header)
            var titleGroupObj = new GameObject("TitleGroup", typeof(RectTransform));
            titleGroupObj.transform.SetParent(headerObj.transform, false);
            var titleGroupRect = titleGroupObj.GetComponent<RectTransform>();
            titleGroupRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleGroupRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleGroupRect.pivot = new Vector2(0.5f, 0.5f);
            titleGroupRect.anchoredPosition = Vector2.zero;
            titleGroupRect.sizeDelta = new Vector2(560f, 110f);

            var titleTextObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleTextObj.transform.SetParent(titleGroupObj.transform, false);
            var titleTextRect = titleTextObj.GetComponent<RectTransform>();
            titleTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleTextRect.anchoredPosition = new Vector2(0f, 18f);
            titleTextRect.sizeDelta = new Vector2(560f, 54f);
            var titleTMP = titleTextObj.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "Tap Away Cars";
            titleTMP.fontSize = 44f;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = PrimaryNavy;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.raycastTarget = false;

            var levelTextObj = new GameObject("Level Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            levelTextObj.transform.SetParent(titleGroupObj.transform, false);
            var levelTextRect = levelTextObj.GetComponent<RectTransform>();
            levelTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            levelTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            levelTextRect.anchoredPosition = new Vector2(0f, -24f);
            levelTextRect.sizeDelta = new Vector2(560f, 38f);
            var levelTMP = levelTextObj.GetComponent<TextMeshProUGUI>();
            levelTMP.text = "Level 23";
            levelTMP.fontSize = 26f;
            levelTMP.fontStyle = FontStyles.Normal;
            levelTMP.color = SubtitleGray;
            levelTMP.alignment = TextAlignmentOptions.Center;
            levelTMP.raycastTarget = false;

            // Settings Button (Top-Right)
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

            var settingsIconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            settingsIconObj.transform.SetParent(settingsBtnObj.transform, false);
            var settingsIconRect = settingsIconObj.GetComponent<RectTransform>();
            settingsIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            settingsIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            settingsIconRect.anchoredPosition = Vector2.zero;
            settingsIconRect.sizeDelta = new Vector2(42f, 42f);
            var settingsIconImg = settingsIconObj.GetComponent<Image>();
            settingsIconImg.sprite = iconSettings;
            settingsIconImg.color = PrimaryNavy;
            settingsIconImg.preserveAspect = true;
            settingsIconImg.raycastTarget = false;

            // 6. Status Row (Anchored Top below Header: anchoredPosition = (0, -165), height 80)
            var statusRowObj = new GameObject("StatusRow", typeof(RectTransform));
            statusRowObj.transform.SetParent(safeAreaObj.transform, false);
            var statusRowRect = statusRowObj.GetComponent<RectTransform>();
            statusRowRect.anchorMin = new Vector2(0f, 1f);
            statusRowRect.anchorMax = new Vector2(1f, 1f);
            statusRowRect.pivot = new Vector2(0.5f, 1f);
            statusRowRect.anchoredPosition = new Vector2(0f, -165f);
            statusRowRect.sizeDelta = new Vector2(0f, 80f);

            // Car Counter Pill (Left)
            var carPillObj = new GameObject("CarCounterPill", typeof(RectTransform), typeof(Image));
            carPillObj.transform.SetParent(statusRowObj.transform, false);
            var carPillRect = carPillObj.GetComponent<RectTransform>();
            carPillRect.anchorMin = new Vector2(0f, 0.5f);
            carPillRect.anchorMax = new Vector2(0f, 0.5f);
            carPillRect.pivot = new Vector2(0f, 0.5f);
            carPillRect.anchoredPosition = new Vector2(44f, 0f);
            carPillRect.sizeDelta = new Vector2(210f, 74f);
            var carPillImg = carPillObj.GetComponent<Image>();
            carPillImg.sprite = badgePill;
            carPillImg.color = Color.white;
            carPillImg.raycastTarget = false;

            var carIconObj = new GameObject("CarIcon", typeof(RectTransform), typeof(Image));
            carIconObj.transform.SetParent(carPillObj.transform, false);
            var carIconRect = carIconObj.GetComponent<RectTransform>();
            carIconRect.anchorMin = new Vector2(0f, 0.5f);
            carIconRect.anchorMax = new Vector2(0f, 0.5f);
            carIconRect.pivot = new Vector2(0f, 0.5f);
            carIconRect.anchoredPosition = new Vector2(24f, 0f);
            carIconRect.sizeDelta = new Vector2(40f, 40f);
            var carIconImg = carIconObj.GetComponent<Image>();
            carIconImg.sprite = iconCar;
            carIconImg.color = PrimaryNavy;
            carIconImg.preserveAspect = true;
            carIconImg.raycastTarget = false;

            var carCountObj = new GameObject("Cars Remaining", typeof(RectTransform), typeof(TextMeshProUGUI));
            carCountObj.transform.SetParent(carPillObj.transform, false);
            var carCountRect = carCountObj.GetComponent<RectTransform>();
            carCountRect.anchorMin = Vector2.zero;
            carCountRect.anchorMax = Vector2.one;
            carCountRect.offsetMin = new Vector2(74f, 0f);
            carCountRect.offsetMax = new Vector2(-16f, 0f);
            var carCountTMP = carCountObj.GetComponent<TextMeshProUGUI>();
            carCountTMP.text = "42";
            carCountTMP.fontSize = 32f;
            carCountTMP.fontStyle = FontStyles.Bold;
            carCountTMP.color = PrimaryNavy;
            carCountTMP.alignment = TextAlignmentOptions.MidlineLeft;
            carCountTMP.raycastTarget = false;

            // Lives Container (Center)
            var livesObj = new GameObject("Lives", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            livesObj.transform.SetParent(statusRowObj.transform, false);
            var livesRect = livesObj.GetComponent<RectTransform>();
            livesRect.anchorMin = new Vector2(0.5f, 0.5f);
            livesRect.anchorMax = new Vector2(0.5f, 0.5f);
            livesRect.pivot = new Vector2(0.5f, 0.5f);
            livesRect.anchoredPosition = Vector2.zero;
            livesRect.sizeDelta = new Vector2(230f, 74f);

            var livesLayout = livesObj.GetComponent<HorizontalLayoutGroup>();
            livesLayout.spacing = 16f;
            livesLayout.childAlignment = TextAnchor.MiddleCenter;
            livesLayout.childControlWidth = false;
            livesLayout.childControlHeight = false;
            livesLayout.childForceExpandWidth = false;
            livesLayout.childForceExpandHeight = false;

            var heart1 = CreateHeart(livesObj, "Heart 1", heartFull);
            var heart2 = CreateHeart(livesObj, "Heart 2", heartFull);
            var heart3 = CreateHeart(livesObj, "Heart 3", heartFull);

            // Difficulty Pill (Right)
            var diffPillObj = new GameObject("DifficultyPill", typeof(RectTransform), typeof(Image));
            diffPillObj.transform.SetParent(statusRowObj.transform, false);
            var diffPillRect = diffPillObj.GetComponent<RectTransform>();
            diffPillRect.anchorMin = new Vector2(1f, 0.5f);
            diffPillRect.anchorMax = new Vector2(1f, 0.5f);
            diffPillRect.pivot = new Vector2(1f, 0.5f);
            diffPillRect.anchoredPosition = new Vector2(-44f, 0f);
            diffPillRect.sizeDelta = new Vector2(210f, 74f);
            var diffPillImg = diffPillObj.GetComponent<Image>();
            diffPillImg.sprite = badgePill;
            diffPillImg.color = Color.white;
            diffPillImg.raycastTarget = false;

            var diffTextObj = new GameObject("Difficulty Badge", typeof(RectTransform), typeof(TextMeshProUGUI));
            diffTextObj.transform.SetParent(diffPillObj.transform, false);
            var diffTextRect = diffTextObj.GetComponent<RectTransform>();
            diffTextRect.anchorMin = Vector2.zero;
            diffTextRect.anchorMax = Vector2.one;
            diffTextRect.sizeDelta = Vector2.zero;
            var diffTMP = diffTextObj.GetComponent<TextMeshProUGUI>();
            diffTMP.text = "Normal";
            diffTMP.fontSize = 28f;
            diffTMP.fontStyle = FontStyles.Bold;
            diffTMP.color = PrimaryNavy;
            diffTMP.alignment = TextAlignmentOptions.Center;
            diffTMP.raycastTarget = false;

            // 7. Bottom Controls (Anchored Bottom: height 200)
            var bottomObj = new GameObject("BottomControls", typeof(RectTransform));
            bottomObj.transform.SetParent(safeAreaObj.transform, false);
            var bottomRect = bottomObj.GetComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0f, 0f);
            bottomRect.anchorMax = new Vector2(1f, 0f);
            bottomRect.pivot = new Vector2(0.5f, 0f);
            bottomRect.anchoredPosition = new Vector2(0f, 48f);
            bottomRect.sizeDelta = new Vector2(0f, 200f);

            // Hint Button
            var hintBtnObj = new GameObject("Hint Button", typeof(RectTransform), typeof(Image), typeof(Button));
            hintBtnObj.transform.SetParent(bottomObj.transform, false);
            var hintBtnRect = hintBtnObj.GetComponent<RectTransform>();
            hintBtnRect.anchorMin = new Vector2(0.5f, 0.5f);
            hintBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
            hintBtnRect.pivot = new Vector2(0.5f, 0.5f);
            hintBtnRect.anchoredPosition = new Vector2(-120f, 0f);
            hintBtnRect.sizeDelta = new Vector2(156f, 156f);
            var hintBtnImg = hintBtnObj.GetComponent<Image>();
            hintBtnImg.sprite = buttonCircle;
            hintBtnImg.color = Color.white;
            var hintBtn = hintBtnObj.GetComponent<Button>();
            hintBtn.targetGraphic = hintBtnImg;

            var hintIconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            hintIconObj.transform.SetParent(hintBtnObj.transform, false);
            var hintIconRect = hintIconObj.GetComponent<RectTransform>();
            hintIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            hintIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            hintIconRect.anchoredPosition = new Vector2(0f, 16f);
            hintIconRect.sizeDelta = new Vector2(50f, 50f);
            var hintIconImg = hintIconObj.GetComponent<Image>();
            hintIconImg.sprite = iconHint;
            hintIconImg.color = PrimaryNavy;
            hintIconImg.preserveAspect = true;
            hintIconImg.raycastTarget = false;

            var hintLabelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            hintLabelObj.transform.SetParent(hintBtnObj.transform, false);
            var hintLabelRect = hintLabelObj.GetComponent<RectTransform>();
            hintLabelRect.anchorMin = new Vector2(0.5f, 0.5f);
            hintLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
            hintLabelRect.anchoredPosition = new Vector2(0f, -38f);
            hintLabelRect.sizeDelta = new Vector2(120f, 32f);
            var hintLabelTMP = hintLabelObj.GetComponent<TextMeshProUGUI>();
            hintLabelTMP.text = "Hint";
            hintLabelTMP.fontSize = 22f;
            hintLabelTMP.fontStyle = FontStyles.Bold;
            hintLabelTMP.color = PrimaryNavy;
            hintLabelTMP.alignment = TextAlignmentOptions.Center;
            hintLabelTMP.raycastTarget = false;

            // Hint Count Badge
            var countBadgeObj = new GameObject("CountBadge", typeof(RectTransform), typeof(Image));
            countBadgeObj.transform.SetParent(hintBtnObj.transform, false);
            var countBadgeRect = countBadgeObj.GetComponent<RectTransform>();
            countBadgeRect.anchorMin = new Vector2(1f, 1f);
            countBadgeRect.anchorMax = new Vector2(1f, 1f);
            countBadgeRect.pivot = new Vector2(1f, 1f);
            countBadgeRect.anchoredPosition = new Vector2(-4f, -4f);
            countBadgeRect.sizeDelta = new Vector2(46f, 46f);
            var countBadgeImg = countBadgeObj.GetComponent<Image>();
            countBadgeImg.sprite = buttonCircle;
            countBadgeImg.color = AccentBlue;
            countBadgeImg.raycastTarget = false;

            var hintCountObj = new GameObject("Hint Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            hintCountObj.transform.SetParent(countBadgeObj.transform, false);
            var hintCountRect = hintCountObj.GetComponent<RectTransform>();
            hintCountRect.anchorMin = Vector2.zero;
            hintCountRect.anchorMax = Vector2.one;
            hintCountRect.sizeDelta = Vector2.zero;
            var hintCountTMP = hintCountObj.GetComponent<TextMeshProUGUI>();
            hintCountTMP.text = "2";
            hintCountTMP.fontSize = 24f;
            hintCountTMP.fontStyle = FontStyles.Bold;
            hintCountTMP.color = Color.white;
            hintCountTMP.alignment = TextAlignmentOptions.Center;
            hintCountTMP.raycastTarget = false;

            // Undo Button
            var undoBtnObj = new GameObject("Undo Button", typeof(RectTransform), typeof(Image), typeof(Button));
            undoBtnObj.transform.SetParent(bottomObj.transform, false);
            var undoBtnRect = undoBtnObj.GetComponent<RectTransform>();
            undoBtnRect.anchorMin = new Vector2(0.5f, 0.5f);
            undoBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
            undoBtnRect.pivot = new Vector2(0.5f, 0.5f);
            undoBtnRect.anchoredPosition = new Vector2(120f, 0f);
            undoBtnRect.sizeDelta = new Vector2(156f, 156f);
            var undoBtnImg = undoBtnObj.GetComponent<Image>();
            undoBtnImg.sprite = buttonCircle;
            undoBtnImg.color = Color.white;
            var undoBtn = undoBtnObj.GetComponent<Button>();
            undoBtn.targetGraphic = undoBtnImg;

            var undoIconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            undoIconObj.transform.SetParent(undoBtnObj.transform, false);
            var undoIconRect = undoIconObj.GetComponent<RectTransform>();
            undoIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            undoIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            undoIconRect.anchoredPosition = new Vector2(0f, 16f);
            undoIconRect.sizeDelta = new Vector2(50f, 50f);
            var undoIconImg = undoIconObj.GetComponent<Image>();
            undoIconImg.sprite = iconUndo;
            undoIconImg.color = PrimaryNavy;
            undoIconImg.preserveAspect = true;
            undoIconImg.raycastTarget = false;

            var undoLabelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            undoLabelObj.transform.SetParent(undoBtnObj.transform, false);
            var undoLabelRect = undoLabelObj.GetComponent<RectTransform>();
            undoLabelRect.anchorMin = new Vector2(0.5f, 0.5f);
            undoLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
            undoLabelRect.anchoredPosition = new Vector2(0f, -38f);
            undoLabelRect.sizeDelta = new Vector2(120f, 32f);
            var undoLabelTMP = undoLabelObj.GetComponent<TextMeshProUGUI>();
            undoLabelTMP.text = "Undo";
            undoLabelTMP.fontSize = 22f;
            undoLabelTMP.fontStyle = FontStyles.Bold;
            undoLabelTMP.color = PrimaryNavy;
            undoLabelTMP.alignment = TextAlignmentOptions.Center;
            undoLabelTMP.raycastTarget = false;

            // 8. Wire GameplayHUD serialized fields
            var hud = canvasObj.GetComponent<GameplayHUD>();
            if (hud == null)
            {
                hud = canvasObj.AddComponent<GameplayHUD>();
            }

            var serializedHUD = new SerializedObject(hud);
            serializedHUD.FindProperty("titleText").objectReferenceValue = titleTMP;
            serializedHUD.FindProperty("levelText").objectReferenceValue = levelTMP;
            serializedHUD.FindProperty("carsRemainingText").objectReferenceValue = carCountTMP;
            serializedHUD.FindProperty("difficultyText").objectReferenceValue = diffTMP;
            serializedHUD.FindProperty("backButton").objectReferenceValue = backBtn;
            serializedHUD.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
            serializedHUD.FindProperty("hintButton").objectReferenceValue = hintBtn;
            serializedHUD.FindProperty("hintCountText").objectReferenceValue = hintCountTMP;
            serializedHUD.FindProperty("undoButton").objectReferenceValue = undoBtn;

            var heartArrayProp = serializedHUD.FindProperty("heartIcons");
            heartArrayProp.arraySize = 3;
            heartArrayProp.GetArrayElementAtIndex(0).objectReferenceValue = heart1.GetComponent<Image>();
            heartArrayProp.GetArrayElementAtIndex(1).objectReferenceValue = heart2.GetComponent<Image>();
            heartArrayProp.GetArrayElementAtIndex(2).objectReferenceValue = heart3.GetComponent<Image>();

            if (resultPopup != null)
            {
                resultPopup.SetAsLastSibling();
                serializedHUD.FindProperty("popupRoot").objectReferenceValue = resultPopup.gameObject;
                serializedHUD.FindProperty("popupTitle").objectReferenceValue = resultPopup.Find("Popup Card/Popup Title")?.GetComponent<TMP_Text>();
                serializedHUD.FindProperty("popupMessage").objectReferenceValue = resultPopup.Find("Popup Card/Popup Message")?.GetComponent<TMP_Text>();
                serializedHUD.FindProperty("popupNextButton").objectReferenceValue = resultPopup.Find("Popup Card/Popup Next Button")?.GetComponent<Button>();
                serializedHUD.FindProperty("popupRestartButton").objectReferenceValue = resultPopup.Find("Popup Card/Popup Restart Button")?.GetComponent<Button>();
                serializedHUD.FindProperty("popupMapButton").objectReferenceValue = resultPopup.Find("Popup Card/Popup Map Button")?.GetComponent<Button>();
            }

            serializedHUD.ApplyModifiedProperties();
            EditorUtility.SetDirty(hud);

            // 9. Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("✅ [GameplayUIBuilder] Successfully reconstructed Gameplay UI according to reference mockup!");
        }

        private static GameObject CreateHeart(GameObject parent, string name, Sprite sprite)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent.transform, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(54f, 54f);
            var img = obj.GetComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return obj;
        }

        private static Sprite LoadSprite(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
    }
}
