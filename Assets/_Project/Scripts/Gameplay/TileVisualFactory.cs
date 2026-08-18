using System;
using System.Collections.Generic;
using ArrowMaze.Core;
using UnityEngine;

namespace ArrowMaze.Gameplay
{
    [Flags]
    public enum TrailConnections
    {
        None = 0,
        Up = 1,
        Right = 2,
        Down = 4,
        Left = 8
    }

    internal static class TileVisualFactory
    {
        public const float StrokeHalfWidth = 0.19f;
        public const float TrailHalfWidth = 0.145f;
        private const int TextureSize = 192;

        private static readonly string[] CarColorNames = { "blue", "red", "yellow", "green", "purple" };
        private static readonly Dictionary<string, Sprite> carSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> roadSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<TrailConnections, Sprite> trailSprites = new Dictionary<TrailConnections, Sprite>();
        private static Sprite exitGateSprite;
        private static Sprite selectionGlowSprite;
        private static Sprite heartFullSprite;
        private static Sprite heartEmptySprite;

        public static Sprite GetCarSprite(int colorIndex)
        {
            var name = CarColorNames[Mathf.Abs(colorIndex) % CarColorNames.Length];
            if (carSprites.TryGetValue(name, out var cached) && cached != null)
            {
                return cached;
            }

            var loaded = Resources.Load<Sprite>($"Sprites/Cars/car_{name}");
            if (loaded != null)
            {
                carSprites[name] = loaded;
                return loaded;
            }

            return null;
        }

        public static Sprite GetRoadSprite(ArrowDirection direction)
        {
            var key = "straight_v";
            if (roadSprites.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var loaded = Resources.Load<Sprite>("Sprites/Roads/road_straight_v");
            if (loaded != null)
            {
                roadSprites[key] = loaded;
                return loaded;
            }

            return null;
        }

        public static Sprite GetExitGateSprite()
        {
            if (exitGateSprite != null)
            {
                return exitGateSprite;
            }

            exitGateSprite = Resources.Load<Sprite>("Sprites/Props/exit_gate");
            return exitGateSprite;
        }

        public static Sprite GetSelectionGlowSprite()
        {
            if (selectionGlowSprite != null)
            {
                return selectionGlowSprite;
            }

            selectionGlowSprite = Resources.Load<Sprite>("Sprites/UI/selection_glow");
            return selectionGlowSprite;
        }

        public static Sprite GetBoardCardSprite()
        {
            return Resources.Load<Sprite>("Sprites/UI/card_board_bg");
        }

        public static Sprite GetHeartSprite(bool filled)
        {
            if (filled)
            {
                if (heartFullSprite == null)
                {
                    heartFullSprite = Resources.Load<Sprite>("Sprites/UI/heart_full");
                }
                return heartFullSprite;
            }

            if (heartEmptySprite == null)
            {
                heartEmptySprite = Resources.Load<Sprite>("Sprites/UI/heart_empty");
            }
            return heartEmptySprite;
        }

        public static Sprite GetTrailSprite(TrailConnections connections)
        {
            if (trailSprites.TryGetValue(connections, out var cached) && cached != null)
            {
                return cached;
            }

            var sprite = BuildProceduralTrailSprite(
                $"RuntimeTrail_{(int)connections}",
                point => TrailDistance(point, connections));
            trailSprites[connections] = sprite;
            return sprite;
        }

        private static float TrailDistance(Vector2 point, TrailConnections connections)
        {
            var distance = point.magnitude - TrailHalfWidth;
            if ((connections & TrailConnections.Up) != 0) distance = Mathf.Min(distance, Arm(point, Vector2.up));
            if ((connections & TrailConnections.Right) != 0) distance = Mathf.Min(distance, Arm(point, Vector2.right));
            if ((connections & TrailConnections.Down) != 0) distance = Mathf.Min(distance, Arm(point, Vector2.down));
            if ((connections & TrailConnections.Left) != 0) distance = Mathf.Min(distance, Arm(point, Vector2.left));
            return distance;
        }

        private static float Arm(Vector2 point, Vector2 direction)
        {
            var along = Vector2.Dot(point, direction);
            var perp = Mathf.Abs(Vector2.Dot(point, new Vector2(-direction.y, direction.x)));
            if (along < 0f) return perp - TrailHalfWidth;
            var box = new Vector2(perp - TrailHalfWidth, along - 0.515f);
            return Mathf.Max(box.x, box.y);
        }

        private static Sprite BuildProceduralTrailSprite(string spriteName, Func<Vector2, float> distanceFunction)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = spriteName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[TextureSize * TextureSize];
            var pixelRadius = 1f / TextureSize;

            for (var y = 0; y < TextureSize; y++)
            {
                var normY = ((y + 0.5f) / TextureSize) - 0.5f;
                for (var x = 0; x < TextureSize; x++)
                {
                    var normX = ((x + 0.5f) / TextureSize) - 0.5f;
                    var distance = distanceFunction(new Vector2(normX, normY));
                    var alpha = Mathf.Clamp01(0.5f - (distance / (pixelRadius * 1.5f)));
                    var byteAlpha = (byte)(alpha * 255f);
                    pixels[(y * TextureSize) + x] = new Color32(255, 255, 255, byteAlpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            return Sprite.Create(
                texture,
                new Rect(0, 0, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                TextureSize);
        }
    }
}
