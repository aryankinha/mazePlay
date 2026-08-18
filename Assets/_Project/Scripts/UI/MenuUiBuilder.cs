using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ArrowMaze.UI
{
    internal static class MenuUiBuilder
    {
        internal static readonly Color Navy = new Color32(28, 45, 78, 255);
        internal static readonly Color Blue = new Color32(32, 139, 241, 255);
        internal static readonly Color PaleBlue = new Color32(235, 244, 255, 255);
        internal static readonly Color Green = new Color32(44, 172, 111, 255);
        internal static readonly Color Muted = new Color32(166, 178, 196, 255);

        internal static RectTransform CreateCanvas(string name)
        {
            var root = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }
            return root.GetComponent<RectTransform>();
        }

        internal static Image Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
            var image = go.GetComponent<Image>(); image.color = color;
            return image;
        }

        internal static TMP_Text Text(Transform parent, string name, string value, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.anchoredPosition = position; rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value; text.fontSize = fontSize; text.color = color; text.alignment = alignment;
            text.enableWordWrapping = false;
            return text;
        }

        internal static Button Button(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor; rect.anchorMax = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.color = color;
            var button = go.GetComponent<Button>();
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1f, 1f, 1f, .86f); colors.pressedColor = new Color(.8f, .88f, 1f, 1f); button.colors = colors;
            Text(go.transform, "Label", label, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, size, 38f, Color.white);
            return button;
        }
    }
}
