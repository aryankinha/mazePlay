using System;
using System.Collections;
using ArrowMaze.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArrowMaze.Gameplay
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class TileController : MonoBehaviour
    {
        private const float ClearDuration = 0.28f;
        private const float WrongTapDuration = 0.20f;
        private const float ColliderInset = 0.95f;

        private static readonly Color TrailColor = new Color32(0x21, 0x96, 0xF3, 0xFF);
        private static readonly Color WrongTapColor = new Color32(244, 67, 54, 255);

        private SpriteRenderer rootRenderer;
        private BoxCollider2D tileCollider;
        
        private SpriteRenderer roadRenderer;
        private SpriteRenderer trailRenderer;
        private SpriteRenderer carRenderer;
        private SpriteRenderer glowRenderer;
        
        private Transform roadTransform;
        private Transform trailTransform;
        private Transform carTransform;
        private Transform glowTransform;

        private bool acceptsInput = true;
        private bool hasCar = true;
        private bool isCleared;
        private float cellSize = 1f;
        private TrailConnections trailConnections = TrailConnections.None;
        private Coroutine wrongTapRoutine;
        private Coroutine hintGlowRoutine;
        private Vector3 restingLocalPosition;
        private int carColorIndex;

        public event Action<GridCoordinate> TapRequested;

        public GridCoordinate Coordinate { get; private set; }
        public ArrowDirection Direction { get; private set; }
        public bool HasCar => hasCar;
        public bool IsCleared => isCleared;

        private void Awake()
        {
            EnsureVisualLayers();
        }

        private void Update()
        {
            if (!acceptsInput || !hasCar || isCleared || !TryGetPressedScreenPosition(out var screenPosition))
            {
                return;
            }

            HandlePointerTap(screenPosition);
        }

        public void Initialize(
            GridCoordinate coordinate,
            ArrowDirection direction,
            float cellWorldSize,
            bool hasCar = true,
            int colorIndex = 0)
        {
            Coordinate = coordinate;
            Direction = direction;
            this.hasCar = hasCar;
            this.carColorIndex = colorIndex;
            gameObject.name = $"Tile ({coordinate.Row}, {coordinate.Column})";

            EnsureVisualLayers();
            SetCellSize(cellWorldSize);

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            var rotZ = RotationFor(direction);
            roadTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            carTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            trailTransform.localRotation = Quaternion.identity;

            restingLocalPosition = transform.localPosition;

            // Road visual (Always visible on road tiles)
            roadRenderer.sprite = TileVisualFactory.GetRoadSprite(direction);
            roadRenderer.color = Color.white;
            roadRenderer.enabled = true;

            // Car visual
            carRenderer.sprite = TileVisualFactory.GetCarSprite(carColorIndex);
            carRenderer.color = Color.white;
            carRenderer.enabled = hasCar;
            carTransform.localPosition = Vector3.zero;
            carTransform.localScale = Vector3.one * (cellSize * 0.72f);

            // Glow / Hint visual
            if (glowRenderer != null)
            {
                glowRenderer.sprite = TileVisualFactory.GetSelectionGlowSprite();
                glowRenderer.enabled = false;
            }

            // Trail visual
            trailConnections = TrailConnections.None;
            trailRenderer.sprite = TileVisualFactory.GetTrailSprite(trailConnections);
            trailRenderer.color = TrailColor;
            trailRenderer.enabled = false;

            isCleared = !hasCar;
            acceptsInput = hasCar;
            tileCollider.enabled = hasCar;
        }

        public void SetCellSize(float cellWorldSize)
        {
            cellSize = Mathf.Max(0.01f, cellWorldSize);
            roadTransform.localScale = Vector3.one * cellSize;
            trailTransform.localScale = Vector3.one * cellSize;
            carTransform.localScale = Vector3.one * (cellSize * 0.72f);
            if (glowTransform != null) glowTransform.localScale = Vector3.one * (cellSize * 1.1f);
            tileCollider.size = Vector2.one * (cellSize * ColliderInset);
        }

        public void SetInputEnabled(bool enabled)
        {
            acceptsInput = enabled && hasCar && !isCleared;
            if (tileCollider != null)
            {
                tileCollider.enabled = acceptsInput;
            }
        }

        public void SetTrailConnections(TrailConnections connections)
        {
            trailConnections = connections;
            if (isCleared && trailRenderer != null)
            {
                trailRenderer.sprite = TileVisualFactory.GetTrailSprite(connections);
            }
        }

        public void PlayClearAnimation(TrailConnections connections)
        {
            if (isCleared || !hasCar)
            {
                return;
            }

            isCleared = true;
            SetInputEnabled(false);
            HideHint();

            if (wrongTapRoutine != null)
            {
                StopCoroutine(wrongTapRoutine);
                wrongTapRoutine = null;
                transform.localPosition = restingLocalPosition;
            }

            trailConnections = connections;
            trailRenderer.sprite = TileVisualFactory.GetTrailSprite(connections);
            trailRenderer.enabled = true;
            StartCoroutine(ClearDriveRoutine());
        }

        public void RestoreCar()
        {
            if (!hasCar)
            {
                return;
            }

            isCleared = false;
            acceptsInput = true;
            tileCollider.enabled = true;

            carTransform.localPosition = Vector3.zero;
            carRenderer.color = Color.white;
            carRenderer.enabled = true;
            trailRenderer.enabled = false;
        }

        public void ShowHint()
        {
            if (glowRenderer == null || isCleared || !hasCar)
            {
                return;
            }

            if (hintGlowRoutine != null)
            {
                StopCoroutine(hintGlowRoutine);
            }

            hintGlowRoutine = StartCoroutine(HintGlowRoutine());
        }

        public void HideHint()
        {
            if (hintGlowRoutine != null)
            {
                StopCoroutine(hintGlowRoutine);
                hintGlowRoutine = null;
            }

            if (glowRenderer != null)
            {
                glowRenderer.enabled = false;
            }
        }

        private IEnumerator HintGlowRoutine()
        {
            glowRenderer.enabled = true;
            var elapsed = 0f;
            while (elapsed < 3f && !isCleared)
            {
                elapsed += Time.deltaTime;
                var pulse = 0.85f + (Mathf.Sin(elapsed * 6f) * 0.15f);
                if (glowTransform != null)
                {
                    glowTransform.localScale = Vector3.one * (cellSize * 1.1f * pulse);
                }
                yield return null;
            }

            if (glowRenderer != null)
            {
                glowRenderer.enabled = false;
            }

            hintGlowRoutine = null;
        }

        private IEnumerator ClearDriveRoutine()
        {
            var elapsed = 0f;
            var driveVector = VectorFor(Direction) * (cellSize * 1.8f);
            var initialPos = carTransform.localPosition;
            var targetPos = initialPos + (Vector3)driveVector;

            while (elapsed < ClearDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / ClearDuration);
                var smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

                carTransform.localPosition = Vector3.Lerp(initialPos, targetPos, smoothProgress);
                var carAlpha = Mathf.Clamp01(1f - (progress * 1.3f));
                carRenderer.color = new Color(1f, 1f, 1f, carAlpha);

                yield return null;
            }

            carRenderer.enabled = false;
            carTransform.localPosition = Vector3.zero;
        }

        public void PlayWrongTapFeedback()
        {
            if (isCleared || !hasCar)
            {
                return;
            }

            if (wrongTapRoutine != null)
            {
                StopCoroutine(wrongTapRoutine);
                transform.localPosition = restingLocalPosition;
                carRenderer.color = Color.white;
            }

            wrongTapRoutine = StartCoroutine(WrongTapRoutine());
        }

        private IEnumerator WrongTapRoutine()
        {
            var elapsed = 0f;
            var shakeAmplitude = cellSize * 0.12f;

            while (elapsed < WrongTapDuration)
            {
                elapsed += Time.deltaTime;
                var progress = elapsed / WrongTapDuration;
                var offset = Mathf.Sin(progress * Mathf.PI * 8f) * shakeAmplitude;
                transform.localPosition = restingLocalPosition + (Vector3.right * offset);

                var flash = Mathf.PingPong(progress * 4f, 1f);
                carRenderer.color = Color.Lerp(Color.white, WrongTapColor, flash);
                yield return null;
            }

            transform.localPosition = restingLocalPosition;
            carRenderer.color = Color.white;
            wrongTapRoutine = null;
        }

        private bool HandlePointerTap(Vector2 screenPosition)
        {
            var gameplayCamera = Camera.main;
            if (gameplayCamera == null || tileCollider == null)
            {
                return false;
            }

            var gameplayPlaneDistance = -gameplayCamera.transform.position.z;
            var worldPosition = gameplayCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, gameplayPlaneDistance));
            if (!ContainsWorldPoint(worldPosition))
            {
                return false;
            }

            TapRequested?.Invoke(Coordinate);
            return true;
        }

        private bool ContainsWorldPoint(Vector3 worldPosition)
        {
            if (!tileCollider.enabled)
            {
                return false;
            }

            var localPoint = (Vector2)transform.InverseTransformPoint(worldPosition);
            var halfSize = tileCollider.size * 0.5f;
            var min = tileCollider.offset - halfSize;
            var max = tileCollider.offset + halfSize;
            return localPoint.x >= min.x && localPoint.x <= max.x &&
                   localPoint.y >= min.y && localPoint.y <= max.y;
        }

        private void EnsureVisualLayers()
        {
            if (rootRenderer == null)
            {
                rootRenderer = GetComponent<SpriteRenderer>();
            }
            rootRenderer.enabled = false;

            if (tileCollider == null)
            {
                tileCollider = GetComponent<BoxCollider2D>();
            }

            if (roadRenderer == null)
            {
                roadRenderer = EnsureChildRenderer("Road", 0, out roadTransform);
            }

            if (trailRenderer == null)
            {
                trailRenderer = EnsureChildRenderer("Trail", 1, out trailTransform);
            }

            if (glowRenderer == null)
            {
                glowRenderer = EnsureChildRenderer("Glow", 2, out glowTransform);
            }

            if (carRenderer == null)
            {
                carRenderer = EnsureChildRenderer("Car", 3, out carTransform);
            }

            RemoveLegacyLayer("Arrow");
            RemoveLegacyLayer("Border");
            RemoveLegacyLayer("Backing");
        }

        private SpriteRenderer EnsureChildRenderer(string layerName, int sortingOrder, out Transform layerTransform)
        {
            layerTransform = transform.Find(layerName);
            if (layerTransform == null)
            {
                var layer = new GameObject(layerName);
                layer.transform.SetParent(transform, false);
                layerTransform = layer.transform;
            }

            layerTransform.localPosition = Vector3.zero;
            var renderer = layerTransform.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = layerTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void RemoveLegacyLayer(string layerName)
        {
            var legacy = transform.Find(layerName);
            if (legacy != null)
            {
                Destroy(legacy.gameObject);
            }
        }

        private static bool TryGetPressedScreenPosition(out Vector2 screenPosition)
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screenPosition = mouse.position.ReadValue();
                return true;
            }

            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = touchscreen.primaryTouch.position.ReadValue();
                return true;
            }

            screenPosition = default;
            return false;
        }

        private static float RotationFor(ArrowDirection direction)
        {
            switch (direction)
            {
                case ArrowDirection.Up: return 0f;
                case ArrowDirection.Right: return -90f;
                case ArrowDirection.Down: return 180f;
                case ArrowDirection.Left: return 90f;
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        private static Vector2 VectorFor(ArrowDirection direction)
        {
            switch (direction)
            {
                case ArrowDirection.Up: return Vector2.up;
                case ArrowDirection.Right: return Vector2.right;
                case ArrowDirection.Down: return Vector2.down;
                case ArrowDirection.Left: return Vector2.left;
                default: return Vector2.zero;
            }
        }
    }
}
