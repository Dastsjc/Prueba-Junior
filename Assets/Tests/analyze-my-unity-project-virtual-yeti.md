# Minesweeper (Prueba-Junior) — Review + Per-Point Implementation Plans

## Context

Unity 2D Minesweeper built as a portfolio/test piece to land a **Junior Game Developer** role that requires the game to run on **mobile and desktop**. Goal: an honest code assessment plus detailed, individually-executable plans for each improvement, so the project both meets the mobile requirement and impresses the engineers reviewing it.

The user does **not** want to implement yet — this document is a planning reference. Each point below is written as a self-contained mini-plan that can be executed on its own, in any order (dependencies noted).

Codebase: `Assets/Scripts/Grid.cs` (447 lines, almost all logic), `Cell.cs` (73), `GameLoader.cs` (32), tests in `Assets/Tests/Tests/`, 4 level scenes, URP 2D, TMP, proper `.asmdef`s + `.gitignore`.

---

## Verdict (short)

Strong **above-average junior** work — not yet mid-level. Real strengths: unit tests exist, assembly definitions, clean `.gitignore`, first-click safety (`Grid.cs:270`), animated BFS flood-fill reveal (`Grid.cs:333`), responsive scaling. The gap to "professional": a God-class `Grid`, reflection-driven tests, copy-paste duplication, frame-polling instead of events, no encapsulation, and **input that cannot run on mobile**. All fixable — and fixing them is exactly what earns the interview.

## Is three tiers "enough"? (answer to the user's question)

No single list is complete, but the tiers cover the high-leverage work. Additional areas worth knowing about are in the **"Further areas (P3)"** section — notably **zoom/pan** and **safe area**, which I'd promote out of "optional" because the role requires mobile. My recommendation for a *test submission*: commit to **P0 + P1 done excellently**, add **selected P2**, and treat P3 as a documented backlog. Depth beats breadth; an over-scoped, half-finished project reads worse than a tight, polished one.

---

# P0 — Correctness & the mobile+desktop requirement

### P0.1 — Cross-platform input (mouse + touch)
- **Why:** `Cell.OnMouseOver()` (`Cell.cs:62-72`) flags with right-click (`Input.GetMouseButtonDown(1)`), which doesn't exist on touch, and relies on per-cell `OnMouseOver` (1,925 colliders on Nivel 4, unreliable with touch). The game currently cannot be played on mobile.
- **Files:** new `Assets/Scripts/InputHandler.cs`; edit `Cell.cs` (remove `OnMouseOver`); edit `Grid.cs` (expose `RevealCell`/`ToggleFlag` to the handler — already public); UI: add a "Flag mode" toggle button to each level scene's Canvas.
- **Steps:** (1) Remove input from `Cell`. (2) Add one `InputHandler` that, on tap/click, raycasts (`Physics2D.OverlapPoint` / `Camera.ScreenToWorldPoint`) to find the cell under the pointer. (3) Add a **Flag-mode toggle button** (works identically on desktop + mobile): tap reveals, or flags when flag-mode is on. (4) Optional: long-press to flag on touch as a shortcut. (5) Unify mouse + `Input.GetTouch` (or adopt the Input System package).
- **Done when:** reveal + flag both work with mouse on desktop and with touch in a mobile build, with no per-cell colliders driving input.

### P0.2 — Replace win-polling with an event (fixes a real bug)
- **Why:** `GameLoader.Update()` (`GameLoader.cs:13-19`) checks `gridManager.winState` every frame and calls `LoadNextLevel()` *repeatedly* once won, re-firing `SetTrigger("Start")` and the coroutine every frame for `transitionTime`.
- **Files:** `Grid.cs` (add `public event Action OnWin;`, raise once in `GameOver(true)`, remove `winState`), `GameLoader.cs` (subscribe in `OnEnable`/`OnDisable`, delete `Update`).
- **Done when:** the transition fires exactly once on win; no per-frame polling; `winState` field removed.

### P0.3 — Deterministic mine placement
- **Why:** `PlaceMines` (`Grid.cs:258-279`) uses rejection sampling capped at 1000 attempts; at high density it can place fewer than `mineCount` mines, desyncing flags and the win check.
- **Files:** `Grid.cs` (`PlaceMines`).
- **Steps:** build the list of candidate coordinates (all cells minus the 3×3 safe area), Fisher-Yates shuffle, take the first `mineCount`. No attempt cap.
- **Done when:** placed mine count always equals `mineCount`, verified by a test, even on Nivel 4 repeated regenerations.

### P0.4 — Lose state UI + restart button
- **Why:** losing only does `Debug.Log("Game Over!")` (`Grid.cs:428`) and reveals mines. The Happy/Doh face sprites exist but aren't wired to clear feedback, and there's no on-screen restart.
- **Files:** `Grid.cs` (raise `OnWin`/`OnLose` events; remove the `Debug.Log`), a small `GameHud` script + UI in each level scene (win/lose face, "Restart" button calling existing `RegenerateGrid`).
- **Done when:** win and loss show distinct, visible feedback and a working restart button.

---

# P1 — Clean code & architecture (earns the interview)

### P1.1 — Extract a plain-C# `Board` class (keystone change)
- **Why:** `Grid` is a God class and the tests can only reach logic via reflection (`GridTests.cs:51-59`). Separating rules from `MonoBehaviour` fixes both.
- **Files:** new `Assets/Scripts/Board.cs` (no `MonoBehaviour`): holds `Cell[,]`/data, `PlaceMines`, `CalculateNeighbors`, `Reveal(x,y)` returning revealed coords, `ToggleFlag`, `IsWin`/`IsLose`. `Grid.cs` becomes a thin view/controller that instantiates cell views and renders `Board` state; keeps Unity-only concerns (layout, coroutine reveal animation, UI).
- **Done when:** all game rules live in `Board`, `Grid` only maps state→sprites, and the project compiles + plays identically. Unlocks P1.2.

### P1.2 — Rewrite tests against the public `Board` API
- **Why:** kill reflection; make tests fast EditMode tests with no GameObjects.
- **Files:** `GridTests.cs` (rewrite as `BoardTests.cs`), keep `TimerTests` (or move timer to its own class first — see P1.3).
- **Steps:** test exact mine count, neighbor counts, flood-fill reveal expands only through zeros, flag toggle/limit, win/lose detection. No `GetMethod(NonPublic)` / `GetField(NonPublic)` anywhere.
- **Done when:** Test Runner is green with zero reflection.

### P1.3 — Remove duplication & split responsibilities
- **Why:** `GenerateGrid` (`179-210`) ≈ `RegenerateGrid` (`212-256`); centering math repeated 3× (`113-114`, `186-189`, `233-234`); timer + flag UI live inside `Grid`.
- **Files:** `Grid.cs` (extract `BuildCells()` and `GetCellWorldPosition(x,y)`; remove the `transform.GetChild(0)` guard at `219`); optional `Timer.cs` and `FlagCounter.cs` components.
- **Done when:** no copy-pasted grid-build loop; one source of truth for cell positioning.

### P1.4 — Encapsulation pass
- **Why:** nearly all fields are `public` (`Cell.cs:5-20`, `Grid.cs:9-46`), incl. the `[HideInInspector] public bool winState` hack (`Grid.cs:30`).
- **Files:** `Cell.cs`, `Grid.cs`.
- **Steps:** `[SerializeField] private` for inspector fields; properties for cross-class reads; mutate cell state through methods; delete `winState` (replaced by the P0.2 event).
- **Done when:** no public mutable state leaks; inspector wiring still works.

### P1.5 — Project hygiene
- **Files:** all scripts.
- **Steps:** wrap code in a namespace (e.g. `Buscaminas.Gameplay`); remove dead `using JetBrains.Annotations;` (`Grid.cs:5`) and unused `System.Collections.Generic` in `GameLoader`; add XML doc comments to public APIs; add an `.editorconfig`.
- **Done when:** no dead usings, code namespaced, public APIs documented.

### P1.6 — README.md (highest-leverage for *getting* the interview)
- **Files:** new `README.md` at repo root.
- **Contents:** one-line pitch; animated GIF + screenshots; how to run; controls for **both** desktop and mobile; a short architecture diagram/overview (Board vs view, events); design decisions (first-click safety, flood-fill); "what I'd do next." A reviewer often reads this before the code.
- **Done when:** a stranger can understand, run, and play the game from the README alone.

---

# P2 — "Complete game" polish (generalist signal)

### P2.1 — `LevelConfig` ScriptableObject + data-driven flow
- **Why:** 4 duplicated scenes + `buildIndex+1` flow (`GameLoader.cs:23`) is fragile.
- **Files:** new `LevelConfig.cs` (ScriptableObject: width/height/mineCount), one gameplay scene that loads a config, a main menu + difficulty select. Removes the per-scene duplication.
- **Done when:** levels are data assets; adding/tuning a level needs no scene edits.

### P2.2 — Theme ScriptableObject for sprites
- **Why:** each of 1,925 cells stores its own copy of all 5 sprite refs (`Cell.cs:15-20`, set per-cell in `Grid.cs:200-204`).
- **Files:** new `CellTheme.cs` (ScriptableObject) referenced once by the view; cells read from shared data.
- **Done when:** sprites defined once; cells hold no duplicate sprite arrays.

### P2.3 — Best-time persistence (PlayerPrefs) per difficulty.
### P2.4 — Audio: reveal / flag / win / lose SFX (+ mute toggle).
### P2.5 — Performance: maintain a `safeRevealedCount` instead of rescanning the grid each reveal batch (`CheckWinCondition`, `401-416`); pool cells across regenerations instead of destroy+instantiate.
### P2.6 — GitHub Actions CI (game-ci) running EditMode tests on push — strong "works in a team" signal.

---

# Further areas (P3) — documented backlog

Honest note: diminishing returns for a test project. Promote the first two given the mobile requirement; the rest are optional standouts.

- **Zoom/pan on large grids (recommend promoting to P0/P1).** Nivel 4 (1,925 cells) is unplayable on a phone without pinch-to-zoom + drag-to-pan.
- **Safe-area handling (recommend promoting).** Respect notch/home-bar insets so UI isn't clipped on modern phones.
- **Game feel / juice:** reveal tween, win particle burst, light haptics on mobile.
- **Classic depth features:** chording (click a satisfied number to reveal neighbors) and `?` marking — signal real Minesweeper knowledge.
- **Settings + pause** (volume/mute, pause menu).
- **Accessibility:** colorblind-safe number palette, larger tap targets on mobile.
- **Defensive validation:** clamp/validate `mineCount` vs grid size; consistent language (scenes "Nivel" vs English code).
- **Save/resume** an in-progress game.

---

## Verification (applies once any tier is implemented)
- **Tests:** Window > General > Test Runner — EditMode green, no reflection after P1.
- **Desktop:** play each level — reveal, flag (toggle + flag-mode), win→single transition, lose→restart.
- **Mobile:** build to a device / iOS Simulator — tap-to-reveal + flag-mode/long-press, grid scales (and zoom/pan if P3 done), UI inside safe area.
- **Robustness:** regenerate Nivel 4 repeatedly; placed-mine count always equals `mineCount`; flag counter stays in sync.

## Committed scope for the submission (decided)

**All of P0 + all of P1 (done excellently) + selected P2 + two P3 items promoted for mobile.** Everything else in P3 stays a documented backlog.

**In scope:**
- **P0** — P0.1 input, P0.2 win-event, P0.3 mine placement, P0.4 lose/restart UI.
- **P1** — P1.1 Board extraction, P1.2 tests, P1.3 dedup, P1.4 encapsulation, P1.5 hygiene, P1.6 README.
- **Selected P2** — P2.1 LevelConfig, P2.4 audio, plus README polish (screenshots/GIF feed back into P1.6).
- **Promoted from P3** — zoom/pan on large grids, and safe-area handling (both required by the mobile target).

**Deferred (documented backlog):** P2.2 theme SO, P2.3 best-time persistence, P2.5 perf/pooling, P2.6 CI, and remaining P3 (juice, chording, `?` marking, settings/pause, accessibility, save/resume).

**Execution order (when implementation begins):**
1. **P0.1 input** + the two promoted mobile items (zoom/pan, safe area) — makes it genuinely playable on a phone.
2. **P0.2–P0.4** — correctness (win-event, mine placement, lose/restart).
3. **P1.1 Board → P1.2 tests → P1.3 dedup → P1.4 encapsulation → P1.5 hygiene** — the architecture spine, in that order (each builds on the last).
4. **P2.1 LevelConfig + P2.4 audio.**
5. **P1.6 README last**, so screenshots/GIF reflect the finished game.

## Effort sizing (in-scope items, relative for a junior)

| Item | Size | Note |
|------|------|------|
| P0.1 Cross-platform input | **L** | Biggest behavioral change; size depends on Input System vs legacy (see risks). |
| Zoom/pan (promoted) | **M** | Camera control + clamping; interacts with auto-scaling. |
| Safe area (promoted) | **S** | A `SafeArea` component on the Canvas. |
| P0.2 Win-event | **S** | |
| P0.3 Mine placement | **S** | |
| P0.4 Lose/restart UI | **S–M** | Mostly UI wiring. |
| P1.1 Board extraction | **L** | Keystone; touches everything. |
| P1.2 Tests rewrite | **M** | |
| P1.3 Dedup | **S** | |
| P1.4 Encapsulation | **S–M** | |
| P1.5 Hygiene | **S** | |
| P1.6 README + media | **M** | Writing + capturing GIF/screenshots. |
| P2.1 LevelConfig + menu | **M–L** | |
| P2.4 Audio | **S–M** | |

Two **L** items (input, Board) dominate — sequence them carefully and don't start them in parallel.

## Design tensions & gotchas (decide before coding)

1. **Board extraction must keep the *animation* in `Grid`.** `RevealRoutine` (`Grid.cs:333`) cascades reveals with `WaitForSeconds`. `Board.Reveal()` should be a pure function returning the ordered coordinates to reveal (e.g. BFS levels); `Grid` keeps the coroutine that animates that data. Do **not** pull timing/`WaitForSeconds` into `Board`, or it stops being unit-testable — which defeats the whole point of P1.1.
2. **Zoom/pan conflicts with auto-fit scaling.** `CalculateScaleAndSpacing` (`Grid.cs:130`) currently fits the *entire* grid to the screen. Zoom/pan only makes sense if the grid can exceed the viewport. Decide the model up front: **auto-fit = the minimum zoom level**, with pinch/scroll zooming in and drag panning on top. Reconcile these two systems; don't bolt pan on independently or they'll fight.
3. **Input System adoption is a project-wide switch.** Enabling `com.unity.inputsystem` changes *Active Input Handling* in Player Settings and can break existing `Input.*` calls until migrated. If you want to avoid that churn for a test piece, unify mouse + `Input.GetTouch` under the legacy Input Manager instead. **Pick one before starting P0.1.**
4. **Hit-testing without per-cell `OnMouseOver`.** Two options: keep lightweight colliders + `Physics2D.OverlapPoint(worldPoint)`, or compute the cell index *mathematically* from the pointer's world position, grid origin, and `spacing` (no colliders — cleaner and scales to 1,925 cells). The math approach pairs naturally with `GetCellWorldPosition` from P1.3.
