using UnityEngine;

namespace Domain.Entities
{
    /// <summary>
    /// Represents the state of a hand (controller) at a given time.
    /// </summary>
    public class HandState
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public bool GripPressed { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 AngularVelocity { get; set; }
    }
}