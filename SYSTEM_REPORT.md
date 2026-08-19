# 📊 Comprehensive System & Architecture Report: Tap Away Cars

**Document Version:** 1.2.0  
**Last Updated:** August 19, 2026  
**Target Repository:** `/Users/aryankinha/Documents/Aryan/Unity/arrowMaze`  
**Engine & Platform:** Unity 6000.5.8f1 (6000.5.8f1-5cb7df797b7d) | macOS / iOS / Android  

---

## 1. Executive Summary

**Tap Away Cars** is a commercial-grade 2D mobile traffic-clearing puzzle game built in Unity 6. 

The game combines directional arrow logic with a top-down traffic theme:
* **The Core Loop:** Players clear a crowded grid of cartoon cars by tapping them in the correct sequence.
* **Movement & Routing:** Tapped cars drive forward along asphalt lanes toward perimeter **EXIT gates**.
* **Obstruction & Lives:** If a car's path is blocked by another vehicle, it bumps with a red collision flash and camera-safe shake, deducting 1 Heart Life (3 Max).
* **Boosters & Meta:** Players can use **Hints 💡** to highlight unblocked vehicles, **Undo ↺** to rewind previous moves, progress through a 23-level authored and procedural campaign catalog, and earn up to 3 stars per level saved via local persistence.

---

## 2. Unity Project Configuration & Environment

| Configuration Area | Specification / Setting | Notes / Details |
|---|---|---|
| **Unity Version** | `6000.5.8f1` | Unity 6 release with C# 9+ and incremental GC |
| **Render Pipeline** | Universal Render Pipeline (`com.unity.render-pipelines.universal` v17.5.0) | 2D Renderer optimized for mobile Sprite and Canvas rendering |
| **Input System** | Unity Input System (`com.unity.inputsystem` v1.20.0) | Multi-touch, mouse click, and safe-area pointer raycasting |
| **UI Framework** | Unity UI (uGUI v2.5.0) & TextMeshPro (`com.unity.ugui`) | Vector-sharp typography, responsive layout anchors, and dynamic notch fitting |
| **Test Framework** | Unity Test Framework (`com.unity.test-framework` v1.7.0) | NUnit EditMode test runner for 100% puzzle solvability verification |
| **Target Orientation** | Portrait (9:16 / 1080×1920 reference) | Dynamic orthographic camera fitting with safe-area reserves |

---

## 3. Assembly Definition Architecture

The project codebase is partitioned into three explicit, modular Assembly Definitions:

```
Assets/_Project/
├── Scripts/
│   ├── ArrowMaze.Runtime.asmdef      (Root: ArrowMaze, References: InputSystem, UnityEngine.UI, TextMeshPro)
│   └── Editor/
│       └── ArrowMaze.Editor.asmdef   (Root: ArrowMaze.Editor, References: ArrowMaze.Runtime, TextMeshPro, UI)
└── Tests/EditMode/
    └── ArrowMaze.EditModeTests.asmdef (Root: ArrowMaze.Tests, References: ArrowMaze.Runtime, TestAssemblies)
```

* **`ArrowMaze.Runtime`**: Core puzzle algorithm, gameplay controllers, data catalogs, player progress, and UI runtime components.
* **`ArrowMaze.Editor`**: Editor tooling, automated UI builders (`Tools/Rebuild Gameplay UI`), and menu utilities.
* **`ArrowMaze.EditModeTests`**: Isolated test harness executing deterministic puzzle generation and validation test suites.

---

## 4. Scene & Navigation Flow

The game is structured across three dedicated scenes:

```mermaid
graph TD
    MainMenu[Assets/_Project/Scenes/MainMenu.unity] -->|Play / Continue| Gameplay[Assets/_Project/Scenes/Gameplay.unity]
    MainMenu -->|Level Map| LevelMap[Assets/_Project/Scenes/LevelMap.unity]
    LevelMap -->|Select Level| Gameplay
    Gameplay -->|Back Button| MainMenu
    Gameplay -->|Map Button| LevelMap
    Gameplay -->|Next Level| Gameplay
```

1. **`MainMenu.unity`**: Title hub with Continue button, Level Map button, Settings modal, and persistent progress overview.
2. **`LevelMap.unity`**: 23-level saga progression map displaying star ratings (`0..3 ⭐`) and level unlock statuses.
3. **`Gameplay.unity`**: Core gameplay screen with responsive HUD, board frame, live cars, connected roads, perimeter EXIT gates, and victory/defeat popup modals.

---

## 5. Asset Inventory & Resources

All visual assets were generated with 4× supersampling (512×512 master resolution, Lanczos filtered) to support high-density mobile screens without blurring.

### 🚗 Cars (`Assets/_Project/Sprites/Cars/` & `Resources/Sprites/Cars/`)
* `car_blue.png`, `car_red.png`, `car_yellow.png`, `car_green.png`, `car_purple.png`
* Top-down cartoon sports cars with glossy windshields, distinct headlights, and rear hazard lights.

### 🛣️ Modular Roads (`Assets/_Project/Sprites/Roads/` & `Resources/Sprites/Roads/`)
* `road_straight_v.png` & `road_straight_h.png`: Straight asphalt lanes with dashed white centerlines.
* `road_corner_0.png`, `road_corner_90.png`, `road_corner_180.png`, `road_corner_270.png`: 90-degree smooth curve corners.
* `road_t_junction.png`, `road_crossroad.png`, `road_end.png`: Multi-lane intersections and dead ends.

### 🚧 Props & Board Decor (`Assets/_Project/Sprites/Props/` & `UI/`)
* `exit_gate.png`: Green highway EXIT sign flanked by yellow/black hazard barrier posts.
* `card_board_bg.png`: Rounded white board card container with soft drop shadow.
* `selection_glow.png`: Concentric glowing pulse ring for hint targeting.
* `hand_pointer.png`: Animated tutorial hand pointer.

### 📱 UI Sprites (`Assets/_Project/Sprites/UI/`)
* `heart_full.png` / `heart_empty.png`: High-gloss 3D shaded red hearts for lives tracking.
* `button_circle.png`: Rounded circular white action button backing.
* `badge_pill.png`: Sliced rounded pill container for car counters and difficulty badges.
* `icon_back.png`, `icon_settings.png`, `icon_car_badge.png`, `icon_hint.png`, `icon_undo.png`: Navy vector UI glyphs.

---

## 6. Codebase Architecture & File-by-File Audit

### 📁 `Assets/_Project/Scripts/Core/`

#### 1. [`RoadTopology.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Core/RoadTopology.cs)
* **Responsibility:** Evaluates level car escape trajectories and constructs bitmask connectivity matrices (`RoadConnections.Up`, `Down`, `Left`, `Right`) and perimeter `RoadExit` markers.
* **Guarantee:** Visual road graphics (curves, straights, junctions) are 100% mathematically synchronized with legal physical car escape routes.

#### 2. [`MazeGenerator.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Core/MazeGenerator.cs)
* **Data Models:** `GridCoordinate` (immutable struct), `ArrowDirection` enum, `MazeLevel` (immutable board state holding directions, car occupancy matrix, and seed), `MazeGenerationSettings`.
* **Algorithm:** Reverse-simulated clear chains validated by `ChainPuzzleSolver`. Empty road cells are opened up via `CreateInitialClearedState()` so cars navigate freely through open corridors toward perimeter exits.

#### 3. [`PathValidator.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Core/PathValidator.cs)
* **Responsibility:** Central state machine for live gameplay validation.
* **Key Features:** Tracks total cars, cleared count, and `RemainingCars`. Manages the Undo stack (`TryUndo`) and dynamic Hint lookup (`GetHint`).

#### 4. [`StraightLineLegality.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Core/StraightLineLegality.cs)
* **Responsibility:** Pure mathematical raycaster validating whether a car has an unobstructed path to an active `RoadExit` with an EXIT gate.

#### 5. [`GridManager.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Core/GridManager.cs)
* **Responsibility:** World-space layout, camera framing, tile instantiation, board background card scaling, and perimeter exit gate placement.
* **Camera Framing:** Calculates safe area insets and reserves header/footer clearance so the board is centered and never clips with UI buttons. Keeps EXIT signs upright across all boundaries.

---

### 📁 `Assets/_Project/Scripts/Gameplay/`

#### 6. [`TileController.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Gameplay/TileController.cs)
* **Internal Layers:** Child 0 (`Road`), Child 1 (`Glow`), Child 2 (`Car`).
* **Animations:**
  * `ClearDriveRoutine`: Drives car forward along its direction vector past the EXIT gate with smooth step and alpha fade.
  * `WrongTapRoutine`: Shakes **only** `carTransform.localPosition` horizontally with red tint, leaving the underlying road static.
  * `HintGlowRoutine`: Pulses concentric selection ring when Hint is triggered.

#### 7. [`TileVisualFactory.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Gameplay/TileVisualFactory.cs)
* **Responsibility:** Asset cache and modular road piece selector.
* **Autotiling:** Dynamically selects `road_straight_v`, `road_straight_h`, `road_corner_0/90/180/270`, `road_t_junction`, `road_crossroad`, or `road_end` based on `RoadConnections`.

#### 8. [`LevelController.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Gameplay/LevelController.cs)
* **Responsibility:** Top-level gameplay orchestrator connecting `GridManager`, `LivesManager`, `PathValidator`, and `GameplayHUD`.

#### 9. [`LivesManager.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Gameplay/LivesManager.cs)
* **Responsibility:** 3-life heart system. Deducts lives on blocked taps and fires `OnGameOver` when lives reach 0.

---

### 📁 `Assets/_Project/Scripts/Data/` & `Meta/`

#### 10. [`LevelCatalog.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Data/LevelCatalog.cs)
* **Responsibility:** 23 authored and procedural level definitions (Tutorial, Authored, Procedural, Challenge) with tailored grid dimensions (1×1 to 6×8), car densities, trap densities, and branching factors.

#### 11. [`PlayerProgress.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Meta/PlayerProgress.cs)
* **Responsibility:** Local save data persistence (`TapAwayCars.PlayerProgress.v1`) tracking `highestUnlockedLevel`, `lastPlayedLevel`, and star ratings per level (`0..3 ⭐`).

#### 12. [`LevelSession.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Meta/LevelSession.cs)
* **Responsibility:** Static session data bridge carrying `SelectedLevel` across scene transitions.

---

### 📁 `Assets/_Project/Scripts/UI/`

#### 13. [`GameplayHUD.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/UI/GameplayHUD.cs)
* **Responsibility:** Live UI presentation controller binding Title, Level number, Car counter pill, 3 dynamic hearts, difficulty pill, Hint button (with live count badge), Undo button, and victory/defeat popup modals.

#### 14. [`SafeAreaFitter.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/UI/SafeAreaFitter.cs)
* **Responsibility:** Dynamically adjusts UI RectTransform anchors to accommodate hardware notches, dynamic islands, and home indicator bars across mobile devices.

#### 15. [`MainMenuController.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/UI/MainMenuController.cs) & [`LevelMapController.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/UI/LevelMapController.cs)
* **Responsibility:** Navigation and presentation controllers for the Main Menu and 23-level saga progression map.

---

### 📁 `Assets/_Project/Scripts/Editor/`

#### 16. [`GameplayUIBuilder.cs`](file:///Users/aryankinha/Documents/Aryan/Unity/arrowMaze/Assets/_Project/Scripts/Editor/GameplayUIBuilder.cs)
* **Responsibility:** Editor automation tool accessible via `Tools/Rebuild Gameplay UI`. Builds the complete responsive canvas hierarchy, configures TextMeshPro styling, assigns sprites, sets non-blocking raycast targets, and wires serialized fields.

---

## 7. Reconstructed Gameplay Canvas Structure (`Gameplay.unity`)

```text
HUD (Canvas: Screen Space - Overlay, Scaler: 1080×1920, Match 0.5)
 └── Safe Area (SafeAreaFitter: Dynamic Mobile Inset)
      │
      ├── Background (Color: #F4F7FC, Raycast Target: False)
      │
      ├── Header (Anchored Top: 0..1 x 1, Height: 130)
      │    ├── BackButton (Circular 96×96, button_circle + icon_back.png, Navy #17233D)
      │    ├── TitleGroup (Centered 560×110)
      │    │    ├── Title ("Tap Away Cars", 44pt Bold, Navy #17233D)
      │    │    └── Level Text ("Level 23", 26pt Regular, Slate Gray #66758F)
      │    └── SettingsButton (Circular 96×96, button_circle + icon_settings.png, Navy #17233D)
      │
      ├── StatusRow (Anchored Below Header: Y = -165, Height: 80)
      │    ├── CarCounterPill (210×74, badge_pill.png)
      │    │    ├── CarIcon (icon_car_badge.png, Navy #17233D)
      │    │    └── Cars Remaining (TextMeshPro "42", 32pt Bold Navy #17233D, Live)
      │    ├── Lives (230×74, HorizontalLayoutGroup, Spacing 16)
      │    │    ├── Heart 1 (54×54, heart_full.png)
      │    │    ├── Heart 2 (54×54, heart_full.png)
      │    │    └── Heart 3 (54×54, heart_full.png)
      │    └── DifficultyPill (210×74, badge_pill.png)
      │         └── Difficulty Badge (TextMeshPro "Normal", 28pt Bold Navy #17233D, Live)
      │
      ├── BottomControls (Anchored Bottom: Y = +48, Height: 200)
      │    ├── Hint Button (Circular 156×156, button_circle.png)
      │    │    ├── Icon (icon_hint.png, Navy #17233D)
      │    │    ├── Label ("Hint", 22pt Bold Navy #17233D)
      │    │    └── CountBadge (46×46 Pill Blue #2F80ED at Top-Right)
      │    │         └── Hint Count ("2", 24pt Bold White, Live)
      │    └── Undo Button (Circular 156×156, button_circle.png)
      │         ├── Icon (icon_undo.png, Navy #17233D)
      │         └── Label ("Undo", 22pt Bold Navy #17233D)
      │
      └── Result Popup (Modal Overlay for Victory / Game Over)
```

---

## 8. Resolution History & Quality Audit

```
┌───────────────────────────────────────┬─────────────────────────────────────────────────────────────┬────────────┐
│ Issue Identified                      │ Root Cause & Resolution Applied                             │ Status     │
├───────────────────────────────────────┼─────────────────────────────────────────────────────────────┼────────────┤
│ 1. Road shook with car on blocked tap │ TileController wrong-tap routine shifted root transform.    │ FIXED ✅   │
│                                       │ Shifting isolated to carTransform.localPosition only.      │            │
├───────────────────────────────────────┼─────────────────────────────────────────────────────────────┼────────────┤
│ 2. Blue grid lines/dots overlay       │ Legacy SDF procedural trail was rendering on empty cells.   │ FIXED ✅   │
│                                       │ Trail renderer removed; clean asphalt roads maintained.    │            │
├───────────────────────────────────────┼─────────────────────────────────────────────────────────────┼────────────┤
│ 3. Chopped-up, disconnected roads     │ Hardcoded road_straight_v was rotating on every cell.       │ FIXED ✅   │
│                                       │ RoadTopology autotiling selects straight/curves/junctions.  │            │
├───────────────────────────────────────┼─────────────────────────────────────────────────────────────┼────────────┤
│ 4. Cars escaping arbitrary borders    │ StraightLineLegality accepted any out-of-bounds coordinate. │ FIXED ✅   │
│                                       │ Now requires valid RoadExit with EXIT gate at perimeter.   │            │
├───────────────────────────────────────┼─────────────────────────────────────────────────────────────┼────────────┤
│ 5. Upside-down EXIT signs on bottom   │ GateRotation flipped text 180 degrees.                      │ FIXED ✅   │
│                                       │ Signs now remain upright and readable across all borders.   │            │
├───────────────────────────────────────┼─────────────────────────────────────────────────────────────┼────────────┤
│ 6. Disorganized Canvas UI layout      │ Prototype UI lacked structured responsive groups.           │ FIXED ✅   │
│                                       │ Reconstructed Header, StatusRow, and BottomControls.        │            │
└───────────────────────────────────────┴─────────────────────────────────────────────────────────────┴────────────┘
```

---

## 9. Current Build & Verification Status

* **Compiler Diagnostics:** Checked via Unity MCP — **0 active errors, 0 active warnings**.
* **Play Mode State:** Full game loop verified:
  * Cars navigate through open corridors toward perimeter exits.
  * Invalid taps decrement player hearts and trigger red collision feedback.
  * Undo restores previous cars and decrements cleared count.
  * Hint highlights immediate solvable cars.
  * Victory popup appears upon clearing all cars with 1..3 stars recorded to player save data.
* **Touch Input:** All background images and labels have `raycastTarget = false`, allowing taps to reach 2D car colliders without obstruction.

---

*Report compiled and verified against the live Unity 6 environment.*
