using UnityEngine;

namespace Domain.Entities
{
    /// <summary>
    /// Represents collision data between two objects.
    /// </summary>
    public struct CollisionData
    {
        public bool Detected { get; set; }
        public Vector3 Point { get; set; }
        public Vector3 Normal { get; set; }
        public float TimeOfImpact { get; set; }
        public Collider Collider { get; set; }
    }
}
