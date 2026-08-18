# 📊 Comprehensive System & Architecture Report: Tap Away Cars (Arrow Maze)

**Document Version:** 1.0.0  
**Generated On:** August 18, 2026  
**Target Repository:** `/Users/aryankinha/Documents/Aryan/Unity/arrowMaze`  
**Engine & Platform:** Unity 6000.5.8f1 (6000.5.8f1-5cb7df797b7d) | macOS / iOS / Android  

---

## 1. Executive Summary

This project is a 2D grid puzzle game built in Unity 6, evolving from an abstract arrow-based directional clearing game ("Arrow Maze") toward a stylized mobile traffic-clearing puzzle game ("Tap Away Cars"). 

The core game loop tasks the player with clearing a board of cars by tapping them in the correct sequence. Cars drive along designated road lanes and exit through perimeter **EXIT gates**. Tapping a car with an unobstructed path allows it to escape; tapping a blocked car triggers a collision/hazard feedback and deducts one heart life.

---

## 2. Unity Project Configuration & Environment

| Configuration Area | Specification / Setting | Notes / Details |
|---|---|---|
| **Unity Version** | `6000.5.8f1` | Unity 6 release with modern C# compiler support |
| **Render Pipeline** | Universal Render Pipeline (`com.unity.render-pipelines.universal` v17.5.0) | 2D Renderer configuration for Sprite and UI rendering |
| **Input System** | Unity Input System (`com.unity.inputsystem` v1.20.0) | Supports mouse click, multi-touch, and safe-area pointer raycasting |
| **UI Framework** | Unity UI (uGUI v2.5.0) & TextMeshPro (`com.unity.ugui`) | Vector-sharp font rendering and canvas safe-area fitting |
| **Test Framework** | Unity Test Framework (`com.unity.test-framework` v1.7.0) | NUnit EditMode unit tests for generation and puzzle solving |
| **Default Scene** | `Assets/_Project/Scenes/Gameplay.unity` | Build Index 1 in `EditorBuildSettings.asset` |
| **Target Orientation** | Portrait (9:16 / 1080×1920 reference) | Dynamic orthographic camera scaling with safe-area reserves |

---

## 3. Package Dependencies (`Packages/manifest.json`)

* `com.unity.2d.sprite` (1.0.0) & `com.unity.2d.spriteshape` (15.0.3): 2D sprite rendering and packing.
* `com.unity.2d.tilemap` (1.0.0): Grid and tilemap layout modules.
* `com.unity.inputsystem` (1.20.0): Hardware-agnostic touch and pointer handling.
* `com.unity.render-pipelines.universal` (17.5.0): Lightweight 2D graphical pipeline.
* `com.unity.ugui` (2.5.0): Canvas, RectTransform, and TextMeshPro UI system.
* `com.unity.test-framework` (1.7.0): Test runner for procedural algorithm validation.

---

## 4. Assembly Definition Architecture

The project is cleanly decoupled into two explicit Assembly Definitions:

```
Assets/_Project/
├── Scripts/
│   └── ArrowMaze.Runtime.asmdef      (Root: ArrowMaze, References: InputSystem, UnityEngine.UI, TextMeshPro)
└── Tests/EditMode/
    └── ArrowMaze.EditModeTests.asmdef (Root: ArrowMaze.Tests, References: ArrowMaze.Runtime, TestAssemblies)
```

* **`ArrowMaze.Runtime`**: Encapsulates all production logic (Core algorithmic solver, Gameplay controller, and UI).
* **`ArrowMaze.EditModeTests`**: Isolated test assembly compiling only in the Editor, preventing test harnesses from leaking into production builds.

---

## 5. Asset Inventory & Resources

All visual assets were generated with 4× supersampling (512×512 master resolution, Lanczos filtered) to support Retina and Ultra-HD mobile viewports.

### 🚗 Cars (`Assets/_Project/Sprites/Cars/` & `Resources/Sprites/Cars/`)
* `car_blue.png` / `car_red.png` / `car_yellow.png` / `car_green.png` / `car_purple.png`
* Top-down cartoon sports cars with glossy windshield highlights, distinct headlights, and rear hazard lights.

### 🛣️ Roads (`Assets/_Project/Sprites/Roads/` & `Resources/Sprites/Roads/`)
* `road_straight_v.png` / `road_straight_h.png`: Straight lanes with white dashed centerlines and asphalt curbs.
* `road_corner_0.png` / `road_corner_90.png` / `road_corner_180.png` / `road_corner_270.png`: 90-degree curved turns.
* `road_t_junction.png` / `road_crossroad.png` / `road_end.png`: Multi-lane intersections and dead ends.

### 🚧 Props & Board Decor (`Assets/_Project/Sprites/Props/` & `UI/`)
* `exit_gate.png`: Green highway EXIT sign flanked by yellow/black hazard barrier posts.
* `card_board_bg.png`: White rounded board card container with soft shadow.
* `selection_glow.png`: Concentric glowing pulse ring for hint targeting.

### 📱 UI Sprites (`Assets/_Project/Sprites/UI/`)
* `heart_full.png` / `heart_empty.png`: High-gloss 3D shaded red hearts for lives tracking.
* `button_circle.png`: Neumorphic circular button backing.
* `icon_hint.png` / `icon_undo.png` / `icon_car_badge.png` / `icon_settings.png` / `icon_back.png`: Vector UI glyphs.
* `badge_pill.png`: Rounded pill container for counters and difficulty badges.

---

## 6. Codebase Architecture & File-by-File Audit

### 📁 `Assets/_Project/Scripts/Core/`

#### 1. [`MazeGenerator.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Core/MazeGenerator.cs)
* **Data Models:**
  * `GridCoordinate`: Immutable struct `(Row, Column)` with hash code and equality operators.
  * `ArrowDirection`: Enum `(Up, Right, Down, Left)`.
  * `MazeLevel`: Immutable board data holding 2D directions `directions[,]`, `hasCar[,]` occupancy matrix, construction metadata, trap coordinates, and `Seed`.
  * `MazeGenerationSettings`: Procedural parameters (rows, columns, seed, trap density, branching factor, solver limit, car density).
* **Algorithm:**
  * Generates puzzles in **reverse order** (from exits back into the board).
  * Validates solvability by running `ChainPuzzleSolver.TrySolve()` on every generated candidate before acceptance.
  * Subsets car placements based on `CarDensity` along the validated solving sequence to maintain 100% solvability.

#### 2. [`PathValidator.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Core/PathValidator.cs)
* **Responsibility:** Central state machine for live gameplay validation.
* **Key Features:**
  * Tracks `totalCars`, `clearedCount`, and `RemainingCars`.
  * `RegisterTap(coordinate)`: Validates if a tap is legal. On success, records into `tapHistory` (Undo stack) and raises `OnCorrectTap`. On failure, raises `OnIncorrectTap`.
  * `TryUndo(out restoredCoordinate)`: Pops the last cleared car from the stack and restores board state.
  * `GetHint()`: Queries `StraightLineLegality.GetLegalTaps()` to find an unblocked car capable of escaping immediately.

#### 3. [`StraightLineLegality.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Core/StraightLineLegality.cs)
* **Responsibility:** Pure mathematical raycaster determining if a cell has a direct clear path.
* **Logic:** Iterates `Move(current, direction)` forward. If it encounters an uncleared occupied cell, returns `false`; if it reaches out-of-bounds, returns `true`.

#### 4. [`ChainPuzzleSolver.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Core/ChainPuzzleSolver.cs)
* **Responsibility:** Backtracking tree search solver used as the acceptance gate for generated mazes.
* **Safety:** Enforces a hard search state budget (`maxStates = 250000`) to guarantee zero engine hangs during generation.

#### 5. [`GridManager.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Core/GridManager.cs)
* **Responsibility:** World-space layout, camera framing, tile instantiation, board background card, and exit sign placement.
* **Camera Fitting:** Dynamically computes orthographic camera bounds based on screen aspect ratio, safe area, and header/footer reserves.

---

### 📁 `Assets/_Project/Scripts/Gameplay/`

#### 6. [`TileController.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Gameplay/TileController.cs)
* **Responsibility:** Per-tile visual layering, collision detection, and animation execution.
* **Internal Layers:**
  * Child 0 (`Road`): `SpriteRenderer` rendering asphalt segment.
  * Child 1 (`Trail`): `SpriteRenderer` for clear trail connectivity.
  * Child 2 (`Glow`): `SpriteRenderer` for Hint pulse animation.
  * Child 3 (`Car`): `SpriteRenderer` for car sprite in designated color.
* **Animations:**
  * `ClearDriveRoutine`: Animates car driving forward along its direction vector with ease-in/ease-out and alpha fade.
  * `WrongTapRoutine`: Shakes position horizontally with red flash tint.

#### 7. [`TileVisualFactory.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Gameplay/TileVisualFactory.cs)
* **Responsibility:** Centralized sprite cache and asset loader.
* **Loading Hierarchy:** Loads from `Resources.Load<Sprite>()` with in-memory dictionary caching, falling back to runtime procedural Signed Distance Field (SDF) rasterization if assets are absent.

#### 8. [`LevelController.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Gameplay/LevelController.cs)
* **Responsibility:** Top-level gameplay orchestrator. Connects `GridManager`, `LivesManager`, `PathValidator`, and `GameplayHUD`.
* **Flow:** Listens to tap events, routes undo and hint triggers, handles win/lose conditions, and manages level restarts.

#### 9. [`LivesManager.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Gameplay/LivesManager.cs)
* **Responsibility:** Manages player hearts (default: 3). Deducts lives on invalid taps and invokes `OnGameOver` when lives reach 0.

---

### 📁 `Assets/_Project/Scripts/UI/`

#### 10. [`GameplayHUD.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/UI/GameplayHUD.cs)
* **Responsibility:** uGUI HUD presentation controller.
* **Elements:** Header title ("Tap Away Cars"), level counter ("Level 23"), real-time car remaining text, 3 red hearts with pulsing loss animations, Hint button with badge counter, Undo button, and victory/defeat popup modal animations.

#### 11. [`SafeAreaFitter.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/UI/SafeAreaFitter.cs)
* **Responsibility:** Dynamically adjusts UI RectTransform anchors to accommodate hardware notches, dynamic islands, and home-indicator bars across Android and iOS devices.

---

## 7. Deep-Dive Audit of Active Issues & Exact Code Root Causes

The following table provides the exact line-by-line evidence for the issues currently observed in play:

```
┌───────────────────────────────────────────────┬─────────────────────────────────────────────────────────────┐
│ Problem Observed                              │ Exact Code Location & Mechanism                             │
├───────────────────────────────────────────────┼─────────────────────────────────────────────────────────────┤
│ 1. Road shakes with car on blocked tap        │ TileController.cs (Lines 293-295, 302):                     │
│                                               │ transform.localPosition is modified. Because Road and Car   │
│                                               │ are children of this root transform, both shake together.   │
├───────────────────────────────────────────────┼─────────────────────────────────────────────────────────────┤
│ 2. Blue grid lines & dots appear on the board │ TileController.cs (Line 114) & GridManager.cs (Line 236):   │
│                                               │ Cleared/empty road cells activate trailRenderer with the    │
│                                               │ legacy SDF RuntimeTrail procedural sprite (#2196F3 blue).   │
├───────────────────────────────────────────────┼─────────────────────────────────────────────────────────────┤
│ 3. Roads look chopped-up & disconnected       │ TileVisualFactory.cs (Line 59) & TileController.cs (L84-95):│
│                                               │ All tiles load road_straight_v and rotate it independently. │
│                                               │ No neighbor autotiling for corners, T-junctions, or curves. │
├───────────────────────────────────────────────┼─────────────────────────────────────────────────────────────┤
│ 4. Cars exit across any arbitrary border      │ StraightLineLegality.cs (Lines 31-36):                      │
│                                               │ Checks only !level.IsInBounds(current) to declare an exit,  │
│                                               │ ignoring whether an actual road or Exit Gate exists there.  │
├───────────────────────────────────────────────┼─────────────────────────────────────────────────────────────┤
│ 5. Missing floating UI buttons in scene       │ Gameplay.unity Scene Hierarchy:                             │
│                                               │ The scene Canvas has legacy Header elements and lacks the   │
│                                               │ floating Hint/Undo buttons and status pills.                │
└───────────────────────────────────────────────┴─────────────────────────────────────────────────────────────┘
```

---

## 8. Architectural Action Plan & Recommendations

To complete the transformation into the "Tap Away Cars" game, the following targeted refinements are required:

1. **Decouple Car Animation from Road Transform:**
   * In [`TileController.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Gameplay/TileController.cs), alter `WrongTapRoutine()` to translate `carTransform.localPosition` instead of `transform.localPosition`.
2. **Deactivate Legacy Prototype Trail:**
   * Disable `trailRenderer` on asphalt road tiles so roads remain clean with asphalt and lane dashes.
3. **Implement Connected Road Mesh / Autotiling:**
   * Enhance [`TileVisualFactory.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Gameplay/TileVisualFactory.cs) to evaluate adjacent road neighbors and assign the appropriate sprite: `road_corner_0/90/180/270`, `road_t_junction`, or `road_crossroad`.
4. **Lane-Bound Exit Routing:**
   * Update [`StraightLineLegality.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Core/StraightLineLegality.cs) and [`GridManager.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Core/GridManager.cs) to ensure cars only exit at designated Exit Gate coordinates.
5. **Finalize Canvas Scene Hierarchy:**
   * Update [`Gameplay.unity`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scenes/Gameplay.unity) Canvas to include the car counter pill, mode badge, and floating Hint/Undo buttons.

---

*Report compiled and verified against the live Unity 6 environment.*
