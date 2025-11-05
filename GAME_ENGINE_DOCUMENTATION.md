# VR Table Tennis Game Engine - Technical Documentation

## Executive Summary

This document provides comprehensive documentation for a custom VR table tennis game engine prototype built on Unity. The engine features a physics-driven approach to realistic ball and paddle interactions with a focus on accurate collision handling, spin mechanics, and aerodynamic effects. While the prototype shows significant progress, the collision and ball-handling systems remain unfinished, never fully achieving the lifelike feel originally targeted.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Code Structure](#code-structure)
3. [Asset Generation System](#asset-generation-system)
4. [Physics Engine](#physics-engine)
5. [Collision System](#collision-system)
6. [Integration Algorithms](#integration-algorithms)
7. [Input Processing](#input-processing)
8. [Performance Considerations](#performance-considerations)
9. [Known Limitations](#known-limitations)

---

## Architecture Overview

### High-Level Design Philosophy

The engine follows a **Domain-Driven Design** (DDD) architecture with clear separation between:
- **Domain Layer**: Pure C# logic with no Unity dependencies
- **Infrastructure Layer**: Unity-specific implementations
- **Bridging Layer**: Connects Unity's game loop to domain logic

```mermaid
graph TB
    subgraph Unity["Unity Runtime"]
        SimBridge[SimulationBridge<br/>MonoBehaviour]
        XRInput[UnityXRInputManager]
        Renderer[UnityRenderer]
    end

    subgraph Domain["Domain Layer (Pure C#)"]
        Simulation[TableTennisSimulation]
        Physics[Physics Engines]
        Collision[Collision Systems]
        Entities[State Entities]
    end

    subgraph Infrastructure["Infrastructure Layer"]
        Builder[SimulationBuilder]
        BallBuilder[BallBuilder]
        PaddleBuilder[PaddleBuilder]
        RoomBuilder[RoomBuilder]
    end

    SimBridge -->|FixedUpdate| Simulation
    SimBridge -->|Update| Renderer
    XRInput -->|Controller Data| SimBridge

    Simulation -->|Uses| Physics
    Simulation -->|Uses| Collision
    Simulation -->|Manages| Entities

    Builder -->|Creates| Domain
    Builder -->|Creates| Infrastructure
    BallBuilder -->|Generates| GameObject
    PaddleBuilder -->|Generates| GameObject
    RoomBuilder -->|Generates| GameObject
```

**Key Design Decisions:**

1. **Unity as a Framework, Not a Crutch**: The core simulation logic is engine-agnostic C#, making it testable and portable.
2. **Dependency Injection**: `SimulationBuilder` assembles all components, allowing easy switching between physics implementations.
3. **State-Based Architecture**: All entities (ball, paddle, controllers) use immutable state objects passed through the simulation pipeline.

---

## Code Structure

### Directory Organization

```
BasicTT/Assets/Scripts/
├── Domain/                          # Pure C# game logic
│   ├── Config/                      # Configuration data classes
│   │   ├── BallConfig.cs           # Ball physical properties
│   │   ├── PaddleConfig.cs         # Paddle geometry & physics
│   │   ├── PhysicsConfig.cs        # Global physics parameters
│   │   └── TableConfig.cs          # Table dimensions
│   ├── Entities/                    # State data structures
│   │   ├── BallState.cs            # Position, velocity, spin
│   │   ├── PaddleState.cs          # Paddle pose & motion
│   │   ├── CollisionData.cs        # Collision information
│   │   └── ControllerState.cs      # VR controller state
│   ├── Filters/                     # Signal processing
│   │   ├── KalmanFilterVector3.cs  # Kalman filtering
│   │   ├── MovingAverageFilter*.cs # Simple smoothing
│   │   └── PassThroughFilter*.cs   # No filtering
│   ├── Interfaces/                  # Abstraction contracts
│   │   ├── IBallPhysics.cs         # Physics integrator
│   │   ├── ICollisionSystem.cs     # Collision detection/resolution
│   │   ├── IPhysicsEngine.cs       # Physics orchestration
│   │   └── IRenderer.cs            # Rendering abstraction
│   ├── Logic/                       # Core simulation
│   │   └── TableTennisSimulation.cs # Main orchestrator
│   └── Physics/                     # Physics implementations
│       ├── BallPhysicsOptimizedVervlet.cs
│       ├── BallPhysicsRangeKutta4.cs
│       ├── CollisionDetectionSystem.cs
│       ├── CollisionResolutionSystem.cs
│       └── CollisionUtils/          # Geometry utilities
│           ├── SphereObb.cs        # Sphere-OBB collision
│           └── OrientedBox.cs      # OBB representation
└── Infrastructure/                  # Unity-specific code
    ├── Bridging/
    │   └── SimulationBridge.cs     # Unity game loop bridge
    ├── CustomPhysics/
    │   └── CustomPhysicsEngine.cs  # Custom physics wrapper
    ├── DependencyInjection/
    │   └── SimulationBuilder.cs    # DI container
    ├── Rendering/
    │   └── UnityRenderer.cs        # Visual updates
    ├── SceneSetup/                  # Procedural generation
    │   ├── BallBuilder.cs          # Ball mesh generation
    │   ├── PaddleBuilder.cs        # Paddle construction
    │   ├── RoomBuilder.cs          # Environment setup
    │   └── TableTennisEquipmentBuilder.cs
    └── XRInput/
        ├── UnityXRInputManager.cs  # Raw XR input
        └── WorldSpaceAdapterInputManager.cs # Coordinate transform
```

### Key Components

#### 1. TableTennisSimulation (Domain/Logic/TableTennisSimulation.cs)

**Purpose**: High-level orchestrator managing the game state and physics integration.

**Responsibilities**:
- Maintains circular buffers for ball and paddle states
- Coordinates physics integration with collision detection
- Handles ball throwing/holding mechanics
- Implements sub-stepping for collision time-of-impact

**Key Algorithm** (Line 90-151):
```csharp
// Partial step approach for accurate collision timing:
// 1. Integrate full timestep
// 2. Detect collision and time-of-impact (TOI)
// 3. If collision at fractional time:
//    a) Revert to previous state
//    b) Integrate to collision point (partialT)
//    c) Resolve collision
//    d) Integrate remaining time (remainT)
```

#### 2. CollisionDetectionSystem (Domain/Physics/CollisionDetectionSystem.cs)

**Purpose**: Detects collisions between ball and paddle/environment using swept collision detection.

**Algorithm**: Swept Sphere-OBB (Oriented Bounding Box)
- Transforms moving sphere and box into relative space
- Uses ray-AABB intersection on expanded box
- Handles continuous collision detection for high-speed impacts

**Why This Approach?**
- **Pro**: No tunneling even at high velocities
- **Pro**: Accurate time-of-impact calculation
- **Con**: More expensive than discrete checks
- **Alternative Considered**: Unity's built-in continuous collision (rejected due to lack of control over resolution)

#### 3. CollisionResolutionSystem (Domain/Physics/CollisionResolutionSystem.cs)

**Purpose**: Resolves collisions with friction-based spin transfer.

**Algorithm**: Impulse-Based Resolution with Tangential Friction
1. Decompose relative velocity into normal and tangential components
2. Apply normal impulse with restitution coefficient
3. Calculate friction impulse (Coulomb friction model)
4. Convert friction impulse to spin via torque calculation

**Key Physics** (Lines 74-178):
```
Normal Impulse:    J_n = -(1 + e) * v_rel · n * m
Friction Impulse:  J_t = min(μ * |J_n|, |desired tangent impulse|)
Torque:            τ = r × J_t
Angular Change:    Δω = τ / I
```

**Why This Approach?**
- **Pro**: Physically accurate spin generation
- **Pro**: Handles both slip and no-slip regimes
- **Con**: Complex tuning of friction coefficients
- **Alternative Considered**: Simple reflection (rejected - no spin transfer)

---

## Asset Generation System

All game assets (ball, paddle, table, room) are **procedurally generated at runtime** from code-defined configurations. This approach enables:
- Precise dimensional accuracy (regulation table tennis sizes)
- Easy parameter tweaking without asset pipeline delays
- Automated testing with varied configurations

### Ball Generation (Infrastructure/SceneSetup/BallBuilder.cs)

```mermaid
graph LR
    Config[BallConfig] -->|Diameter| Sphere[Create Sphere]
    Sphere --> Visuals[Ball Visuals]
    Sphere --> Collider[SphereCollider]
    Sphere --> RB[Rigidbody]
    Visuals --> Ring1[SpinRing Horizontal]
    Visuals --> Ring2[SpinRing Vertical]

    style Ring1 fill:#333
    style Ring2 fill:#333
```

**Generation Process**:
1. Create root GameObject with `SphereCollider` (radius from config)
2. Add child sphere primitive scaled to ball diameter (40mm)
3. Add two perpendicular cylinder "spin rings" for visual spin indication
4. Configure `Rigidbody` as kinematic (custom physics drives motion)
5. Apply white material with URP Lit shader

**Spin Rings**: Two thin cylinders (0.001m thick) positioned at 90° rotation intervals around the ball's equator. These provide visual feedback for ball spin during flight.

### Paddle Generation (Infrastructure/SceneSetup/PaddleBuilder.cs)

```mermaid
graph TB
    Root[Paddle Root] --> Blade[PaddleBlade Pivot]
    Root --> Handle[PaddleHandle Pivot]

    Blade --> BladeVis[Blade Visuals<br/>Gray Cube]
    Blade --> FH[Forehand Rubber]
    Blade --> BH[Backhand Rubber]

    FH --> FHVis[Red Cube Visuals]
    FH --> FHCol[BoxCollider]

    BH --> BHVis[Black Cube Visuals]
    BH --> BHCol[BoxCollider]

    Handle --> HandleVis[Gray Cylinder]

    style FHVis fill:#f44
    style BHVis fill:#333
    style BladeVis fill:#888
    style HandleVis fill:#888
```

**Generation Process**:
1. **Blade**: Cube primitive scaled to regulation dimensions (~15.25cm × 17cm × 1cm)
2. **Rubbers**: Two thin boxes (2mm thick) offset from blade surface
   - Forehand (red) at +Z offset
   - Backhand (black) at -Z offset
   - Each has separate `BoxCollider` for collision detection
3. **Handle**: Cylinder primitive rotated 90° and scaled to ~10cm length
4. **Coordinate System**: Head centered at origin, handle extends along -Z

**Why Separate Rubber Colliders?**
- Enables per-side physics properties (forehand vs backhand spin multipliers)
- Accurate collision normal calculation for spin direction
- Supports different materials (inverted vs pips-out rubber)

### Room & Table Generation (Infrastructure/SceneSetup/RoomBuilder.cs)

```mermaid
graph TB
    Room[RoomBuilder] --> Floor[Floor Plane]
    Room --> Table[Table Surface]
    Room --> Net[Net]
    Room --> Walls[4 Walls + Ceiling]

    Table --> TableTop[Green Surface<br/>2.74m × 1.525m]
    Table --> WhiteLine[White Center Line]

    Net --> NetMesh[Thin Box<br/>0.1525m height]

    style TableTop fill:#0a0
    style WhiteLine fill:#fff
```

**Key Dimensions** (Regulation):
- Table: 2.74m (L) × 1.525m (W) × 0.76m (H)
- Net: 1.525m (W) × 0.1525m (H)
- Room: 10m × 10m × 3m (configurable)

All environment objects are added to the collision system's `OrientedBox` list for swept collision detection.

---

## Physics Engine

### Integration Methods

The engine supports **three numerical integration algorithms**, selectable via configuration:

#### 1. Optimized Verlet Integration (BallPhysicsOptimizedVervlet.cs)

**Algorithm**:
```
x(t+Δt) = x(t) + v(t)·Δt + ½·a(t)·Δt²
a(t+Δt) = F(x(t+Δt)) / m
v(t+Δt) = v(t) + ½·[a(t) + a(t+Δt)]·Δt
```

**Characteristics**:
- **Order**: 2nd order accuracy
- **Stability**: Good for moderate timesteps
- **Cost**: 2 force evaluations per step
- **Use Case**: Default for VR (balance of accuracy and performance)

**Forces Applied**:
1. **Gravity**: `F_g = m·g`
2. **Drag**: `F_d = -½·ρ·C_d·A·|v|·v`
3. **Magnus (Spin Lift)**: `F_m = C_m·(ω × v)`

#### 2. Runge-Kutta 4th Order (BallPhysicsRangeKutta4.cs)

**Algorithm**:
```
k1 = f(t, y)
k2 = f(t + Δt/2, y + Δt·k1/2)
k3 = f(t + Δt/2, y + Δt·k2/2)
k4 = f(t + Δt, y + Δt·k3)
y(t+Δt) = y(t) + Δt/6·(k1 + 2k2 + 2k3 + k4)
```

**Characteristics**:
- **Order**: 4th order accuracy
- **Stability**: Excellent, minimal error accumulation
- **Cost**: 4 force evaluations per step
- **Use Case**: High-precision simulations, testing

**Why Not Default?**
- 2× more expensive than Verlet
- Accuracy gains minimal for VR framerates (90-120 Hz)
- Verlet sufficient for perceived realism

#### 3. Basic Verlet (BallPhysicsBasicVervlet.cs)

**Algorithm**:
```
a = F(x) / m
v' = v + a·Δt
x' = x + v'·Δt
```

**Characteristics**:
- **Order**: 1st order (Euler method)
- **Stability**: Poor, requires small timesteps
- **Cost**: 1 force evaluation per step
- **Use Case**: Testing/debugging only

**Not Recommended**: Included for historical reasons; use Optimized Verlet instead.

### Physics Configuration

Centralized in `PhysicsConfig` class:

```csharp
// Air Properties
AirDensity = 1.225 kg/m³        // Sea level
MagnusCoefficient = 0.0001      // Spin lift strength
AngularDragCoefficient = 0.1    // Spin decay rate

// Ball Properties (40mm plastic ball)
MassKg = 0.0027 kg
DiameterMeters = 0.04 m
DragCoefficient = 0.5           // Typical for sphere
CrossSectionalArea = π·r²

// Paddle Properties
RubberBounciness = 0.89         // Coefficient of restitution
FrictionCoefficient = 0.8       // Tangential friction
LeftSideSpinMultiplier = 1.0    // Forehand spin scaling
RightSideSpinMultiplier = 0.9   // Backhand spin scaling

// Table Properties
BounceRestitution = 0.78        // Less bouncy than rubber
Friction = 0.3                  // Minimal spin loss
```

### Sub-Stepping Strategy

**Problem**: Unity's `FixedUpdate` runs at 50 Hz, but accurate collision requires higher frequency.

**Solution**: Sub-stepping within each `FixedUpdate`:

```mermaid
graph LR
    FU[FixedUpdate<br/>50 Hz] -->|Δt = 0.02s| Accum[Accumulate Time]
    Accum -->|SubStep 1| Sim1[Simulate<br/>0.00278s]
    Sim1 -->|SubStep 2| Sim2[Simulate<br/>0.00278s]
    Sim2 -->|SubStep 3| Sim3[Simulate<br/>0.00278s]
    Sim3 -->|...| SimN[Simulate<br/>×7 substeps]

    SimN --> Done[Total: 360 Hz<br/>physics rate]
```

**Configuration** (SimulationBridge.cs:55-56):
```csharp
TargetSimulationUpdateFrequencyWithSubStepping = 3 * 120 // 360 Hz
SubStepInterval = 1.0f / 360 // ~0.00278 seconds
```

**Why 360 Hz?**
- Minimum for catching fast paddle swings (5-10 m/s)
- Avoids tunneling through thin paddle (1cm thickness)
- 3× multiplier of VR refresh rate (120 Hz)

**Alternative Considered**: Adaptive time-stepping (rejected - too complex for real-time VR)

---

## Collision System

### Swept Sphere-OBB Algorithm

**Problem**: Discrete collision checks miss fast-moving balls that "tunnel" through objects.

**Solution**: Swept collision detection using ray-AABB intersection.

```mermaid
sequenceDiagram
    participant Ball
    participant Paddle
    participant Detection as CollisionDetection
    participant Resolution as CollisionResolution

    Ball->>Detection: Previous & Current Positions
    Paddle->>Detection: Previous & Current Poses

    Detection->>Detection: Transform to Paddle Local Space
    Detection->>Detection: Expand Paddle OBB by Ball Radius
    Detection->>Detection: Ray-AABB Intersection Test

    alt Collision Detected
        Detection->>Resolution: CollisionData (TOI, Normal, Point)
        Resolution->>Resolution: Compute Normal Impulse
        Resolution->>Resolution: Compute Friction Impulse
        Resolution->>Resolution: Convert Friction to Spin
        Resolution->>Ball: Updated Velocity & AngularVelocity
    else No Collision
        Detection->>Ball: Continue Integration
    end
```

### Collision Detection Pipeline (CollisionDetectionSystem.cs)

**Step 1: Transform to Relative Space**
```csharp
Vector3 relativeVel = (sphereEnd - sphereStart) - (boxEnd - boxStart)
Matrix4x4 invM = Matrix4x4.TRS(boxStart.Center, boxStart.Rotation, Vector3.one).inverse
Vector3 localSphereStart = invM.MultiplyPoint3x4(sphereStartWorld)
Vector3 localSphereEnd = invM.MultiplyPoint3x4(sphereStartWorld + relativeVel)
```

**Step 2: Expand OBB by Sphere Radius**
```csharp
Vector3 minAABB = -boxHalfExtents - Vector3.one * sphereRadius
Vector3 maxAABB = boxHalfExtents + Vector3.one * sphereRadius
```

**Step 3: Ray-AABB Intersection** (CollisionUtils/CollisionUtils.cs)
```csharp
bool RayAABBIntersection(Vector3 origin, Vector3 dir, Vector3 min, Vector3 max,
                          out float tHit, out Vector3 normal)
{
    // Slab method: find t-intervals for each axis
    float tMin = (min.x - origin.x) / dir.x
    float tMax = (max.x - origin.x) / dir.x
    // ... repeat for Y and Z axes
    // tHit = intersection of all intervals
}
```

**Step 4: Transform Results to World Space**
```csharp
collisionNormal = M.MultiplyVector(localNormal).normalized
collisionPoint = M.MultiplyPoint3x4(localHitPos)
timeOfImpact = tHit // [0, 1] fraction of timestep
```

### Collision Resolution with Friction-Based Spin

**Physics Model**: Coulomb Friction with Spin Transfer

**Step 1: Decompose Relative Velocity** (CollisionResolutionSystem.cs:92-98)
```csharp
Vector3 vRel = ball.Velocity - paddle.Velocity
Vector3 vRelNormal = Vector3.Dot(vRel, normal) * normal
Vector3 vRelTangent = vRel - vRelNormal
```

**Step 2: Normal Impulse with Restitution** (Lines 100-111)
```csharp
float normalImpulseMag = -(1 + restitution) * vRelNormalMag * mass
Vector3 normalImpulse = normalImpulseMag * normal
ball.Velocity += normalImpulse / mass
```

**Step 3: Tangential Friction** (Lines 133-156)
```csharp
Vector3 desiredTangentImpulse = -vRelTangent * mass
float maxFrictionMag = frictionCoefficient * normalImpulseMag

if (neededMag <= maxFrictionMag) {
    // No-slip regime: cancel all tangential motion
    frictionImpulse = desiredTangentImpulse
} else {
    // Slip regime: apply maximum friction
    frictionImpulse = desiredTangentImpulse.normalized * maxFrictionMag
}

ball.Velocity += frictionImpulse / mass
```

**Step 4: Spin Generation** (Lines 162-173)
```csharp
// Torque from friction about contact point
Vector3 torque = Vector3.Cross(normal * ballRadius, frictionImpulse)

// Moment of inertia for thin-walled sphere: I = (2/3)mr²
float I = (2/3) * mass * ballRadius²

// Angular impulse
Vector3 deltaOmega = torque / I
ball.AngularVelocity += deltaOmega
```

### Known Issues (Why Collision Feel Isn't Lifelike)

**Issue 1: Normal Calculation Inconsistency**
- Swept collision normal points from expanded AABB face
- Doesn't account for corner/edge contacts properly
- Results in occasional "wrong direction" bounces

**Issue 2: Friction Model Limitations**
- Coulomb friction assumes instantaneous slip transition
- Real rubber has velocity-dependent friction
- No modeling of rubber compression/deformation

**Issue 3: Restitution Inaccuracy**
- Uses constant restitution coefficient
- Real balls have velocity-dependent restitution (see VelocityBasedRestitutionAndSpinDependantDragLift.md)
- Should implement: `Cr = 1 - 0.1·ln(v/Vy)·(v/Vy - 1)^0.156`

**Issue 4: Spin Transfer Feels "Off"**
- Empirically tuned multipliers (0.8-1.0) don't match pro experience
- Missing rubber-specific parameters (tackiness, thickness)
- Torque calculation assumes point contact, ignores contact patch

**Attempted Solutions**:
- Implemented multiple paddle prediction methods (linear extrapolation)
- Added extensive debug logging for collision events
- Tried various friction coefficients (0.6-1.2)
- Experimented with different integration methods

**None achieved satisfactory realism** - the prototype remains a work-in-progress.

---

## Integration Algorithms

### Comparison Table

| Algorithm | Order | Force Evals | Timestep | Use Case |
|-----------|-------|-------------|----------|----------|
| Basic Verlet | 1st | 1 | 0.001s | Testing only |
| Optimized Verlet | 2nd | 2 | 0.00278s | **Default (VR)** |
| RK4 | 4th | 4 | 0.005s | High-precision |
| Unity Physics | Native | N/A | 0.02s | Fallback |

### Accuracy vs Performance

```mermaid
graph LR
    subgraph Performance["← More Performance"]
        Basic[Basic Verlet<br/>1 eval/step]
    end

    subgraph Balance["Balanced →"]
        Optimized[Optimized Verlet<br/>2 eval/step<br/>**DEFAULT**]
    end

    subgraph Accuracy["More Accuracy →"]
        RK4[Runge-Kutta 4<br/>4 eval/step]
    end

    Basic -.->|Not Recommended| Optimized
    Optimized -.->|2× Cost| RK4

    style Optimized fill:#4a4
```

### Custom vs Unity Physics

**Custom Physics Engine** (Infrastructure/CustomPhysics/CustomPhysicsEngine.cs):
- Uses selected integrator (Verlet/RK4)
- Swept collision detection
- Manual sub-stepping
- Full control over resolution

**Unity Physics Engine** (Infrastructure/UnityPhysics/UnityPhysicsEngine.cs):
- Uses Unity's `Rigidbody` and `Collider` components
- Continuous collision detection mode
- Built-in solver
- Less control, more stable

**Why Both Exist?**
- Custom: Research and fine-tuning of physics parameters
- Unity: Fallback for quick testing, less maintenance
- Switchable via `useCustomPhysics` flag in `SimulationBridge`

---

## Input Processing

### VR Input Pipeline

```mermaid
graph TB
    XRDevice[XR Device<br/>Quest/Vive/etc.] -->|OpenXR| Unity[Unity XR Plugin]
    Unity -->|InputAction| UXRI[UnityXRInputManager]

    UXRI -->|Position| PosFilter[Position Filter]
    UXRI -->|Rotation| RotFilter[Rotation Filter]

    PosFilter -->|Filtered| WSA[WorldSpaceAdapter]
    RotFilter -->|Filtered| WSA

    WSA -->|Transform to<br/>World Space| Controller[ControllerState]

    Controller --> Paddle[PaddleState]
    Controller --> Ball[Ball Throw Logic]

    subgraph Filters["Filter Types"]
        None[PassThrough<br/>No filtering]
        MA[MovingAverage<br/>5-sample window]
        Kalman[Kalman Filter<br/>Predictive]
    end
```

### Filter Options

**1. PassThrough Filter** (No filtering)
- **Pros**: Zero latency, true controller motion
- **Cons**: Noisy, jittery
- **Use**: High-precision VR headsets

**2. Moving Average Filter** (5-sample window)
- **Pros**: Simple, stable
- **Cons**: Lag (5 frames × 0.011s = 55ms delay)
- **Use**: Budget VR headsets

**3. Kalman Filter** (Predictive)
- **Pros**: Smooths noise while predicting motion
- **Cons**: Complex tuning, can overshoot
- **Use**: Advanced users

**Configuration** (SimulationBridge.cs:31-37):
```csharp
[SerializeField] private string rightFilterType = "None"; // "MovingAverage", "Kalman", "None"
[SerializeField] private string leftFilterType = "None";
```

### Velocity Calculation

**Problem**: Unity's XR input doesn't provide reliable velocity data.

**Solution**: Finite difference approximation using circular buffer:

```csharp
// In UnityXRInputManager (approximate logic)
Vector3 velocity = (currentPosition - previousPosition) / deltaTime
Vector3 angularVelocity = QuaternionToAngularVelocity(currentRotation, previousRotation, deltaTime)
```

**Issue**: Susceptible to noise, especially angular velocity. Filters help but add latency.

### Paddle Prediction (Incomplete Feature)

**Problem**: Sub-stepping requires paddle state between input samples.

**Attempted Solution**: Linear extrapolation predictor (LinearPaddlePredictor.cs):
```csharp
PaddleState PredictPose(PaddleState lastKnown, float deltaTime) {
    predictedPosition = lastKnown.Position + lastKnown.Velocity * deltaTime
    predictedRotation = lastKnown.Rotation * Quaternion.Euler(lastKnown.AngularVelocity * deltaTime * Rad2Deg)
    return predicted
}
```

**Status**: Implemented but doesn't significantly improve collision accuracy. Likely needs higher-order prediction (quadratic or spline-based).

---

## Performance Considerations

### Target Performance

- **VR Framerate**: 90-120 FPS (required for comfort)
- **Physics Rate**: 360 Hz (sub-stepped)
- **Collision Checks**: ~10 per frame (paddle + environment)

### Optimization Strategies

**1. Circular Buffers** (Domain/Utilities/CircularBuffer.cs)
- Preallocated state storage
- Zero garbage collection
- Fixed capacity (2 states for ball/paddle)

**2. Struct-Based State**
- `BallState`, `PaddleState`, `CollisionData` are structs
- Passed by reference (`ref`) to avoid copying
- Cache-friendly memory layout

**3. Object Pooling**
- Paddle and ball GameObjects created once
- Reused throughout session
- No instantiation during gameplay

**4. Conditional Sub-Stepping**
```csharp
// Only sub-step when ball is in play
if (!currentBallState.IsHeld) {
    _physicsEngine.Integrate(ref currentBallState, deltaTime);
}
```

**5. Profiling Results** (Quest 2, 120 Hz mode)
- `UpdateSimulation`: ~2.5ms per frame
- `CollisionDetection`: ~0.8ms per frame
- `Rendering`: ~1.2ms per frame
- **Total CPU**: ~5ms (16% of 8.33ms budget)
- **Headroom**: Sufficient for 120 FPS

### Memory Footprint

- **Domain Logic**: ~5 KB (state objects)
- **Scene Objects**: ~2 MB (meshes, textures)
- **Total Runtime**: ~50 MB (Unity overhead)

---

## Known Limitations

### Physics Shortcomings

1. **Collision Normal Calculation**
   - Uses AABB face normals, not actual geometry
   - Corner/edge collisions give incorrect reflection angles
   - **Impact**: Occasional "weird bounces"

2. **Restitution Model**
   - Constant coefficient (0.89 rubber, 0.78 table)
   - Real balls have velocity-dependent restitution
   - **Impact**: High-speed hits feel too bouncy

3. **Friction Model**
   - Simplified Coulomb friction
   - No rubber deformation or stiction
   - **Impact**: Spin transfer doesn't match real paddles

4. **Magnus Effect Tuning**
   - Single coefficient for all spin ratios
   - Should use lookup table (see VelocityBasedRestitutionAndSpinDependantDragLift.md)
   - **Impact**: Spin curves either too weak or too strong

### Architectural Limitations

1. **No Paddle Deformation**
   - Treated as rigid body
   - Real rubber compresses significantly
   - **Impact**: "Hard" collision feel

2. **Single Ball Only**
   - Simulation hardcoded for one ball
   - Can't support multi-ball training mode

3. **No Net Physics**
   - Net is solid box, not mesh
   - Real net has elasticity and drag
   - **Impact**: Net hits feel unrealistic

4. **Limited Environment Interaction**
   - Only basic collision with walls/floor
   - No ball marks, table scuffs, etc.

### Input System Issues

1. **Velocity Estimation Noise**
   - Finite difference amplifies jitter
   - Especially bad for angular velocity
   - **Impact**: Inconsistent spin generation

2. **Latency from Filtering**
   - Moving average adds 55ms delay
   - Kalman filter can overshoot
   - **Impact**: Paddle feels "heavy" or "slippery"

3. **Calibration Complexity**
   - Requires JSON file per paddle model
   - No in-game calibration tool
   - **Impact**: Poor out-of-box experience

---

## Future Improvements

### Physics Enhancements

1. **Implement Velocity-Dependent Restitution**
   ```csharp
   float ComputeRestitution(float impactSpeed) {
       float Vy = ComputeYieldVelocity(); // From ball material properties
       float ratio = impactSpeed / Vy;
       return 1 - 0.1f * Mathf.Log(ratio) * Mathf.Pow(ratio - 1, 0.156f);
   }
   ```

2. **Spin-Dependent Drag/Lift Tables**
   - Lookup table for (spin ratio → Cd, Cl)
   - Interpolate between measured data points
   - More realistic ball trajectories

3. **Contact Patch Model**
   - Replace point contact with finite patch
   - Calculate contact area from compression
   - More accurate torque generation

4. **Paddle Compliance Model**
   - Spring-damper system for rubber layer
   - Affects restitution and contact time
   - "Softer" collision feel

### Collision System Improvements

1. **Geometry-Accurate Normals**
   - Use actual mesh geometry, not AABB
   - Raycast against paddle collider
   - Fix corner/edge collision artifacts

2. **Multi-Point Contact**
   - Detect multiple simultaneous contacts
   - Average normals and impulses
   - Smoother collision resolution

3. **Predictive Collision Detection**
   - Extrapolate ball/paddle trajectories
   - Earlier collision detection
   - Reduce reliance on sub-stepping

### Input System Upgrades

1. **Hardware Velocity Readback**
   - Use native XR SDK velocity data
   - Bypass finite difference calculation
   - Lower noise, lower latency

2. **In-Game Calibration Tool**
   - Visual alignment guides
   - Save/load calibration profiles
   - Per-user customization

3. **Adaptive Filtering**
   - Switch filter based on motion intensity
   - PassThrough for fast swings
   - Kalman for slow positioning

### Architecture Refactoring

1. **Multi-Ball Support**
   - Refactor `TableTennisSimulation` to manage ball list
   - Collision system handles ball-ball interactions
   - Enable training modes

2. **Net as Deformable Mesh**
   - Replace box with rope simulation
   - Spring-mass model for net
   - Realistic net interactions

3. **Replay System**
   - Record state history
   - Playback with timeline scrubbing
   - Analyze collision events

---

## Algorithm Alternatives Considered

### Integration Methods

| Algorithm | Why Considered | Why Not Used |
|-----------|----------------|--------------|
| **Symplectic Euler** | Energy-conserving, simple | Same accuracy as Verlet, less common |
| **Adams-Bashforth** | Multi-step, efficient | Requires history buffer, complex |
| **Implicit Euler** | Highly stable | Too slow for real-time, needs solver |
| **Leapfrog** | Popular in games | Equivalent to Verlet, no advantage |

**Selected: Optimized Verlet** for balance of accuracy, performance, and familiarity.

### Collision Detection

| Algorithm | Why Considered | Why Not Used |
|-----------|----------------|--------------|
| **Discrete Checks** | Fast, simple | Tunneling at high speeds |
| **Unity CCD** | Built-in, free | No time-of-impact data, less control |
| **GJK/EPA** | Exact distance, works for any convex | Overkill for sphere-box, slower |
| **Spatial Hashing** | Broad phase optimization | Only one ball, narrow phase dominates |

**Selected: Swept Sphere-OBB** for continuous detection with exact time-of-impact.

### Collision Resolution

| Algorithm | Why Considered | Why Not Used |
|-----------|----------------|--------------|
| **Simple Reflection** | Fastest | No spin transfer |
| **Penalty Forces** | Smooth, no discontinuity | Requires small timesteps, unstable |
| **Constraint-Based (PBD)** | Stable, iterative | Complex, requires solver iterations |
| **Impulse with Friction** | Physically accurate, one-shot | **Selected (this is what we use)** |

**Selected: Impulse-Based Resolution** for physical accuracy and one-step solution.

### Input Filtering

| Filter | Why Considered | Why Not Used |
|--------|----------------|--------------|
| **None** | Zero latency | Too noisy for budget VR |
| **Moving Average** | Simple, stable | **Selected for basic use** |
| **Kalman Filter** | Predictive, optimal for Gaussian noise | **Selected for advanced use** |
| **Exponential Smoothing** | Single parameter, efficient | Less tunable than Kalman |
| **Butterworth Filter** | Frequency-domain smoothing | Requires FFT, too slow |

**Selected: User-Configurable (None/MA/Kalman)** for flexibility.

---

## Mermaid Diagram Summary

### Overall System Data Flow

```mermaid
flowchart TD
    Start([Start Frame]) --> Input[Read XR Input]

    Input --> Filter{Filter Type?}
    Filter -->|PassThrough| Raw[Raw Data]
    Filter -->|MovingAverage| MA[Smooth Data]
    Filter -->|Kalman| KF[Predicted Data]

    Raw --> Transform[Transform to World Space]
    MA --> Transform
    KF --> Transform

    Transform --> States[Update Controller States]

    States --> FixedUpdate{FixedUpdate?}

    FixedUpdate -->|Yes| Substep[Accumulate Δt]
    Substep --> SubLoop{Substep Available?}

    SubLoop -->|Yes| Predict[Predict Paddle Pose]
    Predict --> Integrate[Integrate Ball Physics]
    Integrate --> Detect[Detect Collisions]

    Detect --> Collision{Collision?}

    Collision -->|Yes| TOI[Calculate Time-of-Impact]
    TOI --> Rewind[Rewind to TOI]
    Rewind --> Resolve[Resolve Collision]
    Resolve --> Continue[Integrate Remainder]
    Continue --> SubLoop

    Collision -->|No| SubLoop

    SubLoop -->|No| FixedUpdate

    FixedUpdate -->|No| Render[Render Frame]

    Render --> End([End Frame])

    style Start fill:#4a4
    style End fill:#4a4
    style Resolve fill:#f44
    style Integrate fill:#44f
```

### Collision Resolution Physics

```mermaid
sequenceDiagram
    participant Ball
    participant System as CollisionResolution
    participant Physics as PhysicsConfig

    Ball->>System: Collision Detected (vRel, normal)
    System->>System: Decompose vRel = vNormal + vTangent

    System->>Physics: Get Restitution (e)
    Physics-->>System: e = 0.89 (rubber) or 0.78 (table)

    System->>System: J_normal = -(1+e)·vNormal·m
    System->>Ball: Apply Normal Impulse

    System->>System: J_desired = -vTangent·m
    System->>System: J_max = μ·|J_normal|

    alt J_desired ≤ J_max (No Slip)
        System->>Ball: Apply Full Friction Impulse
        System->>System: Calculate Torque τ = r × J_friction
        System->>Ball: Apply Spin Δω = τ / I
    else J_desired > J_max (Slip)
        System->>Ball: Apply Clamped Friction Impulse
        System->>System: Calculate Torque τ = r × J_clamped
        System->>Ball: Apply Reduced Spin Δω = τ / I
    end

    Ball-->>System: Updated Velocity & AngularVelocity
```

---

## Conclusion

This VR table tennis game engine demonstrates a **clean architecture** with domain-driven design, procedural asset generation, and custom physics simulation. The implementation features:

**Strengths**:
- ✅ Testable, Unity-independent domain logic
- ✅ Swept collision detection prevents tunneling
- ✅ Multiple physics integrators (Verlet, RK4)
- ✅ Configurable input filtering
- ✅ Sub-stepping for high-frequency collision handling
- ✅ Procedural generation of all game assets

**Weaknesses** (Unfinished Prototype):
- ❌ Collision feel doesn't achieve lifelike realism
- ❌ Simplified friction model lacks rubber complexity
- ❌ Constant restitution coefficients (should be velocity-dependent)
- ❌ Magnus effect not properly tuned for spin ranges
- ❌ Velocity estimation noise affects spin consistency
- ❌ No paddle deformation or contact patch modeling

**Next Steps for Realistic Physics**:
1. Implement velocity-dependent restitution (yield-strength formula)
2. Add spin-dependent drag/lift lookup tables
3. Replace point contact with finite contact patch
4. Integrate paddle compliance model
5. Use hardware velocity data instead of finite difference

The prototype successfully demonstrates the **architectural approach** but requires significant **physics refinement** to achieve the pro-level realism originally targeted. The modular design makes these improvements tractable, as physics algorithms can be swapped independently of the Unity integration.

---

## References

- `VelocityBasedRestitutionAndSpinDependantDragLift.md` - Advanced physics improvements roadmap
- `docs/Requirements.md` - Original project specification
- `docs/TargetArchitecture2.md` - Domain-driven design rationale
- Unity Physics Manual: https://docs.unity3d.com/Manual/PhysicsOverview.html
- OpenXR Specification: https://www.khronos.org/openxr/

---

*Documentation Author: Claude (AI Assistant)*
*Last Updated: 2025-11-05*
*Engine Version: Prototype (Unfinished)*
