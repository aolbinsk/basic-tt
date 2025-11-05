#include <gtest/gtest.h>
#include "../../TestHelpers.h"
#include "domain/physics/CollisionDetectionSystem.h"

using namespace BasicTT;
using namespace BasicTT::Test;

// ============================================================================
// Table Collision Tests
// ============================================================================

class TableCollisionTest : public ::testing::Test {
protected:
    void SetUp() override {
        ballConfig = StandardBallConfig();
        tableConfig = TableConfig::Default();
        physicsConfig = NoAirResistanceConfig();
        detector = std::make_unique<CollisionDetectionSystem>(
            ballConfig, tableConfig, physicsConfig);
    }

    BallConfig ballConfig;
    TableConfig tableConfig;
    PhysicsConfig physicsConfig;
    std::unique_ptr<CollisionDetectionSystem> detector;
};

TEST_F(TableCollisionTest, BallDroppedOntoTable_DetectsCollision) {
    // Arrange - ball falling toward table
    float tableHeight = tableConfig.height;
    BallState ballAbove = BallAtHeight(tableHeight + 0.5f, Vector3(0, -2.0f, 0));
    BallState ballBelow = BallAtHeight(tableHeight - 0.1f, Vector3(0, -2.0f, 0));

    // Act
    CollisionData collision = detector->DetectTableCollision(ballAbove, ballBelow);

    // Assert
    EXPECT_TRUE(collision.hasCollision) << "Ball should collide with table";
    EXPECT_EQ(collision.type, CollisionType::Table);
    EXPECT_NEAR(collision.collisionPoint.y, tableHeight, 0.05f)
        << "Collision should occur at table surface";
    ExpectNormalPointingUp(collision.collisionNormal);
}

TEST_F(TableCollisionTest, BallAboveTable_NoCollision) {
    // Arrange - ball moving horizontally above table
    float tableHeight = tableConfig.height;
    BallState ballStart = BallAtHeight(tableHeight + 0.5f, Vector3(1.0f, 0, 0));
    BallState ballEnd = BallAtHeight(tableHeight + 0.5f, Vector3(1.0f, 0, 0));
    ballEnd.position.x += 0.1f;

    // Act
    CollisionData collision = detector->DetectTableCollision(ballStart, ballEnd);

    // Assert
    EXPECT_FALSE(collision.hasCollision)
        << "Ball moving above table should not collide";
}

TEST_F(TableCollisionTest, BallOffTableEdge_NoCollision) {
    // Arrange - ball falls past table edge
    float tableHalfLength = tableConfig.length * 0.5f;
    float tableHeight = tableConfig.height;

    Vector3 offTablePosition(tableHalfLength + 0.5f, tableHeight + 0.5f, 0);
    BallState ballAbove = MovingBall(offTablePosition, Vector3(0, -2.0f, 0));

    Vector3 belowPosition = offTablePosition;
    belowPosition.y = tableHeight - 0.5f;
    BallState ballBelow = MovingBall(belowPosition, Vector3(0, -2.0f, 0));

    // Act
    CollisionData collision = detector->DetectTableCollision(ballAbove, ballBelow);

    // Assert
    EXPECT_FALSE(collision.hasCollision)
        << "Ball past table edge should not collide with table surface";
}

TEST_F(TableCollisionTest, BallLandingAtTableCenter_CollisionPointAccurate) {
    // Arrange - ball dropping at table center
    float tableHeight = tableConfig.height;
    BallState ballAbove = BallAtHeight(tableHeight + 1.0f, Vector3(0, -3.0f, 0));
    BallState ballBelow = BallAtHeight(tableHeight - 0.2f, Vector3(0, -3.0f, 0));

    // Act
    CollisionData collision = detector->DetectTableCollision(ballAbove, ballBelow);

    // Assert
    EXPECT_TRUE(collision.hasCollision);
    EXPECT_NEAR(collision.collisionPoint.x, 0.0f, 0.01f)
        << "Ball should land at table center X";
    EXPECT_NEAR(collision.collisionPoint.z, 0.0f, 0.01f)
        << "Ball should land at table center Z";
}

// ============================================================================
// Net Collision Tests
// ============================================================================

class NetCollisionTest : public ::testing::Test {
protected:
    void SetUp() override {
        ballConfig = StandardBallConfig();
        tableConfig = TableConfig::Default();
        physicsConfig = NoAirResistanceConfig();
        detector = std::make_unique<CollisionDetectionSystem>(
            ballConfig, tableConfig, physicsConfig);
    }

    BallConfig ballConfig;
    TableConfig tableConfig;
    PhysicsConfig physicsConfig;
    std::unique_ptr<CollisionDetectionSystem> detector;
};

TEST_F(NetCollisionTest, BallCrossingNet_DetectsCollision) {
    // Arrange - ball crossing net at net height
    float netHeight = tableConfig.height + tableConfig.netHeight * 0.5f;
    BallState ballBeforeNet = MovingBall(Vector3(0.5f, netHeight, 0), Vector3(-2.0f, 0, 0));
    BallState ballAfterNet = MovingBall(Vector3(-0.5f, netHeight, 0), Vector3(-2.0f, 0, 0));

    // Act
    CollisionData collision = detector->DetectNetCollision(ballBeforeNet, ballAfterNet);

    // Assert
    EXPECT_TRUE(collision.hasCollision) << "Ball should hit the net";
    EXPECT_EQ(collision.type, CollisionType::Net);
    EXPECT_NEAR(collision.collisionPoint.x, 0.0f, 0.05f)
        << "Collision should occur at net centerline";
}

TEST_F(NetCollisionTest, BallOverNet_NoCollision) {
    // Arrange - ball flying over net
    float overNetHeight = tableConfig.height + tableConfig.netHeight + 0.5f;
    BallState ballBefore = MovingBall(Vector3(0.5f, overNetHeight, 0), Vector3(-2.0f, 0, 0));
    BallState ballAfter = MovingBall(Vector3(-0.5f, overNetHeight, 0), Vector3(-2.0f, 0, 0));

    // Act
    CollisionData collision = detector->DetectNetCollision(ballBefore, ballAfter);

    // Assert
    EXPECT_FALSE(collision.hasCollision)
        << "Ball clearing net should not collide";
}

TEST_F(NetCollisionTest, BallUnderNet_NoCollision) {
    // Arrange - ball rolling under net (below table)
    float underNetHeight = tableConfig.height - 0.1f;
    BallState ballBefore = MovingBall(Vector3(0.5f, underNetHeight, 0), Vector3(-2.0f, 0, 0));
    BallState ballAfter = MovingBall(Vector3(-0.5f, underNetHeight, 0), Vector3(-2.0f, 0, 0));

    // Act
    CollisionData collision = detector->DetectNetCollision(ballBefore, ballAfter);

    // Assert
    EXPECT_FALSE(collision.hasCollision)
        << "Ball below table should not hit net";
}

TEST_F(NetCollisionTest, BallHitsNetFromLeft_NormalPointsLeft) {
    // Arrange - ball coming from left side (positive X)
    float netHeight = tableConfig.height + tableConfig.netHeight * 0.5f;
    BallState ballBefore = MovingBall(Vector3(0.5f, netHeight, 0), Vector3(-2.0f, 0, 0));
    BallState ballAfter = MovingBall(Vector3(-0.5f, netHeight, 0), Vector3(-2.0f, 0, 0));

    // Act
    CollisionData collision = detector->DetectNetCollision(ballBefore, ballAfter);

    // Assert
    EXPECT_TRUE(collision.hasCollision);
    EXPECT_LT(collision.collisionNormal.x, 0.0f)
        << "Normal should point back toward left side (-X)";
}

// ============================================================================
// Floor Collision Tests
// ============================================================================

class FloorCollisionTest : public ::testing::Test {
protected:
    void SetUp() override {
        ballConfig = StandardBallConfig();
        tableConfig = TableConfig::Default();
        physicsConfig = NoAirResistanceConfig();
        detector = std::make_unique<CollisionDetectionSystem>(
            ballConfig, tableConfig, physicsConfig);
    }

    BallConfig ballConfig;
    TableConfig tableConfig;
    PhysicsConfig physicsConfig;
    std::unique_ptr<CollisionDetectionSystem> detector;
};

TEST_F(FloorCollisionTest, BallDroppedToFloor_DetectsCollision) {
    // Arrange - ball falling to floor
    BallState ballAbove = BallAtHeight(0.5f, Vector3(0, -2.0f, 0));
    BallState ballBelow = BallAtHeight(-0.1f, Vector3(0, -2.0f, 0));

    // Act
    CollisionData collision = detector->DetectFloorCollision(ballAbove, ballBelow);

    // Assert
    EXPECT_TRUE(collision.hasCollision) << "Ball should hit floor";
    EXPECT_EQ(collision.type, CollisionType::Floor);
    ExpectNormalPointingUp(collision.collisionNormal);
}

TEST_F(FloorCollisionTest, BallRollingOnFloor_NoNewCollision) {
    // Arrange - ball already on floor, rolling
    BallState ball1 = MovingBall(Vector3(0, ballConfig.radius, 0), Vector3(1.0f, 0, 0));
    BallState ball2 = MovingBall(Vector3(0.1f, ballConfig.radius, 0), Vector3(1.0f, 0, 0));

    // Act
    CollisionData collision = detector->DetectFloorCollision(ball1, ball2);

    // Assert
    EXPECT_FALSE(collision.hasCollision)
        << "Ball already resting on floor should not trigger new collision";
}

// ============================================================================
// Paddle Collision Tests
// ============================================================================

class PaddleCollisionTest : public ::testing::Test {
protected:
    void SetUp() override {
        ballConfig = StandardBallConfig();
        tableConfig = TableConfig::Default();
        physicsConfig = NoAirResistanceConfig();
        detector = std::make_unique<CollisionDetectionSystem>(
            ballConfig, tableConfig, physicsConfig);
    }

    BallConfig ballConfig;
    TableConfig tableConfig;
    PhysicsConfig physicsConfig;
    std::unique_ptr<CollisionDetectionSystem> detector;
};

TEST_F(PaddleCollisionTest, BallApproachingStationaryPaddle_DetectsCollision) {
    // Arrange - ball moving toward stationary paddle
    PaddleState paddle = StationaryPaddle(Vector3(1.0f, 1.0f, 0));

    BallState ballBefore = MovingBall(Vector3(0.5f, 1.0f, 0), Vector3(2.0f, 0, 0));
    BallState ballAfter = MovingBall(Vector3(1.5f, 1.0f, 0), Vector3(2.0f, 0, 0));

    // Act
    CollisionData collision = detector->DetectPaddleCollision(
        ballBefore, ballAfter, paddle, paddle);

    // Assert
    EXPECT_TRUE(collision.hasCollision) << "Ball should collide with paddle";
    EXPECT_EQ(collision.type, CollisionType::Paddle);
}

TEST_F(PaddleCollisionTest, BallMissingPaddle_NoCollision) {
    // Arrange - ball passing by paddle
    PaddleState paddle = StationaryPaddle(Vector3(1.0f, 1.0f, 0));

    BallState ballBefore = MovingBall(Vector3(0.5f, 2.0f, 0), Vector3(2.0f, 0, 0));
    BallState ballAfter = MovingBall(Vector3(1.5f, 2.0f, 0), Vector3(2.0f, 0, 0));

    // Act
    CollisionData collision = detector->DetectPaddleCollision(
        ballBefore, ballAfter, paddle, paddle);

    // Assert
    EXPECT_FALSE(collision.hasCollision)
        << "Ball passing above paddle should not collide";
}

TEST_F(PaddleCollisionTest, PaddleSwingingAtBall_CapturesRelativeVelocity) {
    // Arrange - paddle moving toward ball
    Vector3 paddlePos(1.0f, 1.0f, 0);
    PaddleState paddleStart = PaddleWithVelocity(paddlePos, Vector3(-3.0f, 0, 0));

    Vector3 paddlePos2 = paddlePos + paddleStart.velocity * 0.1f;
    PaddleState paddleEnd = PaddleWithVelocity(paddlePos2, Vector3(-3.0f, 0, 0));

    BallState ballBefore = MovingBall(Vector3(0.5f, 1.0f, 0), Vector3(2.0f, 0, 0));
    BallState ballAfter = MovingBall(Vector3(1.5f, 1.0f, 0), Vector3(2.0f, 0, 0));

    // Act
    CollisionData collision = detector->DetectPaddleCollision(
        ballBefore, ballAfter, paddleStart, paddleEnd);

    // Assert
    if (collision.hasCollision) {
        // Relative velocity should be ball velocity minus paddle velocity
        Vector3 expectedRelVel = ballBefore.velocity - paddleStart.velocity;
        EXPECT_NEAR(collision.relativeVelocity.x, expectedRelVel.x, 0.1f)
            << "Should capture relative velocity between ball and paddle";
    }
}

// ============================================================================
// Wall Collision Tests
// ============================================================================

TEST(WallCollisionTest, BallHittingWall_DetectsCollision) {
    // Arrange
    BallConfig ballCfg = StandardBallConfig();
    TableConfig tableCfg = TableConfig::Default();
    PhysicsConfig physicsCfg = NoAirResistanceConfig();
    CollisionDetectionSystem detector(ballCfg, tableCfg, physicsCfg);

    // Ball near room boundary (10m room = ±5m walls)
    BallState ballBefore = MovingBall(Vector3(4.5f, 1.0f, 0), Vector3(2.0f, 0, 0));
    BallState ballAtWall = MovingBall(Vector3(6.0f, 1.0f, 0), Vector3(2.0f, 0, 0));

    // Act
    CollisionData collision = detector.DetectWallCollision(ballBefore, ballAtWall);

    // Assert
    EXPECT_TRUE(collision.hasCollision) << "Ball should hit wall";
    EXPECT_EQ(collision.type, CollisionType::Wall);
}

TEST(WallCollisionTest, BallInRoomCenter_NoCollision) {
    // Arrange
    BallConfig ballCfg = StandardBallConfig();
    TableConfig tableCfg = TableConfig::Default();
    PhysicsConfig physicsCfg = NoAirResistanceConfig();
    CollisionDetectionSystem detector(ballCfg, tableCfg, physicsCfg);

    BallState ball1 = MovingBall(Vector3::Zero(), Vector3(1.0f, 0, 0));
    BallState ball2 = MovingBall(Vector3(0.1f, 0, 0), Vector3(1.0f, 0, 0));

    // Act
    CollisionData collision = detector.DetectWallCollision(ball1, ball2);

    // Assert
    EXPECT_FALSE(collision.hasCollision)
        << "Ball in center of room should not hit walls";
}
