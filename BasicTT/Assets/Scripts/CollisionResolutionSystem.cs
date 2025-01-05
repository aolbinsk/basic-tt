using UnityEngine;

/// <summary>
/// Resolves collisions and updates the ball's physics state, including spin and aerodynamic effects.
/// </summary>
public class CollisionResolutionSystem
{
    private const float MIN_VELOCITY_THRESHOLD = 0.05f;

    /// <summary>
    /// Resolves a collision and updates the ball's velocity, position, and spin.
    /// </summary>
    /// <param name="ball">The current state of the ball.</param>
    /// <param name="paddle">The state of the paddle involved in the collision, if any.</param>
    /// <param name="collision">The collision data.</param>
    public static void ResolveCollision(ref BallState ball, PaddleState paddle, CollisionData collision)
    {
        // Reflect the ball's velocity based on collision normal
        Vector3 incomingVelocity = ball.Velocity;
        Vector3 normal = collision.Normal.normalized;
        float restitution;

        if (paddle != null)
        {
            // Collision with paddle
            restitution = TableTennisPhysicsConfig.instance.paddleRubberBounciness;

            // Add paddle's velocity to the ball's velocity
            incomingVelocity += paddle.Velocity;

            // Calculate spin induced by collision
            Vector3 relativeVelocity = incomingVelocity - paddle.Velocity;
            Vector3 spinAxis = Vector3.Cross(normal, relativeVelocity).normalized;
            float spinMagnitude = relativeVelocity.magnitude * TableTennisPhysicsConfig.SpinTransferCoefficient * TableTennisPhysicsConfig.instance.paddleSpinMultiplier;
            ball.AngularVelocity += spinAxis * spinMagnitude;

            // Apply throw multiplier to the lateral component of the velocity
            Vector3 lateralVelocity = Vector3.ProjectOnPlane(incomingVelocity, normal) * TableTennisPhysicsConfig.instance.paddleThrowMultiplier;
            Vector3 normalVelocity = Vector3.Project(incomingVelocity, normal);
            incomingVelocity = normalVelocity + lateralVelocity;
        }
        else
        {
            // Collision with environment (e.g., table)
            restitution = TableTennisPhysicsConfig.instance.tableBounceRestitution;

            // Apply friction to reduce spin
            ball.AngularVelocity *= (1f - TableTennisPhysicsConfig.instance.tableFriction);
        }

        Vector3 reflectedVelocity = Vector3.Reflect(incomingVelocity, normal) * restitution;

        // Stop the ball if velocity is below a small threshold to prevent bobbing
        if (reflectedVelocity.magnitude < MIN_VELOCITY_THRESHOLD)
        {
            reflectedVelocity = Vector3.zero;
        }

        // Update ball state
        ball.Velocity = reflectedVelocity;
        ball.Position = collision.Point;
    }
}