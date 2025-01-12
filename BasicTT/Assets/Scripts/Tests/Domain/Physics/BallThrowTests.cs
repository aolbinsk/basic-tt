using System.Collections.Generic;
using NUnit.Framework;
using Domain.Physics;
using Domain.Entities;
using Domain.Config;
using UnityEngine;
using Domain.Interfaces;
using Domain.Logic;
using Infrastructure.CustomPhysics;
using Infrastructure.Rendering;

namespace Tests.Domain.Physics
{
    [TestFixture]
    public class BallThrowTests
    {
        private PhysicsConfig _physicsConfig;
        private TableTennisSimulation _simulation;

        [SetUp]
        public void SetUp()
        {
            _physicsConfig = new PhysicsConfig();

            // Create mock objects for simulation dependencies
            var collisionSystem = new CollisionDetectionSystem(_physicsConfig);
            var physicsEngine = new CustomPhysicsEngine(_physicsConfig);

            _simulation = new TableTennisSimulation(
                physicsEngine,
                collisionSystem,
                _physicsConfig);
        }

        // Test Case A: Minimal Release Velocity
        [Test]
        public void BallThrow_MinimalVelocity_ShouldEnforceMinimumUpwardVelocity()
        {
            // Arrange
            var ball = new BallState
            {
                IsHeld = true,
                Position = new Vector3(0f, 1f, 0f),
                Velocity = Vector3.zero,
                AngularVelocity = Vector3.zero
            };

            var controllerState = new ControllerState
            {
                Velocity = new Vector3(0f, 0.05f, 0f), // Below minimum threshold
                AngularVelocity = Vector3.zero,
                GripPressed = true,
                Position = Vector3.zero,
                Rotation = Quaternion.identity
            };

            // Act
            BallThrowLogic.ReleaseBall(ref ball, controllerState, _physicsConfig);

            // Assert
            Assert.IsFalse(ball.IsHeld, "Ball should no longer be held.");
            Assert.AreEqual(_physicsConfig.Ball.MinThrowVelocity, ball.Velocity.y, 0.01f, "Upward velocity should meet minimum threshold.");
            Assert.AreEqual(0f, ball.Velocity.x, 0.01f, "No horizontal motion expected.");
            Assert.AreEqual(Vector3.zero, ball.AngularVelocity, "No spin should be imparted.");
        }

        // Test Case B: Gentle Throw
        [Test]
        public void BallThrow_GentleThrow_ShouldMatchControllerVelocity()
        {
            // Arrange
            var ball = new BallState
            {
                IsHeld = true,
                Position = new Vector3(0f, 1f, 0f),
                Velocity = Vector3.zero,
                AngularVelocity = Vector3.zero
            };

            var controllerState = new ControllerState
            {
                Velocity = new Vector3(0f, 2f, 0f),
                AngularVelocity = Vector3.zero,
                GripPressed = true,
                Position = Vector3.zero,
                Rotation = Quaternion.identity
            };

            // Act
            BallThrowLogic.ReleaseBall(ref ball, controllerState, _physicsConfig);

            // Assert
            Assert.IsFalse(ball.IsHeld, "Ball should no longer be held.");
            Assert.AreEqual(controllerState.Velocity, ball.Velocity, "Ball velocity should match controller velocity.");
            Assert.AreEqual(Vector3.zero, ball.AngularVelocity, "No spin should be imparted.");
        }

        // Test Case C: Fast Throw (Velocity Clamping)
        [Test]
        public void BallThrow_FastThrow_ShouldClampToMaxVelocity()
        {
            // Arrange
            var ball = new BallState
            {
                IsHeld = true,
                Position = new Vector3(0f, 1f, 0f),
                Velocity = Vector3.zero,
                AngularVelocity = Vector3.zero
            };
            
            var controllerState = new ControllerState
            {
                Velocity = new Vector3(0f, 10f, 0f), // Exceeds max velocity,
                AngularVelocity = Vector3.zero,
                GripPressed = true,
                Position = Vector3.zero,
                Rotation = Quaternion.identity
            };

            // Act
            BallThrowLogic.ReleaseBall(ref ball, controllerState, _physicsConfig);

            // Assert
            Assert.IsFalse(ball.IsHeld, "Ball should no longer be held.");
            Assert.AreEqual(_physicsConfig.Ball.MaxThrowVelocity, ball.Velocity.magnitude, 0.01f, "Velocity should be clamped to max.");
            Assert.AreEqual(Vector3.zero, ball.AngularVelocity, "No spin should be imparted.");
        }

        // Test Case D: Release with Rotating Controller
        [Test]
        public void BallThrow_WithControllerSpin_ShouldNotImpartSpinToBall()
        {
            // Arrange
            var ball = new BallState
            {
                IsHeld = true,
                Position = new Vector3(0f, 1f, 0f),
                Velocity = Vector3.zero,
                AngularVelocity = Vector3.zero
            };
            var controllerVelocity = new Vector3(0f, 2f, 0f);
            var controllerAngularVelocity = new Vector3(0f, 5f, 0f);

            // Act
            BallThrowLogic.ReleaseBall(ref ball, new ControllerState
            {
                Velocity = controllerVelocity,
                AngularVelocity = controllerAngularVelocity,
                GripPressed = true,
                Position = Vector3.zero,
                Rotation = Quaternion.identity
            }, _physicsConfig);

            // Assert
            Assert.IsFalse(ball.IsHeld, "Ball should no longer be held.");
            Assert.AreEqual(controllerVelocity, ball.Velocity, "Ball velocity should match controller velocity.");
            Assert.AreEqual(Vector3.zero, ball.AngularVelocity, "No spin should be imparted.");
        }

        // Test Case E: Smooth Transition During Release
        [Test]
        public void BallThrow_SmoothTransition_ShouldNotCauseSuddenJumps()
        {
            // Arrange
            var controllerVelocity = new Vector3(0f, 3f, 0f);
            _simulation.SetLeftControllerState(new ControllerState
            {
                GripPressed = true,
                Velocity = controllerVelocity
            });

            // Act
            _simulation.UpdateSimulation(0.02f); // One frame of physics update
            var heldBall = new BallState();
            _simulation.GetBallState(ref heldBall);
          
            Assert.AreEqual(controllerVelocity, heldBall.Velocity, "Ball velocity should be same as controller.");
            Assert.IsTrue(heldBall.IsHeld, "Ball should still be held.");
            
            // Release the ball
            _simulation.SetLeftControllerState(new ControllerState
            {
                GripPressed = false,
                Velocity = controllerVelocity
            });

            _simulation.UpdateSimulation(0.02f); // One frame of physics update

            // Assert
            var updatedBall = new BallState();
            _simulation.GetBallState(ref updatedBall);
            Assert.IsFalse(updatedBall.IsHeld, "Ball should no longer be held.");
            Assert.AreEqual(controllerVelocity.y + _physicsConfig.Gravity.y * 0.02f, updatedBall.Velocity.y, 0.1f, "Ball velocity should be continuous after release.");
        }
    }
}