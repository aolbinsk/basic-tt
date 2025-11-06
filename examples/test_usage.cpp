/**
 * @file test_usage.cpp
 * @brief Example showing how to use domain-specific test helpers
 *
 * This example demonstrates the intended usage of TestHelpers
 * for writing readable, table-tennis-focused tests.
 */

#include "tests/TestHelpers.h"
#include "domain/physics/BallPhysicsOptimizedVerlet.h"
#include "domain/physics/CollisionDetectionSystem.h"
#include "domain/physics/RestitutionModel.h"
#include <gtest/gtest.h>
#include <iostream>

using namespace BasicTT;
using namespace BasicTT::Test;

/**
 * Example 1: Testing ball physics with domain-specific helpers
 */
TEST(ExampleTests, BallDropsUnderGravity) {
    // Arrange - Use domain-specific config builders
    BallConfig ballConfig = StandardBallConfig();
    PhysicsConfig physicsConfig = NoAirResistanceConfig();

    BallPhysicsOptimizedVerlet engine(ballConfig, physicsConfig);

    // Use domain-specific ball builder
    BallState ball = FreeFallingBall(10.0f);  // Ball at 10m height

    // Act - Simulate 1 second of fall
    BallState result = engine.Step(ball, 1.0f);

    // Assert - Use domain-specific assertions
    ExpectBallFalling(result);

    // Calculate expected velocity and check
    float expectedVelY = ExpectedFreeFallVelocity(1.0f);
    EXPECT_NEAR(result.velocity.y, expectedVelY, 0.1f)
        << "Ball should accelerate under gravity";
}

/**
 * Example 2: Testing collision detection with table tennis context
 */
TEST(ExampleTests, BallBouncesOnTable) {
    // Arrange - Standard configuration
    BallConfig ballConfig = StandardBallConfig();
    TableConfig tableConfig = TableConfig::Default();
    PhysicsConfig physicsConfig = NoAirResistanceConfig();

    CollisionDetectionSystem detector(ballConfig, tableConfig, physicsConfig);

    // Ball moving toward table (domain-specific context)
    float tableHeight = tableConfig.height;
    BallState ballAbove = BallAtHeight(tableHeight + 0.5f, Vector3(0, -2.0f, 0));
    BallState ballBelow = BallAtHeight(tableHeight - 0.1f, Vector3(0, -2.0f, 0));

    // Act
    CollisionData collision = detector.DetectTableCollision(ballAbove, ballBelow);

    // Assert - Domain-specific expectations
    EXPECT_TRUE(collision.hasCollision) << "Ball should hit table";
    EXPECT_EQ(collision.type, CollisionType::Table);
    ExpectNormalPointingUp(collision.collisionNormal);
    EXPECT_NEAR(collision.collisionPoint.y, tableHeight, 0.05f)
        << "Collision should occur at table surface";
}

/**
 * Example 3: Testing restitution with realistic scenarios
 */
TEST(ExampleTests, GentleDropBounceIsElastic) {
    // Arrange - Realistic table tennis scenario
    float dropHeight = 0.3f;  // 30cm drop (gentle)
    float impactSpeed = std::sqrt(2.0f * 9.81f * dropHeight);

    // Act - Calculate restitution
    float e = RestitutionModel::CalculateBallTableRestitution(impactSpeed);

    // Assert - Use table tennis context in message
    EXPECT_GT(e, 0.90f) << "Gentle drops should bounce very elastically";

    // Verify realistic bounce height
    float reboundSpeed = e * impactSpeed;
    float reboundHeight = (reboundSpeed * reboundSpeed) / (2.0f * 9.81f);

    EXPECT_GT(reboundHeight, dropHeight * 0.80f)
        << "Ball should rebound to at least 80% of drop height";
}

/**
 * Example 4: Comparing approaches - without vs with domain helpers
 */
class ComparisonExample : public ::testing::Test {
protected:
    void SetUp() override {
        ballConfig = BallConfig::Default();
        physicsConfig = PhysicsConfig::Default();
    }

    BallConfig ballConfig;
    PhysicsConfig physicsConfig;
};

// WITHOUT domain-specific helpers - harder to read
TEST_F(ComparisonExample, WithoutHelpers_LessReadable) {
    // Manual ball state setup
    BallState ball;
    ball.position = Vector3(0, 0, 0);
    ball.velocity = Vector3(0, 0, 0);
    ball.spin = Vector3(0, 0, 0);
    ball.angularVelocity = Vector3(0, 0, 0);

    // Manual config modification
    PhysicsConfig testConfig = physicsConfig;
    testConfig.airDensity = 0.0f;

    // Generic assertions
    EXPECT_FLOAT_EQ(ball.velocity.x, 0.0f);
    EXPECT_FLOAT_EQ(ball.velocity.y, 0.0f);
    EXPECT_FLOAT_EQ(ball.velocity.z, 0.0f);
}

// WITH domain-specific helpers - clear intent
TEST_F(ComparisonExample, WithHelpers_MoreReadable) {
    // Domain-specific builders make intent clear
    BallState ball = StationaryBall();
    PhysicsConfig testConfig = NoAirResistanceConfig();

    // Domain-specific assertions communicate what we're testing
    ExpectBallStationary(ball);
}

/**
 * Example 5: Testing with spin (Magnus effect)
 */
TEST(ExampleTests, TopspinBallCurvesDownward) {
    // Arrange - Ball with topspin
    BallConfig ballConfig = StandardBallConfig();
    PhysicsConfig physicsConfig = PhysicsConfig::Default();  // WITH air

    BallPhysicsOptimizedVerlet engine(ballConfig, physicsConfig);

    // Create ball with topspin (domain-specific)
    float spinRate = 50.0f;  // rad/s
    BallState ball = BallWithTopspin(
        Vector3(0, 1.0f, 0),
        Vector3(5.0f, 0, 0),  // Moving horizontally
        spinRate
    );

    // Act - Simulate physics
    BallState result = engine.Step(ball, 0.1f);

    // Assert - Topspin should curve ball downward (Magnus effect)
    EXPECT_LT(result.velocity.y, ball.velocity.y)
        << "Topspin should add downward component (Magnus force)";
}

/**
 * Example 6: Paddle collision scenario
 */
TEST(ExampleTests, PaddleHitsBall) {
    // Arrange - Table tennis scenario setup
    BallConfig ballConfig = StandardBallConfig();
    TableConfig tableConfig = TableConfig::Default();
    PhysicsConfig physicsConfig = NoAirResistanceConfig();

    CollisionDetectionSystem detector(ballConfig, tableConfig, physicsConfig);

    // Ball approaching paddle
    BallState ballBefore = MovingBall(
        Vector3(0.5f, 1.0f, 0),    // Ball position
        Vector3(2.0f, 0, 0)         // Moving toward paddle
    );

    BallState ballAfter = MovingBall(
        Vector3(1.5f, 1.0f, 0),
        Vector3(2.0f, 0, 0)
    );

    // Stationary paddle
    PaddleState paddle = StationaryPaddle(Vector3(1.0f, 1.0f, 0));

    // Act
    CollisionData collision = detector.DetectPaddleCollision(
        ballBefore, ballAfter, paddle, paddle);

    // Assert - Table tennis context
    EXPECT_TRUE(collision.hasCollision) << "Paddle should hit ball";
    EXPECT_EQ(collision.type, CollisionType::Paddle);
}

/**
 * Example 7: Using scenario helpers for complex tests
 */
TEST(ExampleTests, BallBouncesRealistically) {
    // Arrange
    BallConfig ballConfig = StandardBallConfig();
    PhysicsConfig physicsConfig = PhysicsConfig::Default();

    BallPhysicsOptimizedVerlet engine(ballConfig, physicsConfig);

    // Simulate realistic drop-and-bounce scenario
    float dropHeight = 1.0f;
    BallState ballBefore = FreeFallingBall(dropHeight);

    // Let it fall until near ground
    BallState ballFalling = engine.Step(ballBefore, 0.45f);  // Almost at ground

    // Simulate bounce (using restitution)
    float impactSpeed = std::abs(ballFalling.velocity.y);
    float e = RestitutionModel::CalculateBallFloorRestitution(impactSpeed);

    BallState ballAfter = ballFalling;
    ballAfter.velocity.y = -ballAfter.velocity.y * e;  // Reverse and scale

    // Assert - Use domain helper to verify bounce
    ExpectBallBounced(ballFalling, ballAfter);
}

/**
 * Main function to show usage
 */
int main(int argc, char** argv) {
    std::cout << "Domain-Specific Test Helper Examples\n" << std::endl;

    std::cout << R"(
Key Benefits of Domain-Specific Test Helpers:

1. READABILITY
   - StationaryBall() vs manual Vector3(0,0,0) setup
   - ExpectBallBounced() vs low-level velocity checks

2. TABLE TENNIS CONTEXT
   - Tests describe gameplay scenarios, not math
   - FreeFallingBall(10.0f) immediately clear
   - BallWithTopspin() shows what we're testing

3. MAINTENANCE
   - If ball structure changes, update helpers once
   - Tests remain readable and focused

4. FOCUS
   - Tests verify gameplay feel, not implementation details
   - "Ball should bounce elastically" vs "velocity.y *= 0.89"

Example test structure:
   - Arrange: Use domain builders (StationaryBall, NoAirResistanceConfig)
   - Act: Run simulation/physics
   - Assert: Use domain assertions (ExpectBallAt, ExpectBallBounced)
)" << std::endl;

    ::testing::InitGoogleTest(&argc, argv);
    return RUN_ALL_TESTS();
}
