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
                Debug.Log(
                    $"[CollisionResolutionSystem] Collision with {collision.CollisionTag} at {collision.Point} with normal {collision.Normal}");
                Debug.Log(
                    $"[CollisionResolutionSystem] Incoming velocity: {incomingVelocity}, angular velocity: {ball.AngularVelocity}");

                // Collision with paddle
                bool isForehand = collision.CollisionTag == "Forehand";
                float spinMultiplier =
                    isForehand ? config.Paddle.LeftSideSpinMultiplier : config.Paddle.RightSideSpinMultiplier;
                float throwMultiplier = isForehand
                    ? config.Paddle.LeftSideThrowMultiplier
                    : config.Paddle.RightSideThrowMultiplier;

                restitution = config.Paddle.RubberBounciness;

                ResolveCollisionWithFriction(
                    ref ball,
                    paddle,
                    normal,
                    restitution,
                    config.Paddle.FrictionCoefficient,
                    config.Ball.DiameterMeters / 2f,
                    config);
            }
            else
            {
                // Collision with environment
                restitution = config.Table.BounceRestitution;
                ball.Velocity = Vector3.Reflect(incomingVelocity, normal) * restitution;
                ball.AngularVelocity *= (1f - config.Table.Friction);
            }
        }

        /// <summary>
        /// Resolves friction-based collision impulses and imparts spin to the ball.
        /// </summary>
        /// <param name="ball">The current state of the ball.</param>
        /// <param name="paddle">The state of the paddle (for paddle velocity).</param>
        /// <param name="collisionNormal">The collision normal (pointing outward from the paddle's surface).</param>
        /// <param name="restitution">The coefficient of restitution (bounciness).</param>
        /// <param name="frictionCoefficient">Friction coefficient for the rubber contact.</param>
        /// <param name="ballRadius">The ball's radius, in meters.</param>
        /// <param name="config">Physics configuration parameters.</param>
        public void ResolveCollisionWithFriction(
            ref BallState ball,
            PaddleState paddle,
            Vector3 collisionNormal,
            float restitution,
            float frictionCoefficient,
            float ballRadius,
            IPhysicsConfig config)
        {
            // 1) Basic parameters:
            float mass = config.Ball.MassKg;

            // Approx. moment of inertia for a thin-walled sphere (table-tennis ball):
            // I = (2/3) * m * (r^2)
            float I = (2f / 3f) * mass * (ballRadius * ballRadius);

            // 2) Compute relative velocity at the point of contact.
            //    For a paddle with velocity paddle.Velocity:
            Vector3 vRel = ball.Velocity - paddle.Velocity;

            // 3) Decompose vRel into normal and tangential components.
            Vector3 n = collisionNormal.normalized;
            float vRelNormalMag = Vector3.Dot(vRel, n);
            Vector3 vRelNormal = vRelNormalMag * n;
            Vector3 vRelTangent = vRel - vRelNormal;

            // 4) Normal impulse (with restitution).
            //    We treat the paddle as effectively very massive, so the impulse on the ball is:
            float normalImpulseMag = -(1f + restitution) * vRelNormalMag * mass;
            if (normalImpulseMag < 0f)
            {
                // The ball is moving away or no collision in normal direction => no normal impulse.
                normalImpulseMag = 0f;
                Debug.Log("[CollisionResolutionSystem] No collision in normal direction => no normal impulse");
            }

            // Normal impulse vector:
            Vector3 normalImpulse = normalImpulseMag * n;
            
            Debug.Log(
                $"[CollisionResolutionSystem] Normal impulse: {normalImpulse}, normal impulse magnitude: {normalImpulseMag}");
            Debug.Log(
                $"[CollisionResolutionSystem] Normal velocity: {vRelNormal}, tangential velocity: {vRelTangent}");
            Debug.Log(
                $"[CollisionResolutionSystem] Incoming velocity: {ball.Velocity}, angular velocity: {ball.AngularVelocity}");
            Debug.Log(
                $"[CollisionResolutionSystem] Paddle velocity: {paddle.Velocity}, relative velocity: {vRel}");
            Debug.Log(
                $"[CollisionResolutionSystem] Mass: {mass}, moment of inertia: {I}");
            Debug.Log(
                $"[CollisionResolutionSystem] Restitution: {restitution}, friction coefficient: {frictionCoefficient}");
            Debug.Log(
                $"[CollisionResolutionSystem] Ball radius: {ballRadius}");
            Debug.Log(
                $"[CollisionResolutionSystem] Collision normal: {collisionNormal}");

            // 5) Apply the normal impulse to the ball’s velocity.
            ball.Velocity += normalImpulse / mass;

            // 6) Friction impulse for tangential direction:
            //    We check the velocity difference vRelTangent and see how big an impulse is needed
            //    to bring the ball to no-slip (relative tangential velocity = 0).
            Vector3 desiredTangentImpulse = -vRelTangent * mass;

            // The maximum friction impulse is limited by frictionCoefficient * normalImpulseMag.
            float maxFrictionMag = frictionCoefficient * normalImpulseMag;
            float neededMag = desiredTangentImpulse.magnitude;

            Vector3 frictionImpulse;
            if (neededMag <= maxFrictionMag)
            {
                // NO SLIP => we can apply the full impulse that cancels tangential velocity
                frictionImpulse = desiredTangentImpulse;
                Debug.Log(
                    $"[CollisionResolutionSystem] No slip: {neededMag} <= {maxFrictionMag} => applied full impulse");
            }
            else
            {
                // SLIP => clamp friction to the max
                frictionImpulse = desiredTangentImpulse.normalized * maxFrictionMag;
                Debug.Log(
                    $"[CollisionResolutionSystem] Slipping: {neededMag} > {maxFrictionMag} => clamped to {maxFrictionMag}");
            }

            // 7) Apply the friction impulse to the ball’s velocity.
            ball.Velocity += frictionImpulse / mass;

            // 8) Convert friction impulse into spin:
            //    For a sphere at contact radius ~ ballRadius, the torque τ = r × J.
            //    With r ~ (ballRadius along the collision normal), and J = frictionImpulse,
            //    we compute torque and from torque => Δω = τ / I.
            Vector3 torque = Vector3.Cross(n * ballRadius, frictionImpulse);

            // --- FIX: Clamp near-zero cross product to avoid undefined or excessive spin ---
            float torqueMag = torque.magnitude;
            if (torqueMag > 1e-5f)
            {
                Vector3 deltaOmega = torque / I;
                ball.AngularVelocity += deltaOmega;
                Debug.Log($"[CollisionResolutionSystem] Torque: {torque}, Δω: {deltaOmega}");
            }
            
            Debug.Log(
                $"[CollisionResolutionSystem] Final velocity: {ball.Velocity}, angular velocity: {ball.AngularVelocity}");
        }
    }
}