using UnityEngine;

/// <summary>
/// Detects collisions between the ball and paddle, as well as the ball and the environment.
/// Implements advanced continuous collision detection to handle high-speed interactions.
/// </summary>
public class CollisionDetectionSystem
{
    /// <summary>
    /// Detects collisions between the ball and the paddle using advanced continuous collision detection.
    /// </summary>
    /// <param name="ball">The current state of the ball.</param>
    /// <param name="paddle">The current state of the paddle.</param>
    /// <param name="dt">The time step for the physics update.</param>
    /// <returns>Collision data if a collision is detected, otherwise an empty collision data object.</returns>
    public CollisionData DetectPaddleCollisionWithSphereCast(BallState ball, PaddleState paddle, float dt)
    {
        float ballRadius = TableTennisPhysicsConfig.BallDiameterMm / 2000f;

        // Compute relative motion
        Vector3 relativeVelocity = ball.Velocity - paddle.Velocity;
        Vector3 displacement = relativeVelocity * dt;

        // Perform sphere cast from ball's position in the direction of relative velocity
        RaycastHit hitInfo;
        bool hit = Physics.SphereCast(
            origin: ball.Position,
            radius: ballRadius,
            direction: relativeVelocity.normalized,
            hitInfo: out hitInfo,
            maxDistance: displacement.magnitude,
            layerMask: TableTennisPhysicsConfig.PaddleLayerMask,
            queryTriggerInteraction: QueryTriggerInteraction.Ignore
        );

        if (hit && hitInfo.collider == paddle.Collider)
        {
            float timeOfImpact = (hitInfo.distance / displacement.magnitude) * dt;

            return new CollisionData
            {
                Detected = true,
                Point = hitInfo.point,
                Normal = hitInfo.normal,
                TimeOfImpact = timeOfImpact,
                Collider = hitInfo.collider
            };
        }

        return new CollisionData { Detected = false };
    }

    /// <summary>
    /// Detects collisions between the ball and the environment (e.g., table, floor, walls) using advanced continuous collision detection.
    /// </summary>
    /// <param name="ball">The current state of the ball.</param>
    /// <param name="dt">The time step for the physics update.</param>
    /// <returns>Collision data if a collision is detected, otherwise an empty collision data object.</returns>
    public CollisionData DetectEnvironmentCollision(BallState ball, float dt)
    {
        float ballRadius = TableTennisPhysicsConfig.BallDiameterMm / 2000f;
        Vector3 displacement = ball.Velocity * dt;

        // Perform sphere cast from ball's position in the direction of its velocity
        RaycastHit hitInfo;
        bool hit = Physics.SphereCast(
            origin: ball.Position,
            radius: ballRadius,
            direction: ball.Velocity.normalized,
            hitInfo: out hitInfo,
            maxDistance: displacement.magnitude,
            layerMask: TableTennisPhysicsConfig.EnvironmentLayerMask,
            queryTriggerInteraction: QueryTriggerInteraction.Ignore
        );

        if (hit)
        {
            float timeOfImpact = (hitInfo.distance / displacement.magnitude) * dt;

            return new CollisionData
            {
                Detected = true,
                Point = hitInfo.point,
                Normal = hitInfo.normal,
                TimeOfImpact = timeOfImpact,
                Collider = hitInfo.collider
            };
        }

        return new CollisionData { Detected = false };
    }

    /// <summary>
    /// Detects collisions between the ball and paddle, accounting for paddle movement during the physics sub-step.
    /// </summary>
    /// <param name="ball">The current state of the ball.</param>
    /// <param name="paddle">The current state of the paddle.</param>
    /// <param name="dt">The time step for the physics update.</param>
    /// <returns>Collision data if a collision is detected, otherwise an empty collision data object.</returns>
    public CollisionData DetectPaddleCollisionWithCapsuleCast(BallState ball, PaddleState paddle, float dt)
    {
        float ballRadius = TableTennisPhysicsConfig.BallDiameterMm / 2000f;

        // Get paddle dimensions for capsule
        var bounds = paddle.Collider.bounds;
        float paddleLength = bounds.size.z;
        float paddleWidth = bounds.size.x;
    
        // Use half the paddle width as capsule radius - enough to catch collisions but not too large
        float capsuleRadius = paddleWidth * 0.5f;

        // Create capsule points along paddle's length
        Vector3 paddleForward = paddle.Rotation * Vector3.forward;
        Vector3 capsuleStart = paddle.Position - paddleForward * (paddleLength * 0.5f);
        Vector3 capsuleEnd = paddle.Position + paddleForward * (paddleLength * 0.5f);

        // Calculate relative movement
        Vector3 relativeDisplacement = (ball.Position + ball.Velocity * dt) - ball.Position;

        // Perform single capsule cast
        RaycastHit hitInfo;
        bool hit = Physics.CapsuleCast(
            point1: capsuleStart,
            point2: capsuleEnd,
            radius: capsuleRadius,
            direction: relativeDisplacement.normalized,
            hitInfo: out hitInfo,
            maxDistance: relativeDisplacement.magnitude + ballRadius,
            layerMask: TableTennisPhysicsConfig.PaddleLayerMask,
            queryTriggerInteraction: QueryTriggerInteraction.Ignore
        );

        if (hit && hitInfo.collider == paddle.Collider)
        {
            float timeOfImpact = (hitInfo.distance / relativeDisplacement.magnitude) * dt;

            return new CollisionData
            {
                Detected = true,
                Point = hitInfo.point,
                Normal = hitInfo.normal,
                TimeOfImpact = timeOfImpact,
                Collider = hitInfo.collider
            };
        }

        return new CollisionData { Detected = false };
    }
}