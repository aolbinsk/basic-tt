# VR Table Tennis – Updated Requirements Document

## Purpose
This document is structured in a way that should be maintainable and clear, capturing the intent of the VR Table Tennis project.

## 1. Core Game Requirements

### 1.1 Game Setup
1. **Code-Driven Scene Initialization**
  - The entire table tennis environment (table, net, room, ball, paddle) **must be instantiated and configured via code**.
  - No reliance on scene-placed prefabs for core objects.
  - A single “bootstrap” or “installer” script (like `TableTennisInstaller`) can coordinate the creation of all objects (XR Origin, table, paddle, ball, net, etc.).

2. **Regulation Table Dimensions**
  - Table must be *exactly* regulation size:
    - Length: 2.74 m
    - Width: 1.525 m
    - Height: 0.76 m
    - Net height: 0.1525 m (15.25 cm)
  - Centered at (0,0,0) in world space.
  - Created at runtime, scaling any imported 3D model automatically.

3. **Dynamic Player Position**
  - Player is positioned programmatically at least **1.0 meter** (or custom value from config) from the table’s center on the near side.
  - XR Origin created or configured in code (no required references to scene objects).

4. **Paddle Setup**
  - The right-hand controller attaches to the **paddle** with a 1:1 positional mapping.
  - Paddle sizes must match real dimensions:
    - Head width: ~15.25 cm
    - Head length: ~17 cm
    - Blade thickness: ~1 cm
    - Rubber thickness: ~2 mm
    - Handle length: ~10 cm

  - Final geometry is generated or scaled in code.
  - **Paddle Construction Details**
    - The paddle must visually and structurally represent a real table tennis paddle, consisting of a gray frame (wooden blade) with rubbers glued on top of both forehand and backhand sides.
    - The local centers of the paddle components (blade and rubbers) must be accurately calculated to ensure correct physics interactions and visual alignment.

5. **Ball Setup**
  - The **ball** is code-generated with a diameter of ~40 mm (2.7 g typical).
  - Marking ring (e.g., a spin ring) is attached for visible spin detection.
  - Ball is placed initially at a serve-ready position (e.g., above the table) or in the left-hand upon grip.

6. **AI Opponent (Optional / Future)**
  - AI is positioned on the opposite side.
  - Basic reflection logic for returning the ball.
  - Not mandatory for initial MVP but remains a future requirement.

### 1.2 Player Interaction
1. **Right Hand – Paddle Control**
  - Direct mapping from XR controller to paddle transform.
  - Physical collisions with ball.
  - Must allow for natural swing mechanics, capturing velocity and spin.

2. **Left Hand – Ball Handling**
  - Grip to “hold” the ball, which follows the left-hand position and orientation.
  - On release, the ball inherits the velocity (with a minimum upward component for serves).
  - **Filtering** or smoothing of extreme input to prevent unrealistic throws.

3. **Serving Mechanics**
  - Minimum upward velocity enforced on release.
  - Ball transitions seamlessly into full physics on release.

### 1.3 Physics
1. **Multi-Physics Support**
  - The system should support both **Unity’s built-in physics** and **custom** (Velocity Verlet + advanced collision detection).
  - The choice is **configurable** at runtime (e.g., `useCustomPhysics = true/false`).

2. **Realistic Ball Movement**
  - Gravity, bounce, spin, air resistance, Magnus effect.
  - Configurable aerodynamic parameters (drag coefficient, magnus coefficient, air density).
  - Spin transfer from paddle collisions.

3. **Collision Detection**
  - Continuous or “swept” detection to handle high-speed collisions.
  - Must handle ball vs. paddle and ball vs. environment (table, floor, walls, net).
  - Sub-step approach for collisions (if using custom physics) to avoid tunneling.

4. **Centralized Physics Config**
  - All physical properties for ball, paddle, table, environment in **a single code-based config** object.
  - Material properties (bounciness, friction, spin multipliers) set in code, not in Unity’s inspector.
  - Supports changing spin multipliers, bounce restitution, etc.

5. **Equipment Scaling**
  - Paddle and table are scaled at runtime to **exact** real-world dimensions.
  - Must handle arbitrary 3D model sizes.

### 1.4 Input System
1. **Filtering**
  - Support for multiple filters (Pass-through, MovingAverage, Kalman) for both `Vector3` (position) and `Quaternion` (rotation).
  - Filter type chosen by code config or constructor parameter (e.g., `"Kalman"`, `"MovingAverage"`, `"None"`).

2. **Grip Press Detection**
  - Left grip: hold/release ball.
  - Right grip: can be used for additional interactions or advanced features.

3. **Velocity & Angular Velocity Calculation**
  - The input manager calculates velocity and angular velocity for the right controller, used for paddle collisions, spin generation, or synergy with custom physics.

---

## 2. Technical Requirements

### 2.1 Modular Architecture & Bridging
1. **Single Bridging MonoBehaviour**
  - All Unity game loop callbacks (e.g., `FixedUpdate()`) funnel through a single bridging script (like `SimulationBridge`), which then updates the domain’s simulation code.
  - The rest of the simulation is **pure C#** classes.

2. **Code-Only Scene Setup**
  - `RoomBuilder`, `TableTennisEquipmentBuilder`, etc. handle all geometry creation.
  - The environment is built purely at runtime (no scene references or manual prefab placement).

3. **Maintainable, Testable Domain**
  - Core physics logic and game rules reside in domain classes (e.g., `BallPhysics`, `CollisionDetectionSystem`, etc.) with **no** direct MonoBehaviour usage.
  - Allows for automated unit testing and switching between physics implementations with minimal code changes.

4. **Performance Goals**
  - Must support stable 90–120 FPS for VR comfort.
  - Efficient collision detection.
  - Minimal GC allocations to avoid frame stutters.

### 2.2 VR Integration
1. **InputAction-based**
  - Use Unity’s Input System with XR devices.
  - `devicePosition`, `deviceRotation`, `gripPressed` mapped to left/right controllers.
  - Code-based or script-driven creation of input actions is allowed (no mandatory reliance on scene references).

2. **OpenXR Compatibility**
  - Should run on standard OpenXR devices.
  - XR Origin can be created at runtime, or if the user chooses, from a minimal XR rig in the scene.

### 2.3 Physics System
1. **Custom Physics**
  - Optional custom integrator (Velocity Verlet) for the ball.
  - Swept collision detection to handle high velocities.
  - Sub-stepping for stable collisions.
2. **Unity Physics**
  - Alternative path that uses standard `Rigidbody` and `Collider` with continuous collision detection.
  - Still uses code-based dimension setups and references a bridging approach to read states.

3. **Collision & Spin**
  - Spin transfer logic in collision resolution (i.e., reflection angles, friction, spin multipliers).
  - Paddle rubber bounciness, throw multipliers.

---

## 3. Gameplay Requirements

### 3.1 Basic Rules
1. **Standard Table Tennis**
  - Ball must bounce on the table.
  - Service must be from above the table surface.
  - Out-of-bounds detection if ball leaves the table area.

2. **Scoring & Flow (Future)**
  - Keep simple scoreboard.
  - Reset the ball each point.
  - Optionally track net hits, serving rules, etc.

### 3.2 Player Experience
1. **Responsive Paddle Movement**
  - The user should feel minimal latency or mismatch between real hand motion and in-game paddle.
  - Filters must not introduce excessive lag.

2. **Smooth Ball Release**
  - On left grip release, velocity from user motion is seamlessly transferred to the ball.
  - Minimum upward velocity for serving.

3. **Realistic Physics**
  - Bounces, spins, air resistance must feel like real table tennis.
  - Tweakable parameters for pro-level realism (Magnus coefficient, friction, etc.).

### 3.3 AI Opponent (Future)
1. **Basic Opponent**
  - Tracks the ball, tries to hit it back.
  - No advanced difficulty yet.

2. **Predictable**
  - The AI should not produce “magical” or impossible returns.
  - Possibly revolve around simple reflection logic.

---

## 4. Quality Requirements

### 4.1 Reliability
- **No Physics Glitches**: The system can’t allow ball tunneling, extreme bounce errors, or indefinite collisions.
- **Stable VR Tracking**: The bridging script properly updates the domain states each frame.

### 4.2 Usability
- **Intuitive Controls**: Grip is consistent for holding/throwing the ball.
- **Automatic Setup**: Player need not manually position themselves; code sets position near the table.
- **Clear Visual Cues**: Spin ring on the ball to show spin, etc.

### 4.3 Maintainability
- **Code-Driven**: All environment creation is in code, with domain logic in pure C# for easy testing.
- **Modular**: Sub-systems (input, physics, rendering) are separate interfaces (e.g., `IInputManager`, `IPhysicsEngine`, `IRenderer`).
- **Multiple Physics**: Must be trivial to switch from `UnityPhysicsEngine` to `CustomPhysicsEngine`.

### 4.4 Testing
- **Unit Tests**: For domain classes (filters, collision detection, spin logic).
- **Integration Tests**: Checking bridging script in a minimal test scene.
- **Performance Benchmarks**: Confirm we can maintain stable 90/120 FPS in VR.

---

## 5. Future Considerations

### 5.1 Extensions
1. **Multiple Difficulty Levels** for AI.
2. **Sound Effects** triggered by collisions.
3. **Multiplayer** (network play).
4. **Customizable Paddles** with different materials.

### 5.2 Optional Features
1. **Training Mode**: A “ball machine” firing repeated shots.
2. **Replay/Recording**: Store shot data to replay.
3. **Statistics Tracking**: Ball speed, spin rate, rally length.
4. **Advanced Graphical Effects**: Sweat, dust, etc.

---

## 6. Development Guidelines

### 6.1 Code Standards
- **C#** with XML doc comments for domain classes.
- **SOLID** principles.
- **Well-named** domain classes (e.g. `CollisionDetectionSystem`, `BallPhysics`).
- A single bridging `MonoBehaviour` (`SimulationBridge` or `Installer`) to tie domain to Unity.

### 6.2 Version Control
- **Git** or equivalent.
- Feature branches for major changes.
- Clear commit messages.

### 6.3 Testing Process
- **Playtesting** each feature.
- **Automated Unit Tests** in domain layer.
- **Integration** checks for bridging + input.
- **Performance** tests for frame rate consistency.

### 6.4 Documentation
- **Code Docs**: Summaries on all major classes and interfaces.
- **Setup Steps**: Clear instructions to run code-only scene creation.
- **Change Logs**: Track modifications to config or physics logic.

---

## 7. Summary

This updated Requirements Document ensures a **maintainable**, **testable**, and **realistic** VR Table Tennis simulator, emphasizing:

1. **Code-based** creation of the entire environment.
2. **Bridging architecture** with domain logic separated from Unity’s engine calls.
3. **Multiple physics** implementations (Unity vs. custom).
4. **Input filters** for smooth, realistic controller data.
5. **Precise** real-world scaling of table and paddle.
6. **Config-driven** system for all physics and dimension parameters.

By following these guidelines, developers can **rapidly iterate**, **switch** between physics modes, and maintain a **pro-level** VR table tennis experience.
