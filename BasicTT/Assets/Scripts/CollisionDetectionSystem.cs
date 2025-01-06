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
    /// <param name="previousBallState">The previous state of the ball.</param>
    /// <param name="currentBallState">The current state of the ball.</param>
    /// <param name="previousPaddleState">The previous state of the paddle.</param>
    /// <param name="currentPaddleState">The current state of the paddle.</param>
    /// <param name="dt">The time step for the physics update.</param>
    /// <returns>Collision data if a collision is detected, otherwise an empty collision data object.</returns>
    public CollisionData DetectBallCollisionWithPaddle(
        BallState previousBallState, BallState currentBallState,
        PaddleState previousPaddleState, PaddleState currentPaddleState,
        float dt)
    {
        // Perform swept collision detection between the ball and both sides of the paddle
        CollisionData collisionData = DetectPaddleSideCollision(
            previousBallState, currentBallState,
            previousPaddleState, currentPaddleState,
            dt, previousPaddleState?.LeftCollider, currentPaddleState?.LeftCollider);

        if (collisionData.Detected)
        {
            return collisionData;
        }

        collisionData = DetectPaddleSideCollision(
            previousBallState, currentBallState,
            previousPaddleState, currentPaddleState,
            dt, previousPaddleState?.RightCollider, currentPaddleState?.RightCollider);

        return collisionData;
    }

    /// <summary>
    /// Detects swept collisions between the ball and a specific paddle side, considering their movements over the time step.
    /// </summary>
    /// <param name="previousBallState">The previous state of the ball.</param>
    /// <param name="currentBallState">The current state of the ball.</param>
    /// <param name="previousPaddleState">The previous state of the paddle.</param>
    /// <param name="currentPaddleState">The current state of the paddle.</param>
    /// <param name="dt">The time step for the physics update.</param>
    /// <param name="previousPaddleCollider">The specific paddle collider to check against (previous state).</param>
    /// <param name="currentPaddleCollider">The specific paddle collider to check against (current state).</param>
    /// <returns>Collision data if a collision is detected, otherwise an empty collision data object.</returns>
    private CollisionData DetectPaddleSideCollision(
        BallState previousBallState, BallState currentBallState,
        PaddleState previousPaddleState, PaddleState currentPaddleState,
        float dt, Collider previousPaddleCollider, Collider currentPaddleCollider)
    {
        // Use swept sphere-to-oriented-box collision detection
        bool collisionDetected = SweptBoxCollisionPro.SweptSphereToOrientedBox(
            previousBallState.Position, currentBallState.Position, TableTennisPhysicsConfig.BallDiameterMm / 2000f,
            currentPaddleCollider as BoxCollider,
            out Vector3 collisionPoint, out Vector3 collisionNormal, out float timeOfImpact);

        if (collisionDetected)
        {
            return new CollisionData
            {
                Detected = true,
                Point = collisionPoint,
                Normal = collisionNormal,
                TimeOfImpact = timeOfImpact,
                Collider = currentPaddleCollider
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
}