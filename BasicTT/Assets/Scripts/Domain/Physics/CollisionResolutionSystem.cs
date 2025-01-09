using Domain.Entities;
using Domain.Interfaces;
using UnityEngine;

namespace Domain.Physics
{
    /// <summary>
    /// Resolves collisions and updates the ball's physics state, including spin and aerodynamic effects.
    /// </summary>
    public class CollisionResolutionSystem
    {
        private const float MIN_VELOCITY_THRESHOLD = 0.2f;
        private const float SLEEP_PREPARATION_THRESHOLD = 0.4f;
        private const float CONTACT_OFFSET = 0.001f;

        /// <summary>
        /// Resolves a collision and updates the ball's velocity, position, and spin.
        /// </summary>
        /// <param name="ball">The current state of the ball.</param>
        /// <param name="paddle">The state of the paddle involved in the collision, if any.</param>
        /// <param name="collision">The collision data.</param>
        /// <param name="config">Physics configuration parameters.</param>
        public void ResolveCollision(ref BallState ball, PaddleState paddle, CollisionData collision, IPhysicsConfig config)
        {
            Vector3 incomingVelocity = ball.Velocity;
            Vector3 normal = collision.Normal.normalized;
            float restitution;

            if (paddle != null)
            {
                // Collision with paddle
                bool isForehand = ReferenceEquals(collision.Collider, paddle.ForehandCollider);
                float spinMultiplier = isForehand ? config.Paddle.LeftSideSpinMultiplier : config.Paddle.RightSideSpinMultiplier;
                float throwMultiplier = isForehand ? config.Paddle.LeftSideThrowMultiplier : config.Paddle.RightSideThrowMultiplier;

                restitution = config.Paddle.RubberBounciness;

                Vector3 relativeVelocity = ball.Velocity - paddle.Velocity;
                Vector3 newRelativeVelocity = relativeVelocity - (1 + restitution) * Vector3.Dot(relativeVelocity, normal) * normal;

                newRelativeVelocity *= throwMultiplier;
                ball.Velocity = newRelativeVelocity + paddle.Velocity;

                Vector3 spinAxis = Vector3.Cross(normal, relativeVelocity).normalized;
                float spinMagnitude = relativeVelocity.magnitude * config.Player.SpinTransferCoefficient * spinMultiplier;
                ball.AngularVelocity += spinAxis * spinMagnitude;
            }
            else
            {
                // Collision with environment
                restitution = config.Table.BounceRestitution;
                ball.Velocity = Vector3.Reflect(incomingVelocity, normal) * restitution;
                ball.AngularVelocity *= (1f - config.Table.Friction);
            }

            ball.Position = collision.Point + collision.Normal * CONTACT_OFFSET;

            if (ball.Velocity.magnitude < SLEEP_PREPARATION_THRESHOLD)
            {
                if (collision.Normal.y > 0.7f)
                {
                    ball.Velocity *= 0.8f;
                    ball.AngularVelocity *= 0.8f;
                }
                else
                {
                    ball.Velocity = Vector3.ProjectOnPlane(ball.Velocity, collision.Normal) * 0.9f;
                }
            }

            // Ensure ball velocity does not fall below a minimum threshold
            if (ball.Velocity.magnitude < MIN_VELOCITY_THRESHOLD)
            {
                ball.Velocity = Vector3.zero;
                ball.AngularVelocity = Vector3.zero;
            }
        }
    }
}