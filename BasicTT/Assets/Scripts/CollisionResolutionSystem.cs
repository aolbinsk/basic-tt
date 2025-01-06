using UnityEngine;

/// <summary>
/// Resolves collisions and updates the ball's physics state, including spin and aerodynamic effects.
/// </summary>
public class CollisionResolutionSystem
{
    private const float MIN_VELOCITY_THRESHOLD = 0.2f;
    private const float SLEEP_PREPARATION_THRESHOLD = 0.4f; // Slightly higher than MIN_VELOCITY_THRESHOLD
    private const float CONTACT_OFFSET = 0.001f; // 1mm safety margin

    /// <summary>
    /// Resolves a collision and updates the ball's velocity, position, and spin.
    /// </summary>
    /// <param name="ball">The current state of the ball.</param>
    /// <param name="paddle">The state of the paddle involved in the collision, if any.</param>
    /// <param name="collision">The collision data.</param>
    public void ResolveCollision(ref BallState ball, PaddleState paddle, CollisionData collision)
    {
        Vector3 incomingVelocity = ball.Velocity;
        Vector3 normal = collision.Normal.normalized;
        float restitution;

        if (paddle != null)
        {
            Debug.Log($"Resolving collision. Collision with paddle, Collision point: {collision.Point}, Normal: {collision.Normal}");

            // Collision with paddle
            restitution = TableTennisPhysicsConfig.instance.paddleRubberBounciness;

            // Compute relative velocity
            Vector3 relativeVelocity = ball.Velocity - paddle.Velocity;

            // Compute new relative velocity after collision
            Vector3 newRelativeVelocity = relativeVelocity - (1 + restitution) * Vector3.Dot(relativeVelocity, normal) * normal;

            // Update ball velocity
            ball.Velocity = newRelativeVelocity + paddle.Velocity;

            // Calculate spin induced by collision
            Vector3 spinAxis = Vector3.Cross(normal, relativeVelocity).normalized;
            float spinMagnitude = relativeVelocity.magnitude * TableTennisPhysicsConfig.SpinTransferCoefficient * TableTennisPhysicsConfig.instance.paddleSpinMultiplier;
            ball.AngularVelocity += spinAxis * spinMagnitude;

            Debug.Log($"Post-collision ball velocity: {ball.Velocity}, Position: {ball.Position}, Angular Velocity: {ball.AngularVelocity}");
        }
        else
        {
            // Collision with environment (e.g., table)
            restitution = TableTennisPhysicsConfig.instance.tableBounceRestitution;

            // Reflect the ball's velocity based on collision normal
            Vector3 reflectedVelocity = Vector3.Reflect(incomingVelocity, normal) * restitution;

            // Apply friction to reduce spin
            ball.AngularVelocity *= (1f - TableTennisPhysicsConfig.instance.tableFriction);

            // Update ball velocity
            ball.Velocity = reflectedVelocity;
        }

        // Update ball position to collision point
        ball.Position = collision.Point + collision.Normal * CONTACT_OFFSET;

        // Handle transition to potential sleep state
        if (ball.Velocity.magnitude < SLEEP_PREPARATION_THRESHOLD)
        {
            if (collision.Normal.y > 0.7f) // Horizontal or near-horizontal surfaces
            {
                ball.Velocity *= 0.8f; // Dampen velocity
                ball.AngularVelocity *= 0.8f; // Reduce angular velocity
            }
            else
            {
                ball.Velocity = Vector3.ProjectOnPlane(ball.Velocity, collision.Normal) * 0.9f; // Maintain sliding velocity
            }
        }
    }
}