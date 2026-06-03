# Minesweeper (Prueba-Junior)

A classic Minesweeper game built in Unity, designed to run on both desktop and mobile platforms.

<!-- TODO: Add a gameplay GIF here -->
<!-- ![Gameplay](docs/gameplay.gif) -->

## Features

- 4 difficulty levels with increasing grid sizes and mine counts
- First-click safety — mines are placed after the first reveal, guaranteeing a safe start
- Animated BFS flood-fill reveal for zero-adjacent-mine cells
- Flag system with toggle mode (right-click / long-press / flag-mode button)
- Win/lose face icon feedback with restart button
- Responsive grid scaling that adapts to screen size
- Event-driven architecture (OnWin, OnLose, OnRestart)
- Unit-tested game logic (EditMode tests, zero reflection on Board API)

<!-- TODO: Add screenshots here -->
<!-- | Nivel 1 | Nivel 4 | -->
<!-- |---------|---------| -->
<!-- | ![Nivel 1](docs/nivel1.png) | ![Nivel 4](docs/nivel4.png) | -->

## Requirements

- **Unity 2022.3 LTS** (tested with 2022.3.62f2)
- Universal Render Pipeline (URP) 2D
- TextMeshPro package

## How to Run

1. Clone this repository:
   ```
   git clone https://github.com/your-username/Prueba-Junior.git
   ```
2. Open the project in Unity 2022.3 LTS.
3. Open one of the level scenes from `Assets/Scenes/`:
   - `Nivel 1.unity` — 10×10 grid, 15 mines
   - `Nivel 2.unity` — 16×16 grid, 40 mines
   - `Nivel 3.unity` — 16×30 grid, 99 mines
   - `Nivel 4.unity` — 25×30 grid, 150 mines
4. Press **Play** in the Unity Editor, or build for your target platform.

## Controls

| Action | Desktop | Mobile |
|--------|---------|--------|
| Reveal cell | Left-click | Tap |
| Flag cell | Right-click | Long-press (0.5s) |
| Flag mode | — | Toggle flag-mode button (tap to flag) |
| Restart | Click the face icon | Tap the face icon |

## Architecture

```
Board (plain C#)          GridManager (MonoBehaviour)
┌─────────────────┐       ┌─────────────────────┐
│ Game logic       │       │ Cell instantiation   │
│ Mine placement   │       │ Layout & scaling     │
│ BFS reveal       │◄──────│ Coroutine animation  │
│ Win/lose check   │ events│ UI (timer, flags)    │
│ Flag toggle      │       │ Input delegation     │
└─────────────────┘       └─────────────────────┘
        ▲                           ▲
        │                           │
   CellData[,]                Cell (MonoBehaviour)
   (internal state)           (view: sprite rendering,
                               input: click/long-press)
```

**Key design split:**
- `Board` is a pure C# class with no Unity dependencies. It owns all game state and rules. Fully unit-testable without GameObjects.
- `GridManager` is the MonoBehaviour view/controller. It instantiates cell views, handles layout, animates BFS reveals via coroutines, and manages UI.
- `Cell` is a view component. Its state is set by `GridManager` from `Board` data, and it delegates input back to `GridManager`.

**Event flow:**
- `Board.OnWin` / `Board.OnLose` → `GridManager` re-fires as its own events
- `GameHud` subscribes to `GridManager.OnLose`/`OnWin`/`OnRestart` for face icon
- `GameLoader` subscribes to `GridManager.OnWin` for scene transition

## Design Decisions

### First-Click Safety
Mines are placed lazily on the first `Reveal` call. A 3×3 safe area around the clicked cell is excluded from mine placement, guaranteeing the player never hits a mine on the first click and often gets a flood-fill reveal.

### Deterministic Mine Placement
Uses Fisher-Yates shuffle on candidate cells instead of rejection sampling. This guarantees exactly `mineCount` mines are placed every time, regardless of density.

### BFS Flood-Fill Reveal
Zero-adjacent-mine cells are revealed via breadth-first search, grouped by BFS level. `GridManager` animates each level with a configurable delay (`revealDelay`), creating a cascading reveal effect.

### Event-Driven Win/Lose
The original code used frame-polling (`Update` checking `winState`) which could fire multiple times. This was replaced with one-shot events (`OnWin`, `OnLose`) fired from `Board.GameOver`.

## Testing

The project includes EditMode tests accessible via **Window > General > Test Runner**.

- `BoardTests` — 10 tests covering mine placement, flood-fill, flag toggling, win/lose detection. Pure C# tests with zero reflection and no GameObjects.
- `TimerTests` — 1 test verifying the timer UI updates correctly.

Run all tests:
1. Open **Window > General > Test Runner**
2. Select **EditMode** tab
3. Click **Run All**

## Project Structure

```
Assets/
├── Scripts/
│   ├── Board.cs          — Pure C# game logic (no MonoBehaviour)
│   ├── Cell.cs           — Cell view component (sprite + input)
│   ├── GameHud.cs        — Face icon HUD (win/lose/restart)
│   ├── GameLoader.cs     — Scene transition on win
│   └── GridManager.cs    — Grid view/controller (layout, animation, UI)
├── Tests/
│   └── Tests/
│       ├── BoardTests.cs — Board logic tests (EditMode)
│       └── TimerTests.cs — Timer UI tests (EditMode)
├── Scenes/
│   ├── Nivel 1.unity     — 10×10, 15 mines
│   ├── Nivel 2.unity     — 16×16, 40 mines
│   ├── Nivel 3.unity     — 16×30, 99 mines
│   └── Nivel 4.unity     — 25×30, 150 mines
├── Assets/               — Sprites (tileset, faces, frame)
└── Animations/           — Scene transition animation
```

## What I'd Do Next

From the project backlog:

- **Cross-platform input (P0.1)** — Unified input handler with flag-mode toggle button for mobile
- **Zoom/pan on large grids** — Pinch-to-zoom and drag-to-pan for Nivel 4 on mobile
- **Safe area handling** — Respect notch/home-bar insets on modern phones
- **LevelConfig ScriptableObject** — Data-driven levels instead of duplicated scenes
- **Audio** — Reveal, flag, win, and lose sound effects with mute toggle
- **Best-time persistence** — PlayerPrefs per difficulty
- **GitHub Actions CI** — EditMode tests running on push

## License

This project was created as a portfolio/test piece for a Junior Game Developer role.
