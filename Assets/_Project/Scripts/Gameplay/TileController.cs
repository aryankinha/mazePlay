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
        private const float MinimumClearDuration = 0.30f;
        private const float MaximumClearDuration = 0.78f;
        private const float WrongTapDuration = 0.20f;
        private const float ColliderInset = 0.95f;

        private static readonly Color WrongTapColor = new Color32(244, 67, 54, 255);

        private SpriteRenderer rootRenderer;
        private BoxCollider2D tileCollider;
        
        private SpriteRenderer roadRenderer;
        private SpriteRenderer carRenderer;
        private SpriteRenderer glowRenderer;
        
        private Transform roadTransform;
        private Transform carTransform;
        private Transform glowTransform;

        private bool acceptsInput = true;
        private bool hasCar = true;
        private bool isCleared;
        private float cellSize = 1f;
        private Coroutine wrongTapRoutine;
        private Coroutine hintGlowRoutine;
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
            RoadConnections roadConnections,
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
            carTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);

            // Road visual (Always visible on road tiles)
            var roadVisual = TileVisualFactory.GetRoadSprite(roadConnections);
            roadRenderer.sprite = roadVisual.Sprite;
            roadRenderer.color = Color.white;
            roadRenderer.enabled = roadVisual.Sprite != null;
            roadTransform.localRotation = Quaternion.Euler(0f, 0f, roadVisual.RotationZ);
            roadTransform.localScale = Vector3.one * ScaleSpriteToWorldSize(roadRenderer.sprite, cellSize);

            // Car visual
            carRenderer.sprite = TileVisualFactory.GetCarSprite(carColorIndex);
            carRenderer.color = Color.white;
            carRenderer.enabled = hasCar;
            carTransform.localPosition = Vector3.zero;
            carTransform.localScale = Vector3.one * ScaleSpriteToWorldSize(carRenderer.sprite, cellSize * 0.72f);

            // Glow / Hint visual
            if (glowRenderer != null)
            {
                glowRenderer.sprite = TileVisualFactory.GetSelectionGlowSprite();
                glowRenderer.enabled = false;
                glowTransform.localScale = Vector3.one * ScaleSpriteToWorldSize(glowRenderer.sprite, cellSize * 1.1f);
            }

            isCleared = !hasCar;
            acceptsInput = hasCar;
            tileCollider.enabled = hasCar;
        }

        public void SetCellSize(float cellWorldSize)
        {
            cellSize = Mathf.Max(0.01f, cellWorldSize);
            roadTransform.localScale = Vector3.one * cellSize;
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

        public void PlayClearAnimation()
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
                carTransform.localPosition = Vector3.zero;
            }

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
                    // The glow texture has its own pixels-per-unit. Reusing raw cell size
                    // here bypassed its sprite calibration and made the hint fill the screen.
                    var baseScale = ScaleSpriteToWorldSize(glowRenderer.sprite, cellSize * 1.1f);
                    glowTransform.localScale = Vector3.one * (baseScale * pulse);
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
            var initialPos = carTransform.localPosition;
            var driveDirection = VectorFor(Direction);
            var travelDistance = CalculateOffscreenTravelDistance(driveDirection);
            var targetPos = initialPos + ((Vector3)driveDirection * travelDistance);
            var duration = Mathf.Clamp(travelDistance / 11f, MinimumClearDuration, MaximumClearDuration);
            var initialScale = carTransform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                // An ease-in makes the departure feel like a vehicle accelerating,
                // while retaining a deterministic final position outside the camera.
                var smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

                carTransform.localPosition = Vector3.Lerp(initialPos, targetPos, smoothProgress);
                var launchPulse = 1f + (Mathf.Sin(Mathf.Min(progress, 0.28f) / 0.28f * Mathf.PI) * 0.06f);
                carTransform.localScale = initialScale * launchPulse;
                var carAlpha = progress < 0.72f ? 1f : Mathf.Clamp01(1f - ((progress - 0.72f) / 0.28f));
                carRenderer.color = new Color(1f, 1f, 1f, carAlpha);

                yield return null;
            }

            carRenderer.enabled = false;
            carTransform.localPosition = Vector3.zero;
            carTransform.localScale = initialScale;
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
                carTransform.localPosition = Vector3.zero;
                carRenderer.color = Color.white;
            }

            wrongTapRoutine = StartCoroutine(WrongTapRoutine());
        }

        /// <summary>Routes programmatic UI/accessibility taps through the same hit-test as touch and mouse input.</summary>
        public bool TryTapAtScreenPosition(Vector2 screenPosition)
        {
            if (!acceptsInput || !hasCar || isCleared)
            {
                return false;
            }

            return HandlePointerTap(screenPosition);
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
                carTransform.localPosition = Vector3.right * offset;

                var flash = Mathf.PingPong(progress * 4f, 1f);
                carRenderer.color = Color.Lerp(Color.white, WrongTapColor, flash);
                yield return null;
            }

            carTransform.localPosition = Vector3.zero;
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

            if (glowRenderer == null)
            {
                glowRenderer = EnsureChildRenderer("Glow", 1, out glowTransform);
            }

            if (carRenderer == null)
            {
                carRenderer = EnsureChildRenderer("Car", 2, out carTransform);
            }

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

        private static float ScaleSpriteToWorldSize(Sprite sprite, float targetWorldSize)
        {
            if (sprite == null)
            {
                return targetWorldSize;
            }

            var sourceWorldSize = Mathf.Max(sprite.rect.width, sprite.rect.height) / sprite.pixelsPerUnit;
            return sourceWorldSize > 0.0001f ? targetWorldSize / sourceWorldSize : targetWorldSize;
        }

        private float CalculateOffscreenTravelDistance(Vector2 direction)
        {
            var gameplayCamera = Camera.main;
            if (gameplayCamera == null || carRenderer == null)
            {
                return cellSize * 2f;
            }

            var planeDistance = Mathf.Abs(carTransform.position.z - gameplayCamera.transform.position.z);
            var viewportCenter = new Vector3(0.5f, 0.5f, planeDistance);
            var current = carTransform.position;
            var extents = carRenderer.bounds.extents;
            const float viewportPadding = 0.04f;

            if (direction.x > 0f)
            {
                var edge = gameplayCamera.ViewportToWorldPoint(viewportCenter + new Vector3(0.5f + viewportPadding, 0f, 0f)).x;
                return Mathf.Max(cellSize, edge - current.x + extents.x);
            }

            if (direction.x < 0f)
            {
                var edge = gameplayCamera.ViewportToWorldPoint(viewportCenter - new Vector3(0.5f + viewportPadding, 0f, 0f)).x;
                return Mathf.Max(cellSize, current.x - edge + extents.x);
            }

            if (direction.y > 0f)
            {
                var edge = gameplayCamera.ViewportToWorldPoint(viewportCenter + new Vector3(0f, 0.5f + viewportPadding, 0f)).y;
                return Mathf.Max(cellSize, edge - current.y + extents.y);
            }

            var bottom = gameplayCamera.ViewportToWorldPoint(viewportCenter - new Vector3(0f, 0.5f + viewportPadding, 0f)).y;
            return Mathf.Max(cellSize, current.y - bottom + extents.y);
        }
    }
}
