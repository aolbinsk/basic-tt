using NUnit.Framework;
using System.Diagnostics;
using UnityEngine;
using Domain.Config;
using Domain.Entities;
using Domain.Logic;
using Domain.Physics;
using Domain.Interfaces;
using Infrastructure.CustomPhysics;
using System.Collections.Generic;

namespace Tests.Domain.Performance
{
    [TestFixture]
    public class PerformanceSustainabilityTests
    {
        // Target frequency and test duration
        const float targetSimulationFrequency = 120*20;

        /// <summary>
        /// Ensures 95% of simulation update steps are fast enough to sustain the target frequency
        /// and prints a histogram of the step durations.
        /// </summary>
        [Test]
        public void SustainSimulationAtTargetRateOver10Seconds_95Percentile()
        {
            const float testDurationSeconds = 10f;
            float fixedDeltaTime = 1f / targetSimulationFrequency;
            int totalSteps = Mathf.CeilToInt(testDurationSeconds / fixedDeltaTime);

            // Setup a similar test environment as in SustainSimulationAtTargetRateOver10Seconds
            var config = new PhysicsConfig();
            IPhysicsEngine physicsEngine = new CustomPhysicsEngine(config);
            ICollisionSystem collisionSystem = new CollisionDetectionSystem(config);
            var simulation = new TableTennisSimulation(physicsEngine, collisionSystem, config);

            BallState ballState = new BallState
            {
                Position = Vector3.zero,
                Velocity = new Vector3(2f, 3f, 0f),
                AngularVelocity = new Vector3(0f, 10f, 0f)
            };
            simulation.SetCurrentBallState(ballState);

            // Store update durations
            List<double> stepDurations = new List<double>(totalSteps);
            Stopwatch iterationStopwatch = new Stopwatch();

            // Run simulation for the required steps
            for (int i = 0; i < totalSteps; i++)
            {
                iterationStopwatch.Restart();
                simulation.UpdateSimulation(fixedDeltaTime);
                iterationStopwatch.Stop();
                // Record the elapsed time for this step
                stepDurations.Add(iterationStopwatch.Elapsed.TotalSeconds);
            }

            // Compute 95th percentile of the step durations
            stepDurations.Sort();
            int index95 = (int)(0.95 * stepDurations.Count) - 1;
            double percentile95 = stepDurations[Mathf.Clamp(index95, 0, stepDurations.Count - 1)];

            // The maximum allowed step time to maintain target frequency is (1 / targetSimulationFrequency)
            double maxAllowedStepTime = 1.0 / targetSimulationFrequency;

            // Assert the 95th percentile is within the target time
            Assert.LessOrEqual(
                percentile95,
                maxAllowedStepTime,
                $"95th percentile step time {percentile95:F8}s exceeds the allowed {maxAllowedStepTime:F8}s for {targetSimulationFrequency}Hz."
            );

            // Print a simple histogram of step durations
            const int histogramBuckets = 5;
            double bucketSize = maxAllowedStepTime / histogramBuckets;
            int[] buckets = new int[histogramBuckets + 1];
            foreach (var duration in stepDurations)
            {
                int bucketIndex = (int)Mathf.Floor((float)(duration / bucketSize));
                if (bucketIndex >= 0 && bucketIndex < buckets.Length)
                {
                    buckets[bucketIndex]++;
                }
                else if (bucketIndex >= buckets.Length)
                {
                    buckets[buckets.Length - 1]++;
                }
            }

            TestContext.WriteLine("Step Duration Histogram (up to target step time):");
            for (int i = 0; i < buckets.Length; i++)
            {
                double rangeStart = i * bucketSize;
                double rangeEnd = (i + 1) * bucketSize;
                if (i == buckets.Length - 1)
                {
                    TestContext.WriteLine($" [{rangeStart:F8}s +] : {buckets[i]}");
                }
                else
                {
                    TestContext.WriteLine($" [{rangeStart:F8}s - {rangeEnd:F8}s) : {buckets[i]}");
                }
            }
        }

        /// <summary>
        /// Simulates and measures specific scenarios with histograms for no collision, environment collision, and paddle collision.
        /// </summary>
        [Test]
        public void SimulateAndMeasureScenariosWithHistograms()
        {
            float simDuration = 5f;
            float fixedDelta = 1f / 120f;

            // Scenario 1: No collision
            BallState noCollisionBall = new BallState
            {
                Position = new Vector3(0f, 2f, 10f), // Start far away
                Velocity = new Vector3(0f, 0f, 0f),
                AngularVelocity = Vector3.zero
            };
            PaddleState dummyPaddle = new PaddleState
            {
                Position = new Vector3(0f, -20f, 0f) // well out of reach
            };
            SimulateAndPrintHistogram("NoCollisionPath", noCollisionBall, dummyPaddle, simDuration, fixedDelta);

            // Scenario 2: Environment collision
            BallState environmentCollisionBall = new BallState
            {
                Position = new Vector3(0f, 2f, 0f),
                Velocity = new Vector3(0f, -3f, 0f),
                AngularVelocity = Vector3.zero
            };
            SimulateAndPrintHistogram("EnvironmentCollisionPath", environmentCollisionBall, dummyPaddle, simDuration, fixedDelta);

            // Scenario 3: Paddle collision
            BallState paddleCollisionBall = new BallState
            {
                Position = new Vector3(0f, 1f, -1f),
                Velocity = new Vector3(0f, 0f, 2f),
                AngularVelocity = Vector3.zero
            };
            PaddleState paddleState = new PaddleState
            {
                Position = new Vector3(0f, 1f, 0f), // in the path
                Velocity = new Vector3(0f, 0f, 0f)
            };
            SimulateAndPrintHistogram("PaddleCollisionPath", paddleCollisionBall, paddleState, simDuration, fixedDelta);
        }

        private void SimulateAndPrintHistogram(string scenarioName, BallState ballState, PaddleState paddleState, float simDuration, float fixedDelta)
        {
            var config = new PhysicsConfig();
            IPhysicsEngine physicsEngine = new CustomPhysicsEngine(config);
            ICollisionSystem collisionSystem = new CollisionDetectionSystem(config);
            var simulation = new TableTennisSimulation(physicsEngine, collisionSystem, config);

            simulation.SetCurrentBallState(ballState);
            simulation.SetCurrentPaddleState(paddleState);

            int totalSteps = Mathf.CeilToInt(simDuration / fixedDelta);
            List<double> stepDurations = new List<double>(totalSteps);
            Stopwatch stopwatch = new Stopwatch();

            for (int i = 0; i < totalSteps; i++)
            {
                stopwatch.Restart();
                simulation.UpdateSimulation(fixedDelta);
                stopwatch.Stop();
                stepDurations.Add(stopwatch.Elapsed.TotalSeconds);
            }

            PrintHistogram(stepDurations, scenarioName);
        }

        private void PrintHistogram(List<double> stepDurations, string scenarioName)
        {
            stepDurations.Sort();
            double maxStep = stepDurations[^1];
            int bucketCount = 5;
            double bucketSize = maxStep / bucketCount;
            var buckets = new int[bucketCount + 1];

            foreach (var duration in stepDurations)
            {
                int bucketIndex = (int)Mathf.Floor((float)(duration / bucketSize));
                if (bucketIndex >= bucketCount) bucketIndex = bucketCount;
                buckets[bucketIndex]++;
            }

            TestContext.WriteLine($"[{scenarioName}] Step Duration Histogram (s):");
            for (int i = 0; i < buckets.Length; i++)
            {
                double rangeStart = i * bucketSize;
                double rangeEnd = (i == buckets.Length - 1) ? maxStep : (i + 1) * bucketSize;
                TestContext.WriteLine($"  [{rangeStart:F8} - {rangeEnd:F8}] => {buckets[i]} steps");
            }
        }
    }
}