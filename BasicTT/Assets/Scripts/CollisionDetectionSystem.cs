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
    public CollisionData DetectPaddleCollisionWithCapsuleCast(BallState ball, PaddleState paddle, float dt)
    {
        if (ball == null || paddle == null || ball.Collider == null || paddle.Collider == null)
        {
            Debug.LogWarning("Null state or collider in paddle collision detection");
            return new CollisionData { Detected = false };
        }

        float ballRadius = TableTennisPhysicsConfig.BallDiameterMm / 2000f;

        // Get paddle dimensions from the actual collider bounds
        var bounds = paddle.Collider.bounds;
        float paddleLength = bounds.size.z;
        float paddleWidth = bounds.size.x;

        // Use paddle width for capsule radius to ensure better collision detection
        float capsuleRadius = paddleWidth * 0.5f;

        // Calculate capsule endpoints in world space
        Vector3 paddleForward = paddle.Rotation * Vector3.forward;
        Vector3 capsuleStart = paddle.Position - (paddleForward * (paddleLength * 0.5f));
        Vector3 capsuleEnd = paddle.Position + (paddleForward * (paddleLength * 0.5f));

        // Calculate ball movement vector
        Vector3 ballStart = ball.Position;
        Vector3 ballEnd = ball.Position + ball.Velocity * dt;
        Vector3 ballMovement = ballEnd - ballStart;

        // Validate movement vectors
        if (ballMovement.magnitude < 0.0001f)
        {
            Debug.LogWarning("Ball movement too small for collision detection");
            return new CollisionData { Detected = false };
        }

        // Debug visualization
        Debug.DrawLine(capsuleStart, capsuleEnd, Color.blue, dt);
        Debug.DrawLine(ballStart, ballEnd, Color.red, dt);

        // Perform capsule cast
        RaycastHit[] hits = Physics.CapsuleCastAll(
            point1: capsuleStart,
            point2: capsuleEnd,
            radius: capsuleRadius,
            direction: ballMovement.normalized,
            maxDistance: ballMovement.magnitude + ballRadius,
            layerMask: LayerMask.GetMask("Ball"),  // Explicitly use ball layer
            queryTriggerInteraction: QueryTriggerInteraction.Ignore
        );

        // Find earliest valid hit
        float earliestTime = float.MaxValue;
        RaycastHit? earliestHit = null;

        foreach (var hit in hits)
        {
            if (hit.collider == ball.Collider && hit.distance < earliestTime && hit.point != Vector3.zero)
            {
                earliestTime = hit.distance;
                earliestHit = hit;

                // Enhanced debug logging
                Debug.Log($"Valid hit detected - Point: {hit.point}, Normal: {hit.normal}, Distance: {hit.distance}");
                Debug.DrawLine(hit.point, hit.point + hit.normal * 0.1f, Color.yellow, 1f);
            }
        }

        if (earliestHit.HasValue)
        {
            var hit = earliestHit.Value;

            // Validate hit point
            if (hit.point == Vector3.zero)
            {
                Debug.LogError("Invalid hit point detected (0,0,0)");
                return new CollisionData { Detected = false };
            }

            float timeOfImpact = (hit.distance / ballMovement.magnitude) * dt;
            
            return new CollisionData
            {
                Detected = true,
                Point = hit.point,
                Normal = hit.normal,
                TimeOfImpact = timeOfImpact,
                Collider = paddle.Collider  // Use paddle collider for collision resolution
            };
        }

        return new CollisionData { Detected = false };
    }
    
    public CollisionData DetectBallCollisionWithPaddle(BallState ball, PaddleState paddle, float dt)
    {
        if (ball == null || paddle == null || ball.Collider == null || paddle.Collider == null)
        {
            Debug.LogWarning("Null state or collider in paddle collision detection");
            return new CollisionData { Detected = false };
        }

        float ballRadius = TableTennisPhysicsConfig.BallDiameterMm / 2000f;
        Vector3 ballStart = ball.Position;
        Vector3 ballEnd = ball.Position + ball.Velocity * dt;
        Vector3 ballMovement = ballEnd - ballStart;

        if (ballMovement.magnitude < 0.0001f)
        {
            Debug.LogWarning("Ball movement too small for collision detection");
            return new CollisionData { Detected = false };
        }

        // Perform sphere cast from the ball's position in the direction of movement
        RaycastHit[] hits = Physics.SphereCastAll(
            origin: ballStart,
            radius: ballRadius,
            direction: ballMovement.normalized,
            maxDistance: ballMovement.magnitude,
            layerMask: LayerMask.GetMask("Paddle"),
            queryTriggerInteraction: QueryTriggerInteraction.Ignore
        );

        if (hits.Length > 0)
        {
            // Find earliest valid hit
            float earliestTime = float.MaxValue;
            RaycastHit? earliestHit = null;

            foreach (var hit in hits)
            {
                if (hit.collider == paddle.Collider && hit.distance < earliestTime && hit.point != Vector3.zero)
                {
                    earliestTime = hit.distance;
                    earliestHit = hit;

                    Debug.Log($"Valid hit detected - Point: {hit.point}, Normal: {hit.normal}, Distance: {hit.distance}");
                    Debug.DrawLine(hit.point, hit.point + hit.normal * 0.1f, Color.yellow, 1f);
                }
            }

            if (earliestHit.HasValue)
            {
                var hit = earliestHit.Value;
                float timeOfImpact = (hit.distance / ballMovement.magnitude) * dt;

                return new CollisionData
                {
                    Detected = true,
                    Point = hit.point,
                    Normal = hit.normal,
                    TimeOfImpact = timeOfImpact,
                    Collider = paddle.Collider
                };
            }
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

    // Add helper method for debug visualization
    private void DebugDrawSphere(Vector3 center, float radius, Color color, float duration)
    {
        float angle = 0f;
        for (int i = 0; i < 24; i++)
        {
            angle = i * Mathf.PI * 2 / 24;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            Vector3 pos2 = center + new Vector3(Mathf.Cos(angle + Mathf.PI * 2 / 24), Mathf.Sin(angle + Mathf.PI * 2 / 24), 0) * radius;
            Debug.DrawLine(pos, pos2, color, duration);
        }
    }
}