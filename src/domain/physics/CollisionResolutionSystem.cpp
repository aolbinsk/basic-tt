#include "../../../include/domain/physics/CollisionResolutionSystem.h"
#include <algorithm>
#include <cmath>

namespace BasicTT {

CollisionResolutionSystem::CollisionResolutionSystem(
    const BallConfig& ballConfig,
    const PaddleConfig& paddleConfig,
    const TableConfig& tableConfig,
    const PhysicsConfig& physicsConfig)
    : m_ballConfig(ballConfig),
      m_paddleConfig(paddleConfig),
      m_tableConfig(tableConfig),
      m_physicsConfig(physicsConfig) {}

BallState CollisionResolutionSystem::ResolveCollision(
    const BallState& ballState,
    const CollisionData& collision,
    const PaddleState* paddle) const {

    if (!collision.hasCollision) {
        return ballState;
    }

    switch (collision.type) {
        case CollisionType::Paddle:
            if (paddle != nullptr) {
                return ResolvePaddleCollision(ballState, collision, *paddle);
            }
            break;

        case CollisionType::Table:
            return ResolveStaticCollision(ballState, collision,
                                         m_tableConfig.restitution,
                                         m_tableConfig.friction);

        case CollisionType::Floor:
            return ResolveStaticCollision(ballState, collision, 0.5f, 0.7f);

        case CollisionType::Net:
            return ResolveStaticCollision(ballState, collision, 0.3f, 0.8f);

        case CollisionType::Wall:
            return ResolveStaticCollision(ballState, collision, 0.8f, 0.3f);

        default:
            break;
    }

    return ballState;
}

BallState CollisionResolutionSystem::ResolvePaddleCollision(
    const BallState& ballState,
    const CollisionData& collision,
    const PaddleState& paddle) const {

    BallState newState = ballState;

    // Calculate relative velocity at contact point
    Vector3 relVel = ballState.velocity - paddle.velocity;

    // Normal impulse (restitution)
    float vn = Vector3::Dot(relVel, collision.collisionNormal);

    // Only resolve if approaching
    if (vn >= 0) return ballState;

    // Combined restitution
    float restitution = (m_ballConfig.restitution + m_paddleConfig.restitution) * 0.5f;

    // Calculate normal impulse
    float invMassBall = 1.0f / m_ballConfig.mass;
    float invMassPaddle = 0.0f; // Paddle treated as infinite mass (VR controller)

    float impulseScale = -(1.0f + restitution) * vn / (invMassBall + invMassPaddle);
    Vector3 normalImpulse = collision.collisionNormal * impulseScale;

    // Apply normal impulse
    newState.velocity = ballState.velocity + normalImpulse * invMassBall;

    // Friction impulse (tangential)
    Vector3 tangentVel = relVel - collision.collisionNormal * vn;
    float tangentSpeed = tangentVel.Magnitude();

    if (tangentSpeed > 1e-6f) {
        Vector3 tangentDir = tangentVel / tangentSpeed;

        // Coulomb friction
        float frictionCoef = m_paddleConfig.rubberFriction;
        float maxFriction = frictionCoef * impulseScale;

        float frictionImpulse = std::min(tangentSpeed / invMassBall, maxFriction);
        Vector3 frictionImpulseVec = tangentDir * (-frictionImpulse);

        newState.velocity += frictionImpulseVec * invMassBall;

        // Spin transfer from friction
        Vector3 contactToBall = ballState.position - collision.collisionPoint;
        Vector3 spinTransfer = Vector3::Cross(frictionImpulseVec, contactToBall) /
                              (m_ballConfig.mass * m_ballConfig.radius * m_ballConfig.radius);

        newState.spin += spinTransfer;
        newState.angularVelocity = newState.spin;
    }

    // Add paddle spin contribution
    newState.spin += paddle.angularVelocity * 0.3f; // Partial spin transfer

    return newState;
}

BallState CollisionResolutionSystem::ResolveStaticCollision(
    const BallState& ballState,
    const CollisionData& collision,
    float restitution,
    float friction) const {

    BallState newState = ballState;

    // Reflect velocity along normal
    Vector3 normal = collision.collisionNormal;
    float vn = Vector3::Dot(ballState.velocity, normal);

    // Only resolve if approaching
    if (vn >= 0) return ballState;

    // Normal component (reflection with restitution)
    Vector3 normalVel = normal * vn;
    Vector3 tangentVel = ballState.velocity - normalVel;

    // Apply restitution to normal component
    newState.velocity = tangentVel - normalVel * restitution;

    // Apply friction to tangent component
    float tangentSpeed = tangentVel.Magnitude();
    if (tangentSpeed > 1e-6f) {
        Vector3 tangentDir = tangentVel / tangentSpeed;

        // Reduce tangent velocity by friction
        float frictionEffect = friction * std::abs(vn);
        float newTangentSpeed = std::max(0.0f, tangentSpeed - frictionEffect);

        tangentVel = tangentDir * newTangentSpeed;
        newState.velocity = tangentVel - normalVel * restitution;
    }

    // Reduce spin slightly on bounce
    newState.spin *= 0.9f;
    newState.angularVelocity = newState.spin;

    // Move ball out of penetration
    if (collision.penetrationDepth > 0) {
        newState.position = ballState.position + normal * collision.penetrationDepth;
    }

    return newState;
}

Vector3 CollisionResolutionSystem::CalculateImpulse(
    const Vector3& relativeVelocity,
    const Vector3& normal,
    float restitution,
    float invMass1,
    float invMass2) const {

    float vn = Vector3::Dot(relativeVelocity, normal);
    if (vn >= 0) return Vector3::Zero();

    float impulseScale = -(1.0f + restitution) * vn / (invMass1 + invMass2);
    return normal * impulseScale;
}

Vector3 CollisionResolutionSystem::CalculateFrictionImpulse(
    const Vector3& relativeVelocity,
    const Vector3& normal,
    const Vector3& normalImpulse,
    float friction) const {

    float vn = Vector3::Dot(relativeVelocity, normal);
    Vector3 tangentVel = relativeVelocity - normal * vn;

    float tangentSpeed = tangentVel.Magnitude();
    if (tangentSpeed < 1e-6f) return Vector3::Zero();

    Vector3 tangentDir = tangentVel / tangentSpeed;
    float normalImpulseMag = normalImpulse.Magnitude();

    float frictionImpulseMag = std::min(tangentSpeed, friction * normalImpulseMag);
    return tangentDir * (-frictionImpulseMag);
}

Vector3 CollisionResolutionSystem::CalculateSpinTransfer(
    const Vector3& frictionImpulse,
    const Vector3& contactPoint,
    const Vector3& ballCenter,
    float ballRadius,
    float ballMass) const {

    Vector3 contactToBall = ballCenter - contactPoint;
    float inertia = 0.4f * ballMass * ballRadius * ballRadius; // Sphere inertia: 2/5 * m * r²

    Vector3 angularImpulse = Vector3::Cross(contactToBall, frictionImpulse);
    return angularImpulse / inertia;
}

} // namespace BasicTT
