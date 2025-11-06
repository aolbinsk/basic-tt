#include "../../../include/domain/physics/BallPhysicsOptimizedVerlet.h"
#include <cmath>
#include <algorithm>

namespace BasicTT {

BallPhysicsOptimizedVerlet::BallPhysicsOptimizedVerlet(const BallConfig& ballConfig,
                                                       const PhysicsConfig& physicsConfig)
    : m_ballConfig(ballConfig), m_physicsConfig(physicsConfig) {}

BallState BallPhysicsOptimizedVerlet::Step(const BallState& currentState, float deltaTime) {
    // Optimized Verlet integration (2nd order)
    // x(t+dt) = x(t) + v(t)*dt + 0.5*a(t)*dt²
    // v(t+dt) = v(t) + 0.5*(a(t) + a(t+dt))*dt

    BallState newState = currentState;
    newState.timestamp = currentState.timestamp + deltaTime;

    // Calculate current acceleration
    Vector3 accel0 = CalculateAcceleration(currentState);

    // Update position using current velocity and acceleration
    newState.position = currentState.position +
                       currentState.velocity * deltaTime +
                       accel0 * (0.5f * deltaTime * deltaTime);

    // Update spin/angular velocity (simple decay for now)
    float spinDecay = std::exp(-0.5f * deltaTime); // Approximate spin decay
    newState.spin = currentState.spin * spinDecay;
    newState.angularVelocity = currentState.angularVelocity * spinDecay;

    // Calculate new acceleration at new position
    Vector3 accel1 = CalculateAcceleration(newState);

    // Update velocity using average acceleration
    newState.velocity = currentState.velocity + (accel0 + accel1) * (0.5f * deltaTime);

    // Clamp velocities to prevent instability
    float velMag = newState.velocity.Magnitude();
    if (velMag < m_physicsConfig.minVelocity) {
        newState.velocity = Vector3::Zero();
    } else if (velMag > m_physicsConfig.maxVelocity) {
        newState.velocity = newState.velocity.Normalized() * m_physicsConfig.maxVelocity;
    }

    float angVelMag = newState.angularVelocity.Magnitude();
    if (angVelMag < m_physicsConfig.minAngularVelocity) {
        newState.angularVelocity = Vector3::Zero();
        newState.spin = Vector3::Zero();
    } else if (angVelMag > m_physicsConfig.maxAngularVelocity) {
        newState.angularVelocity = newState.angularVelocity.Normalized() * m_physicsConfig.maxAngularVelocity;
        newState.spin = newState.angularVelocity;
    }

    return newState;
}

Vector3 BallPhysicsOptimizedVerlet::CalculateForces(const BallState& state) const {
    Vector3 totalForce = Vector3::Zero();

    totalForce += CalculateGravityForce();
    totalForce += CalculateDragForce(state.velocity);
    totalForce += CalculateMagnusForce(state.velocity, state.spin);

    return totalForce;
}

Vector3 BallPhysicsOptimizedVerlet::CalculateAcceleration(const BallState& state) const {
    Vector3 force = CalculateForces(state);
    return force / m_ballConfig.mass;
}

Vector3 BallPhysicsOptimizedVerlet::CalculateGravityForce() const {
    // F = m * g
    return m_physicsConfig.gravity * m_ballConfig.mass;
}

Vector3 BallPhysicsOptimizedVerlet::CalculateDragForce(const Vector3& velocity) const {
    // F_drag = -0.5 * ρ * Cd * A * |v| * v
    // where A = π * r² (cross-sectional area)

    float speed = velocity.Magnitude();
    if (speed < 1e-6f) return Vector3::Zero();

    float area = 3.14159f * m_ballConfig.radius * m_ballConfig.radius;
    float dragMagnitude = 0.5f * m_physicsConfig.airDensity *
                         m_ballConfig.dragCoefficient * area * speed;

    return velocity * (-dragMagnitude);
}

Vector3 BallPhysicsOptimizedVerlet::CalculateMagnusForce(const Vector3& velocity,
                                                         const Vector3& spin) const {
    // Improved Magnus force calculation with velocity and spin dependence
    // F_magnus = Cm(S) * 0.5 * ρ * A * v² * (ω × v) / |ω × v|
    // where S = (ω * r) / v is the dimensionless spin parameter

    float speed = velocity.Magnitude();
    float spinRate = spin.Magnitude();

    // No Magnus force if not moving or not spinning
    if (speed < 0.1f || spinRate < 1.0f) {
        return Vector3::Zero();
    }

    // Dimensionless spin parameter: S = (ω * r) / v
    float spinParameter = (spinRate * m_ballConfig.radius) / speed;

    // Magnus coefficient varies non-linearly with spin parameter
    // Based on experimental data for spinning spheres
    float Cm;
    if (spinParameter < 0.5f) {
        // Linear regime at low spin rates
        Cm = 1.0f * spinParameter;
    } else if (spinParameter < 4.0f) {
        // Non-linear transition regime
        Cm = 0.5f * (1.0f - std::exp(-spinParameter));
    } else {
        // Saturation at high spin rates
        Cm = 0.5f;
    }

    // Calculate Magnus direction and magnitude
    Vector3 magnusDir = Vector3::Cross(spin, velocity);
    float magnusDirMag = magnusDir.Magnitude();

    if (magnusDirMag < 1e-6f) return Vector3::Zero();

    magnusDir = magnusDir / magnusDirMag;  // Normalize direction

    // F_magnus = Cm * 0.5 * ρ * A * v²
    float area = 3.14159f * m_ballConfig.radius * m_ballConfig.radius;
    float magnusMag = Cm * 0.5f * m_physicsConfig.airDensity * area * speed * speed;

    return magnusDir * magnusMag;
}

} // namespace BasicTT
