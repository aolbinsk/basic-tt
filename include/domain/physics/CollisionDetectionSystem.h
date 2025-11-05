#pragma once

#include "../entities/BallState.h"
#include "../entities/PaddleState.h"
#include "../entities/CollisionData.h"
#include "../config/BallConfig.h"
#include "../config/TableConfig.h"
#include "../config/PhysicsConfig.h"

namespace BasicTT {

// Swept sphere collision detection system
// Uses continuous collision detection to prevent tunneling
class CollisionDetectionSystem {
public:
    CollisionDetectionSystem(const BallConfig& ballConfig,
                            const TableConfig& tableConfig,
                            const PhysicsConfig& physicsConfig);

    // Detect collision between ball and paddle
    CollisionData DetectPaddleCollision(const BallState& ballStart,
                                       const BallState& ballEnd,
                                       const PaddleState& paddleStart,
                                       const PaddleState& paddleEnd) const;

    // Detect collision between ball and table
    CollisionData DetectTableCollision(const BallState& ballStart,
                                      const BallState& ballEnd) const;

    // Detect collision between ball and net
    CollisionData DetectNetCollision(const BallState& ballStart,
                                    const BallState& ballEnd) const;

    // Detect collision between ball and floor
    CollisionData DetectFloorCollision(const BallState& ballStart,
                                      const BallState& ballEnd) const;

    // Detect collision between ball and walls
    CollisionData DetectWallCollision(const BallState& ballStart,
                                     const BallState& ballEnd) const;

    // Update configurations
    void SetBallConfig(const BallConfig& config) { m_ballConfig = config; }
    void SetTableConfig(const TableConfig& config) { m_tableConfig = config; }
    void SetPhysicsConfig(const PhysicsConfig& config) { m_physicsConfig = config; }

private:
    // Helper: Swept sphere vs OBB (Oriented Bounding Box)
    bool SweptSphereVsOBB(const Vector3& sphereStart, const Vector3& sphereEnd, float radius,
                         const Vector3& obbCenter, const Vector3& obbExtents,
                         const Quaternion& obbRotation,
                         float& toi, Vector3& normal, Vector3& point) const;

    // Helper: Sphere vs plane
    bool SphereVsPlane(const Vector3& spherePos, float radius,
                      const Vector3& planePoint, const Vector3& planeNormal,
                      float& distance, Vector3& point) const;

    // Helper: Ray vs AABB
    bool RayVsAABB(const Vector3& rayOrigin, const Vector3& rayDir, float rayLength,
                  const Vector3& aabbMin, const Vector3& aabbMax,
                  float& tMin, float& tMax) const;

    BallConfig m_ballConfig;
    TableConfig m_tableConfig;
    PhysicsConfig m_physicsConfig;
};

} // namespace BasicTT
