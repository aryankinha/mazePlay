using System;
using System.Collections.Generic;
using ArrowMaze.Gameplay;
using UnityEngine;

namespace ArrowMaze.Core
{
    public sealed class GridManager : MonoBehaviour
    {
        private const float MinCellSize = 0.70f;
        private const float MaxCellSize = 1.35f;
        private const float CellPitchMultiplier = 1f;

        private static readonly ArrowDirection[] CardinalDirections =
        {
            ArrowDirection.Up,
            ArrowDirection.Right,
            ArrowDirection.Down,
            ArrowDirection.Left
        };

        [SerializeField] private TileController tilePrefab;
        [SerializeField, Range(0.02f, 0.2f)] private float horizontalViewportMargin = 0.05f;
        [SerializeField, Min(1f)] private float portraitOrthographicSize = 7.7f;
        [SerializeField, Min(0f)] private float headerReserve = 2.8f;
        [SerializeField, Min(0f)] private float footerReserve = 1.6f;

        private readonly Dictionary<GridCoordinate, TileController> tiles =
            new Dictionary<GridCoordinate, TileController>();

        private readonly HashSet<GridCoordinate> clearedCells = new HashSet<GridCoordinate>();

        private Transform tileContainer;
        private GameObject boardCardObject;
        private MazeLevel currentLevel;
        private float cellSize = MinCellSize;

        public event Action<GridCoordinate> TileTapped;

        public MazeLevel CurrentLevel => currentLevel;
        public IReadOnlyDictionary<GridCoordinate, TileController> Tiles => tiles;
        public float CellSize => cellSize;

        public void BuildLevel(MazeLevel level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (tilePrefab == null)
            {
                throw new InvalidOperationException("GridManager needs a Tile prefab before it can build a level.");
            }

            ClearGrid();
            currentLevel = level;
            EnsureTileContainer();
            FrameGridInCamera(level);

            SpawnBoardCard(level);

            for (var row = 0; row < level.Rows; row++)
            {
                for (var column = 0; column < level.Columns; column++)
                {
                    var coordinate = new GridCoordinate(row, column);
                    var tile = Instantiate(tilePrefab, tileContainer);
                    tile.transform.localPosition = GetLocalPosition(coordinate, level);
                    tile.transform.localScale = Vector3.one;

                    var hasCar = level.HasCar(coordinate);
                    var colorIndex = coordinate.Row * 5 + coordinate.Column * 3 + level.Seed;

                    tile.Initialize(coordinate, level.GetDirection(coordinate), cellSize, hasCar, colorIndex);
                    tile.TapRequested += HandleTileTapped;
                    tiles.Add(coordinate, tile);
                }
            }

            SpawnExitGates(level);
        }

        public void PlayClearAnimation(GridCoordinate coordinate)
        {
            if (!tiles.TryGetValue(coordinate, out var tile))
            {
                return;
            }

            clearedCells.Add(coordinate);
            tile.PlayClearAnimation(GetTrailConnections(coordinate));
            RefreshNeighbourTrails(coordinate);
        }

        public void RestoreCar(GridCoordinate coordinate)
        {
            if (!tiles.TryGetValue(coordinate, out var tile))
            {
                return;
            }

            clearedCells.Remove(coordinate);
            tile.RestoreCar();
            RefreshNeighbourTrails(coordinate);
        }

        public void ShowHint(GridCoordinate coordinate)
        {
            if (tiles.TryGetValue(coordinate, out var tile))
            {
                tile.ShowHint();
            }
        }

        public void HideAllHints()
        {
            foreach (var tile in tiles.Values)
            {
                tile.HideHint();
            }
        }

        public void PlayWrongTapFeedback(GridCoordinate coordinate)
        {
            if (tiles.TryGetValue(coordinate, out var tile))
            {
                tile.PlayWrongTapFeedback();
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            foreach (var tile in tiles.Values)
            {
                tile.SetInputEnabled(enabled);
            }
        }

        private void SpawnBoardCard(MazeLevel level)
        {
            var cardSprite = TileVisualFactory.GetBoardCardSprite();
            if (cardSprite == null)
            {
                return;
            }

            boardCardObject = new GameObject("BoardCard");
            boardCardObject.transform.SetParent(tileContainer, false);
            boardCardObject.transform.localPosition = Vector3.zero;

            var sr = boardCardObject.AddComponent<SpriteRenderer>();
            sr.sprite = cardSprite;
            sr.sortingOrder = -2;

            var boardW = level.Columns * cellSize + (cellSize * 0.40f);
            var boardH = level.Rows * cellSize + (cellSize * 0.40f);
            boardCardObject.transform.localScale = new Vector3(boardW / (cardSprite.rect.width / cardSprite.pixelsPerUnit),
                                                              boardH / (cardSprite.rect.height / cardSprite.pixelsPerUnit),
                                                              1f);
        }

        private void SpawnExitGates(MazeLevel level)
        {
            var gateSprite = TileVisualFactory.GetExitGateSprite();
            if (gateSprite == null)
            {
                return;
            }

            var gateContainer = new GameObject("ExitGates");
            gateContainer.transform.SetParent(tileContainer, false);

            for (var row = 0; row < level.Rows; row++)
            {
                for (var column = 0; column < level.Columns; column++)
                {
                    var coord = new GridCoordinate(row, column);
                    var dir = level.GetDirection(coord);
                    var isExit = false;
                    var rotZ = 0f;
                    var offset = Vector3.zero;

                    if (row == 0 && dir == ArrowDirection.Up)
                    {
                        isExit = true;
                        rotZ = 0f;
                        offset = Vector3.up * (cellSize * 0.50f);
                    }
                    else if (row == level.Rows - 1 && dir == ArrowDirection.Down)
                    {
                        isExit = true;
                        rotZ = 180f;
                        offset = Vector3.down * (cellSize * 0.50f);
                    }
                    else if (column == 0 && dir == ArrowDirection.Left)
                    {
                        isExit = true;
                        rotZ = 90f;
                        offset = Vector3.left * (cellSize * 0.50f);
                    }
                    else if (column == level.Columns - 1 && dir == ArrowDirection.Right)
                    {
                        isExit = true;
                        rotZ = -90f;
                        offset = Vector3.right * (cellSize * 0.50f);
                    }

                    if (isExit)
                    {
                        var gateObj = new GameObject($"ExitGate ({row}, {column})");
                        gateObj.transform.SetParent(gateContainer.transform, false);
                        gateObj.transform.localPosition = GetLocalPosition(coord, level) + offset;
                        gateObj.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
                        gateObj.transform.localScale = Vector3.one * (cellSize * 0.42f);

                        var sr = gateObj.AddComponent<SpriteRenderer>();
                        sr.sprite = gateSprite;
                        sr.sortingOrder = 6;
                    }
                }
            }
        }

        private void RefreshNeighbourTrails(GridCoordinate coordinate)
        {
            foreach (var direction in CardinalDirections)
            {
                var neighbour = StraightLineLegality.Move(coordinate, direction);
                if (clearedCells.Contains(neighbour) && tiles.TryGetValue(neighbour, out var tile))
                {
                    tile.SetTrailConnections(GetTrailConnections(neighbour));
                }
            }
        }

        private TrailConnections GetTrailConnections(GridCoordinate coordinate)
        {
            var connections = TrailConnections.None;
            foreach (var direction in CardinalDirections)
            {
                if (clearedCells.Contains(StraightLineLegality.Move(coordinate, direction)))
                {
                    connections |= ConnectionFor(direction);
                }
            }

            return connections;
        }

        private static TrailConnections ConnectionFor(ArrowDirection direction)
        {
            switch (direction)
            {
                case ArrowDirection.Up:
                    return TrailConnections.Up;
                case ArrowDirection.Right:
                    return TrailConnections.Right;
                case ArrowDirection.Down:
                    return TrailConnections.Down;
                case ArrowDirection.Left:
                    return TrailConnections.Left;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        private void HandleTileTapped(GridCoordinate coordinate)
        {
            TileTapped?.Invoke(coordinate);
        }

        private Vector3 GetLocalPosition(GridCoordinate coordinate, MazeLevel level)
        {
            var pitch = cellSize * CellPitchMultiplier;
            var x = (coordinate.Column - ((level.Columns - 1) * 0.5f)) * pitch;
            var y = (((level.Rows - 1) * 0.5f) - coordinate.Row) * pitch;
            return new Vector3(x, y, 0f);
        }

        private void EnsureTileContainer()
        {
            if (tileContainer != null)
            {
                return;
            }

            var container = new GameObject("Tiles");
            container.transform.SetParent(transform, false);
            tileContainer = container.transform;
        }

        private void ClearGrid()
        {
            foreach (var tile in tiles.Values)
            {
                if (tile != null)
                {
                    tile.TapRequested -= HandleTileTapped;
                }
            }

            tiles.Clear();
            clearedCells.Clear();

            if (tileContainer != null)
            {
                tileContainer.gameObject.SetActive(false);
                Destroy(tileContainer.gameObject);
                tileContainer = null;
            }
        }

        private void FrameGridInCamera(MazeLevel level)
        {
            var gameplayCamera = Camera.main;
            if (gameplayCamera == null || !gameplayCamera.orthographic)
            {
                return;
            }

            gameplayCamera.backgroundColor = new Color32(245, 248, 252, 255);
            gameplayCamera.orthographicSize = portraitOrthographicSize;
            gameplayCamera.transform.position = new Vector3(0f, 0f, -10f);

            var horizontalFraction = 1f - (horizontalViewportMargin * 2f);
            var minimumBoardWidth = level.Columns * MinCellSize;
            var usableWidth = gameplayCamera.orthographicSize * 2f * gameplayCamera.aspect * horizontalFraction;
            if (usableWidth < minimumBoardWidth)
            {
                gameplayCamera.orthographicSize = minimumBoardWidth /
                                                   (2f * gameplayCamera.aspect * horizontalFraction);
                usableWidth = minimumBoardWidth;
            }

            var safeArea = Screen.safeArea;
            var safeBottom = Mathf.Clamp01(safeArea.yMin / Screen.height);
            var safeTop = Mathf.Clamp01(safeArea.yMax / Screen.height);
            var safeLeft = Mathf.Clamp01(safeArea.xMin / Screen.width);
            var safeRight = Mathf.Clamp01(safeArea.xMax / Screen.width);
            var safeBottomWorld = gameplayCamera.ViewportToWorldPoint(new Vector3(0.5f, safeBottom, 10f)).y;
            var safeTopWorld = gameplayCamera.ViewportToWorldPoint(new Vector3(0.5f, safeTop, 10f)).y;
            var safeCenterX = gameplayCamera.ViewportToWorldPoint(
                new Vector3((safeLeft + safeRight) * 0.5f, 0.5f, 10f)).x;
            var playableBottom = safeBottomWorld + footerReserve;
            var playableTop = safeTopWorld - headerReserve;
            var usableHeight = Mathf.Max(playableTop - playableBottom, 0.01f);

            var packedWidthUnits = 1f + ((level.Columns - 1) * CellPitchMultiplier);
            var packedHeightUnits = 1f + ((level.Rows - 1) * CellPitchMultiplier);
            cellSize = Mathf.Clamp(
                Mathf.Min(usableWidth / packedWidthUnits, usableHeight / packedHeightUnits),
                MinCellSize,
                MaxCellSize);

            var gridHeight = cellSize * packedHeightUnits;
            var verticalCenter = (playableBottom + playableTop) * 0.5f;

            if (usableHeight < gridHeight)
            {
                verticalCenter = (safeBottomWorld + safeTopWorld) * 0.5f;
            }

            tileContainer.position = new Vector3(safeCenterX, verticalCenter, 0f);
        }
    }
}
