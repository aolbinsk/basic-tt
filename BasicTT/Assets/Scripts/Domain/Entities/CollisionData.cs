using UnityEngine;

namespace Domain.Entities
{
    /// <summary>
    /// Represents collision data between two objects.
    /// </summary>
    public struct CollisionData
    {
        /// <summary>
        /// Indicates whether a collision was detected.
        /// </summary>
        public bool Detected { get; set; }

        /// <summary>
        /// The point of collision in world space.
        /// </summary>
        public Vector3 Point { get; set; }

        /// <summary>
        /// The normal vector at the point of collision.
        /// </summary>
        public Vector3 Normal { get; set; }

        /// <summary>
        /// The time of impact during the physics step.
        /// </summary>
        public float TimeOfImpact { get; set; }

        /// <summary>
        /// Identifies which side of the paddle or environment was hit.
        /// </summary>
        public string CollisionTag { get; set; }
    }
}