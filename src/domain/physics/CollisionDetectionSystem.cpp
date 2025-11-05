#include "../../../include/domain/physics/CollisionDetectionSystem.h"
#include <algorithm>
#include <cmath>

namespace BasicTT {

CollisionDetectionSystem::CollisionDetectionSystem(const BallConfig& ballConfig,
                                                   const TableConfig& tableConfig,
                                                   const PhysicsConfig& physicsConfig)
    : m_ballConfig(ballConfig), m_tableConfig(tableConfig), m_physicsConfig(physicsConfig) {}

CollisionData CollisionDetectionSystem::DetectPaddleCollision(
    const BallState& ballStart, const BallState& ballEnd,
    const PaddleState& paddleStart, const PaddleState& paddleEnd) const {

    CollisionData collision;

    // Simplified paddle collision: treat paddle as OBB (oriented bounding box)
    Vector3 paddleExtents(
        paddleStart.bladeWidth * 0.5f,
        paddleStart.bladeHeight * 0.5f,
        paddleStart.bladeThickness * 0.5f
    );

    float toi;
    Vector3 normal, point;

    if (SweptSphereVsOBB(ballStart.position, ballEnd.position, m_ballConfig.radius,
                         paddleStart.position, paddleExtents, paddleStart.rotation,
                         toi, normal, point)) {
        collision.hasCollision = true;
        collision.type = CollisionType::Paddle;
        collision.collisionPoint = point;
        collision.collisionNormal = normal;
        collision.timeOfImpact = toi;
        collision.relativeVelocity = ballStart.velocity - paddleStart.velocity;
    }

    return collision;
}

CollisionData CollisionDetectionSystem::DetectTableCollision(
    const BallState& ballStart, const BallState& ballEnd) const {

    CollisionData collision;

    // Table is at height m_tableConfig.height
    float tableTop = m_tableConfig.height;
    float tableHalfLength = m_tableConfig.length * 0.5f;
    float tableHalfWidth = m_tableConfig.width * 0.5f;

    // Check if ball crosses table surface
    float ballStartY = ballStart.position.y - m_ballConfig.radius;
    float ballEndY = ballEnd.position.y - m_ballConfig.radius;

    if (ballStartY > tableTop && ballEndY <= tableTop) {
        // Ball crossed table surface
        float t = (tableTop - ballStartY) / (ballEndY - ballStartY);
        Vector3 collisionPoint = Vector3::Lerp(ballStart.position, ballEnd.position, t);

        // Check if within table bounds
        if (std::abs(collisionPoint.x) <= tableHalfLength &&
            std::abs(collisionPoint.z) <= tableHalfWidth) {

            collision.hasCollision = true;
            collision.type = CollisionType::Table;
            collision.collisionPoint = collisionPoint;
            collision.collisionNormal = Vector3::Up();
            collision.timeOfImpact = t;
            collision.relativeVelocity = ballStart.velocity;
        }
    }

    return collision;
}

CollisionData CollisionDetectionSystem::DetectNetCollision(
    const BallState& ballStart, const BallState& ballEnd) const {

    CollisionData collision;

    // Net is at x = 0, height from table to table + netHeight
    float netBottom = m_tableConfig.height;
    float netTop = m_tableConfig.height + m_tableConfig.netHeight;
    float netHalfWidth = m_tableConfig.width * 0.5f;

    // Check if ball crosses net plane (x = 0)
    float ballStartX = ballStart.position.x;
    float ballEndX = ballEnd.position.x;

    if ((ballStartX > 0 && ballEndX < 0) || (ballStartX < 0 && ballEndX > 0)) {
        // Ball crossed net plane
        float t = std::abs(ballStartX) / std::abs(ballEndX - ballStartX);
        Vector3 collisionPoint = Vector3::Lerp(ballStart.position, ballEnd.position, t);

        // Check if within net height and width
        if (collisionPoint.y >= netBottom && collisionPoint.y <= netTop &&
            std::abs(collisionPoint.z) <= netHalfWidth) {

            collision.hasCollision = true;
            collision.type = CollisionType::Net;
            collision.collisionPoint = collisionPoint;
            collision.collisionNormal = (ballStartX > 0) ? Vector3(-1, 0, 0) : Vector3(1, 0, 0);
            collision.timeOfImpact = t;
            collision.relativeVelocity = ballStart.velocity;
        }
    }

    return collision;
}

CollisionData CollisionDetectionSystem::DetectFloorCollision(
    const BallState& ballStart, const BallState& ballEnd) const {

    CollisionData collision;

    float floorY = 0.0f;
    float ballStartY = ballStart.position.y - m_ballConfig.radius;
    float ballEndY = ballEnd.position.y - m_ballConfig.radius;

    if (ballStartY > floorY && ballEndY <= floorY) {
        float t = (floorY - ballStartY) / (ballEndY - ballStartY);
        Vector3 collisionPoint = Vector3::Lerp(ballStart.position, ballEnd.position, t);

        collision.hasCollision = true;
        collision.type = CollisionType::Floor;
        collision.collisionPoint = collisionPoint;
        collision.collisionNormal = Vector3::Up();
        collision.timeOfImpact = t;
        collision.relativeVelocity = ballStart.velocity;
    }

    return collision;
}

CollisionData CollisionDetectionSystem::DetectWallCollision(
    const BallState& ballStart, const BallState& ballEnd) const {

    CollisionData collision;

    // Simple wall boundaries (room bounds)
    const float roomSize = 10.0f; // 10m room
    float halfRoom = roomSize * 0.5f;

    // Check each wall
    Vector3 pos = ballEnd.position;
    float radius = m_ballConfig.radius;

    if (std::abs(pos.x) + radius > halfRoom) {
        collision.hasCollision = true;
        collision.type = CollisionType::Wall;
        collision.collisionPoint = pos;
        collision.collisionNormal = (pos.x > 0) ? Vector3(-1, 0, 0) : Vector3(1, 0, 0);
        collision.relativeVelocity = ballStart.velocity;
    }
    else if (std::abs(pos.z) + radius > halfRoom) {
        collision.hasCollision = true;
        collision.type = CollisionType::Wall;
        collision.collisionPoint = pos;
        collision.collisionNormal = (pos.z > 0) ? Vector3(0, 0, -1) : Vector3(0, 0, 1);
        collision.relativeVelocity = ballStart.velocity;
    }

    return collision;
}

bool CollisionDetectionSystem::SweptSphereVsOBB(
    const Vector3& sphereStart, const Vector3& sphereEnd, float radius,
    const Vector3& obbCenter, const Vector3& obbExtents, const Quaternion& obbRotation,
    float& toi, Vector3& normal, Vector3& point) const {

    // Simplified swept sphere vs OBB
    // Transform to OBB local space
    Quaternion invRot = obbRotation.Conjugate();
    Vector3 localStart = invRot * (sphereStart - obbCenter);
    Vector3 localEnd = invRot * (sphereEnd - obbCenter);

    // Now it's swept sphere vs AABB in local space
    Vector3 aabbMin = obbExtents * -1.0f;
    Vector3 aabbMax = obbExtents;

    // Expand AABB by sphere radius
    aabbMin = aabbMin - Vector3(radius, radius, radius);
    aabbMax = aabbMax + Vector3(radius, radius, radius);

    // Ray vs AABB test
    Vector3 rayDir = localEnd - localStart;
    float rayLength = rayDir.Magnitude();
    if (rayLength < 1e-6f) return false;

    rayDir = rayDir / rayLength;

    float tMin, tMax;
    if (RayVsAABB(localStart, rayDir, rayLength, aabbMin, aabbMax, tMin, tMax)) {
        if (tMin >= 0.0f && tMin <= rayLength) {
            toi = tMin / rayLength;
            point = Vector3::Lerp(sphereStart, sphereEnd, toi);

            // Calculate normal in local space, then transform back
            Vector3 localPoint = localStart + rayDir * tMin;
            Vector3 localNormal = Vector3::Zero();

            // Find which face was hit
            float minDist = std::numeric_limits<float>::max();
            for (int i = 0; i < 3; i++) {
                float dist;

                dist = std::abs(localPoint.x - aabbMax.x);
                if (dist < minDist) { minDist = dist; localNormal = Vector3(1, 0, 0); }

                dist = std::abs(localPoint.x - aabbMin.x);
                if (dist < minDist) { minDist = dist; localNormal = Vector3(-1, 0, 0); }

                dist = std::abs(localPoint.y - aabbMax.y);
                if (dist < minDist) { minDist = dist; localNormal = Vector3(0, 1, 0); }

                dist = std::abs(localPoint.y - aabbMin.y);
                if (dist < minDist) { minDist = dist; localNormal = Vector3(0, -1, 0); }

                dist = std::abs(localPoint.z - aabbMax.z);
                if (dist < minDist) { minDist = dist; localNormal = Vector3(0, 0, 1); }

                dist = std::abs(localPoint.z - aabbMin.z);
                if (dist < minDist) { minDist = dist; localNormal = Vector3(0, 0, -1); }
            }

            normal = obbRotation * localNormal;
            return true;
        }
    }

    return false;
}

bool CollisionDetectionSystem::SphereVsPlane(
    const Vector3& spherePos, float radius,
    const Vector3& planePoint, const Vector3& planeNormal,
    float& distance, Vector3& point) const {

    float d = Vector3::Dot(spherePos - planePoint, planeNormal);
    distance = d;

    if (d <= radius) {
        point = spherePos - planeNormal * d;
        return true;
    }

    return false;
}

bool CollisionDetectionSystem::RayVsAABB(
    const Vector3& rayOrigin, const Vector3& rayDir, float rayLength,
    const Vector3& aabbMin, const Vector3& aabbMax,
    float& tMin, float& tMax) const {

    tMin = 0.0f;
    tMax = rayLength;

    for (int i = 0; i < 3; i++) {
        float origin, dir, min, max;

        if (i == 0) { origin = rayOrigin.x; dir = rayDir.x; min = aabbMin.x; max = aabbMax.x; }
        else if (i == 1) { origin = rayOrigin.y; dir = rayDir.y; min = aabbMin.y; max = aabbMax.y; }
        else { origin = rayOrigin.z; dir = rayDir.z; min = aabbMin.z; max = aabbMax.z; }

        if (std::abs(dir) < 1e-6f) {
            // Ray parallel to slab
            if (origin < min || origin > max) return false;
        } else {
            float t1 = (min - origin) / dir;
            float t2 = (max - origin) / dir;

            if (t1 > t2) std::swap(t1, t2);

            tMin = std::max(tMin, t1);
            tMax = std::min(tMax, t2);

            if (tMin > tMax) return false;
        }
    }

    return true;
}

} // namespace BasicTT
