using NUnit.Framework;
using Domain.Physics;
using Domain.Entities;
using Domain.Config;
using UnityEngine;

namespace Tests.Domain.Physics
{
    [TestFixture]
    public class BallRollingTests
    {
        private BallPhysicsBasicVervlet _ballPhysicsBasicVervlet;
        private BallState _ballState;
        private PhysicsConfig _physicsConfig;

        [SetUp]
        public void SetUp()
        {
            _physicsConfig = new PhysicsConfig();
            _ballPhysicsBasicVervlet = new BallPhysicsBasicVervlet(_physicsConfig);
            _ballState = new BallState
            {
                Position = Vector3.zero,
                Velocity = Vector3.zero,
                Rotation = Quaternion.identity,
                AngularVelocity = Vector3.zero
            };
        }

        // Case A: Smooth Rolling on a Flat Surface
        [Test]
        public void Ball_SmoothRollingOnFlatSurface_RollsWithoutSlipping()
        {
            // Arrange
            float ballRadius = _physicsConfig.Ball.DiameterMeters / 2f;
            float initialVelocity = 1f;
            _ballState.Position = new Vector3(0f, ballRadius, 0f);
            _ballState.Velocity = new Vector3(initialVelocity, 0f, 0f);
            _ballState.AngularVelocity = new Vector3(0f, initialVelocity / ballRadius, 0f); // v = r * omega

            // Act
            SimulateBallMotion(5f);

            // Assert
            Assert.Less(_ballState.Velocity.magnitude, 0.01f);
            Assert.Less(_ballState.AngularVelocity.magnitude, 0.01f);
            Assert.AreEqual(ballRadius, _ballState.Position.y, 0.001f);
        }

        // Case B: Ball at Rest, Small Push
        [Test]
        public void Ball_AtRestWithSmallPush_RollsAndStops()
        {
            // Arrange
            float ballRadius = _physicsConfig.Ball.DiameterMeters / 2f;
            float initialVelocity = 0.5f;
            _ballState.Position = new Vector3(0f, ballRadius, 0f);
            _ballState.Velocity = new Vector3(initialVelocity, 0f, 0f);
            _ballState.AngularVelocity = Vector3.zero;

            // Act
            SimulateBallMotion(5f);

            // Assert
            Assert.Less(_ballState.Velocity.magnitude, 0.01f);
            Assert.Less(_ballState.AngularVelocity.magnitude, 0.01f);
            Assert.AreEqual(ballRadius, _ballState.Position.y, 0.001f);
        }

        // Case C: Rolling on a Stationary, Flat Paddle
        [Test]
        public void Ball_RollingOnStationaryFlatPaddle_RollsAndStops()
        {
            // Arrange
            float paddleHeight = _physicsConfig.Paddle.HeadBladeThicknessMeters;
            float ballRadius = _physicsConfig.Ball.DiameterMeters / 2f;
            _ballState.Position = new Vector3(0f, ballRadius + paddleHeight, 0f);
            _ballState.Velocity = new Vector3(0.5f, 0f, 0f);
            _ballState.AngularVelocity = new Vector3(0f, 0.5f / ballRadius, 0f);

            // Act
            SimulateBallMotion(5f);

            // Assert
            Assert.Less(_ballState.Velocity.magnitude, 0.01f);
            Assert.AreEqual(ballRadius + paddleHeight, _ballState.Position.y, 0.001f);
        }

        // Case D: Rolling on a Tilted Paddle
        [Test]
        public void Ball_RollingOnTiltedPaddle_AcceleratesDownhill()
        {
            // Arrange
            float tiltAngle = 15f;
            float ballRadius = _physicsConfig.Ball.DiameterMeters / 2f;
            Quaternion tiltRotation = Quaternion.Euler(tiltAngle, 0f, 0f);
            Vector3 gravity = tiltRotation * _physicsConfig.Gravity;
            _physicsConfig.Gravity = gravity;

            _ballState.Position = new Vector3(0f, ballRadius + _physicsConfig.Paddle.HeadBladeThicknessMeters, 0f);
            _ballState.Velocity = Vector3.zero;
            _ballState.AngularVelocity = Vector3.zero;

            // Act
            SimulateBallMotion(2f);

            // Assert
            Assert.Greater(_ballState.Velocity.magnitude, 0.1f);
            Assert.Greater(_ballState.Position.z, 0f);
        }

        // Case E: Ball with Minimal Speed
        [Test]
        public void Ball_WithMinimalSpeed_StopsWithoutJitter()
        {
            // Arrange
            float ballRadius = _physicsConfig.Ball.DiameterMeters / 2f;
            _ballState.Position = new Vector3(0f, ballRadius, 0f);
            _ballState.Velocity = new Vector3(0.01f, 0f, 0f);
            _ballState.AngularVelocity = Vector3.zero;

            // Act
            SimulateBallMotion(2f);

            // Assert
            Assert.Less(_ballState.Velocity.magnitude, 0.01f);
            Assert.Less(_ballState.AngularVelocity.magnitude, 0.01f);
            Assert.AreEqual(ballRadius, _ballState.Position.y, 0.001f);
        }

        // Case F: Rolling with High Angular Velocity, Low Linear Velocity
        [Test]
        public void Ball_HighAngularLowLinearVelocity_TransitionsFromSlippingToRolling()
        {
            // Arrange
            float ballRadius = _physicsConfig.Ball.DiameterMeters / 2f;
            float initialAngularVelocity = 50f;
            _ballState.Position = new Vector3(0f, ballRadius, 0f);
            _ballState.Velocity = new Vector3(0.1f, 0f, 0f);
            _ballState.AngularVelocity = new Vector3(0f, initialAngularVelocity, 0f);

            // Act
            SimulateBallMotion(1f);

            // Assert
            float expectedVelocity = _ballState.AngularVelocity.y * ballRadius;
            Assert.AreEqual(expectedVelocity, _ballState.Velocity.x, 0.01f);
            Assert.AreEqual(ballRadius, _ballState.Position.y, 0.001f);
        }

        // Case G: Near-Zero Angular and Linear Velocity
        [Test]
        public void Ball_NearZeroVelocity_StopsWithoutOscillation()
        {
            // Arrange
            float ballRadius = _physicsConfig.Ball.DiameterMeters / 2f;
            _ballState.Position = new Vector3(0f, ballRadius, 0f);
            _ballState.Velocity = new Vector3(0.001f, 0f, 0f);
            _ballState.AngularVelocity = new Vector3(0f, 0.001f, 0f);

            // Act
            SimulateBallMotion(2f);

            // Assert
            Assert.Less(_ballState.Velocity.magnitude, 0.001f);
            Assert.Less(_ballState.AngularVelocity.magnitude, 0.001f);
            Assert.AreEqual(ballRadius, _ballState.Position.y, 0.001f);
        }

        // Helper method to simulate ball motion over time
        private void SimulateBallMotion(float duration)
        {
            float deltaTime = 0.01f;
            int steps = Mathf.CeilToInt(duration / deltaTime);

            for (int i = 0; i < steps; i++)
            {
                _ballPhysicsBasicVervlet.Integrate(ref _ballState, deltaTime);
            }
        }
    }
}