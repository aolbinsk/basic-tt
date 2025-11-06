#pragma once

#include "../entities/BallState.h"
#include "../config/BallConfig.h"
#include "../config/PhysicsConfig.h"

namespace BasicTT {

// Interface for ball physics integration
class IBallPhysicsEngine {
public:
    virtual ~IBallPhysicsEngine() = default;

    // Step the physics simulation forward by deltaTime
    virtual BallState Step(const BallState& currentState, float deltaTime) = 0;

    // Calculate forces acting on the ball
    virtual Vector3 CalculateForces(const BallState& state) const = 0;

    // Get/Set configurations
    virtual void SetBallConfig(const BallConfig& config) = 0;
    virtual void SetPhysicsConfig(const PhysicsConfig& config) = 0;
    virtual const BallConfig& GetBallConfig() const = 0;
    virtual const PhysicsConfig& GetPhysicsConfig() const = 0;
};

} // namespace BasicTT
