using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.CustomPhysics;
using UnityEngine;

namespace Domain.Physics
{
    /// <summary>
    /// Detects collisions between the ball and paddle, as well as the ball and the environment.
    /// Implements advanced collision detection to handle high-speed interactions.
    /// </summary>
    public class CollisionDetectionSystem : ICollisionSystem
    {
        private readonly IPhysicsConfig _config;

        /// <summary>
        /// Initializes a new instance of the CollisionDetectionSystem class with the specified physics configuration.
        /// </summary>
        /// <param name="config">The physics configuration parameters.</param>
        public CollisionDetectionSystem(IPhysicsConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Detects collisions between the ball and paddle using advanced continuous collision detection.
        /// </summary>
        /// <param name="previousBallState">The previous state of the ball.</param>
        /// <param name="currentBallState">The current state of the ball.</param>
        /// <param name="previousPaddleState">The previous state of the paddle.</param>
        /// <param name="currentPaddleState">The current state of the paddle.</param>
        /// <param name="deltaTime">The time step for the physics update.</param>
        /// <returns>Collision data if a collision is detected, otherwise an empty collision data object.</returns>
        public CollisionData DetectCollision(
            BallState previousBallState, BallState currentBallState,
            PaddleState previousPaddleState, PaddleState currentPaddleState,
            float deltaTime)
        {
            // Check for collisions with the forehand paddle side
            CollisionData collisionData = DetectPaddleSideCollision(
                previousBallState, currentBallState,
                previousPaddleState, currentPaddleState,
                deltaTime, previousPaddleState?.ForehandCollider, currentPaddleState?.ForehandCollider);

            if (collisionData.Detected)
            {
                Debug.Log("Collision detected with forehand paddle side");
                return collisionData;
            }

            // Check for collisions with the backhand paddle side
            collisionData = DetectPaddleSideCollision(
                previousBallState, currentBallState,
                previousPaddleState, currentPaddleState,
                deltaTime, previousPaddleState?.BackhandCollider, currentPaddleState?.BackhandCollider);

            if (collisionData.Detected)
            {
                Debug.Log("Collision detected with backhand paddle side");
                return collisionData;
            }

            // Check for collisions with the environment
            return DetectEnvironmentCollision(currentBallState, deltaTime);
        }

        /// <summary>
        /// Detects swept collisions between the ball and a specific paddle side, considering their movements over the time step.
        /// </summary>
        /// <param name="previousBallState">The previous state of the ball.</param>
        /// <param name="currentBallState">The current state of the ball.</param>
        /// <param name="previousPaddleState">The previous state of the paddle.</param>
        /// <param name="currentPaddleState">The current state of the paddle.</param>
        /// <param name="deltaTime">The time step for the physics update.</param>
        /// <param name="previousPaddleCollider">The specific paddle collider to check against (previous state).</param>
        /// <param name="currentPaddleCollider">The specific paddle collider to check against (current state).</param>
        /// <returns>Collision data if a collision is detected, otherwise an empty collision data object.</returns>
        private CollisionData DetectPaddleSideCollision(
            BallState previousBallState, BallState currentBallState,
            PaddleState previousPaddleState, PaddleState currentPaddleState,
            float deltaTime, BoxCollider previousPaddleCollider, BoxCollider currentPaddleCollider)
        {
            if (currentPaddleCollider == null)
            {
                return new CollisionData { Detected = false };
            }

            // Use swept sphere-to-oriented-box collision detection
            bool collisionDetected = SweptBoxCollisionPro.SweptSphereToOrientedBox(
                previousBallState.Position, currentBallState.Position, _config.Ball.DiameterMeters / 2f,
                currentPaddleCollider,
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
        /// <param name="deltaTime">The time step for the physics update.</param>
        /// <returns>Collision data if a collision is detected, otherwise an empty collision data object.</returns>
        private CollisionData DetectEnvironmentCollision(BallState ball, float deltaTime)
        {
            float ballRadius = _config.Ball.DiameterMeters / 2f;
            Vector3 displacement = ball.Velocity * deltaTime;

            // Perform sphere cast from ball's position in the direction of its velocity
            RaycastHit hitInfo;
            bool hit = UnityEngine.Physics.SphereCast(
                origin: ball.Position,
                radius: ballRadius,
                direction: ball.Velocity.normalized,
                hitInfo: out hitInfo,
                maxDistance: displacement.magnitude,
                layerMask: _config.EnvironmentLayerMask,
                queryTriggerInteraction: QueryTriggerInteraction.Ignore
            );

            if (hit)
            {
                float timeOfImpact = (hitInfo.distance / displacement.magnitude) * deltaTime;

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
        /// Resolves a detected collision and updates the ball's state.
        /// </summary>
        /// <param name="ballState">The current state of the ball to be updated.</param>
        /// <param name="paddleState">The state of the paddle involved in the collision, if any.</param>
        /// <param name="collisionData">The collision data to resolve.</param>
        public void ResolveCollision(ref BallState ballState, PaddleState paddleState, CollisionData collisionData)
        {
            var collisionResolver = new CollisionResolutionSystem();
            collisionResolver.ResolveCollision(ref ballState, paddleState, collisionData, _config);
        }
    }
}