#pragma once

#include "domain/entities/BallState.h"
#include "domain/entities/PaddleState.h"
#include "domain/config/BallConfig.h"
#include "domain/config/PaddleConfig.h"
#include "domain/config/TableConfig.h"
#include "domain/config/PhysicsConfig.h"
#include <gtest/gtest.h>
#include <cmath>

namespace BasicTT {
namespace Test {

/**
 * @brief Test utilities using domain-specific language
 *
 * These helpers make tests readable and focused on table tennis concepts
 * rather than low-level math.
 */

// ============================================================================
// Ball State Builders
// ============================================================================

inline BallState StationaryBall(const Vector3& position = Vector3::Zero()) {
    BallState ball;
    ball.position = position;
    ball.velocity = Vector3::Zero();
    ball.spin = Vector3::Zero();
    ball.angularVelocity = Vector3::Zero();
    return ball;
}

inline BallState MovingBall(const Vector3& position, const Vector3& velocity) {
    BallState ball;
    ball.position = position;
    ball.velocity = velocity;
    ball.spin = Vector3::Zero();
    ball.angularVelocity = Vector3::Zero();
    return ball;
}

inline BallState BallWithTopspin(const Vector3& position, const Vector3& velocity, float spinRate) {
    BallState ball;
    ball.position = position;
    ball.velocity = velocity;
    // Topspin rotates around horizontal axis perpendicular to motion
    ball.spin = Vector3(0, 0, spinRate);
    ball.angularVelocity = ball.spin;
    return ball;
}

inline BallState FreeFallingBall(float height) {
    BallState ball;
    ball.position = Vector3(0, height, 0);
    ball.velocity = Vector3::Zero();
    ball.spin = Vector3::Zero();
    ball.angularVelocity = Vector3::Zero();
    return ball;
}

inline BallState BallAtHeight(float height, const Vector3& velocity) {
    BallState ball;
    ball.position = Vector3(0, height, 0);
    ball.velocity = velocity;
    ball.spin = Vector3::Zero();
    ball.angularVelocity = Vector3::Zero();
    return ball;
}

// ============================================================================
// Paddle State Builders
// ============================================================================

inline PaddleState StationaryPaddle(const Vector3& position) {
    PaddleState paddle;
    paddle.position = position;
    paddle.rotation = Quaternion::Identity();
    paddle.velocity = Vector3::Zero();
    paddle.angularVelocity = Vector3::Zero();
    return paddle;
}

inline PaddleState MovingPaddle(const Vector3& position, const Vector3& velocity) {
    PaddleState paddle;
    paddle.position = position;
    paddle.rotation = Quaternion::Identity();
    paddle.velocity = velocity;
    paddle.angularVelocity = Vector3::Zero();
    return paddle;
}

inline PaddleState PaddleWithVelocity(const Vector3& position, const Vector3& velocity) {
    return MovingPaddle(position, velocity);
}

// ============================================================================
// Configuration Builders
// ============================================================================

inline PhysicsConfig NoAirResistanceConfig() {
    PhysicsConfig config = PhysicsConfig::Default();
    config.airDensity = 0.0f;
    return config;
}

inline PhysicsConfig NoGravityConfig() {
    PhysicsConfig config = PhysicsConfig::Default();
    config.gravity = Vector3::Zero();
    return config;
}

inline PhysicsConfig VacuumConfig() {
    PhysicsConfig config = NoAirResistanceConfig();
    config.gravity = Vector3::Zero();
    return config;
}

inline BallConfig StandardBallConfig() {
    return BallConfig::Default();
}

inline BallConfig NoSpinBallConfig() {
    BallConfig config = BallConfig::Default();
    config.magnusCoefficient = 0.0f;
    return config;
}

// ============================================================================
// Assertions with Table Tennis Context
// ============================================================================

inline void ExpectBallAt(const BallState& ball, const Vector3& expectedPos, float tolerance = 0.01f) {
    EXPECT_NEAR(ball.position.x, expectedPos.x, tolerance)
        << "Ball X position incorrect";
    EXPECT_NEAR(ball.position.y, expectedPos.y, tolerance)
        << "Ball Y position incorrect";
    EXPECT_NEAR(ball.position.z, expectedPos.z, tolerance)
        << "Ball Z position incorrect";
}

inline void ExpectBallVelocity(const BallState& ball, const Vector3& expectedVel, float tolerance = 0.01f) {
    EXPECT_NEAR(ball.velocity.x, expectedVel.x, tolerance)
        << "Ball X velocity incorrect";
    EXPECT_NEAR(ball.velocity.y, expectedVel.y, tolerance)
        << "Ball Y velocity incorrect";
    EXPECT_NEAR(ball.velocity.z, expectedVel.z, tolerance)
        << "Ball Z velocity incorrect";
}

inline void ExpectBallStationary(const BallState& ball, float tolerance = 0.001f) {
    EXPECT_NEAR(ball.velocity.Magnitude(), 0.0f, tolerance)
        << "Ball should be stationary";
}

inline void ExpectBallMoving(const BallState& ball, float minSpeed = 0.1f) {
    EXPECT_GT(ball.velocity.Magnitude(), minSpeed)
        << "Ball should be moving";
}

inline void ExpectBallFalling(const BallState& ball) {
    EXPECT_LT(ball.velocity.y, -0.1f)
        << "Ball should be falling (negative Y velocity)";
}

inline void ExpectBallBounced(const BallState& before, const BallState& after) {
    EXPECT_LT(after.velocity.y, 0.0f)
        << "Ball should be moving upward after bounce";
    EXPECT_LT(std::abs(after.velocity.y), std::abs(before.velocity.y))
        << "Ball should have lost energy in bounce";
}

inline void ExpectNormalPointingUp(const Vector3& normal) {
    EXPECT_NEAR(normal.y, 1.0f, 0.1f)
        << "Normal should point upward (Y=1)";
    EXPECT_NEAR(normal.x, 0.0f, 0.1f)
        << "Normal X component should be near zero";
    EXPECT_NEAR(normal.z, 0.0f, 0.1f)
        << "Normal Z component should be near zero";
}

inline void ExpectVectorNear(const Vector3& actual, const Vector3& expected, float tolerance = 0.01f) {
    EXPECT_NEAR(actual.x, expected.x, tolerance) << "X component mismatch";
    EXPECT_NEAR(actual.y, expected.y, tolerance) << "Y component mismatch";
    EXPECT_NEAR(actual.z, expected.z, tolerance) << "Z component mismatch";
}

// ============================================================================
// Physics Scenario Helpers
// ============================================================================

/**
 * @brief Simulate ball dropping from height onto table
 */
inline BallState SimulateDropOnTable(float dropHeight, float duration,
                                     IBallPhysicsEngine& physics, float dt = 0.01f) {
    BallState ball = FreeFallingBall(dropHeight);

    float elapsed = 0.0f;
    while (elapsed < duration) {
        ball = physics.Step(ball, dt);
        elapsed += dt;

        // Stop if hit ground
        if (ball.position.y < 0.01f) {
            break;
        }
    }

    return ball;
}

/**
 * @brief Calculate expected free fall velocity
 */
inline float ExpectedFreeFallVelocity(float time, float gravity = 9.81f) {
    return -gravity * time;
}

/**
 * @brief Calculate expected free fall distance
 */
inline float ExpectedFreeFallDistance(float time, float gravity = 9.81f) {
    return 0.5f * gravity * time * time;
}

} // namespace Test
} // namespace BasicTT
