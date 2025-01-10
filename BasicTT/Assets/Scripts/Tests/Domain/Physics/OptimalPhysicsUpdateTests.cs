using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Physics;
using Domain.Config;

namespace Tests.Domain.Physics
{
    /// <summary>
    /// Demonstrates testing collision detection under various combinations of
    /// - Ball & paddle speeds (low, moderate, extreme)
    /// - Optional accelerations on paddle and/or ball
    /// - Different fixedDeltaTimes and substepRates
    /// 
    /// Helps ensure collisions are robustly detected in all realistic scenarios.
    /// </summary>
    [TestFixture]
    public class ComplexCollisionTests
    {
        private CollisionDetectionSystem _collisionDetectionSystem;

        // We can define some typical "global" parameters for the tests:
        private float _reactionDistance = 1.0f; // Ball travels from z = -1.0 to z = 0f for collisions
        private float[] _fixedDeltaTimes = { 0.005f, 0.01f, 0.016f, 0.02f };
        private int[] _substepRates = { 1, 2, 4, 8 };

        /// <summary>
        /// A small struct to capture parameters for a single test scenario.
        /// </summary>
        private struct CollisionScenario
        {
            public string Name;

            public float InitialBallSpeed; // Ball starts with this speed in +Z
            public float BallAcceleration; // Ball accelerates along +Z
            public float InitialPaddleSpeed; // Paddle starts with speed in +Z
            public float PaddleAcceleration; // Paddle accelerates along +Z

            // Helps us read logs more easily
            public override string ToString() =>
                $"[Scenario: {Name}, BallSpeed={InitialBallSpeed}m/s, BallAccel={BallAcceleration}m/s², " +
                $"PaddleSpeed={InitialPaddleSpeed}m/s, PaddleAccel={PaddleAcceleration}m/s²]";
        }

        [SetUp]
        public void SetUp()
        {
            // Create a standard physics config and collision system
            var physicsConfig = new PhysicsConfig
            {
                Gravity = new Vector3(0f, -9.81f, 0f)
            };
            _collisionDetectionSystem = new CollisionDetectionSystem(physicsConfig);
        }

        /// <summary>
        /// Main test method enumerates sub-step combos plus scenario combos
        /// and checks for collisions.
        /// </summary>
        [Test]
        public void RunAllCollisionScenarios()
        {
            // Build or fetch all scenario combos
            var scenarios = GetCollisionScenarios();

            // For logging and final assertion
            var resultLogs = new List<string>();
            bool anyScenarioSucceeded = false;

            // For each scenario, test each (fixedDeltaTime, substepRate) pair
            foreach (var scenario in scenarios)
            {
                bool scenarioSuccess = false;

                foreach (float fixedDelta in _fixedDeltaTimes)
                {
                    foreach (int substeps in _substepRates)
                    {
                        bool success = SimulateVerticalScenario(
                            scenario,
                            fixedDelta,
                            substeps,
                            out float timeOfCollision,
                            out int substepIndex);

                        if (success)
                        {
                            scenarioSuccess = true;
                            resultLogs.Add(
                                $"SUCCESS: {scenario} " +
                                $"@FixedDT={fixedDelta}, Substeps={substeps}, " +
                                $"CollisionTime={timeOfCollision:F3}s (substep {substepIndex})");
                            // We could break early if we only need
                            // one working sub-step config per scenario
                            break;
                        }
                    }

                    if (scenarioSuccess) break;
                }

                if (!scenarioSuccess)
                {
                    resultLogs.Add($"FAIL: {scenario} - No collision detected in any sub-step config!");
                }

                anyScenarioSucceeded |= scenarioSuccess;
            }

            // Print all results
            foreach (var line in resultLogs)
            {
                Debug.Log(line);
            }

            // If you require that *all* scenarios must succeed in at least one config:
            //   Assert.IsFalse(resultLogs.Any(r => r.Contains("FAIL")), "Some scenario(s) never collided!");
            // Or if it's enough that at least one scenario works:
            //   Assert.IsTrue(anyScenarioSucceeded, "No scenario had a successful collision detection!");

            // Here we do the stricter approach: all must succeed in at least one sub-step combo
            Assert.IsFalse(resultLogs.Exists(r => r.Contains("FAIL")),
                "At least one scenario never detected a collision in any configuration!");
        }

        /// <summary>
        /// Provides a variety of collision scenarios:
        ///   1) Ball stationary, paddle moving
        ///   2) Ball moving, paddle stationary
        ///   3) Both moving
        ///   4) Various accelerations to simulate realistic swings or ball spin-ups
        /// Speed and acceleration values are chosen to reflect low, moderate, high extremes.
        /// </summary>
        private IEnumerable<CollisionScenario> GetCollisionScenarios()
        {
            // Example speed sets (m/s):
            float[] speeds = { 0f, 5f, 15f }; // e.g. stationary, moderate, fast
            // Example acceleration sets (m/s²):
            float[] accels = { 0f, 5f, 15f }; // no accel, moderate accel, high accel

            // We'll generate combos (ballSpeed, ballAccel) x (paddleSpeed, paddleAccel).
            // For readability, we might limit or hand-craft a few combos.
            // This example enumerates them systematically.

            foreach (float bSpeed in speeds)
            {
                foreach (float bAccel in accels)
                {
                    foreach (float pSpeed in speeds)
                    {
                        foreach (float pAccel in accels)
                        {
                            yield return new CollisionScenario
                            {
                                Name = "Ball/Paddle Speeds & Accels",
                                InitialBallSpeed = bSpeed,
                                BallAcceleration = bAccel,
                                InitialPaddleSpeed = pSpeed,
                                PaddleAcceleration = pAccel
                            };
                        }
                    }
                }
            }
        }

        private bool SimulateVerticalScenario(
            CollisionScenario scenario,
            float fixedDeltaTime,
            int substepRate,
            out float collisionTime,
            out int collisionSubstep)
        {
            collisionTime = 0f;
            collisionSubstep = -1;

            // 1) Log scenario
            Debug.Log(
                $"[SimulateVerticalScenario] START scenario={scenario}, fixedDT={fixedDeltaTime}, substeps={substepRate}");

            // Create a local config for gravity, etc.
            var localConfig = new PhysicsConfig
            {
                Gravity = new Vector3(0f, -9.81f, 0f)
            };
            IBallPhysics ballPhysics = new BallPhysicsBasicVervlet(localConfig);

            // 2) Place Ball ~1 meter above paddle by default
            float paddleY = 0.0f; // Paddle at y=0
            float ballY = 1.0f; // Ball at y=1
            // You can push these up if you want more space to accelerate

            // Create initial BallState: 
            // If you want the ball to move downward, set velocity.y to -(scenario.InitialBallSpeed).
            // Or rely on gravity alone if scenario.InitialBallSpeed=0.
            var ballState = new BallState
            {
                Position = new Vector3(0f, ballY, 0f),
                Velocity = new Vector3(0f, -scenario.InitialBallSpeed, 0f),
                AngularVelocity = Vector3.zero,
                Rotation = Quaternion.identity,
                IsHeld = false
            };

            // 3) Create a paddle below the ball
            var paddleGO = new GameObject("TestPaddle");
            var paddleCollider = paddleGO.AddComponent<BoxCollider>();
            // This might represent a “flat” area that collides with the ball from below:
            paddleCollider.size = new Vector3(0.2f, 0.02f, 0.2f);
            paddleCollider.center = Vector3.zero;

            var paddleState = new PaddleState
            {
                Position = new Vector3(0f, paddleY, 0f),
                // If you want the paddle to move upwards, velocity.y = +scenario.InitialPaddleSpeed
                Velocity = new Vector3(0f, scenario.InitialPaddleSpeed, 0f),
                AngularVelocity = Vector3.zero,
                Rotation = Quaternion.identity,
                ForehandCollider = paddleCollider,
                BackhandCollider = null
            };

            paddleGO.transform.position = paddleState.Position;

            // Optionally log collider transform
            Debug.Log($"[PaddleCollider] localSize={paddleCollider.size}, pos={paddleGO.transform.position}");

            // 4) Sub-step settings:
            float substepDt = fixedDeltaTime / substepRate;
            // If you want to ensure enough time for collisions, pick a decent duration:
            float simDuration = 2f; // For example, 2 seconds of simulation
            int totalSubsteps = Mathf.CeilToInt(simDuration / substepDt);

            Debug.Log(
                $"Simulating scenario for up to {simDuration:F2}s => {totalSubsteps} substeps. Ball starts at y={ballY}, paddle at y={paddleY}.");

            // 5) Run the sub-step loop
            for (int i = 0; i < totalSubsteps; i++)
            {
                float currentTime = i * substepDt;
                BallState prevBall = ballState.Clone();
                PaddleState prevPadd = paddleState.Clone();

                // Apply ball acceleration if any (straight down is negative y):
                if (!Mathf.Approximately(scenario.BallAcceleration, 0f))
                {
                    // scenario.BallAcceleration = positive means ball accelerating downward if we interpret it that way
                    ballState.Velocity += new Vector3(0f, -scenario.BallAcceleration * substepDt, 0f);
                }

                // Integrate the ball
                ballPhysics.Integrate(ref ballState, substepDt);

                // Apply paddle acceleration if any (straight up is positive y):
                if (!Mathf.Approximately(scenario.PaddleAcceleration, 0f))
                {
                    paddleState.Velocity += new Vector3(0f, scenario.PaddleAcceleration * substepDt, 0f);
                }

                // Move the paddle
                paddleState.Position += paddleState.Velocity * substepDt;
                paddleGO.transform.position = paddleState.Position;

                // Debug info
                if (i < 5 || i % 10 == 0)
                {
                    Debug.Log(
                        $"Substep {i} (t={currentTime:F3}s): BallY={ballState.Position.y:F3}, BallVelY={ballState.Velocity.y:F3}, " +
                        $"PaddleY={paddleState.Position.y:F3}, PaddleVelY={paddleState.Velocity.y:F3}");
                }

                // 6) Collision detection
                var collisionData = _collisionDetectionSystem.DetectCollision(
                    prevBall, ballState,
                    prevPadd, paddleState,
                    substepDt);

                if (collisionData.Detected)
                {
                    collisionTime = currentTime + collisionData.TimeOfImpact;
                    collisionSubstep = i;
                    Debug.Log(
                        $"[SimulateVerticalScenario] COLLISION DETECTED substep={i}, time={collisionTime:F3}, scenario={scenario}");
                    Object.DestroyImmediate(paddleGO);
                    return true;
                }
            }

            Debug.Log(
                $"[SimulateVerticalScenario] NO collision after {totalSubsteps} substeps (time up to {simDuration:F2}s). scenario={scenario}");
            Object.DestroyImmediate(paddleGO);
            return false;
        }


        private void LogColliderInfo(BoxCollider box, Transform parent, string name)
        {
            Vector3 lossy = parent.lossyScale;
            Bounds bounds = box.bounds;
            Debug.Log(
                $"[{name} Collider Info]\n" +
                $"  Center (local): {box.center}\n" +
                $"  Size (local): {box.size}\n" +
                $"  Transform pos: {parent.position}\n" +
                $"  Transform scale: {lossy}\n" +
                $"  World Bounds center: {bounds.center}\n" +
                $"  World Bounds size: {bounds.size}"
            );
        }
    }
}