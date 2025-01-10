using UnityEngine;

namespace Domain.Config
{
    /// <summary>
    /// Configuration for paddle properties.
    /// </summary>
    public class PaddleConfig
    {
        // Constants
        private const float DefaultHeadWidthMeters = 0.1525f;  // 15.25cm
        private const float DefaultHeadLengthMeters = DefaultHeadWidthMeters;
        private const float DEFAULT_HEAD_THICKNESS_METERS = 0.02f; // 2cm
        private const float DEFAULT_HANDLE_LENGTH_METERS = 0.10f; // 10cm
        private const float DEFAULT_HANDLE_RADIUS_METERS = 0.02f; // 2cm

        // Properties
        public float HeadWidthMeters => DefaultHeadWidthMeters;
        public float HeadLengthMeters => DefaultHeadLengthMeters;
        public float HeadThicknessMeters => DEFAULT_HEAD_THICKNESS_METERS;
        public float HandleLengthMeters => DEFAULT_HANDLE_LENGTH_METERS;
        public float HandleRadiusMeters => DEFAULT_HANDLE_RADIUS_METERS;

        public float RubberBounciness => 0.75f;
        public float ThrowMultiplier => 1.0f;
        public float SpinMultiplier => 0.8f;

        public float LeftSideSpinMultiplier => 1.0f;
        public float RightSideSpinMultiplier => 1.0f;
        public float LeftSideThrowMultiplier => 1.0f;
        public float RightSideThrowMultiplier => 1.0f;

        public PhysicsMaterial Material { get; set; }

        // Calibration offsets
        public Vector3 CalibrationPositionOffset { get; set; } = Vector3.zero;
        public Quaternion CalibrationRotationOffset { get; set; } = Quaternion.identity;
    }
}