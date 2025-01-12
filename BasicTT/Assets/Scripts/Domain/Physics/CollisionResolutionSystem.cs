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
        /// <summary>
        /// Resolves a collision and updates the ball's velocity, position, and spin.
        /// </summary>
        /// <param name="ball">The current state of the ball.</param>
        /// <param name="paddle">The state of the paddle involved in the collision, if any.</param>
        /// <param name="collision">The collision data.</param>
        /// <param name="config">Physics configuration parameters.</param>
        public void ResolveCollision(
            ref BallState ball, 
            PaddleState paddle, 
            CollisionData collision, 
            IPhysicsConfig config)
        {
            Vector3 incomingVelocity = ball.Velocity;
            Vector3 normal = collision.Normal.normalized;
            float restitution;

            if (collision.CollisionTag is "Forehand" or "Backhand")
            {
                //Debug.Log($"[CollisionResolutionSystem] Collision with {collision.CollisionTag} at {collision.Point} with normal {collision.Normal}");
                //Debug.Log($"[CollisionResolutionSystem] Incoming velocity: {incomingVelocity}, angular velocity: {ball.AngularVelocity}");

                // Collision with paddle
                bool isForehand = collision.CollisionTag == "Forehand";
                float spinMultiplier = isForehand ? config.Paddle.LeftSideSpinMultiplier : config.Paddle.RightSideSpinMultiplier;
                float throwMultiplier = isForehand ? config.Paddle.LeftSideThrowMultiplier : config.Paddle.RightSideThrowMultiplier;

                restitution = config.Paddle.RubberBounciness;

                Vector3 relativeVelocity = ball.Velocity - paddle.Velocity;
                Vector3 newRelativeVelocity = relativeVelocity 
                                              - (1 + restitution) * Vector3.Dot(relativeVelocity, normal) * normal;

                newRelativeVelocity *= throwMultiplier;
                ball.Velocity = newRelativeVelocity + paddle.Velocity;

                Vector3 spinAxis = Vector3.Cross(normal, relativeVelocity).normalized;
                float spinMagnitude = relativeVelocity.magnitude * config.Player.SpinTransferCoefficient * spinMultiplier;
                ball.AngularVelocity += spinAxis * spinMagnitude;
                
                //Debug.Log($"[CollisionResolutionSystem] Spin axis: {spinAxis}, spin magnitude: {spinMagnitude}");
                //Debug.Log($"[CollisionResolutionSystem] Outgoing velocity: {ball.Velocity}, angular velocity: {ball.AngularVelocity}");
            }
            else
            {
                // Collision with environment
                restitution = config.Table.BounceRestitution;
                ball.Velocity = Vector3.Reflect(incomingVelocity, normal) * restitution;
                ball.AngularVelocity *= (1f - config.Table.Friction);
            }
        }
    }
}