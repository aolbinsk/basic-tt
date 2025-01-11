using Domain.Interfaces;
using UnityEngine;

namespace Domain.Config
{
    /// <summary>
    /// Configuration for physics properties.
    /// </summary>
    public class PhysicsConfig : IPhysicsConfig
    {
        private Vector3 _gravity = UnityEngine.Physics.gravity;
        
        // Constants
        private const float DEFAULT_AIR_DENSITY = 1.225f; // kg/m^3 at sea level
        private const float DEFAULT_MAGNUS_COEFFICIENT = 0.0001f;
        private const float DEFAULT_ANGULAR_DRAG_COEFFICIENT = 0.1f;
        private const float DEFAULT_SPIN_TRANSFER_COEFFICIENT = 0.5f;

        // Properties
        public float AirDensity => DEFAULT_AIR_DENSITY;
        public float MagnusCoefficient => DEFAULT_MAGNUS_COEFFICIENT;
        public float AngularDragCoefficient => DEFAULT_ANGULAR_DRAG_COEFFICIENT;
        public float SpinTransferCoefficient => DEFAULT_SPIN_TRANSFER_COEFFICIENT;
        
        public Vector3 Gravity
        {
            get => _gravity;
            set => _gravity = value;
        }

        public int BallLayerMask { get; set; }
        public int EnvironmentLayerMask { get; set; }

        // Sub-configurations
        public BallConfig Ball { get; } = new();
        public AirConfig Air { get; set; } = new();
        public PaddleConfig Paddle { get; } = new();
        public TableConfig Table { get; } = new();
        public PlayerConfig Player { get; } = new();
        public RoomConfig Room { get; } = new();
    }
}