# 2D Platformer Demo (Unity)

A compact, production-minded 2D platformer built with Unity 6 to showcase **clean code**, **incremental evolution**, and **readable gameplay scripts**. 
The first milestone ships fast with a single, self-contained `PlayerController` handling movement and jump using `Rigidbody2D` and the New Input System. 
Future milestones focus on adding features and improving architecture step by step.

## Purpose

This repository is a portfolio piece emphasizing readability, low coupling, and a clear upgrade path—from a working MVP to a scalable, maintainable codebase.

## Highlights

* Unity 6 • C# • New Input System
* Deterministic physics loop with `FixedUpdate`
* Simple, reusable ground check
* Small, focused components (separation of concerns)

## Current Architecture — Milestone 4
The initial monolithic `PlayerController` was split and extended with shooting and animation states:
- **PlayerInputAdapter** – reads `PlayerInput` and raises high-level events: `OnMoveX(float)`, `OnJump()`, `OnStartAttack()`, `OnStopAttack()`
- **PlayerController** – thin orchestrator wiring input to movement, jump, attack, facing updates, and attack animation flags
- **PlayerMovement** – horizontal movement (+ sprite flip)
- **PlayerJump** – jump request/consumption with grounded check
- **PlayerAttack** – fires repeatedly while the attack button is held and supports crouched shots
- **Projectile** – travels in the selected horizontal or diagonal aim direction using `Rigidbody2D`, and despawns on hit or when offscreen
- **PlayerAnimation** – resolves movement/attack poses and swaps to a foot-anchored crouch sprite without scaling or bobbing the character
- **GroundSensor2D** (reusable) – overlap-based ground detection
- **CameraController** – follows the player in the scene

## Crystal Cavern — Gauntlet

The gameplay scene generates a complete cave run at runtime using the included animated crab, jumper, and octopus artwork. An editor preview also places the full layout into `20_Gameplay.unity` so it can be inspected before Play Mode; the same preview is replaced by the active runtime layout when the game starts. To rebuild the preview after changing the layout, use **Tools > Cave Run > Generate or Refresh Scene Level** in Unity.
- Five connected cavern floor sections, widened jumping ledges, and moving platforms across a longer final approach
- Spike beds and a periodically pulsing laser
- Thirteen patrolling enemies across the lower route and elevated ledges, including a three-phase guardian boss with telegraphed single shots and spreading volleys
- Score crystals, an extra-life crystal, and a checkpoint; every 500 points upgrades shot damage and firing rate, up to three upgrades
- A locked exit, three lives, score and elapsed-time HUD, pause, and replay
- Hits consume one life, keep the player in place, and deduct 250 points (half an upgrade milestone), with a brief blinking invulnerability window; falling out of the level still returns to the latest checkpoint
- A completion score bonus that rewards faster runs

Controls: **A/D** or **arrow keys** to move, **Up Arrow** to aim diagonally up, **S/Down Arrow** to crouch on the ground or aim diagonally down in the air, **Space** to jump, hold **left mouse button** to fire, **Esc** to pause/resume, and **R** to retry after a run ends. Shots pass through one another and score crystals; upward shots also pass through the elevated route platforms so sentries above can be hit from below. The alternate route now alternates between more widely separated high and low ledges, with enemies and crystals.

## Folder Structure (lean)

```
Assets/
  _Project/
    Animations/
      Player/
      Projectile/
    Data/
      Audio Files/
      Input/
      Scriptable Objects/
    Materials/
      Physics Materials/
    Prefabs/
    Scenes/
    Scripts/
      Audio/
      Camera/
      Input/
      Managers/
      Player/
      Projectile/
      States/
        Player/
      Utilities/
        Sensors/
        Singletons/
```

## Getting Started

1. Open Unity 6 and clone the repo.
2. Open `Assets/_Project/Scenes/20_Gameplay.unity`.
3. Press Play.

### Controls (Keyboard & Mouse)

* **A/D** or **←/→**: Move
* **↑**: Aim diagonally up; **↓**: crouch on the ground or aim diagonally down in the air
* **Space**: Jump
* **Hold Left Mouse Button**: Attack

## Milestones & Roadmap

* ✅ **Milestone 1:** Single-script `PlayerController` (move + jump).
* ✅ **Milestone 2:** Split into input adapter, controller (orchestrator), movement, jump, reusable ground sensor and camera script.
* ✅ **Milestone 3 (completed):** New feature: **Shooting** (projectiles), new sprites, sound effects and basic animations.
* ✅ **Milestone 4:** Enemies, hazards, moving platforms, checkpoint, score/lives/timer UI, pause/restart, and a complete cave gauntlet.
* ⏭️ **Milestone 5:** Polish pass (animation states, coyote time, jump buffering, camera tweaks).

## Tech Notes

* Uses Unity’s **New Input System** (`GameInput.inputactions`) with an action map `Player`:

  * `Move` (Vector2)
  * `Jump` (Button)
  * `Attack` (Button)
* Character Physics tuned via `Rigidbody2D` linearVelocity writes in `FixedUpdate`.

## Coding Rules

* Always use curly braces, even in simple "if" statements.
* Use the C# standard for opening curly braces: on a new line.
* Avoid "nesting ifs": prefer using early return pattern, also known as a guard clause, and safety checks.
* Avoid excessive code coupling.
* Use #region/#endregion to organize your code.

## Commit Philosophy

Start **simple**, ship a working baseline, then iterate with focused commits that either:

* Add a feature, or
* Improve architecture/clarity without changing behavior.

Commits can be:

* Small: for code fixes and improvements
* Large: for new features added after testing

## License

MIT (feel free to fork and extend).
