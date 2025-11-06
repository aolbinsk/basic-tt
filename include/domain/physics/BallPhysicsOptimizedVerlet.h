#pragma once

#include "IBallPhysicsEngine.h"

namespace BasicTT {

// 2nd-order Verlet integrator with velocity calculation
// Provides good stability with 2 force evaluations per step
class BallPhysicsOptimizedVerlet : public IBallPhysicsEngine {
public:
    BallPhysicsOptimizedVerlet(const BallConfig& ballConfig, const PhysicsConfig& physicsConfig);
    virtual ~BallPhysicsOptimizedVerlet() = default;

    BallState Step(const BallState& currentState, float deltaTime) override;
    Vector3 CalculateForces(const BallState& state) const override;

    void SetBallConfig(const BallConfig& config) override { m_ballConfig = config; }
    void SetPhysicsConfig(const PhysicsConfig& config) override { m_physicsConfig = config; }
    const BallConfig& GetBallConfig() const override { return m_ballConfig; }
    const PhysicsConfig& GetPhysicsConfig() const override { return m_physicsConfig; }

private:
    // Calculate acceleration from forces
    Vector3 CalculateAcceleration(const BallState& state) const;

    // Individual force calculations
    Vector3 CalculateGravityForce() const;
    Vector3 CalculateDragForce(const Vector3& velocity) const;
    Vector3 CalculateMagnusForce(const Vector3& velocity, const Vector3& spin) const;

    BallConfig m_ballConfig;
    PhysicsConfig m_physicsConfig;
};

} // namespace BasicTT
