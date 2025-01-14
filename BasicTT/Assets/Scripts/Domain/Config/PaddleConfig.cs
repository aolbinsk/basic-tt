using Domain.Physics;
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
        private const float DefaultHeadLengthMeters = 0.17f; // 17cm
        private const float DefaultHeadBladeThicknessMeters = 0.01f; // 1cm
        private const float DefaultRubberThicknessMeters = 0.002f; // 2mm
        private const float DefaultHandleLengthMeters = 0.10f; // 10cm
        private const float DefaultHandleRadiusMeters = 0.013f; // 1.3cm
        private const float DefaultFrictionCoefficient = 0.4f; // Tacky rubber, 0.8 for normal


        // Properties
        public float HeadWidthMeters => DefaultHeadWidthMeters;
        public float HeadLengthMeters => DefaultHeadLengthMeters;
        public float HeadBladeThicknessMeters => DefaultHeadBladeThicknessMeters;
        public float HeadRubberThicknessMeters => DefaultRubberThicknessMeters;
        public float HandleLengthMeters => DefaultHandleLengthMeters;
        public float HandleRadiusMeters => DefaultHandleRadiusMeters;

        public float FrictionCoefficient => DefaultFrictionCoefficient;
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
                BladeHalfExtents = new Vector3(
                    HeadBladeThicknessMeters / 2f,
                    HeadWidthMeters / 2f,
                    HeadLengthMeters / 2f),

                ForehandRubberCenter = new Vector3(
                    -(HeadBladeThicknessMeters / 2f + HeadRubberThicknessMeters / 2f),
                    0f,
                    0f),
                
                ForehandRubberHalfExtents = new Vector3(
                    HeadRubberThicknessMeters / 2f,
                    HeadWidthMeters / 2f,
                    HeadLengthMeters / 2f),

                BackhandRubberCenter = new Vector3(
                    (HeadBladeThicknessMeters / 2f + HeadRubberThicknessMeters / 2f),
                    0f,
                    0f),
                
                BackhandRubberHalfExtents = new Vector3(
                    HeadRubberThicknessMeters / 2f,
                    HeadWidthMeters / 2f,
                    HeadLengthMeters / 2f)
            };
        }
    }

    /// <summary>
    /// Represents the geometry of the paddle for collision detection.
    /// </summary>
    public class PaddleGeometry
    {
        public Vector3 BladeHalfExtents { get; set; }
        public Vector3 ForehandRubberCenter { get; set; }
        public Vector3 BackhandRubberCenter { get; set; }
        public Vector3 ForehandRubberHalfExtents { get; set; }
        public Vector3 BackhandRubberHalfExtents { get; set; }
    }
}