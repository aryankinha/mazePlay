using System;
using System.Collections;
using System.Collections.Generic;
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
        private const float MaximumClearDuration = 1.80f;
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
        private Coroutine clearDriveRoutine;
        private Vector3 carRestingScale = Vector3.one;
        private int carColorIndex;
        private IReadOnlyList<GridCoordinate> route;
        private ArrowDirection exitDirection;

        public event Action<GridCoordinate> TapRequested;
        public event Action<GridCoordinate> ExitAnimationCompleted;

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
            int colorIndex = 0,
            IReadOnlyList<GridCoordinate> route = null,
            ArrowDirection exitDirection = ArrowDirection.Up)
        {
            Coordinate = coordinate;
            Direction = direction;
            this.hasCar = hasCar;
            this.carColorIndex = colorIndex;
            this.route = route;
            this.exitDirection = exitDirection;
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
            carRestingScale = Vector3.one * ScaleSpriteToWorldSize(carRenderer.sprite, cellSize * 0.72f);
            carTransform.localScale = carRestingScale;

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

            if (clearDriveRoutine != null)
            {
                StopCoroutine(clearDriveRoutine);
            }

            clearDriveRoutine = StartCoroutine(ClearDriveRoutine());
        }

        public void RestoreCar()
        {
            if (!hasCar)
            {
                return;
            }

            if (clearDriveRoutine != null)
            {
                StopCoroutine(clearDriveRoutine);
                clearDriveRoutine = null;
            }

            if (wrongTapRoutine != null)
            {
                StopCoroutine(wrongTapRoutine);
                wrongTapRoutine = null;
            }

            HideHint();
            isCleared = false;
            acceptsInput = true;
            if (tileCollider != null)
            {
                tileCollider.enabled = true;
            }

            carRenderer.enabled = true;
            carRenderer.color = Color.white;
            carTransform.localPosition = Vector3.zero;
            carTransform.localRotation = Quaternion.Euler(0f, 0f, RotationFor(Direction));
            carTransform.localScale = carRestingScale;
        }

        public void ShowHint()
        {
            if (isCleared || !hasCar)
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
            if (glowRenderer == null)
            {
                yield break;
            }

            glowRenderer.enabled = true;
            glowRenderer.color = Color.white;
            var elapsed = 0f;

            while (true)
            {
                elapsed += Time.deltaTime;
                var pulse = 0.95f + (Mathf.Sin(elapsed * 6f) * 0.08f);
                if (glowRenderer.sprite != null)
                {
                    var baseScale = ScaleSpriteToWorldSize(glowRenderer.sprite, cellSize * 1.1f);
                    glowTransform.localScale = Vector3.one * (baseScale * pulse);
                }
                yield return null;
            }
        }

        private IEnumerator ClearDriveRoutine()
        {
            var waypoints = BuildLocalWaypoints();
            if (waypoints.Count < 2)
            {
                ExitAnimationCompleted?.Invoke(Coordinate);
                carRenderer.enabled = false;
                yield break;
            }

            // Calculate segment lengths and cumulative distances
            var segmentLengths = new float[waypoints.Count - 1];
            var cumulativeDist = new float[waypoints.Count];
            cumulativeDist[0] = 0f;
            var totalDistance = 0f;

            for (var i = 0; i < waypoints.Count - 1; i++)
            {
                var len = Vector3.Distance(waypoints[i], waypoints[i + 1]);
                segmentLengths[i] = len;
                totalDistance += len;
                cumulativeDist[i + 1] = totalDistance;
            }

            var duration = Mathf.Clamp(totalDistance / (cellSize * 7.5f), MinimumClearDuration, MaximumClearDuration);
            var elapsed = 0f;
            var initialScale = carRestingScale;
            var currentRotation = carTransform.localRotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
                var currentDist = smoothProgress * totalDistance;

                // Find active segment
                var segIndex = 0;
                for (var i = 0; i < waypoints.Count - 1; i++)
                {
                    if (currentDist <= cumulativeDist[i + 1] || i == waypoints.Count - 2)
                    {
                        segIndex = i;
                        break;
                    }
                }

                var segStartDist = cumulativeDist[segIndex];
                var segLen = segmentLengths[segIndex];
                var segFraction = segLen > 0.0001f ? Mathf.Clamp01((currentDist - segStartDist) / segLen) : 0f;

                carTransform.localPosition = Vector3.Lerp(waypoints[segIndex], waypoints[segIndex + 1], segFraction);

                // Smooth rotation to face forward along the current segment
                var segDelta = waypoints[segIndex + 1] - waypoints[segIndex];
                if (segDelta.sqrMagnitude > 0.001f)
                {
                    var targetRotZ = Mathf.Atan2(segDelta.y, segDelta.x) * Mathf.Rad2Deg - 90f;
                    var targetRot = Quaternion.Euler(0f, 0f, targetRotZ);
                    currentRotation = Quaternion.RotateTowards(currentRotation, targetRot, 720f * Time.deltaTime);
                    carTransform.localRotation = currentRotation;
                }

                var launchPulse = 1f + (Mathf.Sin(Mathf.Min(progress, 0.28f) / 0.28f * Mathf.PI) * 0.06f);
                carTransform.localScale = initialScale * launchPulse;
                carRenderer.color = Color.white;

                yield return null;
            }

            clearDriveRoutine = null;
            ExitAnimationCompleted?.Invoke(Coordinate);
            carRenderer.enabled = false;
            carTransform.localPosition = Vector3.zero;
            carTransform.localScale = initialScale;
            carRenderer.color = Color.white;
        }

        private List<Vector3> BuildLocalWaypoints()
        {
            var list = new List<Vector3>();
            if (route != null && route.Count > 0)
            {
                foreach (var cell in route)
                {
                    var deltaCol = cell.Column - Coordinate.Column;
                    var deltaRow = cell.Row - Coordinate.Row;
                    list.Add(new Vector3(deltaCol * cellSize, -deltaRow * cellSize, 0f));
                }
            }
            else
            {
                list.Add(Vector3.zero);
            }

            // Append offscreen exit point past the last cell
            var lastPos = list[list.Count - 1];
            var exitVec = VectorFor(exitDirection);
            var travelDistance = CalculateOffscreenTravelDistance(exitVec);
            list.Add(lastPos + ((Vector3)exitVec * travelDistance));

            return list;
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
                if (rootRenderer == null)
                {
                    rootRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }
            if (rootRenderer != null)
            {
                rootRenderer.enabled = false;
            }

            if (tileCollider == null)
            {
                tileCollider = GetComponent<BoxCollider2D>();
                if (tileCollider == null)
                {
                    tileCollider = gameObject.AddComponent<BoxCollider2D>();
                }
            }

            roadTransform = transform.Find("Road");
            if (roadTransform == null)
            {
                var roadObj = new GameObject("Road");
                roadObj.transform.SetParent(transform, false);
                roadTransform = roadObj.transform;
            }
            roadRenderer = roadTransform.GetComponent<SpriteRenderer>();
            if (roadRenderer == null)
            {
                roadRenderer = roadTransform.gameObject.AddComponent<SpriteRenderer>();
            }
            if (roadRenderer != null)
            {
                roadRenderer.sortingOrder = 0;
            }

            glowTransform = transform.Find("Glow");
            if (glowTransform == null)
            {
                var glowObj = new GameObject("Glow");
                glowObj.transform.SetParent(transform, false);
                glowTransform = glowObj.transform;
            }
            glowRenderer = glowTransform.GetComponent<SpriteRenderer>();
            if (glowRenderer == null)
            {
                glowRenderer = glowTransform.gameObject.AddComponent<SpriteRenderer>();
            }
            if (glowRenderer != null)
            {
                glowRenderer.sortingOrder = 1;
            }

            carTransform = transform.Find("Car");
            if (carTransform == null)
            {
                var carObj = new GameObject("Car");
                carObj.transform.SetParent(transform, false);
                carTransform = carObj.transform;
            }
            carRenderer = carTransform.GetComponent<SpriteRenderer>();
            if (carRenderer == null)
            {
                carRenderer = carTransform.gameObject.AddComponent<SpriteRenderer>();
            }
            if (carRenderer != null)
            {
                carRenderer.sortingOrder = 2;
            }
        }

        private static bool TryGetPressedScreenPosition(out Vector2 position)
        {
            position = Vector2.zero;
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                position = touch.primaryTouch.position.ReadValue();
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                position = mouse.position.ReadValue();
                return true;
            }

            return false;
        }

        private static Vector2 VectorFor(ArrowDirection direction)
        {
            switch (direction)
            {
                case ArrowDirection.Up: return Vector2.up;
                case ArrowDirection.Right: return Vector2.right;
                case ArrowDirection.Down: return Vector2.down;
                case ArrowDirection.Left: return Vector2.left;
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
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

        private float CalculateOffscreenTravelDistance(Vector2 exitDirectionVector)
        {
            var gameplayCamera = Camera.main;
            if (gameplayCamera == null)
            {
                return Mathf.Max(6f, cellSize * 5f);
            }

            var halfHeight = gameplayCamera.orthographicSize;
            var halfWidth = halfHeight * gameplayCamera.aspect;
            var cameraCenter = gameplayCamera.transform.position;
            var startWorldPos = transform.position;

            var extraClearance = cellSize * 1.5f;

            if (exitDirectionVector.x > 0.5f)
            {
                var cameraRight = cameraCenter.x + halfWidth;
                return Mathf.Max(1.5f, (cameraRight - startWorldPos.x) + extraClearance);
            }
            if (exitDirectionVector.x < -0.5f)
            {
                var cameraLeft = cameraCenter.x - halfWidth;
                return Mathf.Max(1.5f, (startWorldPos.x - cameraLeft) + extraClearance);
            }
            if (exitDirectionVector.y > 0.5f)
            {
                var cameraTop = cameraCenter.y + halfHeight;
                return Mathf.Max(1.5f, (cameraTop - startWorldPos.y) + extraClearance);
            }
            if (exitDirectionVector.y < -0.5f)
            {
                var cameraBottom = cameraCenter.y - halfHeight;
                return Mathf.Max(1.5f, (startWorldPos.y - cameraBottom) + extraClearance);
            }

            return Mathf.Max(6f, cellSize * 5f);
        }

        private static float ScaleSpriteToWorldSize(Sprite sprite, float targetWorldSize)
        {
            if (sprite == null)
            {
                return 1f;
            }

            var size = Mathf.Max(sprite.rect.width, sprite.rect.height) / sprite.pixelsPerUnit;
            if (size <= 0.0001f)
            {
                return 1f;
            }

            return targetWorldSize / size;
        }
    }
}
