using System.Collections.Generic;
using ArrowMaze.Core;
using UnityEngine;

namespace ArrowMaze.Gameplay
{
    internal readonly struct RoadSpriteVisual
    {
        public RoadSpriteVisual(Sprite sprite, float rotationZ)
        {
            Sprite = sprite;
            RotationZ = rotationZ;
        }

        public Sprite Sprite { get; }
        public float RotationZ { get; }
    }

    internal static class TileVisualFactory
    {
        private static readonly string[] CarColorNames = { "blue", "red", "yellow", "green", "purple" };
        private static readonly Dictionary<string, Sprite> carSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> roadSprites = new Dictionary<string, Sprite>();
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

        public static RoadSpriteVisual GetRoadSprite(RoadConnections connections)
        {
            switch (connections)
            {
                case RoadConnections.Up | RoadConnections.Down:
                    return new RoadSpriteVisual(LoadRoadSprite("road_straight_v"), 0f);
                case RoadConnections.Left | RoadConnections.Right:
                    return new RoadSpriteVisual(LoadRoadSprite("road_straight_h"), 0f);
                case RoadConnections.Up | RoadConnections.Right:
                    return new RoadSpriteVisual(LoadRoadSprite("road_corner_0"), 0f);
                case RoadConnections.Right | RoadConnections.Down:
                    return new RoadSpriteVisual(LoadRoadSprite("road_corner_90"), 0f);
                case RoadConnections.Down | RoadConnections.Left:
                    return new RoadSpriteVisual(LoadRoadSprite("road_corner_180"), 0f);
                case RoadConnections.Left | RoadConnections.Up:
                    return new RoadSpriteVisual(LoadRoadSprite("road_corner_270"), 0f);
                case RoadConnections.Up | RoadConnections.Right | RoadConnections.Down:
                    return new RoadSpriteVisual(LoadRoadSprite("road_t_junction"), 90f);
                case RoadConnections.Up | RoadConnections.Right | RoadConnections.Left:
                    return new RoadSpriteVisual(LoadRoadSprite("road_t_junction"), 180f);
                case RoadConnections.Up | RoadConnections.Down | RoadConnections.Left:
                    return new RoadSpriteVisual(LoadRoadSprite("road_t_junction"), -90f);
                case RoadConnections.Right | RoadConnections.Down | RoadConnections.Left:
                    return new RoadSpriteVisual(LoadRoadSprite("road_t_junction"), 0f);
                case RoadConnections.Up | RoadConnections.Right | RoadConnections.Down | RoadConnections.Left:
                    return new RoadSpriteVisual(LoadRoadSprite("road_crossroad"), 0f);
                case RoadConnections.Up:
                    return new RoadSpriteVisual(LoadRoadSprite("road_end"), 180f);
                case RoadConnections.Right:
                    return new RoadSpriteVisual(LoadRoadSprite("road_end"), 90f);
                case RoadConnections.Down:
                    return new RoadSpriteVisual(LoadRoadSprite("road_end"), 0f);
                case RoadConnections.Left:
                    return new RoadSpriteVisual(LoadRoadSprite("road_end"), -90f);
                default:
                    return new RoadSpriteVisual(null, 0f);
            }
        }

        private static Sprite LoadRoadSprite(string key)
        {
            if (roadSprites.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var loaded = Resources.Load<Sprite>($"Sprites/Roads/{key}");
            if (loaded != null)
            {
                roadSprites[key] = loaded;
            }

            return loaded;
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

    }
}
