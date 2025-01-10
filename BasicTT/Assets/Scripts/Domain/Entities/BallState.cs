using UnityEngine;

namespace Domain.Entities
{
    /// <summary>
    /// Represents the state of the ball at a given time.
    /// </summary>
    public class BallState
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 AngularVelocity { get; set; }

        /// <summary>
        /// Indicates whether the ball is currently held by the player.
        /// </summary>
        public bool IsHeld { get; set; }
        
        public BallState Clone()
        {
            return new BallState
            {
                Position = Position,
                Velocity = Velocity,
                AngularVelocity = AngularVelocity,
                Rotation = Rotation,
                IsHeld = IsHeld
            };
        }        
    }
}