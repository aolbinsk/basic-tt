Below is an **actionable plan** for reorganizing your current codebase into a **modular architecture** with clear separation between **domain** (pure table tennis logic), **infrastructure** (Unity, XR, input, rendering, etc.), and a **single bridging layer** (the minimal MonoBehaviour that ties them together). The goal is to ensure **maintainability, testability, and flexibility** so you can easily switch between different physics approaches (Unity vs. custom), input methods, or rendering pipelines.

---

# 1. Proposed Folder / Assembly Structure

A recommended layout might look like this (using C# project/assembly or Unity folder grouping):

```
/Domain
   /Physics
      BallPhysics.cs
      CollisionDetectionSystem.cs
      CollisionResolutionSystem.cs
   /Entities
      BallState.cs
      PaddleState.cs
      CollisionData.cs
   /Interfaces
      IPhysicsEngine.cs
      ICollisionSystem.cs
      IInputManager.cs
      IRenderer.cs
   /Logic
      TableTennisSimulation.cs  // High-level "game logic" orchestrator, if desired
      ...
   /Filters
      IFilter<T>.cs
      PassThroughFilter*.cs
      MovingAverageFilter*.cs
      KalmanFilter*.cs

/Infrastructure
   /UnityPhysics
      UnityPhysicsEngine.cs      // Implementation of IPhysicsEngine for Unity
      SweptCollisionPro.cs       // Or integrate as part of your "custom" collision approach
   /CustomPhysics
      CustomPhysicsEngine.cs     // If you want your own integrator, collisions, etc.
   /XRInput
      UnityXRInputManager.cs     // Implementation of IInputManager
      // Possibly other VR device managers
   /Rendering
      UnityRenderer.cs           // Implementation of IRenderer
      // Or a HeadlessRenderer, etc.
   /SceneSetup
      TableTennisSceneSetup.cs   // The bridging MonoBehaviour (or a small set)
      RoomBuilder.cs
      TableTennisEquipmentBuilder.cs
      PlayerSetupBuilder.cs
      PaddleBuilder.cs
      BallBuilder.cs
      // ...
   /Config
      TableTennisPhysicsConfig.cs
      // Possibly a scriptable object or data container for configuration

/Bridging (Optional)
   // If you prefer a single bridging script in its own folder, or keep it in /SceneSetup

/Tests
   /DomainTests
   /IntegrationTests
```

Below are **step-by-step** details for reorganizing the **existing** classes into this structure.

---

# 2. Step-by-Step Reorganization

## 2.1 Domain (Core) Layer

### Goal
Move **all purely table tennis logic** or **mathematical / algorithmic logic** that does *not* rely on `MonoBehaviour` or Unity classes here.

1. **Filters**
    - Already nicely decoupled from Unity.
    - **Keep** `IFilter<T>`, `PassThroughFilter`, `MovingAverageFilter`, `KalmanFilter` in `Domain/Filters/`.
    - They can stay in a single “Filters” namespace or break into separate files if desired.

2. **Physics Logic**
    - `BallPhysics`, `CollisionDetectionSystem`, and `CollisionResolutionSystem` implement core physics integration and collision logic.
    - **Remove** MonoBehaviour inheritance or direct Unity calls (if any).
    - Place them in `Domain/Physics/`.
    - Let them be **pure** C# classes (no `MonoBehaviour`, no `Update()` methods).

3. **Entity States**
    - `BallState`, `PaddleState`, `CollisionData` are purely data.
    - Place them in `Domain/Entities/`.

4. **Interfaces**
    - If you want to eventually unify collision detection or allow multiple approaches, create `IPhysicsEngine`, `ICollisionSystem`, etc.
    - For input, define `IInputManager` in `Domain/Interfaces/`.
    - For rendering, define `IRenderer`.
    - (Optional) For a single consolidated domain orchestrator (like a “TableTennisSimulation” that steps ball & paddle states), you can place that in `Domain/Logic/`.

### Rationale
- This makes your core table tennis logic testable **without** Unity.
- Reduces coupling to engine specifics.

---

## 2.2 Infrastructure Layer

### Goal
Implement the domain **interfaces** or provide Unity-specific logic here. This is where most `MonoBehaviour` classes currently live, but we will **trim** them down and move domain logic out.

1. **Unity Physics vs. Custom Physics**
    - If you want to use Unity’s `Rigidbody`, `Physics.Raycast`, etc., create a `UnityPhysicsEngine : IPhysicsEngine` class.
    - If you want your own integrator, create `CustomPhysicsEngine : IPhysicsEngine`.
    - **SweptCollisionPro** can remain in `Infrastructure/UnityPhysics` (if it’s part of your “custom approach”), or if it’s purely domain-agnostic, it can be in `Domain/Physics`. Decide based on whether it depends on Unity-specific data structures.

2. **Input**
    - **VRInputManager** currently is a `Singleton` `MonoBehaviour`. We want a decoupled approach:
        - Extract the input logic (filters, references to actions) into a `UnityXRInputManager : IInputManager`.
        - If you need the `MonoBehaviour` for hooking up `InputActionReference`s, that’s fine, but the *core logic* of reading device input and applying filters should be in a plain C# class or partial class.
    - The `MonoBehaviour` could exist as a “bridge component,” but it calls into `UnityXRInputManager` which implements the domain interface.

3. **Rendering**
    - If you want a more advanced approach, create `UnityRenderer : IRenderer` that knows how to create or update `GameObjects`.
    - Currently, the building classes (like `RoomBuilder`, `EquipmentBuilder`) do the rendering or object creation. That’s okay—just keep them in `Infrastructure` or `SceneSetup`.

4. **Scene Setup**
    - `TableTennisSceneSetup`, `RoomBuilder`, `TableTennisEquipmentBuilder`, `PlayerSetupBuilder`, `PaddleBuilder`, `BallBuilder`
    - These classes create GameObjects, set transforms, etc.
    - Move them to `Infrastructure/SceneSetup/` or a subfolder like `Builder/`.
    - They can remain `MonoBehaviour`s if you want them part of a scene-based workflow. Alternatively, convert them to plain classes if you want to do all scene creation manually.

5. **Configuration**
    - `TableTennisPhysicsConfig` can remain a ScriptableObject or `MonoBehaviour` that holds global config.
    - Move it to `Infrastructure/Config/`.
    - Provide domain code with numeric values (table size, ball mass, etc.) from this config.

6. **PerformanceMonitor**
    - Also a `MonoBehaviour` measuring frame times. This is purely a runtime utility—keep it in `Infrastructure/` or `Utilities/` as it depends on Unity’s lifecycle.

---

## 2.3 Bridging (MonoBehaviour) Layer

### Goal
Have a **single** or very few `MonoBehaviour` classes that **tie** the domain logic to Unity’s game loop.

1. **One “SimulationBridge”**
    - For example, rename `PhysicsManager` to `TableTennisSimulationBridge` or similar.
    - This script does the following:
        1. Creates/initializes your domain objects (like `BallPhysics`, `CollisionDetectionSystem`) if you aren’t using separate builder calls.
        2. Calls domain updates each frame or in `FixedUpdate()`.
        3. Retrieves input from `IInputManager` (which might be a `UnityXRInputManager`) and updates domain states.
        4. Updates or spawns Unity objects if the domain logic says so, or calls the relevant builder classes.

2. **Sub-Stepping**
    - The logic in `PhysicsManager` that accumulates `_accumulatedTime` and does multiple sub-steps is **good** for VR table tennis. Keep that approach, but ensure it calls **domain** methods (like `BallPhysics.Integrate`) rather than mixing domain code in the bridging script.

3. **Others**
    - If you like having separate bridging scripts for e.g., `BallController` vs. `PaddleController`, you can keep them but reduce domain logic in them.
    - Gradually, you can fold them into a single bridging approach if you prefer minimal MonoBehaviours.

---

## 2.4 Tests Folder

### Goal
Create a `/Tests` folder with domain-level unit tests and possibly integration tests.

1. **DomainTests**
    - Test `BallPhysics`, `CollisionDetectionSystem`, filters, etc.
    - You can do these in a standard .NET testing framework, since domain classes are not MonoBehaviours.

2. **IntegrationTests**
    - Possibly run Unity test runner or editor tests that instantiate your bridging script and see if the entire system behaves as expected.

---

# 3. Implementation Steps

1. **Create Domain Folder & Namespace**
    - Move `BallPhysics`, `CollisionDetectionSystem`, `CollisionResolutionSystem`, `BallState`, `PaddleState`, `CollisionData`, etc., into `Domain.*` namespaces.
    - Remove/replace any direct usage of `MonoBehaviour` or `Time.deltaTime`. Pass those in from the bridging script.

2. **Extract Interfaces**
    - If you want the domain code to be engine-agnostic, define `IPhysicsEngine`, `IInputManager`, etc. in `Domain/Interfaces/`.
    - That way, your domain logic can call `_physics.Step(dt)` or `_input.GetPaddlePose()`.

3. **Refactor VRInputManager**
    - The part that references `InputActionReference`, updates filters, and reads XR data can go into a plain `UnityXRInputManager : IInputManager` in `Infrastructure.XRInput`.
    - The part that’s a `MonoBehaviour` could be trimmed down to an inspector referencing these actions. On `Awake()`, it initializes the `UnityXRInputManager` with the needed references.

4. **Refactor PhysicsManager**
    - Rename it or unify it into the single bridging script (e.g., `TableTennisSimulationBridge`).
    - It obtains domain-level classes like `BallPhysics` from some config or builder.
    - It calls domain’s `.Integrate()`, `.DetectCollisions()`, `.ResolveCollisions()` in sub-steps.
    - It updates or retrieves positions from `IPhysicsEngine` or domain states, then transforms Unity objects.

5. **Refactor Builders**
    - `RoomBuilder`, `TableTennisEquipmentBuilder`, etc., stay in `Infrastructure.SceneSetup`.
    - If they need domain data (like table size from `TableTennisPhysicsConfig`), they can read from config. They create Unity objects, not domain objects.

6. **Migrate Colliders & Rigidbodies**
    - If you rely heavily on Unity’s colliders, that’s part of the “UnityPhysics” approach.
    - If you want purely custom collisions, your domain code uses your collision detection, and you might not need Unity’s colliders. Or you might keep them for rendering or for references only.

7. **Testing**
    - After reorganizing, write or adapt some domain tests (e.g., test your KalmanFilter logic, or test a ball bounce scenario).
    - Add an integration test that spawns your bridging script + a mocked input manager to verify everything runs end-to-end.

---

# 4. Aftermath & Benefits

By following this plan:

1. **Domain** code (filters, collision detection, ball/paddle states, physics integration) is now **pure C#** and testable without Unity.
2. **Infrastructure** code (builders, XR input, Unity-based physics, etc.) is isolated—**swappable** if you want different input or physics systems.
3. **One bridging** script (or minimal set) orchestrates the flow between domain and Unity (`Update` or `FixedUpdate`, sub-steps, etc.).
4. **Scene setup** (Room, Table, Net, Paddle, Ball) is done in specialized builder classes but remains out of the domain logic.
5. You can now quickly experiment with **multiple** physics approaches (Unity vs. custom), or a different VR input system, or even a headless approach for automated testing.

This structure ensures you can **maintain**, **test**, **change**, and **evaluate** different parts of the system far more easily than before.