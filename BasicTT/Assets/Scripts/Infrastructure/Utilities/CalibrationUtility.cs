using Domain.Config;
using UnityEngine;

namespace Infrastructure.Utilities
{
    public static class CalibrationUtility
    {
        /// <summary>
        /// Applies calibration offsets to the given position and rotation.
        /// </summary>
        /// <param name="basePosition">The base position to adjust.</param>
        /// <param name="baseRotation">The base rotation to adjust.</param>
        /// <param name="calibration">The calibration offsets.</param>
        /// <returns>A tuple containing the adjusted position and rotation.</returns>
        public static (Vector3 Position, Quaternion Rotation) ApplyCalibration(
            Vector3 basePosition, Quaternion baseRotation, PaddleCalibration calibration)
        {
            Vector3 adjustedPosition = basePosition + baseRotation * calibration.PositionOffset;
            Quaternion adjustedRotation = baseRotation * calibration.RotationOffset;
            return (adjustedPosition, adjustedRotation);
        }
    }

}