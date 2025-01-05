using UnityEngine;

/// <summary>
/// Detects collisions between the ball and paddle, as well as the ball and the environment.
/// </summary>
public class CollisionDetectionSystem
{
    /// <summary>
    /// Detects collisions between the ball and the paddle using swept sphere collision detection.
    /// </summary>
    /// <param name="ball">The current state of the ball.</param>
    /// <param name="paddle">The current state of the paddle.</param>
    /// <param name="dt">The time step for the physics update.</param>
    /// <returns>Collision data if a collision is detected, otherwise an empty collision data object.</returns>
    public static CollisionData DetectCollision(BallState ball, PaddleState paddle, float dt)
    {
        Vector3 ballStart = ball.Position;
        Vector3 ballEnd = ball.Position + ball.Velocity * dt;
        float ballRadius = TableTennisPhysicsConfig.BallDiameterMm / 2000f; // Radius in meters

        Vector3 paddleStart = paddle.Position;
        Vector3 paddleEnd = paddle.Position + paddle.Velocity * dt;

        // Calculate relative movement
        Vector3 relativeMovement = (ballEnd - ballStart) - (paddleEnd - paddleStart);
        float relativeDistance = relativeMovement.magnitude;

        if (relativeDistance > 0f)
        {
            RaycastHit hit;
            if (Physics.SphereCast(ballStart, ballRadius, relativeMovement.normalized, out hit, relativeDistance))
            {
                if (hit.collider.CompareTag("Paddle"))
                {
                    return new CollisionData
                    {
                        Detected = true,
                        Normal = hit.normal,
                        Point = hit.point
                    };
                }
            }
        }

        return new CollisionData { Detected = false };
    }

    /// <summary>
    /// Detects collisions between the ball and the environment (e.g., table).
    /// </summary>
    /// <param name="ball">The current state of the ball.</param>
    /// <param name="dt">The time step for the physics update.</param>
    /// <returns>Collision data if a collision is detected, otherwise an empty collision data object.</returns>
    public CollisionData DetectEnvironmentCollision(BallState ball, float dt)
    {
        Vector3 ballStart = ball.Position;
        Vector3 ballEnd = ball.Position + ball.Velocity * dt;
        float ballRadius = TableTennisPhysicsConfig.BallDiameterMm / 2000f; // Radius in meters

        // Table plane at y = Table surface height
        float tableY = TableTennisPhysicsConfig.TableHeightMeters + TableTennisPhysicsConfig.TableThicknessMeters / 2f;

        // Check if the ball crosses the table plane during this timestep
        if ((ballStart.y - ballRadius >= tableY && ballEnd.y - ballRadius <= tableY) ||
            (ballStart.y + ballRadius >= tableY && ballEnd.y + ballRadius <= tableY))
        {
            // Ball has collided with the table
            return new CollisionData
            {
                Detected = true,
                Normal = Vector3.up,
                Point = new Vector3(ball.Position.x, tableY + ballRadius, ball.Position.z)
            };
        }

        return new CollisionData { Detected = false };
    }
}