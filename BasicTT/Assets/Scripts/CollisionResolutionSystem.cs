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
        // Log collision details
        Debug.Log($"Resolving collision. Collision with paddle: {(paddle != null)}, Collision point: {collision.Point}, Normal: {collision.Normal}");

        // Reflect the ball's velocity based on collision normal
        Vector3 incomingVelocity = ball.Velocity;
        Vector3 normal = collision.Normal.normalized;
        float restitution;

        if (paddle != null)
        {
            // Collision with paddle
            restitution = TableTennisPhysicsConfig.instance.paddleRubberBounciness;

            // Compute relative velocity
            Vector3 relativeVelocity = ball.Velocity - paddle.Velocity;

            // Calculate spin induced by collision
            Vector3 spinAxis = Vector3.Cross(normal, relativeVelocity).normalized;
            float spinMagnitude = relativeVelocity.magnitude * TableTennisPhysicsConfig.SpinTransferCoefficient * TableTennisPhysicsConfig.instance.paddleSpinMultiplier;
            ball.AngularVelocity += spinAxis * spinMagnitude;

            // Reflect the relative velocity
            Vector3 normalVelocity = Vector3.Project(relativeVelocity, normal);
            Vector3 tangentialVelocity = Vector3.ProjectOnPlane(relativeVelocity, normal);

            Vector3 reflectedNormalVelocity = -normalVelocity * restitution;

            // Apply throw multiplier to the lateral component of the velocity
            Vector3 adjustedTangentialVelocity = tangentialVelocity * TableTennisPhysicsConfig.instance.paddleThrowMultiplier;

            Vector3 newRelativeVelocity = reflectedNormalVelocity + adjustedTangentialVelocity;

            // Update ball velocity
            ball.Velocity = paddle.Velocity + newRelativeVelocity;
        }
        else
        {
            // Collision with environment (e.g., table)
            restitution = TableTennisPhysicsConfig.instance.tableBounceRestitution;

            // Apply friction to reduce spin
            ball.AngularVelocity *= (1f - TableTennisPhysicsConfig.instance.tableFriction);
        }

        Vector3 reflectedVelocity = Vector3.Reflect(incomingVelocity, normal) * restitution;

        // Handle transition to potential sleep state
        if (reflectedVelocity.magnitude < SLEEP_PREPARATION_THRESHOLD)
        {
            // For horizontal or near-horizontal surfaces (like table)
            if (collision.Normal.y > 0.7f)
            {
                // Don't zero out velocity - let Unity's physics handle it
                // Just ensure the ball is properly positioned above the surface
                ball.Position = collision.Point + collision.Normal * (TableTennisPhysicsConfig.BallDiameterMm / 2000f + CONTACT_OFFSET);
                
                // Dampen velocity but don't eliminate it
                reflectedVelocity *= 0.8f;
                
                // Reduce angular velocity as well
                ball.AngularVelocity *= 0.8f;
            }
            else 
            {
                // For non-horizontal surfaces, maintain a small sliding velocity
                reflectedVelocity = Vector3.ProjectOnPlane(reflectedVelocity, collision.Normal) * 0.9f;
            }
        }

        // Update ball state
        ball.Velocity = reflectedVelocity;
        ball.Position = collision.Point;

        // Log post-collision state
        Debug.Log($"Post-collision ball velocity: {ball.Velocity}, Position: {ball.Position}, Angular Velocity: {ball.AngularVelocity}");
    }
}