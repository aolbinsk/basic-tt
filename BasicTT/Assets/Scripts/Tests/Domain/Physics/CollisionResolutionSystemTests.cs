using NUnit.Framework;
using Domain.Physics;
using Domain.Entities;
using Domain.Config;
using UnityEngine;

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
            _physicsConfig.Air.AngularDragCoefficient = 0f;
            _physicsConfig.Air.MagnusCoefficient = 0f;
            _physicsConfig.Air.Density = 0f;
            _physicsConfig.Table.Friction = 0f;
            _physicsConfig.Table.BounceRestitution = 1f;
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
                CollisionTag = "Forehand"
            };

            // Act
            _collisionResolutionSystem.ResolveCollision(ref ballState, paddleState, collisionData, _physicsConfig);

            // Assert
            Assert.Greater(ballState.Velocity.z, 0f);
        }
        
        [Test]
        public void Collision_Forehand_ImpartsSpin()
        {
            // Arrange
            var config = new PhysicsConfig { /* put small angular drag, etc. */ };
            var resolutionSystem = new CollisionResolutionSystem();
    
            var ballState = new BallState
            {
                Velocity = new Vector3(2f, 0f, 0f), // tangential velocity
                AngularVelocity = Vector3.zero
            };
            var paddleState = new PaddleState
            {
                Velocity = Vector3.zero // stationary paddle
            };
            var collisionData = new CollisionData
            {
                Detected = true,
                CollisionTag = "Forehand",
                Normal = Vector3.up // or whichever normal the test uses
            };
    
            // Act
            resolutionSystem.ResolveCollision(ref ballState, paddleState, collisionData, config);
    
            // Assert
            Assert.IsTrue(ballState.AngularVelocity.magnitude > 0.01f, 
                "Expected non-zero spin from a tangential forehand collision.");
        }

    }
}