using NUnit.Framework;
using Domain.Physics;
using Domain.Entities;
using Domain.Config;
using UnityEngine;
using Domain.Interfaces;

namespace Tests.Domain.Physics
{
    [TestFixture]
    public class CollisionResolutionSystemTests
    {
        private CollisionResolutionSystem _collisionResolutionSystem;
        private PhysicsConfig _physicsConfig;

        [SetUp]
        public void SetUp()
        {
            _physicsConfig = new PhysicsConfig();
            _collisionResolutionSystem = new CollisionResolutionSystem();
        }

        [Test]
        public void ResolveCollision_BallWithEnvironment_ReflectsVelocity()
        {
            // Arrange
            var ballState = new BallState
            {
                Velocity = new Vector3(0f, -5f, 0f),
                Position = new Vector3(0f, 0f, 0f)
            };

            var collisionData = new CollisionData
            {
                Detected = true,
                Normal = Vector3.up,
                Point = Vector3.zero
            };

            // Act
            _collisionResolutionSystem.ResolveCollision(ref ballState, null, collisionData, _physicsConfig);

            // Assert
            Assert.AreEqual(5f, ballState.Velocity.y, 0.1f);
        }

        [Test]
        public void ResolveCollision_BallWithPaddle_AppliesThrowMultiplier()
        {
            // Arrange
            var ballState = new BallState
            {
                Velocity = new Vector3(0f, 0f, -5f),
                Position = new Vector3(0f, 0f, 0f)
            };

            var paddleState = new PaddleState
            {
                Velocity = Vector3.zero
            };

            var collisionData = new CollisionData
            {
                Detected = true,
                Normal = Vector3.forward,
                Point = Vector3.zero,
                Collider = new BoxCollider() // Assume forehand side
            };

            // Act
            _collisionResolutionSystem.ResolveCollision(ref ballState, paddleState, collisionData, _physicsConfig);

            // Assert
            Assert.Greater(ballState.Velocity.z, 0f);
        }
    }
}