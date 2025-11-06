#pragma once

#include "../utilities/Vector3.h"
#include "../utilities/Quaternion.h"

namespace BasicTT {

enum class ControllerHand {
    Left,
    Right
};

struct ControllerState {
    ControllerHand hand;
    Vector3 position;           // Position in world space (meters)
    Quaternion rotation;        // Orientation as quaternion
    Vector3 velocity;           // Linear velocity (m/s) - derived from position delta
    Vector3 angularVelocity;    // Angular velocity (rad/s) - derived from rotation delta
    float gripValue;            // Grip button value (0-1)
    float triggerValue;         // Trigger button value (0-1)
    bool isActive;              // Controller is tracked and active
    float timestamp;            // Time of this state (seconds)

    ControllerState()
        : hand(ControllerHand::Right),
          position(Vector3::Zero()),
          rotation(Quaternion::Identity()),
          velocity(Vector3::Zero()),
          angularVelocity(Vector3::Zero()),
          gripValue(0.0f),
          triggerValue(0.0f),
          isActive(false),
          timestamp(0.0f) {}

    ControllerState(ControllerHand h)
        : hand(h),
          position(Vector3::Zero()),
          rotation(Quaternion::Identity()),
          velocity(Vector3::Zero()),
          angularVelocity(Vector3::Zero()),
          gripValue(0.0f),
          triggerValue(0.0f),
          isActive(false),
          timestamp(0.0f) {}

    // Copy constructor
    ControllerState(const ControllerState& other) = default;
    ControllerState& operator=(const ControllerState& other) = default;
};

} // namespace BasicTT
