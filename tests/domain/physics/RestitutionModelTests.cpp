#include <gtest/gtest.h>
#include "../../TestHelpers.h"
#include "domain/physics/RestitutionModel.h"

using namespace BasicTT;
using namespace BasicTT::Test;

// ============================================================================
// Ball-Table Restitution Tests
// ============================================================================

TEST(BallTableRestitution, LowSpeedBounce_NearlyElastic) {
    // Arrange - gentle drop onto table
    float lowSpeed = 0.5f;  // m/s - slow drop

    // Act
    float e = RestitutionModel::CalculateBallTableRestitution(lowSpeed);

    // Assert
    EXPECT_GT(e, 0.90f) << "Low speed bounces should be nearly elastic (>90%)";
    EXPECT_LE(e, 1.0f) << "Restitution cannot exceed 1.0";
}

TEST(BallTableRestitution, MediumSpeedBounce_StandardPlay) {
    // Arrange - typical rally bounce
    float rallySpeed = 3.0f;  // m/s - normal play

    // Act
    float e = RestitutionModel::CalculateBallTableRestitution(rallySpeed);

    // Assert
    EXPECT_GT(e, 0.85f) << "Rally speed should maintain good bounce";
    EXPECT_LT(e, 0.91f) << "Rally speed should show some energy loss";
}

TEST(BallTableRestitution, HighSpeedBounce_CompressionLoss) {
    // Arrange - hard smash onto table
    float smashSpeed = 8.0f;  // m/s - powerful shot

    // Act
    float e = RestitutionModel::CalculateBallTableRestitution(smashSpeed);

    // Assert
    EXPECT_GT(e, 0.75f) << "Even high speed bounces should retain reasonable energy";
    EXPECT_LT(e, 0.87f) << "High speed should show more compression loss";
}

TEST(BallTableRestitution, VelocityDependence_DecreasesWithSpeed) {
    // Arrange - test multiple speeds
    float slowSpeed = 0.5f;
    float mediumSpeed = 3.0f;
    float fastSpeed = 8.0f;

    // Act
    float slowE = RestitutionModel::CalculateBallTableRestitution(slowSpeed);
    float mediumE = RestitutionModel::CalculateBallTableRestitution(mediumSpeed);
    float fastE = RestitutionModel::CalculateBallTableRestitution(fastSpeed);

    // Assert
    EXPECT_GT(slowE, mediumE)
        << "Slower bounces should be more elastic than medium speed";
    EXPECT_GT(mediumE, fastE)
        << "Medium speed should be more elastic than high speed";
}

TEST(BallTableRestitution, RealisticBounceScenario_DropAndRebound) {
    // Arrange - ball dropped from 1m height
    float dropHeight = 1.0f;
    float impactSpeed = std::sqrt(2.0f * 9.81f * dropHeight);  // v = sqrt(2gh)

    // Act
    float e = RestitutionModel::CalculateBallTableRestitution(impactSpeed);
    float reboundSpeed = e * impactSpeed;
    float reboundHeight = (reboundSpeed * reboundSpeed) / (2.0f * 9.81f);

    // Assert
    EXPECT_GT(reboundHeight, 0.70f)
        << "Ball dropped from 1m should rebound to at least 70cm";
    EXPECT_LT(reboundHeight, 0.92f)
        << "Ball dropped from 1m should not rebound to more than 92cm";
}

// ============================================================================
// Ball-Paddle Restitution Tests
// ============================================================================

TEST(BallPaddleRestitution, SoftSponge_AbsorbsMoreEnergy) {
    // Arrange
    float impactSpeed = 5.0f;
    float softRubber = 0.0f;   // Soft sponge (30 degrees)
    float hardRubber = 1.0f;   // Hard sponge (50 degrees)

    // Act
    float softE = RestitutionModel::CalculateBallPaddleRestitution(impactSpeed, softRubber);
    float hardE = RestitutionModel::CalculateBallPaddleRestitution(impactSpeed, hardRubber);

    // Assert
    EXPECT_LT(softE, hardE)
        << "Soft sponge should absorb more energy (lower e) than hard sponge";
}

TEST(BallPaddleRestitution, HardSponge_MoreElastic) {
    // Arrange - hard sponge for fast attacking play
    float impactSpeed = 4.0f;
    float hardRubber = 1.0f;

    // Act
    float e = RestitutionModel::CalculateBallPaddleRestitution(impactSpeed, hardRubber);

    // Assert
    EXPECT_GT(e, 0.84f) << "Hard sponge should provide good speed";
    EXPECT_LT(e, 0.90f) << "Paddle restitution should still have some loss";
}

TEST(BallPaddleRestitution, VeryFastHit_AdditionalCompressionLoss) {
    // Arrange - powerful smash
    float normalSpeed = 6.0f;
    float smashSpeed = 12.0f;
    float mediumRubber = 0.5f;

    // Act
    float normalE = RestitutionModel::CalculateBallPaddleRestitution(normalSpeed, mediumRubber);
    float smashE = RestitutionModel::CalculateBallPaddleRestitution(smashSpeed, mediumRubber);

    // Assert
    EXPECT_LT(smashE, normalE)
        << "Very fast paddle hits should lose more energy due to compression";
}

TEST(BallPaddleRestitution, RubberHardnessRange_RealisticValues) {
    // Arrange - test full range of sponge hardness
    float impactSpeed = 5.0f;

    for (float hardness = 0.0f; hardness <= 1.0f; hardness += 0.1f) {
        // Act
        float e = RestitutionModel::CalculateBallPaddleRestitution(impactSpeed, hardness);

        // Assert
        EXPECT_GE(e, 0.70f) << "Minimum paddle restitution should be reasonable";
        EXPECT_LE(e, 0.90f) << "Maximum paddle restitution should be realistic";
    }
}

// ============================================================================
// Ball-Net Restitution Tests
// ============================================================================

TEST(BallNetRestitution, NetAbsorbsMostEnergy_LowRestitution) {
    // Arrange - ball hitting net
    float impactSpeed = 3.0f;

    // Act
    float e = RestitutionModel::CalculateBallNetRestitution(impactSpeed);

    // Assert
    EXPECT_LT(e, 0.40f) << "Net should absorb most energy (e < 0.4)";
    EXPECT_GT(e, 0.15f) << "Net should still have some bounce";
}

TEST(BallNetRestitution, FastHit_NetAbsorbsMore) {
    // Arrange
    float slowHit = 1.0f;
    float fastHit = 5.0f;

    // Act
    float slowE = RestitutionModel::CalculateBallNetRestitution(slowHit);
    float fastE = RestitutionModel::CalculateBallNetRestitution(fastHit);

    // Assert
    EXPECT_GT(slowE, fastE)
        << "Faster hits should cause more net vibration and energy loss";
}

TEST(BallNetRestitution, NetHit_BallDropsNearNet) {
    // Arrange - realistic scenario: ball hits net with horizontal velocity
    float horizontalSpeed = 4.0f;

    // Act
    float e = RestitutionModel::CalculateBallNetRestitution(horizontalSpeed);

    // Assert
    // After hitting net, ball should lose most horizontal momentum
    float reboundSpeed = e * horizontalSpeed;
    EXPECT_LT(reboundSpeed, 1.5f)
        << "Ball hitting net should slow dramatically and drop nearby";
}

// ============================================================================
// Ball-Floor Restitution Tests
// ============================================================================

TEST(BallFloorRestitution, SlowBounce_ModerateRestitution) {
    // Arrange - ball rolling off table edge
    float slowSpeed = 1.5f;

    // Act
    float e = RestitutionModel::CalculateBallFloorRestitution(slowSpeed);

    // Assert
    EXPECT_NEAR(e, 0.85f, 0.05f)
        << "Slow floor bounce should be moderately elastic";
}

TEST(BallFloorRestitution, FastBounce_MoreLoss) {
    // Arrange - ball falling from table height
    float tableHeight = 0.76f;
    float impactSpeed = std::sqrt(2.0f * 9.81f * tableHeight);

    // Act
    float e = RestitutionModel::CalculateBallFloorRestitution(impactSpeed);

    // Assert
    EXPECT_LT(e, 0.85f) << "Fast floor bounce should show energy loss";
    EXPECT_GT(e, 0.70f) << "Floor should still provide reasonable bounce";
}

TEST(BallFloorRestitution, FloorLessBouncyThanTable) {
    // Arrange - same impact speed
    float impactSpeed = 3.0f;

    // Act
    float floorE = RestitutionModel::CalculateBallFloorRestitution(impactSpeed);
    float tableE = RestitutionModel::CalculateBallTableRestitution(impactSpeed);

    // Assert
    EXPECT_LT(floorE, tableE)
        << "Floor should be less bouncy than table surface";
}

// ============================================================================
// Ball-Wall Restitution Tests
// ============================================================================

TEST(BallWallRestitution, WallAbsorbsSignificantEnergy) {
    // Arrange - ball hitting wall (drywall/painted surface)
    float impactSpeed = 3.0f;

    // Act
    float e = RestitutionModel::CalculateBallWallRestitution(impactSpeed);

    // Assert
    EXPECT_LT(e, 0.75f) << "Walls should absorb significant energy";
    EXPECT_GT(e, 0.60f) << "Walls should still provide some bounce";
}

TEST(BallWallRestitution, FastHit_MoreAbsorption) {
    // Arrange
    float slowHit = 2.0f;
    float fastHit = 8.0f;

    // Act
    float slowE = RestitutionModel::CalculateBallWallRestitution(slowHit);
    float fastE = RestitutionModel::CalculateBallWallRestitution(fastHit);

    // Assert
    EXPECT_GT(slowE, fastE)
        << "Faster wall hits should lose more energy";
}

// ============================================================================
// Comparative Restitution Tests
// ============================================================================

TEST(RestitutionComparison, SurfaceBounciness_RealisticOrdering) {
    // Arrange - same impact speed on different surfaces
    float impactSpeed = 4.0f;
    float mediumRubber = 0.5f;

    // Act
    float tableE = RestitutionModel::CalculateBallTableRestitution(impactSpeed);
    float floorE = RestitutionModel::CalculateBallFloorRestitution(impactSpeed);
    float paddleE = RestitutionModel::CalculateBallPaddleRestitution(impactSpeed, mediumRubber);
    float wallE = RestitutionModel::CalculateBallWallRestitution(impactSpeed);
    float netE = RestitutionModel::CalculateBallNetRestitution(impactSpeed);

    // Assert - expected ordering from most to least bouncy
    EXPECT_GT(tableE, floorE) << "Table should be bouncier than floor";
    EXPECT_GT(floorE, wallE) << "Floor should be bouncier than wall";
    EXPECT_GT(wallE, netE) << "Wall should be bouncier than net";

    // Net should be least bouncy
    EXPECT_LT(netE, 0.40f) << "Net should be the least bouncy surface";
}

TEST(RestitutionComparison, PaddleVsTableBounce_DependsOnRubber) {
    // Arrange
    float impactSpeed = 4.0f;
    float tableE = RestitutionModel::CalculateBallTableRestitution(impactSpeed);

    // Act
    float softPaddleE = RestitutionModel::CalculateBallPaddleRestitution(impactSpeed, 0.0f);
    float hardPaddleE = RestitutionModel::CalculateBallPaddleRestitution(impactSpeed, 1.0f);

    // Assert
    EXPECT_LT(softPaddleE, tableE)
        << "Soft paddle should be less bouncy than table";
    EXPECT_GT(hardPaddleE, softPaddleE)
        << "Hard paddle should be bouncier than soft paddle";
}
