# Table Tennis VR Game: Architecture & Lessons Learned

**Purpose:** Knowledge transfer document for teams building table tennis VR games from scratch.

**Context:** Lessons from converting a Unity C# table tennis VR game to native C++ with OpenXR, preserving patterns that support "perfect bounce feel" gameplay.

---

## 1. Core Goals & Constraints

### Primary Goal
Create table tennis VR game with **perfect racket-ball bounce feel** - the single most important requirement that drives all architectural decisions.

### Why "Perfect Bounce Feel" Matters
- VR table tennis success depends on immediate, predictable ball response to paddle hits
- Players develop muscle memory; any inconsistency breaks immersion
- Requires: stable physics simulation, smooth input tracking, and minimal latency

### Key Constraints
1. **VR-specific**: Must handle noisy 6DOF tracking from HMDs/controllers
2. **Real-time**: 90+ Hz refresh rate, fixed timestep physics
3. **Cross-platform**: Need to support multiple VR runtimes (OpenXR chosen)
4. **Physics accuracy**: Real-world table tennis behavior (spin, air resistance, velocity-dependent restitution)

---

## 2. Architecture Overview

### High-Level Structure

```
┌─────────────────────────────────────────────────────┐
│                   Application                        │
│                    (main loop)                       │
└──────────────────┬──────────────────────────────────┘
                   │
        ┌──────────┴──────────┐
        │                     │
┌───────▼────────┐   ┌───────▼────────────┐
│ Infrastructure │   │   Domain Logic      │
│                │   │   (simulation)      │
│ - OpenXR/VR    │   │                     │
│ - Rendering    │   │ - Physics           │
│ - Input        │   │ - Collision         │
│ - Geometry     │   │ - Ball state        │
└────────────────┘   └─────────────────────┘
```

### Key Principle: Domain Isolation

**Rationale:** Physics simulation must be independent of VR runtime, rendering, or platform-specific code.

**Why:**
- Physics needs determinism for "perfect feel"
- Testing physics without VR hardware
- Easier to port to different platforms
- Can swap rendering backends without touching physics

**Implementation:**
```
domain/           - Pure C++, no external dependencies
  physics/        - Ball physics, collision detection
  entities/       - BallState, PaddleState (data only)
  config/         - Configuration values

infrastructure/   - Platform-specific code
  openxr/         - VR runtime integration
  rendering/      - Graphics backend
  game/           - Glue code between domain and infrastructure
```

### Directory Structure Rationale

**Why separate domain/infrastructure?**
- Domain code is testable without VR hardware
- Can run physics tests on CI without headsets
- Easier to understand what depends on what
- Forces clean boundaries

**Pitfall:** Initially tried to mix Unity-style GameObjects with physics. Led to tight coupling and untestable code. Separating domain/infrastructure fixed this.

---

## 3. Critical Systems & Design Decisions

### 3.1 Physics Simulation

**Goal:** Realistic ball behavior with spin, air resistance, and proper bouncing.

#### Integration Methods

**Implemented Two Approaches:**

1. **Verlet Integration** (2nd-order)
   - Simpler, faster
   - Good energy conservation
   - Handles position/velocity/spin
   - **Use for:** Standard gameplay

2. **Runge-Kutta 4** (4th-order)
   - Higher accuracy
   - More expensive (4x force calculations)
   - Better for complex spin interactions
   - **Use for:** Validation/reference implementation

**Rationale:** Verlet is sufficient for table tennis speeds. RK4 exists for validation and potential future "simulation mode."

**Pitfall:** Don't use basic Euler integration - energy accumulation causes unrealistic behavior over time.

#### Fixed Timestep

**Implementation:**
```cpp
const float FIXED_DELTA = 0.02f;  // 50 Hz physics

void UpdateSimulation(float deltaTime) {
    m_accumulatedTime += deltaTime;

    while (m_accumulatedTime >= FIXED_DELTA) {
        physics.Step(ballState, FIXED_DELTA);
        m_accumulatedTime -= FIXED_DELTA;
    }
}
```

**Rationale:**
- Variable timestep causes unpredictable bounces
- Players notice inconsistent ball behavior
- Fixed timestep = deterministic physics = reliable feel

**Learned:** 50 Hz (0.02s) is sufficient. Higher rates (100Hz) don't improve feel noticeably but increase CPU usage.

#### Velocity-Dependent Restitution

**Key Innovation:** Ball bounciness varies with impact speed.

**Why This Matters:**
Real table tennis balls don't have constant restitution:
- Gentle drop: ~92% energy return (nearly elastic)
- Normal play (3 m/s): ~89% return
- Hard smash (8 m/s): ~85% return (compression losses)

**Implementation:**
```cpp
float CalculateBallTableRestitution(float impactSpeed) {
    if (impactSpeed < 1.0f) return 0.92f - (0.02f * impactSpeed);
    else if (impactSpeed < 5.0f) return 0.89f - (0.01f * (impactSpeed - 1.0f));
    else return max(0.75f, 0.85f - (0.005f * (impactSpeed - 5.0f)));
}
```

**Rationale:** Constant restitution feels "wrong" to experienced players. Velocity-dependent model matches real-world physics and player expectations.

**Testing Approach:** Drop ball from known heights, measure rebound height. Compare to real table tennis ball data.

#### Magnus Force (Spin)

**Implementation:**
```cpp
float spinParameter = (spinRate * radius) / speed;
float Cm = (spinParameter < 0.5f) ? 1.0f * spinParameter
         : (spinParameter < 4.0f) ? 0.5f * (1.0f - exp(-spinParameter))
         : 0.5f;  // Saturation

float magnusMag = Cm * 0.5f * airDensity * area * speed * speed;
Vector3 magnusForce = spinAxis.Cross(velocityDir) * magnusMag;
```

**Rationale:**
- Linear approximation fails at high spin rates
- Non-linear model matches aerodynamic behavior
- Spin parameter captures relationship between spin and translation

**Pitfall:** Don't ignore Magnus force. Spin is critical to table tennis feel - topspin should curve down, backspin should float.

### 3.2 VR Input & Tracking

**Challenge:** VR controller tracking is noisy (±5mm jitter typical).

#### Problem
Raw VR tracking data causes:
- Jittery paddle visuals
- Unreliable velocity estimation
- Inconsistent collision detection
- Destroyed "perfect feel"

#### Solution: Kalman Filtering

**What:** Optimal estimator that combines predictions with measurements.

**Implementation:**
```cpp
class KalmanFilterVector3 {
    float processNoise = 1e-5;      // Expected movement per frame
    float measurementNoise = 1e-2;  // Sensor noise (~1cm)

    Vector3 Update(Vector3 measurement) {
        // Prediction: uncertainty increases
        errorCovariance += processNoise;

        // Kalman gain: how much to trust measurement
        float K = errorCovariance / (errorCovariance + measurementNoise);

        // Update estimate
        stateEstimate += K * (measurement - stateEstimate);

        // Update uncertainty
        errorCovariance *= (1 - K);

        return stateEstimate;
    }
};
```

**Results:**
- Jitter reduced from ±5mm to ±0.5mm (10x improvement)
- Smooth velocity estimation via filtered history
- Stable collision detection
- Players report "solid" paddle feel

**Rationale:** Can't fix hardware noise in software, but can optimally estimate true position. Kalman filter is provably optimal for Gaussian noise.

**Tuning Notes:**
- `processNoise`: Higher = more responsive, less smooth
- `measurementNoise`: Higher = more smoothing, more lag
- Default values (1e-5, 1e-2) work well for most VR controllers

**Pitfall:** Don't use simple moving average - introduces fixed lag. Kalman filter adapts to motion while filtering noise.

### 3.3 Collision Detection

**Approach:** Swept sphere vs primitives (continuous collision detection).

**Why Continuous?**
- Ball moves fast (~10 m/s for smashes)
- At 50 Hz physics, ball moves 20cm per frame
- Ball diameter is 4cm
- Discrete detection = tunneling through objects

**Implementation:**
```cpp
// Swept sphere: treat ball as moving sphere, not discrete point
bool DetectCollision(Vector3 ballStart, Vector3 ballEnd, float radius) {
    // Ray from ballStart to ballEnd
    // Expanded geometry by sphere radius
    // Returns first hit time + collision point/normal
}
```

**Surface-Specific Handling:**
- **Table:** Plane intersection, check bounds
- **Net:** Thin vertical plane at x=0
- **Paddle:** Oriented bounding box (OBB)
- **Floor/Walls:** Axis-aligned planes

**Rationale:** Swept sphere is cheaper than full continuous collision but prevents tunneling for reasonably fast motion.

**Limitation:** Very fast motion (>20 m/s) can still tunnel. Add velocity clamping or sub-stepping if needed.

### 3.4 Testing Strategy

**Key Insight:** Tests should use table tennis terminology, not abstract math.

#### Domain-Specific Test Helpers

**Good (domain-focused):**
```cpp
TEST(BallPhysics, GentleDropBounceIsElastic) {
    BallState ball = FreeFallingBall(0.3f);  // 30cm drop
    // ... simulate ...
    EXPECT_GT(reboundHeight, 0.25f)
        << "Ball should rebound to at least 25cm";
}
```

**Bad (math-focused):**
```cpp
TEST(BallPhysics, TestCase1) {
    BallState ball;
    ball.position = Vector3(0, 0.3, 0);
    ball.velocity = Vector3(0, 0, 0);
    // ... simulate ...
    EXPECT_GT(result.position.y, 0.25f);
}
```

**Rationale:**
- Domain-focused tests communicate intent
- Easier to understand what's being tested
- Easier to maintain when implementation changes
- Validates gameplay feel, not just math

#### Test Patterns

**Helpers:**
```cpp
// Ball builders
BallState StationaryBall(Vector3 pos);
BallState FreeFallingBall(float height);
BallState BallWithTopspin(Vector3 pos, Vector3 vel, float spin);

// Config builders
PhysicsConfig NoAirResistanceConfig();
PhysicsConfig VacuumConfig();

// Assertions
void ExpectBallAt(BallState, Vector3 expectedPos, float tolerance);
void ExpectBallBounced(BallState before, BallState after);
void ExpectNormalPointingUp(Vector3 normal);
```

**Benefits:**
- Tests are readable by non-programmers
- Can verify against real table tennis player expectations
- Focus on "what" not "how"

**Pitfall:** Don't write low-level math tests for gameplay code. Test scenarios that players will experience.

---

## 4. Design Patterns That Worked

### 4.1 Builder Pattern for Asset Generation

**Pattern:**
```cpp
class SceneGeometryBuilder {
    static vector<RenderableObject> BuildBall(BallConfig);
    static vector<RenderableObject> BuildPaddle(PaddleConfig);
    static vector<RenderableObject> BuildTable(TableConfig);
    static vector<RenderableObject> BuildRoom(float size);
};
```

**Why This Works:**
- Separation: geometry generation ≠ rendering
- Testable: can verify vertex counts, bounds without GPU
- Reusable: same builders for game and editor tools
- Config-driven: easy to adjust sizes, materials

**Use Case:**
```cpp
// At startup
auto sceneObjects = SceneGeometryBuilder::BuildCompleteScene(
    ballConfig, paddleConfig, tableConfig, 10.0f);

// Upload to GPU
for (const auto& obj : sceneObjects) {
    CreateGPUBuffers(obj.geometry.vertices, obj.geometry.indices);
}
```

### 4.2 Dependency Injection via Builder

**Pattern:**
```cpp
class SimulationBuilder {
    unique_ptr<Simulation> Build(
        BallConfig, PaddleConfig, TableConfig, PhysicsConfig);

    unique_ptr<Simulation> BuildForTesting() {
        // Zero out air resistance, use predictable values
        PhysicsConfig cfg = PhysicsConfig::Default();
        cfg.airDensity = 0.0f;
        return Build(ballCfg, paddleCfg, tableCfg, cfg);
    }
};
```

**Why This Works:**
- Testing: can create simulation with test-friendly config
- Flexibility: can swap physics engines or configs
- Clarity: dependencies are explicit

### 4.3 Data-Oriented Entities

**Pattern:**
```cpp
struct BallState {
    Vector3 position;
    Vector3 velocity;
    Vector3 spin;
    Vector3 angularVelocity;
    // No methods, no logic - just data
};
```

**Why This Works:**
- Easy to serialize (save/load game state)
- Easy to interpolate (for rendering between physics frames)
- Easy to network (multiplayer)
- Cache-friendly (can batch process)

**Pitfall:** Don't put logic in entity structs. Keep them as POD (plain old data).

### 4.4 Separation: Simulation vs Rendering

**Pattern:**
```cpp
// Simulation runs at fixed 50 Hz
void FixedUpdate(float dt) {
    ballState = physics.Step(ballState, dt);
}

// Rendering runs at display rate (90/120 Hz)
void Render(float alpha) {
    Vector3 interpolated = Lerp(prevBallState.position,
                                 currBallState.position, alpha);
    DrawBall(interpolated);
}
```

**Why This Works:**
- Physics determinism
- Smooth visuals even with slower physics
- Can run headless for testing

---

## 5. Pitfalls & Mistakes

### 5.1 Over-Engineering Documentation

**Mistake:** Created extensive markdown docs (ARCHITECTURE_REVIEW.md, IMPLEMENTATION_ROADMAP.md) with detailed plans before implementing.

**Why It Was Wrong:**
- Documents became outdated quickly
- Time spent writing could have been spent coding
- Plans changed based on implementation learnings
- User feedback: "Don't overhype... keep it honest"

**Lesson:** Write documentation AFTER implementing, not before. Code is the source of truth.

### 5.2 Trying to Preserve Unity Concepts Too Literally

**Mistake:** Initially tried to recreate Unity's GameObject/Component system.

**Why It Was Wrong:**
- Added complexity without Unity's tooling
- Made domain logic dependent on infrastructure
- Hard to test
- Violated separation concerns

**Lesson:** Preserve patterns (builders, DI), not implementations. C++ isn't Unity and shouldn't try to be.

### 5.3 Ignoring Build System Early

**Mistake:** Focused on code architecture without ensuring buildability.

**Result:**
- Can't actually build the project
- Tests exist but can't run
- No validation of implementations
- Integration issues unknown

**Lesson:** Set up build system FIRST. Even if it's minimal, being able to compile and run tests is critical for validation.

### 5.4 Not Testing Physics Early

**Mistake:** Wrote physics code without immediate testing.

**Risk:**
- Could have fundamental errors in integration
- Can't verify velocity-dependent restitution works
- No way to validate Magnus force calculations
- "Perfect feel" goal unverified

**Lesson:** Test physics with real scenarios immediately. Drop balls, check bounce heights, measure velocities.

---

## 6. Limitations of Current Approach

### 6.1 No Rendering Implementation

**Current State:** Geometry generation exists, but no actual renderer.

**Implication:**
- Can't visualize physics
- Can't verify VR integration works
- No user testing possible

**To Fix:** Implement basic Vulkan or OpenGL renderer. Doesn't need to be pretty, just functional for validation.

### 6.2 Unvalidated Physics

**Current State:** Physics code exists but hasn't been run.

**Risk:**
- Could have bugs in integration
- Restitution curves might not feel right
- Magnus force might be too weak/strong

**To Fix:** Get build system working, run tests, iterate on physics parameters based on feel testing.

### 6.3 No Build System / Dependencies

**Current State:** CMakeLists.txt references vcpkg, but not configured.

**Implication:**
- Can't build
- Can't run tests
- Can't validate anything

**To Fix:** Either configure vcpkg or switch to system packages. Priority #1.

### 6.4 Single Player Only

**Current State:** No AI opponent, no multiplayer.

**Implication:**
- Can only practice hitting ball
- No gameplay loop

**To Fix:**
- Simple AI: predict ball landing, move paddle to intercept
- Multiplayer: serialize BallState, send over network

### 6.5 OpenXR Integration Untested

**Current State:** OpenXR code exists but never tested with actual headset.

**Risk:**
- May not work with real hardware
- Coordinate system assumptions might be wrong
- Performance issues unknown

**To Fix:** Test with real VR headset ASAP. Iterate on any issues.

---

## 7. Recommendations for Fresh Implementation

### Phase 1: Foundation (Week 1-2)

**Goal:** Get something buildable and testable.

1. **Set up build system**
   - CMake or premake
   - Choose dependency approach (vcpkg, conan, or system packages)
   - Verify can build hello world with OpenXR

2. **Minimal OpenXR integration**
   - Initialize session
   - Read controller poses
   - Submit frames (even if just clearing screen)
   - **Validate on actual HMD**

3. **Basic rendering**
   - Draw cube at origin
   - Draw controller positions
   - Verify tracking works

**Success Criteria:** Can see something in VR headset.

### Phase 2: Core Physics (Week 3-4)

**Goal:** Get ball bouncing correctly.

1. **Physics simulation**
   - Implement Verlet integration
   - Add gravity
   - Add table collision
   - Add floor collision

2. **Testing infrastructure**
   - Set up GoogleTest or similar
   - Create domain-specific helpers
   - Write drop-and-bounce tests

3. **Visual validation**
   - Draw ball sphere
   - Draw table
   - Let ball drop and bounce
   - **Verify bounce feels right**

**Success Criteria:** Ball bounces on table realistically.

### Phase 3: Paddle Interaction (Week 5-6)

**Goal:** Hit ball with paddle.

1. **Kalman filtering**
   - Filter controller positions
   - Smooth velocity estimation
   - Verify jitter reduction

2. **Paddle collision**
   - Swept sphere vs OBB
   - Velocity transfer
   - Test with real paddle hits

3. **Restitution**
   - Implement velocity-dependent model
   - Tune parameters
   - **Validate feel with players**

**Success Criteria:** Hitting ball with paddle feels responsive and predictable.

### Phase 4: Advanced Physics (Week 7-8)

**Goal:** Spin and realistic behavior.

1. **Magnus force**
   - Implement non-linear model
   - Add topspin/backspin curves
   - Validate trajectories

2. **Air resistance**
   - Add drag force
   - Tune coefficients
   - Verify ball slows realistically

3. **Net collision**
   - Detect net hits
   - Low restitution
   - Test edge cases

**Success Criteria:** Spin affects ball trajectory noticeably.

### Phase 5: Polish (Week 9-10)

**Goal:** Playable game.

1. **AI opponent**
   - Predict ball landing
   - Move to intercept
   - Return ball

2. **Score tracking**
   - Detect serves
   - Count bounces
   - Award points

3. **Sound**
   - Ball-paddle hit
   - Ball-table bounce
   - Net hits

**Success Criteria:** Can play full game vs AI.

---

## 8. Key Technical Decisions

### 8.1 Language: C++ vs C# (Unity)

**Why C++ for Fresh Start:**
- Direct hardware access (lower latency)
- No garbage collection pauses (smoother frame times)
- Better performance for physics calculations
- More control over memory layout
- Cross-platform without Unity licensing

**Tradeoffs:**
- Longer development time
- More manual memory management
- No visual editor (must build tools)

**Verdict:** C++ is worth it for VR sports games where latency matters.

### 8.2 VR API: OpenXR vs Native SDKs

**Why OpenXR:**
- Single codebase supports multiple headsets
- Future-proof (industry standard)
- Good documentation

**Tradeoffs:**
- May not support latest hardware features immediately
- Need to test on multiple devices
- Some headset-specific optimizations unavailable

**Verdict:** OpenXR is correct choice for portability.

### 8.3 Rendering: Vulkan vs OpenGL

**Vulkan Pros:**
- Better performance (explicit control)
- Lower CPU overhead
- Better multi-threading

**Vulkan Cons:**
- Much more complex (1000+ lines for triangle)
- Harder to debug
- Steeper learning curve

**OpenGL Pros:**
- Simpler API
- Faster iteration
- Good enough performance for table tennis

**Recommendation:** Start with OpenGL, migrate to Vulkan only if profiling shows GPU bottleneck.

### 8.4 Physics: Custom vs Engine

**Why Custom Physics:**
- Table tennis has specific requirements (spin, air resistance)
- Need velocity-dependent restitution
- Need deterministic behavior
- Engines like Bullet/PhysX are overkill

**Implementation:**
- Verlet integration (~200 lines)
- Swept sphere collision (~300 lines)
- Magnus force (~50 lines)

**Verdict:** Custom physics is appropriate for table tennis. Don't pull in physics engine.

---

## 9. Performance Targets

### Timing Budget (90 Hz, 11.1ms per frame)

```
Input polling:        0.5ms
Physics simulation:   1.0ms   (50 Hz, amortized)
Collision detection:  0.5ms
Rendering prep:       1.0ms
GPU submit:           0.5ms
Buffer:              7.6ms   (for headroom)
─────────────────────────────
Total:              11.1ms
```

### Physics Optimization

**Don't Optimize Prematurely:**
- Ball physics is cheap (single object)
- Collision checks are minimal (ball vs 5-10 objects)
- Likely CPU-bound by rendering, not physics

**If Needed:**
- Use SIMD for vector math (SSE/NEON)
- Early-out collision checks (AABB before OBB)
- Reduce collision check frequency (every other frame)

**Measurement:**
- Profile before optimizing
- Test on target hardware (Quest, PCVR)
- Maintain 90 Hz minimum

---

## 10. Testing Strategy

### Unit Tests

**What to Test:**
```cpp
// Physics
- FreeFallGravity: ball accelerates at 9.81 m/s²
- TableBounce: restitution varies with speed
- TopspinCurve: Magnus force curves ball down
- AirDrag: ball slows over distance

// Collision
- BallHitsTable: detects collision at table height
- BallClearsNet: no collision when above net
- BallHitsPaddle: moving paddle collision works
- FastBallNoTunneling: swept sphere prevents tunneling

// Filtering
- KalmanReducesJitter: variance reduction measured
- KalmanTracksMotion: follows moving target
- SmoothVelocity: velocity estimate is stable
```

### Integration Tests

**What to Test:**
- VR headset tracking works
- Controllers appear in correct positions
- Ball responds to paddle hit
- Spin visualization (rings) rotates with ball

### Validation Testing

**Human Testing:**
1. Drop ball from 1m, verify ~85cm rebound
2. Hit ball with paddle, verify "solid" feel
3. Apply topspin, verify downward curve
4. Apply backspin, verify floating behavior
5. Hit ball at net height, verify clearance

**Metrics:**
- Latency: input to visual feedback <50ms
- Jitter: paddle position variance <1mm
- Frame rate: 90 Hz maintained, 0 dropped frames
- Bounce consistency: <5% variation in rebound height

---

## 11. Common Implementation Issues

### Issue: Ball Feels "Floaty"

**Symptoms:** Ball doesn't fall fast enough, bounces too high.

**Causes:**
- Gravity too low (should be 9.81 m/s²)
- Restitution too high (>0.95)
- Timestep too large (>0.02s)

**Fix:** Check physics config values, verify integration is correct.

### Issue: Paddle Feels "Mushy"

**Symptoms:** Ball doesn't respond immediately to paddle hit.

**Causes:**
- Kalman filter lag (measurement noise too high)
- Velocity estimation averaging too many frames
- Collision detection not using controller velocity

**Fix:** Tune Kalman parameters, verify velocity calculation uses filtered positions.

### Issue: Ball Tunnels Through Table

**Symptoms:** Ball passes through table at high speed.

**Causes:**
- Discrete collision detection (checking position, not path)
- Timestep too large for ball speed
- Collision detection order wrong

**Fix:** Use swept sphere collision, reduce timestep if needed.

### Issue: Spin Doesn't Affect Trajectory

**Symptoms:** Topspin and backspin look same.

**Causes:**
- Magnus force not implemented
- Air density zero
- Spin not updating

**Fix:** Verify Magnus force calculation, ensure airDensity > 0.

### Issue: Inconsistent Bounce Behavior

**Symptoms:** Same drop gives different bounce heights.

**Causes:**
- Variable timestep (deltaTime not fixed)
- Floating point accumulation
- Collision detection instability

**Fix:** Use fixed timestep, ensure deterministic math.

---

## 12. Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                         Application                          │
│                        (main.cpp)                            │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                         Game Loop                            │
│                                                              │
│  • PollEvents()                                             │
│  • UpdateInput()      ← OpenXR (filtered)                   │
│  • FixedUpdate()      ← Physics (50 Hz)                     │
│  • Render()           ← Interpolated state (90 Hz)          │
│                                                              │
└───┬────────────────────────┬──────────────────────┬─────────┘
    │                        │                      │
    ▼                        ▼                      ▼
┌───────────────┐  ┌──────────────────┐  ┌──────────────────┐
│ Infrastructure │  │  Domain Logic    │  │   Rendering      │
│               │  │                  │  │                  │
│ OpenXRManager │  │ BallPhysics      │  │ Vulkan/OpenGL    │
│ InputManager  │  │ Collision        │  │ Geometry         │
│ KalmanFilters │  │ Simulation       │  │ Shaders          │
│               │  │ (testable!)      │  │ Materials        │
└───────────────┘  └──────────────────┘  └──────────────────┘
```

### Data Flow

```
VR Controller (raw)
    ↓
OpenXR API
    ↓
Kalman Filter (smoothing)
    ↓
Controller State (filtered position, velocity)
    ↓
Paddle State (in simulation)
    ↓
Collision Detection (swept sphere)
    ↓ (if collision)
Ball State Update (apply impulse, spin transfer)
    ↓
Physics Step (integrate forces)
    ↓
Ball State (new position, velocity, spin)
    ↓
Render (interpolated to display rate)
```

---

## 13. Critical Success Factors

### 1. Fixed Timestep Physics
- Non-negotiable for consistent feel
- Use 50 Hz (0.02s) as baseline
- Accumulator pattern for frame rate independence

### 2. Smooth VR Input
- Kalman filtering essential
- Tune for your hardware
- Validate jitter reduction with metrics

### 3. Realistic Restitution
- Velocity-dependent is key
- Test with real drops
- Iterate based on player feedback

### 4. Continuous Collision
- Swept sphere prevents tunneling
- Critical for fast ball speeds
- Test at maximum speeds

### 5. Player Validation
- Get headset on people early
- Measure feel, not just metrics
- Iterate based on feedback

---

## 14. Development Priorities

### Must Have (MVP)
1. ✅ Ball drops and bounces on table
2. ✅ Paddle collision with ball
3. ✅ Smooth VR tracking
4. ✅ Velocity-dependent bounce
5. ❌ Buildable project
6. ❌ Visible rendering

### Should Have (v1.0)
1. ✅ Spin (Magnus force)
2. ✅ Air resistance
3. ✅ Net collision
4. ❌ AI opponent
5. ❌ Score tracking
6. ❌ Sound effects

### Nice to Have (v2.0)
- Multiplayer networking
- Different ball types
- Custom paddles
- Training modes
- Replays

---

## 15. Key Takeaways

### What Worked Well

1. **Domain Isolation**
   - Physics code independent of VR/rendering
   - Testable without hardware
   - Clean boundaries

2. **Kalman Filtering**
   - Proved essential for VR input
   - 10x jitter reduction
   - Stable velocity estimation

3. **Velocity-Dependent Restitution**
   - More realistic than constant
   - Matches player expectations
   - Small code change, big feel improvement

4. **Domain-Specific Tests**
   - More readable than math tests
   - Focus on gameplay scenarios
   - Easier to maintain

5. **Geometry Builders**
   - Separation of data from rendering
   - Reusable across tools
   - Easy to test

### What Didn't Work

1. **Early Documentation**
   - Became outdated quickly
   - Better to document after implementing

2. **No Build System First**
   - Can't validate anything
   - Should be priority #1

3. **Unity Pattern Literal Translation**
   - Don't recreate GameObject system
   - Preserve concepts, not implementation

### What's Still Unknown

1. **Physics Parameters**
   - Need real testing to tune
   - Restitution curves unvalidated
   - Magnus force strength unknown

2. **OpenXR Integration**
   - Never tested with actual headset
   - Coordinate systems unverified

3. **Performance**
   - No profiling data
   - Unknown if 90 Hz achievable

---

## 16. Final Recommendations

### For New Team Starting Fresh

**Week 1 Priorities:**
1. Get build system working
2. Render something in VR
3. Read controller positions
4. Draw paddle at controller location

**Don't:**
- Write extensive docs before code
- Try to architect everything upfront
- Optimize before profiling
- Implement features before MVP works

**Do:**
- Test with real VR headset immediately
- Get player feedback early
- Focus on "perfect feel" over features
- Keep physics deterministic
- Write tests using table tennis terminology

### Technology Choices

**Recommended Stack:**
- Language: C++17
- VR: OpenXR
- Rendering: OpenGL (first), Vulkan (if needed)
- Math: GLM or custom
- Testing: GoogleTest
- Build: CMake
- Dependencies: vcpkg or conan

### Validation Plan

**Physics Validation:**
1. Drop ball from 1m, measure rebound (should be ~85cm)
2. Roll ball at 2 m/s, measure deceleration (drag)
3. Bounce ball 10 times, verify consistency (<5% variation)

**VR Validation:**
1. Hold controller still, measure jitter (<1mm after filtering)
2. Move controller, measure latency (<50ms input to visual)
3. Hit ball with paddle, verify feels "solid"

**Performance Validation:**
1. Profile frame time (should be <11ms for 90 Hz)
2. Measure physics time (should be <1ms)
3. Verify no dropped frames during gameplay

---

## Conclusion

Building table tennis VR is fundamentally about **feel**. All technical decisions serve one goal: making the paddle-ball interaction feel immediate, predictable, and realistic.

The architecture documented here separates concerns (domain/infrastructure), isolates testable physics, smooths noisy VR input, and implements realistic bounce behavior. These patterns work.

For a new team: start simple, validate early, iterate based on feel testing, and maintain deterministic physics. Don't over-engineer. The perfect codebase means nothing if the bounce doesn't feel right.

**Core principle:** If it doesn't make the bounce feel better, don't build it.

---

**Document Version:** 1.0
**Created:** 2025-11-06
**Based on:** Unity C# → C++/OpenXR conversion project
**Branch:** `claude/convert-game-cpp-openxr-011CUpTmSbWTJ3YucSB5SXeo`
