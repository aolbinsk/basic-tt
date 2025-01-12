namespace Domain.Physics
{
    using Domain.Entities;
    using Domain.Interfaces;
    using UnityEngine;

    /// <summary>
    /// Provides utility methods for handling ball throwing logic during a serve.
    /// </summary>
    public static class BallThrowLogic
    {
        public static void HoldBall(ref BallState ball, ControllerState controllerState)
        {
            if (!ball.IsHeld)
            {
                ball.IsHeld = true;
            }
            ball.Position = controllerState.Position;
            ball.Rotation = controllerState.Rotation;
            ball.Velocity = controllerState.Velocity;
            ball.AngularVelocity = controllerState.AngularVelocity;
        }   

        /// <summary>
        /// Releases the ball from the player's hand, imparting velocity based on the controller's movement.
        /// Ensures compliance with ITTF regulations by preventing spin on release and enforcing minimum upward velocity.
        /// </summary>
        /// <param name="ball">The ball state to be updated.</param>
        /// <param name="controllerState">The state of the controller at the time of release.</param>
        /// <param name="config">The physics configuration.</param>
        public static void ReleaseBall(ref BallState ball, ControllerState controllerState, IPhysicsConfig config)
        {
            // Transition to released state
            ball.IsHeld = false;

            // Set linear velocity (clamp to max/min if needed)
            ball.Position = controllerState.Position;
            ball.Rotation = controllerState.Rotation;
            ball.Velocity = controllerState.Velocity;
            // Set angular velocity to zero (per ITTF regulations)
            ball.AngularVelocity = Vector3.zero;

            // Ensure a minimum upward velocity
            if (ball.Velocity.y < config.Ball.MinThrowVelocity)
            {
                var vector3 = ball.Velocity;
                vector3.y = config.Ball.MinThrowVelocity;
                ball.Velocity = vector3;
                Debug.LogWarning($"Ball velocity clamped to {config.Ball.MinThrowVelocity} m/s");
            }

            // Clamp to maximum throw velocity
            float maxVelocity = config.Ball.MaxThrowVelocity;
            if (ball.Velocity.magnitude > maxVelocity)
            {
                ball.Velocity = ball.Velocity.normalized * maxVelocity;
                Debug.LogWarning($"Ball velocity clamped to {maxVelocity} m/s");
            }
        }
    }
}