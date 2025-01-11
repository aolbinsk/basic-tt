using System.Collections.Generic;
using NUnit.Framework;
using Domain.Physics;
using Domain.Entities;
using Domain.Config;
using UnityEngine;
using Domain.Interfaces;
using Infrastructure.SceneSetup;

namespace Tests.Domain.Physics
{
    [TestFixture]
    public class CollisionDetectionSystemTests
    {
        private CollisionDetectionSystem _collisionDetectionSystem;
        private PhysicsConfig _physicsConfig;
        private BallState _previousBallState;
        private BallState _currentBallState;
        private PaddleState _previousPaddleState;
        private PaddleState _currentPaddleState;

        [SetUp]
        public void SetUp()
        {
            _physicsConfig = new PhysicsConfig();
            _collisionDetectionSystem = new CollisionDetectionSystem(_physicsConfig);

            _previousBallState = new BallState();
            _currentBallState = new BallState();
            _previousPaddleState = new PaddleState();
            _currentPaddleState = new PaddleState();
        }

        [Test]
        public void DetectCollision_NoCollision_ReturnsFalse()
        {
            // Arrange
            _currentBallState.Position = new Vector3(0f, 0f, 0f);
            _currentPaddleState.Position = new Vector3(10f, 0f, 0f);

            // Act
            var collisionData = _collisionDetectionSystem.DetectCollision(
                _previousBallState, _currentBallState,
                _previousPaddleState, _currentPaddleState,
                0.016f);

            // Assert
            Assert.IsFalse(collisionData.Detected);
        }

        [Test]
        public void DetectCollision_BallHitsPaddle_ReturnsTrue()
        {
            // Arrange
            _previousBallState.Position = new Vector3(0f, 1f, 0f);
            _currentBallState.Position = new Vector3(0f, 0f, 0f); // Moving towards paddle
            _currentBallState.Velocity = new Vector3(0f, -1f, 0f);

            _currentPaddleState.Position = new Vector3(0f, 0f, 0f);

            // Act
            var collisionData = _collisionDetectionSystem.DetectCollision(
                _previousBallState, _currentBallState,
                _previousPaddleState, _currentPaddleState,
                0.016f);

            // Assert
            Assert.IsTrue(collisionData.Detected);
        }
    }
}