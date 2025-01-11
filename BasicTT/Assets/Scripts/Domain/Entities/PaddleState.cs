using UnityEngine;

namespace Domain.Entities
{
    /// <summary>
    /// Represents the state of the paddle at a given time.
    /// </summary>
    public class PaddleState
    {
        public Vector3 Position { get; set; } = Vector3.zero;
        public Quaternion Rotation { get; set; } = Quaternion.identity;
        public Vector3 Velocity { get; set; } = Vector3.zero;
        public Vector3 AngularVelocity { get; set; } = Vector3.zero;

        /// <summary>
        /// Creates a deep copy of the current PaddleState.
        /// </summary>
        /// <returns>A new PaddleState instance with the same values.</returns>
        public PaddleState Clone()
        {
            return new PaddleState
            {
                Position = Position,
                Rotation = Rotation,
                Velocity = Velocity,
                AngularVelocity = AngularVelocity
            };
        }
    }
}