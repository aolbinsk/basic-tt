#pragma once

#include "../utilities/Vector3.h"

namespace BasicTT {

enum class CollisionType {
    None,
    Paddle,
    Table,
    Net,
    Floor,
    Wall
};

struct CollisionData {
    bool hasCollision;
    CollisionType type;
    Vector3 collisionPoint;     // Point of collision in world space
    Vector3 collisionNormal;    // Surface normal at collision point
    float timeOfImpact;         // Time of collision relative to timestep (0-1)
    Vector3 relativeVelocity;   // Relative velocity at collision point
    float penetrationDepth;     // Depth of penetration (meters)

    CollisionData()
        : hasCollision(false),
          type(CollisionType::None),
          collisionPoint(Vector3::Zero()),
          collisionNormal(Vector3::Up()),
          timeOfImpact(0.0f),
          relativeVelocity(Vector3::Zero()),
          penetrationDepth(0.0f) {}

    void Reset() {
        hasCollision = false;
        type = CollisionType::None;
        collisionPoint = Vector3::Zero();
        collisionNormal = Vector3::Up();
        timeOfImpact = 0.0f;
        relativeVelocity = Vector3::Zero();
        penetrationDepth = 0.0f;
    }

    // Copy constructor
    CollisionData(const CollisionData& other) = default;
    CollisionData& operator=(const CollisionData& other) = default;
};

} // namespace BasicTT
