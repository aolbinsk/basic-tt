#include <gtest/gtest.h>
#include "../../TestHelpers.h"
#include "domain/filters/KalmanFilterVector3.h"
#include "domain/filters/KalmanFilterQuaternion.h"
#include <random>

using namespace BasicTT;
using namespace BasicTT::Test;

// ============================================================================
// Vector3 Kalman Filter Tests
// ============================================================================

class KalmanVector3Test : public ::testing::Test {
protected:
    void SetUp() override {
        // Default VR tracking parameters
        float processNoise = 1e-5f;      // Position doesn't change much per frame
        float measurementNoise = 1e-2f;  // VR tracking has ~1cm noise
        filter = std::make_unique<KalmanFilterVector3>(processNoise, measurementNoise);
    }

    std::unique_ptr<KalmanFilterVector3> filter;
};

TEST_F(KalmanVector3Test, StationaryController_SmoothesMeasurements) {
    // Arrange - controller held still at a position, but VR tracking is noisy
    Vector3 truePosition(1.0f, 1.5f, 0.5f);
    std::default_random_engine generator(42);  // Fixed seed for reproducibility
    std::normal_distribution<float> noise(0.0f, 0.01f);  // ±1cm noise

    // Act - simulate 60 frames of noisy measurements
    Vector3 lastEstimate = Vector3::Zero();
    for (int i = 0; i < 60; i++) {
        Vector3 noisyMeasurement(
            truePosition.x + noise(generator),
            truePosition.y + noise(generator),
            truePosition.z + noise(generator)
        );
        lastEstimate = filter->Update(noisyMeasurement);
    }

    // Assert - after convergence, estimate should be close to true position
    EXPECT_NEAR(lastEstimate.x, truePosition.x, 0.005f)
        << "Filtered X should converge to true position";
    EXPECT_NEAR(lastEstimate.y, truePosition.y, 0.005f)
        << "Filtered Y should converge to true position";
    EXPECT_NEAR(lastEstimate.z, truePosition.z, 0.005f)
        << "Filtered Z should converge to true position";
}

TEST_F(KalmanVector3Test, NoisyVRTracking_ReducesJitter) {
    // Arrange - measure variance before and after filtering
    Vector3 staticPosition(0.5f, 1.2f, -0.3f);
    std::default_random_engine generator(42);
    std::normal_distribution<float> noise(0.0f, 0.01f);

    std::vector<float> rawVariance, filteredVariance;
    Vector3 rawSum = Vector3::Zero(), filteredSum = Vector3::Zero();

    // Act - collect measurements
    for (int i = 0; i < 100; i++) {
        Vector3 rawMeasurement(
            staticPosition.x + noise(generator),
            staticPosition.y + noise(generator),
            staticPosition.z + noise(generator)
        );
        Vector3 filteredMeasurement = filter->Update(rawMeasurement);

        if (i > 20) {  // Skip initial convergence period
            rawVariance.push_back(rawMeasurement.x);
            filteredVariance.push_back(filteredMeasurement.x);
        }
    }

    // Calculate variance
    float rawMean = 0.0f, filteredMean = 0.0f;
    for (size_t i = 0; i < rawVariance.size(); i++) {
        rawMean += rawVariance[i];
        filteredMean += filteredVariance[i];
    }
    rawMean /= rawVariance.size();
    filteredMean /= filteredVariance.size();

    float rawVar = 0.0f, filteredVar = 0.0f;
    for (size_t i = 0; i < rawVariance.size(); i++) {
        rawVar += (rawVariance[i] - rawMean) * (rawVariance[i] - rawMean);
        filteredVar += (filteredVariance[i] - filteredMean) * (filteredVariance[i] - filteredMean);
    }
    rawVar /= rawVariance.size();
    filteredVar /= filteredVariance.size();

    // Assert - filtered signal should have much lower variance
    EXPECT_LT(filteredVar, rawVar * 0.2f)
        << "Kalman filter should reduce position jitter by at least 80%";
}

TEST_F(KalmanVector3Test, MovingController_TracksPosition) {
    // Arrange - controller moving in straight line
    Vector3 startPos(0.0f, 1.0f, 0.0f);
    Vector3 velocity(0.5f, 0.0f, 0.0f);  // Moving 50cm/s in X
    float dt = 1.0f / 60.0f;  // 60 Hz

    std::default_random_engine generator(42);
    std::normal_distribution<float> noise(0.0f, 0.01f);

    // Act - simulate 60 frames of movement
    Vector3 truePos = startPos;
    Vector3 lastEstimate = Vector3::Zero();
    for (int i = 0; i < 60; i++) {
        truePos = truePos + velocity * dt;

        Vector3 noisyMeasurement(
            truePos.x + noise(generator),
            truePos.y + noise(generator),
            truePos.z + noise(generator)
        );
        lastEstimate = filter->Update(noisyMeasurement);
    }

    // Assert - filter should track moving position
    Vector3 expectedPos = startPos + velocity * (60.0f * dt);
    EXPECT_NEAR(lastEstimate.x, expectedPos.x, 0.05f)
        << "Filter should track moving controller in X";
    EXPECT_NEAR(lastEstimate.y, expectedPos.y, 0.02f)
        << "Filter should track stationary Y position";
}

TEST_F(KalmanVector3Test, Reset_ClearsState) {
    // Arrange - filter with some history
    filter->Update(Vector3(1.0f, 2.0f, 3.0f));
    filter->Update(Vector3(1.1f, 2.1f, 3.1f));
    filter->Update(Vector3(1.2f, 2.2f, 3.2f));

    // Act
    filter->Reset();
    Vector3 estimate = filter->GetEstimate();

    // Assert - should reset to zero
    ExpectVectorNear(estimate, Vector3::Zero(), 0.001f);
}

TEST_F(KalmanVector3Test, HighProcessNoise_MoreResponsive) {
    // Arrange - two filters with different process noise
    KalmanFilterVector3 lowNoiseFilter(1e-6f, 1e-2f);   // Very stable
    KalmanFilterVector3 highNoiseFilter(1e-3f, 1e-2f);  // More responsive

    // Initialize both at same position
    Vector3 initialPos(1.0f, 1.0f, 1.0f);
    for (int i = 0; i < 20; i++) {
        lowNoiseFilter.Update(initialPos);
        highNoiseFilter.Update(initialPos);
    }

    // Act - sudden position change
    Vector3 newPos(2.0f, 2.0f, 2.0f);
    Vector3 lowResult = lowNoiseFilter.Update(newPos);
    Vector3 highResult = highNoiseFilter.Update(newPos);

    // Assert - high process noise filter should react faster
    float lowDist = (lowResult - newPos).Magnitude();
    float highDist = (highResult - newPos).Magnitude();

    EXPECT_LT(highDist, lowDist)
        << "Higher process noise should make filter more responsive to changes";
}

// ============================================================================
// Quaternion Kalman Filter Tests
// ============================================================================

class KalmanQuaternionTest : public ::testing::Test {
protected:
    void SetUp() override {
        float processNoise = 1e-5f;
        float measurementNoise = 1e-2f;
        filter = std::make_unique<KalmanFilterQuaternion>(processNoise, measurementNoise);
    }

    std::unique_ptr<KalmanFilterQuaternion> filter;
};

TEST_F(KalmanQuaternionTest, StationaryController_SmoothesRotation) {
    // Arrange - controller held at fixed rotation, noisy tracking
    Quaternion trueRotation = Quaternion::Identity();
    std::default_random_engine generator(42);
    std::normal_distribution<float> noise(0.0f, 0.01f);

    // Act - simulate 60 frames
    Quaternion lastEstimate = Quaternion::Identity();
    for (int i = 0; i < 60; i++) {
        Quaternion noisyMeasurement(
            trueRotation.w + noise(generator),
            trueRotation.x + noise(generator),
            trueRotation.y + noise(generator),
            trueRotation.z + noise(generator)
        );
        noisyMeasurement = noisyMeasurement.Normalized();
        lastEstimate = filter->Update(noisyMeasurement);
    }

    // Assert - should converge close to identity
    EXPECT_NEAR(lastEstimate.w, 1.0f, 0.02f)
        << "Filtered rotation should be close to identity";
}

TEST_F(KalmanQuaternionTest, RotatingController_TracksOrientation) {
    // Arrange - controller rotating around Y axis
    float angularVelocity = 90.0f;  // degrees per second
    float dt = 1.0f / 60.0f;
    float anglePerFrame = angularVelocity * dt;

    std::default_random_engine generator(42);
    std::normal_distribution<float> noise(0.0f, 0.01f);

    // Act - simulate rotation
    Quaternion currentRotation = Quaternion::Identity();
    Quaternion lastEstimate = Quaternion::Identity();

    for (int i = 0; i < 30; i++) {
        // Rotate by small angle
        Quaternion deltaRot = Quaternion::FromAxisAngle(Vector3::Up(), anglePerFrame);
        currentRotation = currentRotation * deltaRot;

        Quaternion noisyMeasurement(
            currentRotation.w + noise(generator),
            currentRotation.x + noise(generator),
            currentRotation.y + noise(generator),
            currentRotation.z + noise(generator)
        );
        noisyMeasurement = noisyMeasurement.Normalized();
        lastEstimate = filter->Update(noisyMeasurement);
    }

    // Assert - should be tracking the rotation
    // After 30 frames at 90 deg/s and 60Hz = 45 degree rotation
    float expectedAngle = 45.0f;
    Quaternion expectedRot = Quaternion::FromAxisAngle(Vector3::Up(), expectedAngle);

    // Check if quaternions are close (either q or -q represent same rotation)
    float dot = std::abs(
        lastEstimate.w * expectedRot.w +
        lastEstimate.x * expectedRot.x +
        lastEstimate.y * expectedRot.y +
        lastEstimate.z * expectedRot.z
    );

    EXPECT_GT(dot, 0.95f)
        << "Filter should track rotating controller orientation";
}

TEST_F(KalmanQuaternionTest, Reset_ClearsRotationState) {
    // Arrange - filter with rotation history
    Quaternion rot1 = Quaternion::FromAxisAngle(Vector3::Up(), 30.0f);
    filter->Update(rot1);
    filter->Update(rot1);

    // Act
    filter->Reset();
    Quaternion estimate = filter->GetEstimate();

    // Assert - should reset
    EXPECT_NEAR(estimate.w, 0.0f, 0.1f) << "Should reset w component";
    EXPECT_NEAR(estimate.x, 0.0f, 0.1f) << "Should reset x component";
}

// ============================================================================
// VR Tracking Scenario Tests
// ============================================================================

TEST(VRTrackingScenario, PaddlePositionFiltering_ReducesJitter) {
    // Arrange - simulating VR controller tracking a paddle
    KalmanFilterVector3 posFilter(1e-5f, 1e-2f);

    Vector3 paddleRestPosition(0.3f, 1.2f, -0.4f);  // Player's natural paddle hold
    std::default_random_engine generator(42);
    std::normal_distribution<float> trackingNoise(0.0f, 0.005f);  // ±5mm VR noise

    // Act - simulate hand holding paddle steady (with natural micro-tremor)
    std::vector<Vector3> rawPositions, filteredPositions;
    for (int frame = 0; frame < 120; frame++) {  // 2 seconds at 60Hz
        // Simulate VR tracking noise
        Vector3 noisyPos(
            paddleRestPosition.x + trackingNoise(generator),
            paddleRestPosition.y + trackingNoise(generator),
            paddleRestPosition.z + trackingNoise(generator)
        );

        Vector3 filtered = posFilter.Update(noisyPos);

        if (frame > 30) {  // After convergence
            rawPositions.push_back(noisyPos);
            filteredPositions.push_back(filtered);
        }
    }

    // Measure jitter (max deviation from mean)
    Vector3 rawMean = Vector3::Zero(), filteredMean = Vector3::Zero();
    for (const auto& p : rawPositions) rawMean = rawMean + p;
    for (const auto& p : filteredPositions) filteredMean = filteredMean + p;
    rawMean = rawMean / static_cast<float>(rawPositions.size());
    filteredMean = filteredMean / static_cast<float>(filteredPositions.size());

    float maxRawJitter = 0.0f, maxFilteredJitter = 0.0f;
    for (size_t i = 0; i < rawPositions.size(); i++) {
        maxRawJitter = std::max(maxRawJitter, (rawPositions[i] - rawMean).Magnitude());
        maxFilteredJitter = std::max(maxFilteredJitter,
            (filteredPositions[i] - filteredMean).Magnitude());
    }

    // Assert - filtered should have much less jitter
    EXPECT_LT(maxFilteredJitter, maxRawJitter * 0.3f)
        << "Kalman filter should reduce paddle position jitter by 70%+";
    EXPECT_LT(maxFilteredJitter, 0.002f)
        << "Filtered paddle jitter should be under 2mm for stable feel";
}

TEST(VRTrackingScenario, SwingingPaddle_MaintainsResponsiveness) {
    // Arrange - player swinging paddle for a shot
    KalmanFilterVector3 posFilter(1e-5f, 1e-2f);

    std::default_random_engine generator(42);
    std::normal_distribution<float> trackingNoise(0.0f, 0.005f);

    // Paddle swing trajectory: forehand from right to left
    Vector3 startPos(0.5f, 1.0f, -0.3f);
    Vector3 endPos(-0.2f, 1.3f, 0.2f);
    int swingFrames = 20;  // ~333ms swing

    // Act - simulate swing
    Vector3 lastRaw, lastFiltered;
    for (int frame = 0; frame <= swingFrames; frame++) {
        float t = static_cast<float>(frame) / static_cast<float>(swingFrames);

        // True paddle position during swing
        Vector3 truePos = Vector3::Lerp(startPos, endPos, t);

        // VR tracking with noise
        Vector3 noisyPos(
            truePos.x + trackingNoise(generator),
            truePos.y + trackingNoise(generator),
            truePos.z + trackingNoise(generator)
        );

        Vector3 filtered = posFilter.Update(noisyPos);

        lastRaw = noisyPos;
        lastFiltered = filtered;
    }

    // Assert - filter should not introduce significant lag
    float lag = (lastFiltered - lastRaw).Magnitude();
    EXPECT_LT(lag, 0.02f)
        << "Filter lag should be minimal (<2cm) to maintain responsive feel";

    // Final filtered position should be near end of swing
    EXPECT_NEAR(lastFiltered.x, endPos.x, 0.05f)
        << "Filter should track paddle swing accurately";
}
