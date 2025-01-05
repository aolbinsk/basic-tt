using UnityEngine;

/// <summary>
/// Resolves collisions and updates the ball's physics state, including spin and aerodynamic effects.
/// </summary>
public class CollisionResolutionSystem
{
    private const float MIN_VELOCITY_THRESHOLD = 0.2f;

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

        // Stop the ball if velocity is below a small threshold to prevent bobbing
        if (reflectedVelocity.magnitude < MIN_VELOCITY_THRESHOLD)
        {
            reflectedVelocity = Vector3.zero;

            // Adjust the ball's position to sit on top of the collision point
            ball.Position = collision.Point + collision.Normal * (TableTennisPhysicsConfig.BallDiameterMm / 2000f);
        }

        // Update ball state
        ball.Velocity = reflectedVelocity;
        ball.Position = collision.Point;

        // Log post-collision state
        Debug.Log($"Post-collision ball velocity: {ball.Velocity}, Position: {ball.Position}, Angular Velocity: {ball.AngularVelocity}");
    }
}