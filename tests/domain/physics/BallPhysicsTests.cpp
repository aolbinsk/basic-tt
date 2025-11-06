#include <gtest/gtest.h>
#include "../../TestHelpers.h"
#include "domain/physics/BallPhysicsOptimizedVerlet.h"
#include "domain/physics/BallPhysicsRK4.h"

using namespace BasicTT;
using namespace BasicTT::Test;

// ============================================================================
// Verlet Integration Tests
// ============================================================================

class VerletPhysicsTest : public ::testing::Test {
protected:
    void SetUp() override {
        ballConfig = StandardBallConfig();
        physicsConfig = NoAirResistanceConfig();
        engine = std::make_unique<BallPhysicsOptimizedVerlet>(ballConfig, physicsConfig);
    }

    BallConfig ballConfig;
    PhysicsConfig physicsConfig;
    std::unique_ptr<BallPhysicsOptimizedVerlet> engine;
};

TEST_F(VerletPhysicsTest, StationaryBall_NoMovement) {
    // Arrange
    physicsConfig.gravity = Vector3::Zero();
    engine = std::make_unique<BallPhysicsOptimizedVerlet>(ballConfig, physicsConfig);
    BallState ball = StationaryBall();

    // Act
    BallState result = engine->Step(ball, 0.016f);

    // Assert
    ExpectBallAt(result, Vector3::Zero());
    ExpectBallStationary(result);
}

TEST_F(VerletPhysicsTest, BallWithVelocity_PositionUpdates) {
    // Arrange
    physicsConfig.gravity = Vector3::Zero();
    engine = std::make_unique<BallPhysicsOptimizedVerlet>(ballConfig, physicsConfig);
    BallState ball = MovingBall(Vector3::Zero(), Vector3(5.0f, 0.0f, 0.0f));

    // Act
    BallState result = engine->Step(ball, 1.0f);

    // Assert
    ExpectBallAt(result, Vector3(5.0f, 0.0f, 0.0f), 0.1f);
}

TEST_F(VerletPhysicsTest, FreeFall_VelocityIncreasesLinearly) {
    // Arrange
    BallState ball = FreeFallingBall(10.0f);

    // Act - simulate 1 second of free fall
    BallState result = engine->Step(ball, 1.0f);

    // Assert
    float expectedVelY = ExpectedFreeFallVelocity(1.0f, physicsConfig.gravity.Magnitude());
    EXPECT_NEAR(result.velocity.y, expectedVelY, 0.1f)
        << "Ball should accelerate under gravity";
}

TEST_F(VerletPhysicsTest, FreeFall_PositionFollowsQuadratic) {
    // Arrange
    BallState ball = FreeFallingBall(10.0f);
    float initialHeight = ball.position.y;

    // Act - simulate 1 second
    BallState result = engine->Step(ball, 1.0f);

    // Assert
    float expectedDrop = ExpectedFreeFallDistance(1.0f, physicsConfig.gravity.Magnitude());
    float actualDrop = initialHeight - result.position.y;
    EXPECT_NEAR(actualDrop, expectedDrop, 0.1f)
        << "Ball should follow h = 0.5*g*t²";
}

TEST_F(VerletPhysicsTest, Topspin_AppliesMagnusEffect) {
    // Arrange
    ballConfig.magnusCoefficient = 0.29f;  // Standard value
    engine = std::make_unique<BallPhysicsOptimizedVerlet>(ballConfig, physicsConfig);

    BallState ball = BallWithTopspin(
        Vector3::Zero(),
        Vector3(10.0f, 0.0f, 0.0f),  // Moving forward
        100.0f                        // Spinning
    );

    // Act
    BallState result = engine->Step(ball, 0.016f);

    // Assert - topspin should push ball downward
    EXPECT_LT(result.velocity.y, ball.velocity.y)
        << "Topspin should push ball downward via Magnus effect";
}

// ============================================================================
// RK4 Integration Tests
// ============================================================================

class RK4PhysicsTest : public ::testing::Test {
protected:
    void SetUp() override {
        ballConfig = StandardBallConfig();
        physicsConfig = NoAirResistanceConfig();
        engine = std::make_unique<BallPhysicsRK4>(ballConfig, physicsConfig);
    }

    BallConfig ballConfig;
    PhysicsConfig physicsConfig;
    std::unique_ptr<BallPhysicsRK4> engine;
};

TEST_F(RK4PhysicsTest, StationaryBall_NoMovement) {
    // Arrange
    physicsConfig.gravity = Vector3::Zero();
    engine = std::make_unique<BallPhysicsRK4>(ballConfig, physicsConfig);
    BallState ball = StationaryBall();

    // Act
    BallState result = engine->Step(ball, 0.016f);

    // Assert
    ExpectBallStationary(result);
}

TEST_F(RK4PhysicsTest, FreeFall_MatchesVerlet) {
    // Arrange
    BallPhysicsOptimizedVerlet verletEngine(ballConfig, physicsConfig);
    BallState ball = FreeFallingBall(5.0f);

    // Act
    BallState rk4Result = engine->Step(ball, 0.016f);
    BallState verletResult = verletEngine.Step(ball, 0.016f);

    // Assert - should be very close (both are accurate)
    EXPECT_NEAR(rk4Result.velocity.y, verletResult.velocity.y, 0.01f)
        << "RK4 and Verlet should give similar results for simple free fall";
}

// ============================================================================
// Physics Comparison Tests
// ============================================================================

TEST(PhysicsComparison, MultipleSteps_RK4MoreAccurate) {
    // Arrange
    BallConfig ballConfig = StandardBallConfig();
    PhysicsConfig physicsConfig = NoAirResistanceConfig();

    BallPhysicsOptimizedVerlet verlet(ballConfig, physicsConfig);
    BallPhysicsRK4 rk4(ballConfig, physicsConfig);

    BallState initialBall = FreeFallingBall(10.0f);

    // Act - simulate 2 seconds with large timesteps
    BallState verletBall = initialBall;
    BallState rk4Ball = initialBall;

    for (int i = 0; i < 20; i++) {
        verletBall = verlet.Step(verletBall, 0.1f);
        rk4Ball = rk4.Step(rk4Ball, 0.1f);
    }

    // Assert - RK4 should be closer to analytical solution
    float analyticalVelY = ExpectedFreeFallVelocity(2.0f);
    float verletError = std::abs(verletBall.velocity.y - analyticalVelY);
    float rk4Error = std::abs(rk4Ball.velocity.y - analyticalVelY);

    EXPECT_LT(rk4Error, verletError)
        << "RK4 should be more accurate with large timesteps";
}
