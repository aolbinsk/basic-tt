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

        // Properties
        public float LengthMeters => DEFAULT_LENGTH_METERS;
        public float WidthMeters => DEFAULT_WIDTH_METERS;
        public float HeightMeters => DEFAULT_HEIGHT_METERS;
        public float ThicknessMeters => DEFAULT_THICKNESS_METERS;
        public float NetHeightMeters => DEFAULT_NET_HEIGHT_METERS;
        
        public float BounceRestitution => 0.85f;
        public float Friction => 0.2f;
        
        public PhysicsMaterial TableMaterial { get; set; }
        public PhysicsMaterial NetMaterial { get; set; }
    }
}