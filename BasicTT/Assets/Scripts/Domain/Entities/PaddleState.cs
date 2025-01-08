using UnityEngine;

namespace Domain.Entities
{
    /// <summary>
    /// Represents the state of the paddle at a given time.
    /// </summary>
    public class PaddleState
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 AngularVelocity { get; set; }
        public BoxCollider LeftCollider { get; set; }
        public BoxCollider RightCollider { get; set; }
        public GameObject GameObject { get; set; }
    }
}
