#pragma once

#include "../utilities/Vector3.h"

namespace BasicTT {

struct BallState {
    Vector3 position;          // Position in world space (meters)
    Vector3 velocity;          // Linear velocity (m/s)
    Vector3 angularVelocity;   // Angular velocity (rad/s)
    Vector3 spin;              // Spin axis and magnitude (rad/s)
    float timestamp;           // Time of this state (seconds)

    BallState()
        : position(Vector3::Zero()),
          velocity(Vector3::Zero()),
          angularVelocity(Vector3::Zero()),
          spin(Vector3::Zero()),
          timestamp(0.0f) {}

    BallState(const Vector3& pos, const Vector3& vel, const Vector3& angVel, const Vector3& spin, float time)
        : position(pos),
          velocity(vel),
          angularVelocity(angVel),
          spin(spin),
          timestamp(time) {}

    // Copy constructor
    BallState(const BallState& other) = default;
    BallState& operator=(const BallState& other) = default;
};

} // namespace BasicTT
