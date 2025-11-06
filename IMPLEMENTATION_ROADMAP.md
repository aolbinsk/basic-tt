# Implementation Roadmap - Achieving "Perfect Bounce Feel"

## Priority Matrix

```
HIGH IMPACT, HIGH PRIORITY:
┌─────────────────────────────────────────┐
│ 1. Input Filtering (Kalman/Moving Avg) │
│ 2. Velocity-Dependent Restitution      │
│ 3. Haptic Feedback                     │
│ 4. Logging & Profiling                 │
└─────────────────────────────────────────┘

MEDIUM IMPACT, HIGH PRIORITY:
┌─────────────────────────────────────────┐
│ 5. Unit Tests                          │
│ 6. Configuration System                │
│ 7. Improved Magnus Force               │
└─────────────────────────────────────────┘

HIGH IMPACT, MEDIUM PRIORITY:
┌─────────────────────────────────────────┐
│ 8. Contact Duration Modeling           │
│ 9. Rubber Properties System            │
│ 10. Paddle Prediction                  │
└─────────────────────────────────────────┘

LOWER PRIORITY:
┌─────────────────────────────────────────┐
│ 11. Audio System                       │
│ 12. Menu/UI System                     │
│ 13. Score Tracking                     │
│ 14. Spin Visualization                 │
└─────────────────────────────────────────┘
```

---

## Phase 1: Critical Foundation (Week 1-2)

### 1. Input Filtering - CRITICAL for Feel

**Problem:** VR controller tracking is noisy. Without filtering, paddles jitter and velocity estimation is unreliable, making collision feel unpredictable.

**Solution:** Implement Kalman filter from Unity version.

#### File: `include/domain/filters/KalmanFilterVector3.h`

```cpp
#pragma once

#include "../utilities/Vector3.h"

namespace BasicTT {

class KalmanFilterVector3 {
public:
    KalmanFilterVector3(float processNoise = 1e-5f, float measurementNoise = 1e-2f)
        : m_processNoise(processNoise)
        , m_measurementNoise(measurementNoise) {
        Reset();
    }

    Vector3 Update(const Vector3& measurement) {
        // Prediction phase - uncertainty increases
        m_errorCovariance.x += m_processNoise;
        m_errorCovariance.y += m_processNoise;
        m_errorCovariance.z += m_processNoise;

        // Kalman gain - how much to trust new measurement vs estimate
        Vector3 kalmanGain(
            m_errorCovariance.x / (m_errorCovariance.x + m_measurementNoise),
            m_errorCovariance.y / (m_errorCovariance.y + m_measurementNoise),
            m_errorCovariance.z / (m_errorCovariance.z + m_measurementNoise)
        );

        // Update estimate with measurement
        m_stateEstimate.x += kalmanGain.x * (measurement.x - m_stateEstimate.x);
        m_stateEstimate.y += kalmanGain.y * (measurement.y - m_stateEstimate.y);
        m_stateEstimate.z += kalmanGain.z * (measurement.z - m_stateEstimate.z);

        // Update error covariance
        m_errorCovariance.x *= (1.0f - kalmanGain.x);
        m_errorCovariance.y *= (1.0f - kalmanGain.y);
        m_errorCovariance.z *= (1.0f - kalmanGain.z);

        return m_stateEstimate;
    }

    void Reset() {
        m_stateEstimate = Vector3::Zero();
        m_errorCovariance = Vector3::One();
    }

    const Vector3& GetEstimate() const { return m_stateEstimate; }

private:
    Vector3 m_stateEstimate;
    Vector3 m_errorCovariance;
    float m_processNoise;
    float m_measurementNoise;
};

} // namespace BasicTT
```

#### Usage in OpenXRInputManager:

```cpp
// In OpenXRInputManager.h:
class OpenXRInputManager {
    // Add filter members:
    KalmanFilterVector3 m_leftPosFilter;
    KalmanFilterVector3 m_rightPosFilter;

    // For velocity estimation:
    CircularBuffer<ControllerState> m_leftHistory;
    CircularBuffer<ControllerState> m_rightHistory;
};

// In UpdateController():
void OpenXRInputManager::UpdateController(ControllerHand hand, XrTime displayTime, XrSpace playSpace) {
    // ... existing position reading code ...

    // RAW position from OpenXR:
    Vector3 rawPosition(location.pose.position.x, location.pose.position.y, location.pose.position.z);

    // FILTERED position:
    KalmanFilterVector3& filter = (hand == ControllerHand::Left) ? m_leftPosFilter : m_rightPosFilter;
    controller.position = filter.Update(rawPosition);

    // Store in history
    CircularBuffer<ControllerState>& history = (hand == ControllerHand::Left) ? m_leftHistory : m_rightHistory;
    history.Push(controller);

    // Calculate SMOOTH velocity from filtered history
    if (history.Size() >= 2) {
        const ControllerState& prev = history.Get(history.Size() - 2);
        float dt = controller.timestamp - prev.timestamp;
        if (dt > 0.001f) {  // At least 1ms
            controller.velocity = (controller.position - prev.position) / dt;
        }
    }
}
```

**Impact:** ⭐⭐⭐⭐⭐ (Massive improvement to feel)

---

### 2. Velocity-Dependent Restitution - CRITICAL for Realism

**Problem:** Constant restitution (0.89) doesn't match real ball physics. Fast hits feel the same as slow hits.

#### File: `include/domain/physics/RestitutionModel.h`

```cpp
#pragma once

#include <cmath>
#include <algorithm>

namespace BasicTT {

class RestitutionModel {
public:
    // Calculate restitution based on impact velocity
    static float CalculateBallTableRestitution(float impactSpeed) {
        // Based on experimental data for table tennis balls on wood
        // Low speed: more elastic (0.92)
        // High speed: more energy loss due to deformation (0.85)

        if (impactSpeed < 1.0f) {
            // Very low speed: nearly perfectly elastic
            return 0.92f - (0.02f * impactSpeed);
        } else if (impactSpeed < 5.0f) {
            // Medium speed: standard play
            return 0.89f - (0.01f * (impactSpeed - 1.0f));
        } else {
            // High speed: compression losses
            float speedFactor = std::min(impactSpeed - 5.0f, 10.0f);
            return 0.85f - (0.005f * speedFactor);
        }
    }

    static float CalculateBallPaddleRestitution(float impactSpeed, float rubberHardness) {
        // Softer rubber (lower hardness) = more energy absorption
        // Harder rubber = faster

        float baseRestitution = 0.82f + (rubberHardness * 0.05f);  // 0.82-0.87

        // Very fast hits lose more energy
        if (impactSpeed > 8.0f) {
            float penalty = (impactSpeed - 8.0f) * 0.01f;
            baseRestitution -= std::min(penalty, 0.08f);
        }

        return baseRestitution;
    }

    static float CalculateBallNetRestitution(float impactSpeed) {
        // Net is more damping than table
        return std::max(0.25f, 0.35f - impactSpeed * 0.02f);
    }

    static float CalculateBallFloorRestitution(float impactSpeed) {
        // Floor similar to table but slightly less elastic
        return std::max(0.70f, 0.82f - impactSpeed * 0.02f);
    }
};

} // namespace BasicTT
```

#### Usage in CollisionResolutionSystem:

```cpp
// In ResolvePaddleCollision():
float impactSpeed = collision.relativeVelocity.Magnitude();
float paddleRestitution = RestitutionModel::CalculateBallPaddleRestitution(
    impactSpeed, m_paddleConfig.rubberHardness);

// In ResolveStaticCollision():
float impactSpeed = std::abs(vn);
float restitution;

switch (collision.type) {
    case CollisionType::Table:
        restitution = RestitutionModel::CalculateBallTableRestitution(impactSpeed);
        break;
    case CollisionType::Net:
        restitution = RestitutionModel::CalculateBallNetRestitution(impactSpeed);
        break;
    case CollisionType::Floor:
        restitution = RestitutionModel::CalculateBallFloorRestitution(impactSpeed);
        break;
    default:
        restitution = 0.8f;
}
```

**Impact:** ⭐⭐⭐⭐⭐ (Essential for realistic bounce)

---

### 3. Haptic Feedback - CRITICAL for VR Feel

**Problem:** No tactile feedback when hitting the ball. Players don't "feel" the contact.

#### Add to OpenXRInputManager.h:

```cpp
class OpenXRInputManager {
public:
    // Add haptic feedback support
    void TriggerHapticPulse(ControllerHand hand, float amplitude, float duration);

private:
    XrAction m_hapticAction = XR_NULL_HANDLE;
    XrPath m_leftHandPath;
    XrPath m_rightHandPath;
};
```

#### In OpenXRInputManager.cpp:

```cpp
bool OpenXRInputManager::CreateActions() {
    // ... existing code ...

    // Create haptic feedback action
    XrActionCreateInfo hapticInfo = {XR_TYPE_ACTION_CREATE_INFO};
    hapticInfo.actionType = XR_ACTION_TYPE_VIBRATION_OUTPUT;
    std::strncpy(hapticInfo.actionName, "haptic_feedback", XR_MAX_ACTION_NAME_SIZE - 1);
    std::strncpy(hapticInfo.localizedActionName, "Haptic Feedback", XR_MAX_LOCALIZED_ACTION_NAME_SIZE - 1);

    XrPath handPaths[2];
    xrStringToPath(m_instance, "/user/hand/left", &handPaths[0]);
    xrStringToPath(m_instance, "/user/hand/right", &handPaths[1]);
    hapticInfo.countSubactionPaths = 2;
    hapticInfo.subactionPaths = handPaths;

    XrResult result = xrCreateAction(m_actionSet, &hapticInfo, &m_hapticAction);
    if (XR_FAILED(result)) {
        std::cerr << "Failed to create haptic action" << std::endl;
        return false;
    }

    // Store paths for later use
    m_leftHandPath = handPaths[0];
    m_rightHandPath = handPaths[1];

    return true;
}

bool OpenXRInputManager::SuggestBindings() {
    // ... existing bindings ...

    // Add haptic bindings
    XrPath path;
    xrStringToPath(m_instance, "/user/hand/left/output/haptic", &path);
    bindings.push_back({m_hapticAction, path});

    xrStringToPath(m_instance, "/user/hand/right/output/haptic", &path);
    bindings.push_back({m_hapticAction, path});

    // ... rest of function ...
}

void OpenXRInputManager::TriggerHapticPulse(ControllerHand hand, float amplitude, float duration) {
    if (m_hapticAction == XR_NULL_HANDLE) return;

    // Clamp amplitude 0-1
    amplitude = std::max(0.0f, std::min(1.0f, amplitude));

    XrHapticVibration vibration{XR_TYPE_HAPTIC_VIBRATION};
    vibration.amplitude = amplitude;
    vibration.duration = static_cast<XrDuration>(duration * 1e9f);  // Convert seconds to nanoseconds
    vibration.frequency = XR_FREQUENCY_UNSPECIFIED;

    XrHapticActionInfo hapticInfo{XR_TYPE_HAPTIC_ACTION_INFO};
    hapticInfo.action = m_hapticAction;
    hapticInfo.subactionPath = (hand == ControllerHand::Left) ? m_leftHandPath : m_rightHandPath;

    XrResult result = xrApplyHapticFeedback(m_session, &hapticInfo,
                                           (XrHapticBaseHeader*)&vibration);
    if (XR_FAILED(result)) {
        // Non-critical failure
    }
}
```

#### Usage in TableTennisSimulation:

```cpp
// In ResolveCollisions(), after collision resolution:
void TableTennisSimulation::ResolveCollisions() {
    if (!m_lastCollision.hasCollision) return;

    // ... existing collision resolution ...

    // HAPTIC FEEDBACK
    if (m_lastCollision.type == CollisionType::Paddle) {
        // Calculate haptic intensity based on impact
        float impactSpeed = m_lastCollision.relativeVelocity.Magnitude();
        float intensity = std::min(impactSpeed / 15.0f, 1.0f);  // 15 m/s = max

        // Longer pulse for harder hits
        float duration = 0.01f + (intensity * 0.03f);  // 10-40ms

        // Determine which hand
        ControllerHand hand = (m_lastCollision.collisionPoint.x < 0) ?
                              ControllerHand::Left : ControllerHand::Right;

        // Trigger haptics (would need to pass input manager or use event system)
        // m_inputManager->TriggerHapticPulse(hand, intensity, duration);
    }
}
```

**Note:** You'll need to pass OpenXRInputManager to the simulation, or use an event/callback system.

**Impact:** ⭐⭐⭐⭐⭐ (Essential for VR immersion)

---

### 4. Logging System

**Problem:** Debug prints everywhere, can't control verbosity, no file output.

#### Add vcpkg.json:

```json
{
  "name": "basictt-openxr",
  "version": "0.1.0",
  "dependencies": [
    "openxr-loader",
    "spdlog",
    "fmt"
  ]
}
```

#### Update CMakeLists.txt:

```cmake
# After find_package(OpenXR REQUIRED)
find_package(spdlog CONFIG REQUIRED)

# In target_link_libraries:
target_link_libraries(${PROJECT_NAME}
    PRIVATE
    OpenXR::openxr_loader
    spdlog::spdlog
    # ... other libs ...
)
```

#### Create: `include/infrastructure/utilities/Logger.h`

```cpp
#pragma once

#include <spdlog/spdlog.h>
#include <spdlog/sinks/stdout_color_sinks.h>
#include <spdlog/sinks/basic_file_sink.h>
#include <memory>

namespace BasicTT {

class Logger {
public:
    static void Initialize() {
        auto console_sink = std::make_shared<spdlog::sinks::stdout_color_sink_mt>();
        auto file_sink = std::make_shared<spdlog::sinks::basic_file_sink_mt>("basictt.log", true);

        std::vector<spdlog::sink_ptr> sinks{console_sink, file_sink};
        auto logger = std::make_shared<spdlog::logger>("BasicTT", sinks.begin(), sinks.end());

        logger->set_level(spdlog::level::debug);
        logger->set_pattern("[%H:%M:%S.%e] [%^%l%$] [%s:%#] %v");

        spdlog::set_default_logger(logger);

        SPDLOG_INFO("Logger initialized");
    }

    static void Shutdown() {
        spdlog::shutdown();
    }
};

} // namespace BasicTT

// Convenience macros
#define LOG_TRACE(...)    SPDLOG_TRACE(__VA_ARGS__)
#define LOG_DEBUG(...)    SPDLOG_DEBUG(__VA_ARGS__)
#define LOG_INFO(...)     SPDLOG_INFO(__VA_ARGS__)
#define LOG_WARN(...)     SPDLOG_WARN(__VA_ARGS__)
#define LOG_ERROR(...)    SPDLOG_ERROR(__VA_ARGS__)
#define LOG_CRITICAL(...) SPDLOG_CRITICAL(__VA_ARGS__)
```

#### Usage:

```cpp
// In main.cpp:
int main() {
    BasicTT::Logger::Initialize();

    LOG_INFO("BasicTT starting...");

    // ... game code ...

    BasicTT::Logger::Shutdown();
}

// In physics code:
LOG_DEBUG("Ball velocity: ({}, {}, {})", vel.x, vel.y, vel.z);
LOG_WARN("High collision count: {}", collisionCount);
LOG_ERROR("Physics integration failed: {}", error);
```

**Impact:** ⭐⭐⭐⭐ (Essential for debugging)

---

## Phase 2: Enhanced Physics (Week 3-4)

### 5. Improved Magnus Force

**Current Problem:**
```cpp
Vector3 CalculateMagnusForce(const Vector3& velocity, const Vector3& spin) {
    Vector3 magnusDir = Vector3::Cross(spin, velocity);
    float magnusMagnitude = m_ballConfig.magnusCoefficient;  // CONSTANT!
    return magnusDir * magnusMagnitude;
}
```

**Improved Version:**

```cpp
Vector3 CalculateMagnusForce(const Vector3& velocity, const Vector3& spin) {
    float speed = velocity.Magnitude();
    float spinRate = spin.Magnitude();

    // No Magnus force if not moving or not spinning
    if (speed < 0.1f || spinRate < 1.0f) {
        return Vector3::Zero();
    }

    // Dimensionless spin parameter: S = (ω * r) / v
    // Where ω is spin rate, r is radius, v is velocity
    float spinParameter = (spinRate * m_ballConfig.radius) / speed;

    // Magnus coefficient varies non-linearly with spin parameter
    // Based on experimental data for spinning spheres
    // Cm increases with spin but saturates at high spin rates
    float Cm;
    if (spinParameter < 0.5f) {
        Cm = 1.0f * spinParameter;  // Linear regime
    } else if (spinParameter < 4.0f) {
        // Non-linear transition
        Cm = 0.5f * (1.0f - std::exp(-spinParameter));
    } else {
        Cm = 0.5f;  // Saturation
    }

    // Magnus force: F = Cm * (0.5 * ρ * A * v²) * (ω × v) / |ω × v|
    Vector3 magnusDir = Vector3::Cross(spin, velocity);
    float magnusDirMag = magnusDir.Magnitude();

    if (magnusDirMag < 1e-6f) return Vector3::Zero();

    magnusDir = magnusDir / magnusDirMag;  // Normalize

    // Calculate Magnus force magnitude
    float area = M_PI * m_ballConfig.radius * m_ballConfig.radius;
    float magnusMag = Cm * 0.5f * m_physicsConfig.airDensity * area * speed * speed;

    return magnusDir * magnusMag;
}
```

**Impact:** ⭐⭐⭐⭐ (Much more realistic spin curves)

---

### 6. Rubber Properties System

**Add to PaddleConfig.h:**

```cpp
enum class RubberType {
    TackyChinese,      // DHS Hurricane, very spinny
    EuropeanTensor,    // Butterfly Tenergy, fast
    JapaneseTensor,    // Dignics, balanced
    AntiSpin,          // Very low grip
    LongPips,          // Reverses spin
    ShortPips          // Less spin, more control
};

struct RubberProperties {
    RubberType type;

    // Friction coefficients
    float staticFriction;      // Initial grip
    float dynamicFriction;     // Sliding friction
    float tackiness;           // Velocity-dependent grip multiplier

    // Mechanical properties
    float hardness;            // 30-50 degrees (sponge hardness)
    float thickness;           // 1.5mm - 2.3mm (sponge thickness)

    // Spin properties
    float spinMultiplier;      // How much of paddle spin transfers to ball
    float spinRetention;       // How much incoming spin is preserved
    bool reversesIncomingSpin; // For long pips

    // Speed properties
    float restitution;         // Energy return (0.75 - 0.88)
    float speedMultiplier;     // Overall speed factor

    // Factory methods
    static RubberProperties TackyChinese() {
        return {
            .type = RubberType::TackyChinese,
            .staticFriction = 1.3f,
            .dynamicFriction = 1.1f,
            .tackiness = 1.2f,
            .hardness = 38.0f,
            .thickness = 2.2f,
            .spinMultiplier = 1.3f,
            .spinRetention = 0.95f,
            .reversesIncomingSpin = false,
            .restitution = 0.79f,
            .speedMultiplier = 0.95f
        };
    }

    static RubberProperties EuropeanTensor() {
        return {
            .type = RubberType::EuropeanTensor,
            .staticFriction = 1.0f,
            .dynamicFriction = 0.85f,
            .tackiness = 0.9f,
            .hardness = 42.0f,
            .thickness = 2.1f,
            .spinMultiplier = 1.0f,
            .spinRetention = 0.88f,
            .reversesIncomingSpin = false,
            .restitution = 0.85f,
            .speedMultiplier = 1.15f
        };
    }

    // ... other rubber types ...
};
```

**Usage in collision resolution:**

```cpp
BallState ResolvePaddleCollision(const BallState& ballState,
                                const CollisionData& collision,
                                const PaddleState& paddle) {
    const RubberProperties& rubber = m_paddleConfig.rubberProperties;

    // Velocity-dependent friction
    float relSpeed = collision.relativeVelocity.Magnitude();
    float effectiveFriction = rubber.dynamicFriction *
                             (1.0f + rubber.tackiness * std::exp(-relSpeed / 5.0f));

    // Calculate spin transfer with rubber properties
    Vector3 spinTransfer = paddle.angularVelocity * rubber.spinMultiplier;

    // Preserve incoming spin based on rubber type
    newState.spin = ballState.spin * rubber.spinRetention + spinTransfer;

    // Reverse spin for long pips
    if (rubber.reversesIncomingSpin) {
        newState.spin = -ballState.spin * 0.5f + spinTransfer * 0.3f;
    }

    // Speed adjustment
    newState.velocity *= rubber.speedMultiplier;

    return newState;
}
```

**Impact:** ⭐⭐⭐⭐ (Major gameplay variety)

---

## Phase 3: Advanced Features (Week 5-6)

### 7. Contact Duration Modeling

**Problem:** Instant collision is unrealistic. Real contacts last 2-5ms.

```cpp
class ContactModel {
public:
    struct ContactState {
        bool inContact;
        float contactTime;
        float targetDuration;
        Vector3 contactPoint;
        Vector3 contactNormal;
        float initialPenetration;
        float compressionPhase;  // 0-1, 0=start, 1=max compression
    };

    ContactState BeginContact(const CollisionData& collision, float impactSpeed) {
        ContactState state;
        state.inContact = true;
        state.contactTime = 0.0f;

        // Contact duration depends on impact speed and rubber softness
        // Faster hits = shorter contact
        state.targetDuration = 0.003f - (impactSpeed * 0.0001f);  // 2-4ms
        state.targetDuration = std::max(0.002f, std::min(0.005f, state.targetDuration));

        state.contactPoint = collision.collisionPoint;
        state.contactNormal = collision.collisionNormal;
        state.initialPenetration = collision.penetrationDepth;
        state.compressionPhase = 0.0f;

        return state;
    }

    Vector3 CalculateContactForce(ContactState& state, float dt,
                                  const BallState& ball, const PaddleState& paddle) {
        state.contactTime += dt;
        state.compressionPhase = state.contactTime / state.targetDuration;

        if (state.compressionPhase >= 1.0f) {
            state.inContact = false;
            return Vector3::Zero();
        }

        // Contact force follows a smooth curve
        // Peak force at 50% compression
        float forceCurve = std::sin(state.compressionPhase * M_PI);

        // Calculate spring-damper force
        float stiffness = 10000.0f;  // N/m
        float damping = 50.0f;        // N·s/m

        Vector3 relVel = ball.velocity - paddle.velocity;
        float normalVel = Vector3::Dot(relVel, state.contactNormal);

        float springForce = stiffness * state.initialPenetration * forceCurve;
        float dampingForce = damping * normalVel;

        return state.contactNormal * (springForce + dampingForce);
    }
};
```

**Impact:** ⭐⭐⭐⭐ (Much more realistic feel)

---

## Testing Strategy

### Essential Unit Tests

```cpp
// tests/physics/PhysicsEngineTest.cpp
#include <gtest/gtest.h>
#include "domain/physics/BallPhysicsOptimizedVerlet.h"

TEST(BallPhysicsVerlet, FreeFall) {
    BallConfig ballCfg;
    PhysicsConfig physicsCfg;
    BallPhysicsOptimizedVerlet engine(ballCfg, physicsCfg);

    // Ball starting at 1m height, zero velocity
    BallState initial;
    initial.position = Vector3(0, 1, 0);
    initial.velocity = Vector3::Zero();

    // Simulate 1 second of free fall
    BallState result = initial;
    for (int i = 0; i < 50; i++) {
        result = engine.Step(result, 0.02f);
    }

    // After 1s: y = 1 - 0.5*9.81*1² ≈ -3.905m
    EXPECT_NEAR(result.position.y, -3.905, 0.1);
}

TEST(RestitutionModel, VelocityDependence) {
    float lowSpeed = RestitutionModel::CalculateBallTableRestitution(0.5f);
    float midSpeed = RestitutionModel::CalculateBallTableRestitution(3.0f);
    float highSpeed = RestitutionModel::CalculateBallTableRestitution(8.0f);

    EXPECT_GT(lowSpeed, midSpeed);
    EXPECT_GT(midSpeed, highSpeed);
    EXPECT_NEAR(lowSpeed, 0.91, 0.02);
}

TEST(KalmanFilter, ReducesNoise) {
    KalmanFilterVector3 filter;

    Vector3 truePos(1.0f, 2.0f, 3.0f);

    // Add noise to measurements
    std::vector<Vector3> noisyMeasurements;
    for (int i = 0; i < 100; i++) {
        float noise = (rand() % 100 - 50) * 0.001f;  // ±0.05m noise
        noisyMeasurements.push_back(truePos + Vector3(noise, noise, noise));
    }

    // Filter should converge to true position
    Vector3 filtered;
    for (const auto& meas : noisyMeasurements) {
        filtered = filter.Update(meas);
    }

    EXPECT_NEAR(filtered.x, truePos.x, 0.01);
    EXPECT_NEAR(filtered.y, truePos.y, 0.01);
    EXPECT_NEAR(filtered.z, truePos.z, 0.01);
}
```

---

## Implementation Timeline

| Week | Focus | Deliverables |
|------|-------|-------------|
| 1 | Foundation | Logging, basic tests, config system |
| 2 | Input Quality | Kalman filter, velocity estimation |
| 3 | Physics Accuracy | Velocity-dependent restitution, improved Magnus |
| 4 | VR Polish | Haptic feedback, paddle prediction |
| 5 | Feel Polish | Rubber properties, contact modeling |
| 6 | Game Features | Score tracking, menu system, audio |

---

## Success Metrics

After implementation, the game should achieve:

- ✅ **Jitter-free paddle tracking** (Kalman filter working)
- ✅ **Realistic bounce heights** (velocity-dependent restitution)
- ✅ **Visible spin effects** (improved Magnus force)
- ✅ **Tactile feedback** (haptics on every hit)
- ✅ **Smooth 90+ FPS** (proper optimization)
- ✅ **<20ms latency** (prediction working)
- ✅ **No physics tunneling** (swept collision working)

**The "perfect bounce feel" comes from the combination of:**
1. Smooth input (filtering)
2. Correct physics (velocity-dependent restitution)
3. Good feedback (haptics + audio)
4. Low latency (prediction)
5. Proper contact modeling

Each piece alone isn't enough - you need them ALL working together!
