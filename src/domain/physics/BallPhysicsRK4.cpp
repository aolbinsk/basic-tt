#include "../../../include/domain/physics/BallPhysicsRK4.h"
#include <cmath>
#include <algorithm>

namespace BasicTT {

BallPhysicsRK4::BallPhysicsRK4(const BallConfig& ballConfig, const PhysicsConfig& physicsConfig)
    : m_ballConfig(ballConfig), m_physicsConfig(physicsConfig) {}

BallState BallPhysicsRK4::Step(const BallState& currentState, float deltaTime) {
    // 4th-order Runge-Kutta integration
    // k1 = f(t, y)
    // k2 = f(t + dt/2, y + k1*dt/2)
    // k3 = f(t + dt/2, y + k2*dt/2)
    // k4 = f(t + dt, y + k3*dt)
    // y(t+dt) = y(t) + (k1 + 2*k2 + 2*k3 + k4) * dt/6

    // k1
    StateDerivative k1 = CalculateDerivative(currentState);

    // k2
    BallState state2 = currentState;
    state2.position += k1.velocity * (deltaTime * 0.5f);
    state2.velocity += k1.acceleration * (deltaTime * 0.5f);
    state2.spin += k1.angularVelocity * (deltaTime * 0.5f);
    StateDerivative k2 = CalculateDerivative(state2);

    // k3
    BallState state3 = currentState;
    state3.position += k2.velocity * (deltaTime * 0.5f);
    state3.velocity += k2.acceleration * (deltaTime * 0.5f);
    state3.spin += k2.angularVelocity * (deltaTime * 0.5f);
    StateDerivative k3 = CalculateDerivative(state3);

    // k4
    BallState state4 = currentState;
    state4.position += k3.velocity * deltaTime;
    state4.velocity += k3.acceleration * deltaTime;
    state4.spin += k3.angularVelocity * deltaTime;
    StateDerivative k4 = CalculateDerivative(state4);

    // Combine derivatives
    BallState newState = currentState;
    newState.timestamp = currentState.timestamp + deltaTime;

    Vector3 positionDelta = (k1.velocity + k2.velocity * 2.0f + k3.velocity * 2.0f + k4.velocity) *
                           (deltaTime / 6.0f);
    Vector3 velocityDelta = (k1.acceleration + k2.acceleration * 2.0f + k3.acceleration * 2.0f + k4.acceleration) *
                           (deltaTime / 6.0f);
    Vector3 spinDelta = (k1.angularVelocity + k2.angularVelocity * 2.0f + k3.angularVelocity * 2.0f + k4.angularVelocity) *
                       (deltaTime / 6.0f);

    newState.position = currentState.position + positionDelta;
    newState.velocity = currentState.velocity + velocityDelta;
    newState.spin = currentState.spin + spinDelta;
    newState.angularVelocity = newState.spin;

    // Clamp velocities
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

BallPhysicsRK4::StateDerivative BallPhysicsRK4::CalculateDerivative(const BallState& state) const {
    StateDerivative deriv;

    deriv.velocity = state.velocity;
    deriv.acceleration = CalculateAcceleration(state);
    deriv.angularVelocity = Vector3::Zero(); // Simplified: no angular acceleration for now
    deriv.angularAcceleration = Vector3::Zero();

    return deriv;
}

Vector3 BallPhysicsRK4::CalculateForces(const BallState& state) const {
    Vector3 totalForce = Vector3::Zero();

    totalForce += CalculateGravityForce();
    totalForce += CalculateDragForce(state.velocity);
    totalForce += CalculateMagnusForce(state.velocity, state.spin);

    return totalForce;
}

Vector3 BallPhysicsRK4::CalculateAcceleration(const BallState& state) const {
    Vector3 force = CalculateForces(state);
    return force / m_ballConfig.mass;
}

Vector3 BallPhysicsRK4::CalculateGravityForce() const {
    return m_physicsConfig.gravity * m_ballConfig.mass;
}

Vector3 BallPhysicsRK4::CalculateDragForce(const Vector3& velocity) const {
    float speed = velocity.Magnitude();
    if (speed < 1e-6f) return Vector3::Zero();

    float area = 3.14159f * m_ballConfig.radius * m_ballConfig.radius;
    float dragMagnitude = 0.5f * m_physicsConfig.airDensity *
                         m_ballConfig.dragCoefficient * area * speed;

    return velocity * (-dragMagnitude);
}

Vector3 BallPhysicsRK4::CalculateMagnusForce(const Vector3& velocity, const Vector3& spin) const {
    // Improved Magnus force calculation (same as Verlet version)
    float speed = velocity.Magnitude();
    float spinRate = spin.Magnitude();

    if (speed < 0.1f || spinRate < 1.0f) {
        return Vector3::Zero();
    }

    // Dimensionless spin parameter
    float spinParameter = (spinRate * m_ballConfig.radius) / speed;

    // Magnus coefficient varies non-linearly
    float Cm;
    if (spinParameter < 0.5f) {
        Cm = 1.0f * spinParameter;
    } else if (spinParameter < 4.0f) {
        Cm = 0.5f * (1.0f - std::exp(-spinParameter));
    } else {
        Cm = 0.5f;
    }

    Vector3 magnusDir = Vector3::Cross(spin, velocity);
    float magnusDirMag = magnusDir.Magnitude();

    if (magnusDirMag < 1e-6f) return Vector3::Zero();

    magnusDir = magnusDir / magnusDirMag;

    float area = 3.14159f * m_ballConfig.radius * m_ballConfig.radius;
    float magnusMag = Cm * 0.5f * m_physicsConfig.airDensity * area * speed * speed;

    return magnusDir * magnusMag;
}

} // namespace BasicTT
