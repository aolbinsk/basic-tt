using UnityEngine;

namespace Domain.Config
{
    /// <summary>
    /// Configuration for ball physics properties.
    /// </summary>
    public class BallConfig
    {
        // Constants
        private const float DEFAULT_DIAMETER_MM = 40f;
        private const float DEFAULT_MASS_GRAMS = 2.7f;
        private const float MM_TO_METERS = 0.001f;
        private const float GRAMS_TO_KG = 0.001f;
        
        // Properties
        public float DiameterMeters => DEFAULT_DIAMETER_MM * MM_TO_METERS;
        public float MassKg => DEFAULT_MASS_GRAMS * GRAMS_TO_KG;
        public float DragCoefficient => 0.47f;
        public float CrossSectionalArea => Mathf.PI * Mathf.Pow(DiameterMeters / 2f, 2);
        public float MaxThrowVelocity => 8.0f;
        public float MinThrowVelocity => 0.1f;
        public PhysicsMaterial Material { get; set; }
    }
}