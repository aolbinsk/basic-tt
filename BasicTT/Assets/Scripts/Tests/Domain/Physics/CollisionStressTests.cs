using NUnit.Framework;
using Domain.Physics;
using Domain.Entities;
using Domain.Config;
using UnityEngine;
using System.Collections;

namespace Tests.Domain.Physics
{
    [TestFixture]
    public class CollisionStressTests
    {
        private CollisionDetectionSystem _collisionDetectionSystem;
        private CollisionResolutionSystem _collisionResolutionSystem;
        private PhysicsConfig _physicsConfig;

        [SetUp]
        public void SetUp()
        {
            _physicsConfig = new PhysicsConfig();
            _collisionDetectionSystem = new CollisionDetectionSystem(_physicsConfig);
            _collisionResolutionSystem = new CollisionResolutionSystem();
        }

        [Test]
        public void HighSpeedCollision_DoesNotTunnelThroughPaddle()
        {
            // Arrange
            var ballState = new BallState
            {
                Position = new Vector3(0f, 1f, -1f),
                Velocity = new Vector3(0f, 0f, 50f)
            };

            var paddleState = new PaddleState
            {
                Position = new Vector3(0f, 1f, 0f)
            };

            var previousBallState = new BallState
            {
                Position = ballState.Position - ballState.Velocity * 0.016f
            };

            // Act
            var collisionData = _collisionDetectionSystem.DetectCollision(
                previousBallState, ballState, null, paddleState, 0.016f);

            // Assert
            Assert.IsTrue(collisionData.Detected);
        }

        [Test]
        public void RepeatedCollisions_BallRemainsStable()
        {
            // Arrange
            var ballState = new BallState
            {
                Position = new Vector3(0f, 1f, 0f),
                Velocity = new Vector3(0f, -2f, 0f)
            };

            // Act
            for (int i = 0; i < 100; i++)
            {
                var collisionData = new CollisionData
                {
                    Detected = true,
                    Normal = Vector3.up,
                    Point = Vector3.zero
                };
                _collisionResolutionSystem.ResolveCollision(ref ballState, null, collisionData, _physicsConfig);
            }

            // Assert
            Assert.AreEqual(0f, ballState.Velocity.y, 0.1f);
        }
    }
}