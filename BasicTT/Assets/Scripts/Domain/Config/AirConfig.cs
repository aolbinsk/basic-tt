using UnityEngine;

namespace Domain.Config
{
    /// <summary>
    /// Configuration for air properties affecting ball physics.
    /// </summary>
    public class AirConfig
    {
        // Constants
        private const float DEFAULT_AIR_DENSITY = 1.225f; // kg/m^3 at sea level
        private const float DEFAULT_MAGNUS_COEFFICIENT = 0.0001f;
        private const float DEFAULT_ANGULAR_DRAG_COEFFICIENT = 0.1f;

        public float Density { get; set; } = DEFAULT_AIR_DENSITY;
        public float MagnusCoefficient { get; set; } = DEFAULT_MAGNUS_COEFFICIENT;
        public float AngularDragCoefficient { get; set; } = DEFAULT_ANGULAR_DRAG_COEFFICIENT;
    }
}