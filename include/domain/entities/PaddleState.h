#pragma once

#include "../utilities/Vector3.h"
#include "../utilities/Quaternion.h"

namespace BasicTT {

struct PaddleState {
    Vector3 position;           // Position in world space (meters)
    Quaternion rotation;        // Orientation as quaternion
    Vector3 velocity;           // Linear velocity (m/s)
    Vector3 angularVelocity;    // Angular velocity (rad/s)
    float timestamp;            // Time of this state (seconds)

    // Paddle dimensions (meters)
    float bladeWidth;
    float bladeHeight;
    float bladeThickness;
    float handleLength;
    float handleRadius;

    PaddleState()
        : position(Vector3::Zero()),
          rotation(Quaternion::Identity()),
          velocity(Vector3::Zero()),
          angularVelocity(Vector3::Zero()),
          timestamp(0.0f),
          bladeWidth(0.15f),
          bladeHeight(0.15f),
          bladeThickness(0.01f),
          handleLength(0.1f),
          handleRadius(0.015f) {}

    // Get forward direction (blade normal)
    Vector3 GetForward() const {
        return rotation * Vector3::Forward();
    }

    // Get right direction
    Vector3 GetRight() const {
        return rotation * Vector3::Right();
    }

    // Get up direction
    Vector3 GetUp() const {
        return rotation * Vector3::Up();
    }

    // Copy constructor
    PaddleState(const PaddleState& other) = default;
    PaddleState& operator=(const PaddleState& other) = default;
};

} // namespace BasicTT
