using NUnit.Framework;
using Domain.Physics;
using Domain.Entities;
using Domain.Config;
using UnityEngine;

namespace Tests.Domain.Physics
{
    [TestFixture]
    public class BallPhysicsTests
    {
        private BallPhysicsBasicVervlet _ballPhysicsBasicVervlet;
        private BallState _ballState;
        private PhysicsConfig _physicsConfig;

        [SetUp]
        public void SetUp()
        {
            _physicsConfig = new PhysicsConfig();
            _physicsConfig.Air.AngularDragCoefficient = 0f;
            _physicsConfig.Air.Density = 0f;
            _ballPhysicsBasicVervlet = new BallPhysicsBasicVervlet(_physicsConfig);
            _ballState = new BallState
            {
                Position = Vector3.zero,
                Velocity = Vector3.zero,
                Rotation = Quaternion.identity,
                AngularVelocity = Vector3.zero
            };
        }

        [Test]
        public void Integrate_StationaryBall_NoMovement()
        {
            // Arrange
            _physicsConfig.Gravity = Vector3.zero;
            _ballState.Velocity = Vector3.zero;

            // Act
            _ballPhysicsBasicVervlet.Integrate(ref _ballState, 0.016f);

            // Assert
            Assert.AreEqual(Vector3.zero, _ballState.Velocity);
            Assert.AreEqual(Vector3.zero, _ballState.Position);
        }

        [Test]
        public void Integrate_BallWithInitialVelocity_PositionUpdates()
        {
            // Arrange
            _ballState.Velocity = new Vector3(5f, 0f, 0f);

            // Act
            _ballPhysicsBasicVervlet.Integrate(ref _ballState, 1f);

            // Assert
            Assert.AreEqual(5f, _ballState.Position.x, 0.1f);
        }

        [Test]
        public void Integrate_BallAffectedByGravity_VelocityChanges()
        {
            // Arrange
            _ballState.Velocity = Vector3.zero;

            // Act
            _ballPhysicsBasicVervlet.Integrate(ref _ballState, 1f);

            // Assert
            Assert.AreEqual(_physicsConfig.Gravity.y, _ballState.Velocity.y, 0.1f);
        }

        [Test]
        public void Integrate_WithSpin_AppliesMagnusEffect()
        {
            // Arrange
            _ballState.Velocity = new Vector3(10f, 0f, 0f);
            _ballState.AngularVelocity = new Vector3(0f, 100f, 0f);

            // Act
            _ballPhysicsBasicVervlet.Integrate(ref _ballState, 0.016f);

            // Assert
            Assert.AreNotEqual(0f, _ballState.Velocity.y);
        }
        
        [Test]
        public void Performance_SubstepExecutionTimeWithinLimit()
        {
            // Arrange
            var ballPhysics = new BallPhysicsOptimizedVervlet(_physicsConfig); // Selected implementation
            float timeStep = 1f / 720f; // Substep duration
            int iterations = 1000; // Simulate multiple frames for a realistic benchmark
            var stopwatch = new System.Diagnostics.Stopwatch();
    
            _ballState = new BallState
            {
                Position = Vector3.zero,
                Velocity = new Vector3(1f, 2f, 3f),
                AngularVelocity = new Vector3(10f, 20f, 30f)
            };

            // Act
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                ballPhysics.Integrate(ref _ballState, timeStep);
            }
            stopwatch.Stop();

            // Calculate average execution time per substep
            double averageTimePerSubstep = stopwatch.Elapsed.TotalSeconds / iterations;

            // Assert
            Assert.LessOrEqual(averageTimePerSubstep, timeStep, "Integration exceeds time limit per substep.");
        }
        
        [Test]
        public void Performance_MultipleBallsExecutionTimeWithinLimit()
        {
            // Arrange
            int ballCount = 100; // Number of balls to simulate
            float timeStep = 1f / 720f; // Substep duration
            var ballPhysics = new BallPhysicsOptimizedVervlet(_physicsConfig);
            var ballStates = new BallState[ballCount];

            for (int i = 0; i < ballCount; i++)
            {
                ballStates[i] = new BallState
                {
                    Position = new Vector3(i, i, i),
                    Velocity = new Vector3(i * 0.1f, i * 0.2f, i * 0.3f),
                    AngularVelocity = new Vector3(i, i, i)
                };
            }

            var stopwatch = new System.Diagnostics.Stopwatch();

            // Act
            stopwatch.Start();
            for (int i = 0; i < ballCount; i++)
            {
                ballPhysics.Integrate(ref ballStates[i], timeStep);
            }
            stopwatch.Stop();

            // Assert
            double averageTimePerBall = stopwatch.Elapsed.TotalSeconds / ballCount;
            Assert.LessOrEqual(averageTimePerBall, timeStep, "Integration exceeds time limit per ball.");
        }

        [Test]
        public void Evaluation_AccuracyComparedToReferenceImplementation()
        {
            // Arrange
            var referencePhysics = new BallPhysicsRangeKutta4(_physicsConfig); // Most accurate assumed implementation
            //var testPhysics = new BallPhysicsOptimizedVervlet(_physicsConfig); // Selected implementation
            var testPhysics = new BallPhysicsBasicVervlet(_physicsConfig); // Selected implementation
            float timeStep = 0.01f; // Simulation time step
            int iterations = 100; // Number of steps to simulate

            var referenceState = new BallState
            {
                Position = Vector3.zero,
                Velocity = new Vector3(10f, 0f, 0f),
                AngularVelocity = new Vector3(0f, 100f, 0f)
            };

            var testState = referenceState;

            // Act
            for (int i = 0; i < iterations; i++)
            {
                referencePhysics.Integrate(ref referenceState, timeStep);
                testPhysics.Integrate(ref testState, timeStep);
            }

            // Assert
            float positionDifference = Vector3.Distance(referenceState.Position, testState.Position);
            float velocityDifference = Vector3.Distance(referenceState.Velocity, testState.Velocity);

            Assert.LessOrEqual(positionDifference, 0.01f, "Position deviation exceeds threshold.");
            Assert.LessOrEqual(velocityDifference, 0.01f, "Velocity deviation exceeds threshold.");
        }
        
        [Test]
        public void Evaluation_SpeedComparedToReferenceImplementation()
        {
            // Arrange
            var referencePhysics = new BallPhysicsRangeKutta4(_physicsConfig); // Most accurate assumed implementation
            var testPhysics = new BallPhysicsOptimizedVervlet(_physicsConfig); // Selected implementation
            float timeStep = 1f / 360f; // Physics update rate
            int iterations = 1000;

            var ballState = new BallState
            {
                Position = Vector3.zero,
                Velocity = new Vector3(5f, 0f, 0f),
                AngularVelocity = new Vector3(0f, 100f, 0f)
            };

            var stopwatch = new System.Diagnostics.Stopwatch();

            // Act - Reference Implementation
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                referencePhysics.Integrate(ref ballState, timeStep);
            }
            stopwatch.Stop();
            double referenceTime = stopwatch.Elapsed.TotalSeconds;

            // Act - Selected Implementation
            stopwatch.Restart();
            for (int i = 0; i < iterations; i++)
            {
                testPhysics.Integrate(ref ballState, timeStep);
            }
            stopwatch.Stop();
            double testTime = stopwatch.Elapsed.TotalSeconds;

            // Assert
            Assert.Less(testTime, referenceTime, "Selected implementation is slower than the reference.");
        }

        [Test]
        public void Evaluation_ErrorAccumulationOverTime()
        {
            // Arrange
            var referencePhysics = new BallPhysicsRangeKutta4(_physicsConfig); // Most accurate assumed implementation
            var testPhysics = new BallPhysicsOptimizedVervlet(_physicsConfig); // Selected implementation
            float timeStep = 1f / 360f; // Physics update rate
            float simulationTime = 10f; // Total simulation duration
            int iterations = Mathf.CeilToInt(simulationTime / timeStep);

            var referenceState = new BallState
            {
                Position = Vector3.zero,
                Velocity = new Vector3(5f, 5f, 5f),
                AngularVelocity = new Vector3(10f, 10f, 10f)
            };

            var testState = referenceState;

            // Act
            for (int i = 0; i < iterations; i++)
            {
                referencePhysics.Integrate(ref referenceState, timeStep);
                testPhysics.Integrate(ref testState, timeStep);
            }

            // Assert
            float positionDifference = Vector3.Distance(referenceState.Position, testState.Position);
            float velocityDifference = Vector3.Distance(referenceState.Velocity, testState.Velocity);

            Assert.LessOrEqual(positionDifference, 0.5f, "Position error accumulates excessively over time.");
            Assert.LessOrEqual(velocityDifference, 0.5f, "Velocity error accumulates excessively over time.");
        }

    }
}