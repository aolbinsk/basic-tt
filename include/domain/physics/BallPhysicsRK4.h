#pragma once

#include "IBallPhysicsEngine.h"

namespace BasicTT {

// 4th-order Runge-Kutta integrator
// Higher precision than Verlet but requires 4 force evaluations per step
class BallPhysicsRK4 : public IBallPhysicsEngine {
public:
    BallPhysicsRK4(const BallConfig& ballConfig, const PhysicsConfig& physicsConfig);
    virtual ~BallPhysicsRK4() = default;

    BallState Step(const BallState& currentState, float deltaTime) override;
    Vector3 CalculateForces(const BallState& state) const override;

    void SetBallConfig(const BallConfig& config) override { m_ballConfig = config; }
    void SetPhysicsConfig(const PhysicsConfig& config) override { m_physicsConfig = config; }
    const BallConfig& GetBallConfig() const override { return m_ballConfig; }
    const PhysicsConfig& GetPhysicsConfig() const override { return m_physicsConfig; }

private:
    // State derivative for RK4 integration
    struct StateDerivative {
        Vector3 velocity;
        Vector3 acceleration;
        Vector3 angularVelocity;
        Vector3 angularAcceleration;
    };

    // Calculate state derivative
    StateDerivative CalculateDerivative(const BallState& state) const;

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
