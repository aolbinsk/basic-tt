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
        private const float DefaultHeadThicknessMeters = 0.02f; // 2cm
        private const float DefaultHandleLengthMeters = 0.10f; // 10cm
        private const float DefaultHandleRadiusMeters = 0.02f; // 2cm

        // Properties
        public float HeadWidthMeters => DefaultHeadWidthMeters;
        public float HeadLengthMeters => DefaultHeadLengthMeters;
        public float HeadThicknessMeters => DefaultHeadThicknessMeters;
        public float HandleLengthMeters => DefaultHandleLengthMeters;
        public float HandleRadiusMeters => DefaultHandleRadiusMeters;

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

        // Paddle geometry for collision detection
        public PaddleGeometry Geometry { get; private set; }

        /// <summary>
        /// Initializes a new instance of the PaddleConfig class.
        /// </summary>
        public PaddleConfig()
        {
            Geometry = new PaddleGeometry
            {
                // Now interpret the geometry as X= length, Y= thickness, Z= width
                ForehandLocalCenter = new Vector3(0, 0, +HeadWidthMeters/2f),
                BackhandLocalCenter = new Vector3(0, 0, -HeadWidthMeters/2f),
                HalfExtents = new Vector3(HeadLengthMeters/2f, HeadThicknessMeters/4f, HeadWidthMeters/2f),
            };
        }
    }

    /// <summary>
    /// Represents the geometry of the paddle for collision detection.
    /// </summary>
    public class PaddleGeometry
    {
        public Vector3 ForehandLocalCenter { get; set; }
        public Vector3 BackhandLocalCenter { get; set; }
        public Vector3 HalfExtents { get; set; }
    }
}