# Controller System Architecture

## 1. Tech stack

- **Unity 6** (6000.4.8f1), **URP** 17.4.0, C#.
- **`UnityEngine.InputSystem`** — action-based input (`InputActionAsset` / `InputAction`).
- **`UnityEngine.LowLevelPhysics2D`** — Unity 6's low-level 2D physics API
  (`PhysicsWorld`, `PhysicsBody`, `PhysicsShape`, `PhysicsDestructor`, `PhysicsComposer`).
  This is a deliberate choice over classic `Rigidbody2D`/`Collider2D`.
- `Unity.Mathematics` / `Unity.Collections` — used by the destructible-terrain fragmentation code.

The module is defined by a single assembly definition,
`Runtime/Controllers/Game.Gameplay.Controllers.asmdef`, referencing only
`Unity.Mathematics`, `Unity.InputSystem`, `Unity.Collections`. Although the wider project
template ships VContainer (DI), VitalRouter (pub/sub), R3 (Rx) and ObservableCollections, the
Controllers assembly deliberately uses **none** of them — it is a self-contained, hand-wired
vertical slice (physics + input + FSM), isolated from the rest of the template's
MVVM/DI/reactive conventions.

## 2. High-level shape

Three independent, player-controllable **agents** — **Capsule**, **Pilot**, **Drone** — each
built from the same recipe:

1. A `*Config` ScriptableObject holding designer-tunable numbers.
2. A `PhysicsBody` created against the shared low-level `PhysicsWorld`.
3. A per-agent `ControllerContext` (the data bag passed into state callbacks).
4. A set of `IControllerState<TId>` implementations, driven by a generic
   `ControllerStateMachine<TId>`.

A `ControllerDirector` sits above all three and acts as a lightweight possession/orchestration
layer — the closest thing to a "root controller" in the scene — toggling which agent currently
has input/simulation active. There is no factory, DI container, or code-driven bootstrap:
composition happens in each controller's own `Awake()`, plus one scene GameObject
("Systems") that wires the director to the three agents.

```
                      ┌───────────────────────┐
                      │   PhysicsWorldBootstrap │  (static World / Probe accessors)
                      └───────────┬────────────┘
                                  │
                      ┌───────────▼────────────┐
                      │    ControllerDirector    │  possession + drone launch
                      └──┬──────────┬─────────┬──┘
                         │          │          │
              ┌──────────▼─┐  ┌─────▼──────┐ ┌─▼───────────┐
              │CapsuleController│ │PilotController│ │DroneController│
              └──────────┬─┘  └─────┬──────┘ └─┬───────────┘
                         │          │          │
                 each owns: PhysicsBody, EnergyModel, ControllerContext,
                 PlayerInputReader, ControllerStateMachine<TId>, and its
                 own set of IControllerState<TId> implementations
```

## 3. Core abstractions (`Core/`)

These types form the shared "FSM engine" and supporting infrastructure used by every agent.
None of them know about Capsule/Pilot/Drone specifically.

| Type | Role |
|---|---|
| `IControllerState<TId>` | Generic FSM state contract: `Id`, `Enter/Exit/Tick/FixedTick(ControllerContext, …)`, `CanTransitionTo(TId, ControllerContext)`. Parameterized per agent by that agent's own state-id enum. |
| `ControllerStateMachine<TId>` | Sealed generic FSM engine. Holds a `Dictionary<TId, IControllerState<TId>>`. `RequestTransition` validates via the current state's `CanTransitionTo` and **defers** the switch to the start of the next `Tick`/`FixedTick`, so a state's `Exit` never fires mid-callback. |
| `ControllerContext` | Sealed, non-reactive data bag passed into every state call: `PhysicsBody Body`, `Transform Transform`, `PhysicsWorld World`, `IEnvironmentProbe Probe`, `IControllerInput Input`, `EnergyModel Energy`, `InputSample CurrentInput`. Kept deliberately minimal — mode-specific collaborators (aim line, terrain, shooting module, …) are injected directly into the states/processors that need them instead of being piled onto the context. |
| `IControllerInput` / `PlayerInputReader` | `Sample() -> InputSample` abstraction over input polling; `PlayerInputReader` wraps the "Player" action map (Move, Look→Aim, Jump, Attack, Interact→Drill). Each controller owns its **own** reader instance. |
| `IEnvironmentProbe` / `EnvironmentProbe` | World-sensing contract: `IsGrounded`, `IsSubmerged`, `TryGetRockAhead`. The one implementation does `PhysicsWorld.OverlapPoint` queries against `PhysicsCategories` masks; submersion delegates to `WaterVolume`. |
| `InputSample` | Readonly struct: one frame of sampled input (`Move`, `Aim`, Jump/Attack/Drill held/pressed/released). |
| `EnergyModel` | Plain (non-reactive) class modeling shared battery/energy resources: `TrySpendBattery`, `TryDrainEnergy`, `RechargeBatteries/Energy`, `ResetFull`, plus a plain `event Action Changed` for UI polling. |
| `LowLevelBody` | Static helper that builds a circle `PhysicsBody` + shape and binds it to a `Transform`. Used by every agent's `Awake()`. |
| `PhysicsWorldBootstrap` | `[DefaultExecutionOrder(-1000)]` MonoBehaviour that configures the shared `PhysicsWorld` once. Exposes **static** `World` / `Probe` accessors so controllers never depend on this component's `Awake` order. |
| `PhysicsCategories` | Static `PhysicsMask` constants: `Player`, `Ground`, `Terrain`, `Debris`, `Projectile`. |
| `TimeFreezeController` | Owns global `Time.timeScale` for the capsule's hold-to-aim jump; outlives the aiming state's `Exit()` so the slow-motion recovery can keep running. |
| `WaterVolume` | MonoBehaviour marking a rectangular water area; static registry + `TryFindSurface`, consumed by `EnvironmentProbe`. |
| `LowLevelStaticBaker` | Level-authoring convenience: converts a placeholder `BoxCollider2D` into a static low-level physics body. Unrelated to the FSM controllers themselves. |
| `ControllerTelemetry` | Readonly snapshot struct (`StateName`, `Batteries`, `Energy`) intended for UI/debug polling — defined but not yet wired up anywhere. |

## 4. Agents

### 4.1 Capsule — `Capsule/`

The main ball/vehicle-like avatar; the most complex agent (4 states + two always-on overlay
behaviors).

- **`CapsuleController`** — composition root. `Awake()` builds the physics body, `EnergyModel`,
  `TimeFreezeController`, `JumpHoldGesture`, `PlasmaBulletSpawner` + `ShootingModule`,
  `PlayerInputReader`, `ControllerContext`, the 4 states, and the
  `ControllerStateMachine<CapsuleStateId>` (initial state chosen from `IsSubmerged`).
  `Update()` ticks `TimeFreezeController`, samples input, runs top-level transition logic
  (`EvaluateTransitions`), then ticks the machine. `FixedUpdate()` runs `FixedTick`.
  Public API: `Energy`, `CurrentState`, `SetInputEnabled(bool)`.
- **States** (`CapsuleStateId`: `Ground`, `Underwater`, `JumpAiming`, `Drilling`):
  - `CapsuleGroundState` — rolling via torque; delegates to `ShootingModule`.
  - `CapsuleUnderwaterState` — swim forces, drains `EnergyModel`; delegates to `ShootingModule`.
  - `CapsuleJumpAimingState` — bullet-time aim mode: freezes time, shows `AimLineView`, spends
    a battery and launches an impulse on release.
  - `CapsuleDrillState` — kinematic drilling through `DestructibleTerrain` (probes ahead,
    carves rock, moves along the latched drill direction).
- **Overlay behaviors (outside the FSM, by design)**:
  - `JumpHoldGesture` — tap vs. hold disambiguation for the Jump button; lives above the FSM
    because *which* state to enter is the controller's decision, not a state's.
  - `ShootingModule` — fire-rate-gated plasma shooting; usable from both Ground and Underwater,
    so it isn't modeled as a state.
- **`PlasmaBullet` / `PlasmaBulletSpawner`** — projectile spawning and self-destruction on
  contact/timeout.
- **`CapsuleConfig`** (ScriptableObject) — one asset aggregating nested settings per state
  (`GroundSettings`, `UnderwaterSettings`, `JumpAimSettings`, `DrillSettings`,
  `ShootSettings`) plus top-level `bodyRadius`, `maxBatteries`, `maxEnergy`,
  `jumpHoldThreshold`.

### 4.2 Pilot — `Pilot/`

On-foot humanoid used when exiting the capsule. Structurally mirrors Capsule but simpler:
only 2 states, no shooting/drilling/jump-aim.

- **`PilotController`** — `SetActive(bool)` toggles input/simulation (disables the
  `PhysicsBody`) while the GameObject itself stays active, so `Awake()`/body-creation runs
  exactly once at scene load regardless of possession state. `Update()` auto-switches
  Ground↔Underwater every frame from `IsSubmerged` — no gesture layer needed.
- **States** (`PilotStateId`: `Ground`, `Underwater`): `PilotGroundState` (direct
  velocity-based walk + jump impulse), `PilotSwimState` (mirrors Capsule's underwater state,
  energy drain unused — `EnergyModel` constructed as `(0, 1f)`).
- **`PilotConfig`** — `GroundSettings` (moveSpeed, jumpImpulse), `SwimSettings`
  (gravityScale, linearDamping, moveForce), `bodyRadius`.

### 4.3 Drone — `Drone/`

Flying, deployable/launchable scout.

- **`DroneController`** — body created with `gravityScale 0`. Starts in `Depleted` and
  inactive. `Launch()` resets energy, activates, and transitions to `Flying`.
  `Update()` itself drives the `Flying → EmergencyLanding` (energy depleted) and
  `EmergencyLanding → Depleted` (grounded) transitions — this exit-condition logic lives in
  the controller, not in `CanTransitionTo`. Public API: `CurrentState`, `Position`, `Launch()`,
  `SetActive(bool)`.
- **States** (`DroneStateId`: `Flying`, `EmergencyLanding`, `Depleted`):
  `DroneFlyingState` (thrust + energy drain + max-speed clamp), `DroneEmergencyLandingState`
  (sets landing gravity/damping; actual grounded detection is external), `DroneDepletedState`
  (idle/docked, doubles as pre-launch state).
- **`DroneConfig`** — `FlightSettings` (thrustForce, maxSpeed, linearDamping,
  energyDrainPerSecond), `LandingSettings` (gravityScale, groundCheckRadius), `bodyRadius`,
  `maxEnergy`.

## 5. Director — `Director/`

- **`ControllerDirector`** — the scene's possession/orchestration root. Holds serialized
  references to all three controllers plus the shared `InputActionAsset`. Polls
  "Previous"/"Next" input actions: `TogglePossession()` swaps active control between Capsule
  and Pilot (repositioning one above the other, toggling `SetInputEnabled`/`SetActive`);
  `LaunchOrRelaunchDrone()` launches the drone (only while `Depleted`) above whichever agent is
  currently possessed. It is the only class aware of all three agent types, but it does not
  create or own them — it only flips flags on controllers that already self-constructed in
  their own `Awake()`. Its own comment frames it as "minimal possession/launch orchestration
  for the sandbox… intended as a starting point to refine."
- **`AimLineView`** — `[RequireComponent(typeof(LineRenderer))]`, pure presentation
  (`Show(origin, direction, power01)` / `Hide()`). Shared by `CapsuleJumpAimingState` and
  `ShootingModule`.

## 6. Terrain — `Terrain/` (supporting system)

Consumed only by `CapsuleDrillState`; otherwise independent of the agent controllers.

- **`DestructibleTerrain`** — owns a destructible rock `PhysicsBody`. `HasRockAt(point)` /
  `Carve(point, radius)` use `PhysicsWorld.OverlapPoint` + `PhysicsDestructor.Fragment` to
  split rock into a remainder body and debris pieces. Explicitly adapted from
  `RuntimeRnD/SpriteDestructionExample/DestructionController.cs`, with two differences: debris
  excludes the Player contact category, and debris despawns on a timer via `DebrisReaper`
  rather than on ground contact.
- **`TerrainConfig`** — `fragmentRadius`, `fragmentCount`, `fragmentFriction`,
  `fragmentBounciness`, `debrisLifetime`.
- **`DebrisReaper`** — schedules timed destruction of debris bodies.
- **`FragmentMeshRenderer`** — builds/tears down runtime fragment meshes and syncs their
  transforms to physics bodies each `LateUpdate`.

## 7. Design patterns

- **Generic, enum-keyed FSM** (`ControllerStateMachine<TId>` / `IControllerState<TId>`),
  instantiated three times with three different state-id enums. Transitions are deferred to
  the next tick to avoid re-entrancy during `Exit`/`Enter`.
- **Strategy pattern** for states, input (`IControllerInput`), and world-sensing
  (`IEnvironmentProbe`) — each has exactly one concrete implementation today but is designed
  for substitution (e.g. AI-driven input, mock probes).
- **Composition over inheritance**: there is **no** shared base class or `IController`
  interface across `CapsuleController` / `PilotController` / `DroneController`. Each is an
  independent sealed MonoBehaviour that hand-assembles the same building blocks (body,
  context, machine, energy). The repeated `Awake → build / Update → sample+tick /
  FixedUpdate → fixed-tick / OnDestroy → destroy body` shape is a convention, not an enforced
  contract.
- **Overlay/satellite behaviors kept outside the FSM** where a behavior doesn't fit a single
  exclusive state — `ShootingModule`, `JumpHoldGesture`, `TimeFreezeController` each exist
  specifically because their behavior spans states or must outlive one.
- **Config-as-data**: every agent has a `*Config : ScriptableObject` with nested
  `[Serializable]` settings, injected into states' constructors — designer-tunable data fully
  decoupled from code.
- **Manual constructor injection**: states/modules/spawners receive dependencies via
  constructor parameters assembled by the owning controller's `Awake()`. No DI container is
  used (VContainer is available project-wide but unused here).
- **Static accessors for true singletons**: `PhysicsWorldBootstrap.World` / `.Probe` are static
  properties rather than a MonoBehaviour singleton reference, specifically to avoid Awake-order
  coupling.
- **Lightweight mediator**: `ControllerDirector` mediates between three otherwise-independent
  controllers by toggling their active/input state, rather than being a formal state machine
  itself.

## 8. Composition & wiring

There is no factory or DI-driven bootstrap. Wiring happens in two layers:

1. **Per-agent `Awake()`** — each of `CapsuleController` / `PilotController` /
   `DroneController` builds its own full dependency graph (body, `EnergyModel`,
   `PlayerInputReader`, `ControllerContext`, states, state machine) from its serialized
   `*Config` asset, its serialized `InputActionAsset`, and the static
   `PhysicsWorldBootstrap.World`/`.Probe`. The three controllers are mutually unaware of each
   other.
2. **Scene-level "Systems" GameObject** (`Scenes/RnD/ControllersTest.unity`) — carries
   `PhysicsWorldBootstrap` (`[DefaultExecutionOrder(-1000)]`, configures the shared world
   before anything else runs) and `ControllerDirector`, whose serialized fields link it to the
   scene's Capsule, Pilot, and Drone GameObjects plus the shared `InputActionAsset`. The scene
   also contains a `Water` volume, `AimLine`, `Terrain`, several static ground pieces, and a
   camera — all scene-authored GameObjects with direct serialized cross-references, consistent
   with this being a sandbox prototype rather than a production spawning pipeline.

## 9. Notable gaps / likely refactor targets

- No common base class/interface unifies `CapsuleController`, `PilotController`,
  `DroneController` despite their nearly identical lifecycle shape — a natural extraction
  point (e.g. a shared `AgentControllerBase`) if the pattern needs to be formalized.
- `ControllerTelemetry` is defined but not yet produced or consumed anywhere.
- `ControllerDirector` is explicitly a placeholder ("a starting point to refine") rather than a
  finished game-mode/orchestration layer.
- The module intentionally does not use the project's DI/reactive/pub-sub stack
  (VContainer, R3, VitalRouter); if the Controllers system needs to integrate with the rest of
  the game template later, that boundary is where the integration work would happen.
