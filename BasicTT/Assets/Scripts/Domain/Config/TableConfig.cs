using UnityEngine;

namespace Domain.Config
{
    /// <summary>
    /// Configuration for table properties.
    /// </summary>
    public class TableConfig
    {
        // Constants
        private const float DEFAULT_LENGTH_METERS = 2.74f;   // Regulation length
        private const float DEFAULT_WIDTH_METERS = 1.525f;   // Regulation width
        private const float DEFAULT_HEIGHT_METERS = 0.76f;   // Regulation height
        private const float DEFAULT_THICKNESS_METERS = 0.02f; // Standard thickness
        private const float DEFAULT_NET_HEIGHT_METERS = 0.1525f; // Regulation net height
        private const float DEFAULT_FRICTION = 0.2f;
        private const float DEFAULT_RESTITUTION = 0.8f;

        // Properties
        public float LengthMeters => DEFAULT_LENGTH_METERS;
        public float WidthMeters => DEFAULT_WIDTH_METERS;
        public float HeightMeters => DEFAULT_HEIGHT_METERS;
        public float ThicknessMeters => DEFAULT_THICKNESS_METERS;
        public float NetHeightMeters => DEFAULT_NET_HEIGHT_METERS;
        
        public float BounceRestitution { get; set; } = DEFAULT_RESTITUTION;
        public float Friction { get; set; } = DEFAULT_FRICTION;

        public PhysicsMaterial TableMaterial { get; set; }
        public PhysicsMaterial NetMaterial { get; set; }
    }
}