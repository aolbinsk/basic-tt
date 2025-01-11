using System.Collections.Generic;
using Domain.Entities;
using Domain.Interfaces;
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
        private readonly List<OrientedBox> _environmentShapes;
        private readonly CollisionResolutionSystem _collisionResolver;

        public CollisionDetectionSystem(IPhysicsConfig config) 
            : this(config, new List<OrientedBox>()) {}

        /// <summary>
        /// Initializes a new instance of the CollisionDetectionSystem class with the specified physics configuration.
        /// </summary>
        /// <param name="config">The physics configuration parameters.</param>
        /// <param name="environmentShapes">The oriented boxes representing the environment geometry.</param>
        public CollisionDetectionSystem(IPhysicsConfig config, List<OrientedBox> environmentShapes)
        {
            _config = config;
            _environmentShapes = environmentShapes;
            _collisionResolver = new CollisionResolutionSystem();
        }

        /// <summary>
        /// Detects collisions between the ball and paddle using geometry-based collision detection.
        /// </summary>
        public CollisionData DetectCollision(
            BallState previousBallState, BallState currentBallState,
            PaddleState previousPaddleState, PaddleState currentPaddleState,
            float deltaTime)
        {
            // Check for collisions with the forehand side
            CollisionData fhCollisionData = DetectPaddleSideCollision(
                previousBallState, currentBallState,
                previousPaddleState, currentPaddleState,
                deltaTime,
                _config.Paddle.Geometry.ForehandRubberCenter,
                _config.Paddle.Geometry.ForehandRubberHalfExtents,
                "Forehand");

            // Check for collisions with the backhand side
            var bhCollisionData = DetectPaddleSideCollision(
                previousBallState, currentBallState,
                previousPaddleState, currentPaddleState,
                deltaTime,
                _config.Paddle.Geometry.BackhandRubberCenter,
                _config.Paddle.Geometry.BackhandRubberHalfExtents,
                "Backhand");

            // Return the collision data for the side with the earliest impact
            if (fhCollisionData.Detected && bhCollisionData.Detected)
            {
                return fhCollisionData.TimeOfImpact < bhCollisionData.TimeOfImpact
                    ? fhCollisionData
                    : bhCollisionData;
            }

            // Check for collisions with the environment
            return DetectEnvironmentCollision(previousBallState, currentBallState, deltaTime);
        }

        private CollisionData DetectPaddleSideCollision(
            BallState previousBallState, BallState currentBallState,
            PaddleState previousPaddleState, PaddleState currentPaddleState,
            float deltaTime,
            Vector3 localCenter,
            Vector3 halfExtents,
            string sideName)
        {
            // Build oriented boxes for the paddle side at previous and current states
            OrientedBox paddleBox = BuildOrientedBox(
                currentPaddleState.Position, 
                currentPaddleState.Rotation, 
                localCenter, 
                halfExtents);

            // Get ball radius
            float ballRadius = _config.Ball.DiameterMeters * 0.5f;
            
            // Perform swept sphere to box collision detection
            bool hit = SweptBoxCollisionPro.SweptSphereToOrientedBox(
                previousBallState.Position, currentBallState.Position,
                ballRadius,
                paddleBox,
                out Vector3 collisionPoint,
                out Vector3 collisionNormal,
                out float timeOfImpact);

            if (hit)
            {
                return new CollisionData
                {
                    Detected = true,
                    Point = collisionPoint,
                    Normal = collisionNormal,
                    TimeOfImpact = timeOfImpact,
                    CollisionTag = sideName
                };
            }

            return new CollisionData { Detected = false };
        }

        private OrientedBox BuildOrientedBox(
            Vector3 paddleCenterPos,
            Quaternion paddleRot,
            Vector3 localCenterOffset,
            Vector3 halfExtents)
        {
            // Validate rotation
            if (!IsValidQuaternion(paddleRot))
            {
                //Debug.LogWarning("Invalid quaternion detected= " + paddleRot);
                // Happens when the paddle is not being tracked.
                paddleRot = Quaternion.identity;
            }

            var worldCenter = paddleCenterPos + (paddleRot * localCenterOffset);
            // TODO: Prevent thrashing the heap, reuse the same half extents vectors
            return new OrientedBox(worldCenter, paddleRot, halfExtents);
        }

        private bool IsValidQuaternion(Quaternion q)
        {
            return !Mathf.Approximately(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w, 0f);
        }

        private CollisionData DetectEnvironmentCollision(BallState previousBallState, BallState currentBallState, float deltaTime)
        {
            float ballRadius = _config.Ball.DiameterMeters * 0.5f;

            // Use advanced swept collision detection for environment shapes
            CollisionData bestCollision = new CollisionData { Detected = false };
            float earliestTime = float.MaxValue;

            foreach (var shape in _environmentShapes)
            {
                bool hit = SweptBoxCollisionPro.SweptSphereToOrientedBox(
                    previousBallState.Position, currentBallState.Position,
                    ballRadius,
                    shape,
                    out Vector3 collisionPoint,
                    out Vector3 collisionNormal,
                    out float timeOfImpact);

                if (hit && timeOfImpact < earliestTime)
                {
                    earliestTime = timeOfImpact;
                    bestCollision = new CollisionData
                    {
                        Detected = true,
                        Point = collisionPoint,
                        Normal = collisionNormal,
                        TimeOfImpact = timeOfImpact * deltaTime,
                        CollisionTag = "Environment"
                    };
                }
            }

            return bestCollision;
        }

        public void ResolveCollision(ref BallState ballState, PaddleState paddleState, CollisionData collisionData)
        {
            _collisionResolver.ResolveCollision(ref ballState, paddleState, collisionData, _config);
        }
    }

    /// <summary>
    /// Represents an oriented bounding box used for collision detection.
    /// </summary>
    public struct OrientedBox
    {
        public Vector3 Center;
        public Quaternion Rotation;
        public Vector3 HalfExtents;

        public OrientedBox(Vector3 center, Quaternion rotation, Vector3 halfExtents)
        {
            Center = center;
            Rotation = rotation;
            HalfExtents = halfExtents;
        }
    }
}