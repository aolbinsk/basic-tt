#pragma once

#include "../entities/BallState.h"
#include "../entities/PaddleState.h"
#include "../entities/CollisionData.h"
#include "../config/BallConfig.h"
#include "../config/PaddleConfig.h"
#include "../config/TableConfig.h"
#include "../config/PhysicsConfig.h"

namespace BasicTT {

// Impulse-based collision resolution with friction and spin transfer
class CollisionResolutionSystem {
public:
    CollisionResolutionSystem(const BallConfig& ballConfig,
                             const PaddleConfig& paddleConfig,
                             const TableConfig& tableConfig,
                             const PhysicsConfig& physicsConfig);

    // Resolve collision and update ball state
    BallState ResolveCollision(const BallState& ballState,
                              const CollisionData& collision,
                              const PaddleState* paddle = nullptr) const;

    // Update configurations
    void SetBallConfig(const BallConfig& config) { m_ballConfig = config; }
    void SetPaddleConfig(const PaddleConfig& config) { m_paddleConfig = config; }
    void SetTableConfig(const TableConfig& config) { m_tableConfig = config; }
    void SetPhysicsConfig(const PhysicsConfig& config) { m_physicsConfig = config; }

private:
    // Resolve paddle collision with spin transfer
    BallState ResolvePaddleCollision(const BallState& ballState,
                                    const CollisionData& collision,
                                    const PaddleState& paddle) const;

    // Resolve table/floor/wall collision
    BallState ResolveStaticCollision(const BallState& ballState,
                                    const CollisionData& collision,
                                    float restitution,
                                    float friction) const;

    // Calculate impulse for collision resolution
    Vector3 CalculateImpulse(const Vector3& relativeVelocity,
                            const Vector3& normal,
                            float restitution,
                            float invMass1,
                            float invMass2) const;

    // Calculate friction impulse and spin transfer
    Vector3 CalculateFrictionImpulse(const Vector3& relativeVelocity,
                                    const Vector3& normal,
                                    const Vector3& normalImpulse,
                                    float friction) const;

    // Apply spin to ball from friction
    Vector3 CalculateSpinTransfer(const Vector3& frictionImpulse,
                                 const Vector3& contactPoint,
                                 const Vector3& ballCenter,
                                 float ballRadius,
                                 float ballMass) const;

    BallConfig m_ballConfig;
    PaddleConfig m_paddleConfig;
    TableConfig m_tableConfig;
    PhysicsConfig m_physicsConfig;
};

} // namespace BasicTT
