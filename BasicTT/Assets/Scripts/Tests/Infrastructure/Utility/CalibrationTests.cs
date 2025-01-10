using Domain.Config;
using Infrastructure.Utilities;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Infrastructure.Utility
{
    [TestFixture]
    public class CalibrationTests
    {
        [Test]
        public void CalibrationUtility_AppliesOffsetsCorrectly()
        {
            // Arrange
            Vector3 basePosition = new Vector3(1f, 2f, 3f);
            Quaternion baseRotation = Quaternion.Euler(10f, 20f, 30f);
            var calibration = new PaddleCalibration
            {
                PositionOffset = new Vector3(0.1f, 0.2f, 0.3f),
                RotationOffset = Quaternion.Euler(5f, 10f, 15f)
            };

            // Act
            var result = CalibrationUtility.ApplyCalibration(basePosition, baseRotation, calibration);

            // Assert
            Vector3 expectedPosition = basePosition + baseRotation * calibration.PositionOffset;
            Quaternion expectedRotation = baseRotation * calibration.RotationOffset;

            Assert.AreEqual(expectedPosition, result.Position, "Calibrated position is incorrect.");
            Assert.AreEqual(expectedRotation.eulerAngles, result.Rotation.eulerAngles, "Calibrated rotation is incorrect.");
        }
        
        [Test]
        public void CalibrationUtility_HandlesZeroOffsets()
        {
            // Arrange
            Vector3 basePosition = new Vector3(1f, 2f, 3f);
            Quaternion baseRotation = Quaternion.Euler(10f, 20f, 30f);
            var calibration = new PaddleCalibration
            {
                PositionOffset = Vector3.zero,
                RotationOffset = Quaternion.identity
            };

            // Act
            var result = CalibrationUtility.ApplyCalibration(basePosition, baseRotation, calibration);

            // Assert
            Assert.AreEqual(basePosition, result.Position, "Position should remain unchanged for zero offsets.");
            Assert.AreEqual(baseRotation.eulerAngles, result.Rotation.eulerAngles, "Rotation should remain unchanged for zero offsets.");
        }

        [Test]
        public void CalibrationUtility_HandlesNegativeOffsets()
        {
            // Arrange
            Vector3 basePosition = new Vector3(1f, 2f, 3f);
            Quaternion baseRotation = Quaternion.Euler(10f, 20f, 30f);
            var calibration = new PaddleCalibration
            {
                PositionOffset = new Vector3(-0.1f, -0.2f, -0.3f),
                RotationOffset = Quaternion.Euler(-5f, -10f, -15f)
            };

            // Act
            var result = CalibrationUtility.ApplyCalibration(basePosition, baseRotation, calibration);

            // Assert
            Vector3 expectedPosition = basePosition + baseRotation * calibration.PositionOffset;
            Quaternion expectedRotation = baseRotation * calibration.RotationOffset;

            Assert.AreEqual(expectedPosition, result.Position, "Calibrated position is incorrect with negative offsets.");
            Assert.AreEqual(expectedRotation.eulerAngles, result.Rotation.eulerAngles, "Calibrated rotation is incorrect with negative offsets.");
        }
    }
}