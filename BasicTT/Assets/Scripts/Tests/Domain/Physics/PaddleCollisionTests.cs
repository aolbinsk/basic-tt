using Domain.Config;
using Domain.Entities;
using Domain.Physics;
using Infrastructure.SceneSetup;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Domain.Physics
{
    [TestFixture]
    public class PaddleCollisionTests
    {
        private PhysicsConfig _physicsConfig;
        private CollisionDetectionSystem _collisionSystem;
        private BallState _ballState;
        private PaddleState _paddleState;

        [SetUp]
        public void SetUp()
        {
            Debug.Log("[SetUp] Initializing PhysicsConfig and CollisionDetectionSystem...");

            _physicsConfig = new PhysicsConfig
            {
                // If you need to override or set specific values, do so here
            };

            _collisionSystem = new CollisionDetectionSystem(_physicsConfig);

            _ballState = new BallState
            {
                Position = Vector3.zero,
                Velocity = Vector3.zero,
                Rotation = Quaternion.identity,
                AngularVelocity = Vector3.zero,
                IsHeld = false
            };

            _paddleState = new PaddleState
            {
                Position = Vector3.zero,
                Rotation = Quaternion.identity,
                Velocity = Vector3.zero,
                AngularVelocity = Vector3.zero
            };

            Debug.Log("[SetUp] Done initializing test states.");
        }

        /// <summary>
        /// Test where we rotate the paddle so that local forehand (-X) faces upward in world space,
        /// then drop the ball from above (Y=some positive). We expect a forehand collision.
        /// </summary>
        [Test]
        public void Collision_ForehandFacingUp_BallDropsDown_ReturnsForehandTag()
        {
            Debug.Log("[Test] Collision_ForehandFacingUp_BallDropsDown_ReturnsForehandTag START");

            // If forehand is local -X, rotating around Z by -90° will align local -X with +Y in world:
            _paddleState.Position = Vector3.zero;
            _paddleState.Rotation = Quaternion.Euler(0f, 0f, -90f);

            // Ball above paddle, dropping down
            var prevBall = _ballState.Clone();
            prevBall.Position = new Vector3(0f, 1f, 0f);

            var currBall = _ballState.Clone();
            currBall.Position = new Vector3(0f, -1f, 0f);

            Debug.Log($"[Test] Forehand Up: Paddle rotation = {_paddleState.Rotation.eulerAngles}, " +
                      $"prevBallPos={prevBall.Position}, currBallPos={currBall.Position}");

            float deltaTime = 0.016f;
            CollisionData collisionData = _collisionSystem.DetectCollision(
                prevBall, currBall,
                _paddleState, _paddleState,
                deltaTime
            );

            Debug.Log($"[Test] collisionData.Detected={collisionData.Detected}, " +
                      $"Tag={collisionData.CollisionTag}, Normal={collisionData.Normal}");

            Assert.IsTrue(collisionData.Detected, "[ForehandUp] Expected collision with the paddle forehand side facing up.");
            Assert.AreEqual("Forehand", collisionData.CollisionTag, "Wrong collision side reported (expected Forehand).");
        }

        /// <summary>
        /// Test where we rotate the paddle so that local backhand (+X) faces upward in world space,
        /// then drop the ball from above. We expect a backhand collision.
        /// </summary>
        [Test]
        public void Collision_BackhandFacingUp_BallDropsDown_ReturnsBackhandTag()
        {
            Debug.Log("[Test] Collision_BackhandFacingUp_BallDropsDown_ReturnsBackhandTag START");

            // If backhand is +X, rotating around Z by +90° will put +X in +Y
            _paddleState.Position = Vector3.zero;
            _paddleState.Rotation = Quaternion.Euler(0f, 0f, 90f);

            var prevBall = _ballState.Clone();
            prevBall.Position = new Vector3(0f, 1f, 0f);

            var currBall = _ballState.Clone();
            currBall.Position = new Vector3(0f, -1f, 0f);

            Debug.Log($"[Test] Backhand Up: Paddle rotation = {_paddleState.Rotation.eulerAngles}, " +
                      $"prevBallPos={prevBall.Position}, currBallPos={currBall.Position}");

            float deltaTime = 0.016f;
            CollisionData collisionData = _collisionSystem.DetectCollision(
                prevBall, currBall,
                _paddleState, _paddleState,
                deltaTime
            );

            Debug.Log($"[Test] collisionData.Detected={collisionData.Detected}, " +
                      $"Tag={collisionData.CollisionTag}, Normal={collisionData.Normal}");

            Assert.IsTrue(collisionData.Detected, "[BackhandUp] Expected collision with the paddle backhand side facing up.");
            Assert.AreEqual("Backhand", collisionData.CollisionTag, "Wrong collision side reported (expected Backhand).");
        }

        /// <summary>
        /// Test a scenario where the ball moves slightly horizontally while dropping,
        /// but the paddle is oriented so forehand is up. 
        /// We ensure it only detects 'Forehand' once, not double collisions or misses.
        /// </summary>
        [Test]
        public void Collision_ForehandUp_BallMovingSlightlySideways_StillForehand()
        {
            Debug.Log("[Test] Collision_ForehandUp_BallMovingSlightlySideways_StillForehand START");

            _paddleState.Position = Vector3.zero;
            // Forehand up again
            _paddleState.Rotation = Quaternion.Euler(0f, 0f, -90f);

            var prevBall = _ballState.Clone();
            prevBall.Position = new Vector3(-0.02f, 1f, 0f); // a bit left

            var currBall = _ballState.Clone();
            currBall.Position = new Vector3(-0.019f, -1f, 0f);

            Debug.Log($"[Test] ForehandUp + sideways: Paddle rotation = {_paddleState.Rotation.eulerAngles}, " +
                      $"prevBallPos={prevBall.Position}, currBallPos={currBall.Position}, " +
                      $"velocity={currBall.Velocity}");

            float dt = 0.016f;
            CollisionData collisionData = _collisionSystem.DetectCollision(
                prevBall, currBall,
                _paddleState, _paddleState,
                dt
            );

            Debug.Log($"[Test] collisionData.Detected={collisionData.Detected}, Tag={collisionData.CollisionTag}");

            if (collisionData.Detected)
            {
                Assert.AreEqual("Forehand", collisionData.CollisionTag, "Expected only Forehand collision.");
            }
            else
            {
                Debug.LogWarning("[Test] The ball might have missed the paddle altogether in this scenario!");
                Assert.Fail("No collision detected, but the ball should have hit the paddle.");
            }
        }

        /// <summary>
        /// Tests a "center approach" scenario. However, instead of approaching from X=0 horizontally,
        /// we can do a minimal lateral offset and see if there's no double detection. 
        /// This basically checks we don't get "Forehand" and "Backhand" simultaneously.
        /// </summary>
        [Test]
        public void Collision_CenterApproach_DoesNotDoubleDetect()
        {
            Debug.Log("[Test] Collision_CenterApproach_DoesNotDoubleDetect START");

            // We'll orient the paddle so that the "split" between forehand/backhand is along X,
            // but the ball is falling near X=0. We'll see if it triggers only one side or none.

            _paddleState.Position = Vector3.zero;
            // Keep rotation = identity, so the paddle is presumably "flat" in XY, 
            // meaning forehand is negative X, backhand is positive X horizontally.

            // Ball above center, dropping down. 
            var prevBall = _ballState.Clone();
            prevBall.Position = new Vector3(0f, 1.0f, 0f);

            var currBall = _ballState.Clone();
            currBall.Position = new Vector3(0f, -1f, 0f);

            float dt = 0.016f;
            CollisionData collisionData = _collisionSystem.DetectCollision(
                prevBall, currBall,
                _paddleState, _paddleState,
                dt
            );

            Debug.Log($"[Test] collisionData.Detected={collisionData.Detected}, Tag={collisionData.CollisionTag}");
            if (collisionData.Detected)
            {
                // If we do detect a collision, we want to ensure it's only one side
                Assert.IsFalse(collisionData.CollisionTag.Contains(","), 
                    "[CenterApproach] Should not get multiple collision tags at once.");
            }
            else
            {
                Debug.LogWarning("[CenterApproach] The ball might be missing both colliders if there's a gap or the geometry is off.");
                Assert.Fail("No collision detected, but the ball should have hit the paddle.");
            }
        }

        /// <summary>
        /// Test rotating the paddle around Y, just to see if forehand or backhand is angled. 
        /// Let's do forehand up, but also rotate Y=45°, meaning the paddle has a diagonal orientation in the plane.
        /// </summary>
        [Test]
        public void Collision_WithPaddleRotated45Y_ForehandUpDiagonal()
        {
            Debug.Log("[Test] Collision_WithPaddleRotated45Y_ForehandUpDiagonal START");

            // We'll combine -90 around Z (forehand up) + 45 around Y to get a diagonal tilt
            _paddleState.Position = Vector3.zero;
            _paddleState.Rotation = Quaternion.Euler(0f, 45f, -90f);

            var prevBall = _ballState.Clone();
            prevBall.Position = new Vector3(0f, 1f, 0f);

            var currBall = _ballState.Clone();
            currBall.Position = new Vector3(0f, -1f, 0f);

            float dt = 0.016f;
            CollisionData collisionData = _collisionSystem.DetectCollision(
                prevBall, currBall,
                _paddleState, _paddleState,
                dt
            );

            Debug.Log($"[Test] collisionData.Detected={collisionData.Detected}, Tag={collisionData.CollisionTag}");
            if (collisionData.Detected)
            {
                // We still expect "Forehand" if the local -X is pointed up, 
                // even though we have an additional Y-rotation. 
                Assert.AreEqual("Forehand", collisionData.CollisionTag, 
                    "[PaddleRotated45Y] Expected Forehand collision with diagonal orientation.");
            }
            else
            {
                Debug.LogWarning("[PaddleRotated45Y] No collision detected at all - geometry mismatch or ball missed the paddle?");
                Assert.Fail("No collision detected, but the ball should have hit the paddle.");
            }
        }
        
                /// <summary>
        /// Test a corner collision on the forehand side (near the 'top-front-left' corner, 
        /// assuming forehand is at negative X, and top = +Y, front = +Z in local coords).
        /// We'll keep the paddle at identity rotation so local space matches world space.
        /// </summary>
        [Test]
        public void Collision_ForehandCorner_Hits_ForehandTag()
        {
            // Arrange
            _paddleState.Position = Vector3.zero;
            // Forehand up 
            _paddleState.Rotation = Quaternion.Euler(0f, 0f, -90f);

            var leftCornerX = -_physicsConfig.Paddle.HeadWidthMeters / 2f;
            var topCornerZ = _physicsConfig.Paddle.HeadLengthMeters / 2f;
            
            var prevBall = _ballState.Clone();
            prevBall.Position = new Vector3(-leftCornerX, 1f, topCornerZ);
            var currBall = _ballState.Clone();
            currBall.Position = new Vector3(-leftCornerX, -1f, topCornerZ);

            float deltaTime = 0.016f;

            // Act
            CollisionData collisionData = _collisionSystem.DetectCollision(
                prevBall, currBall,
                _paddleState, _paddleState,
                deltaTime
            );

            // Assert
            Debug.Log($"[ForehandCorner] collisionData={collisionData.Detected}, Tag={collisionData.CollisionTag}");
            Assert.IsTrue(collisionData.Detected, "Expected collision at the forehand corner.");
            Assert.AreEqual("Forehand", collisionData.CollisionTag, "Should detect forehand collision, not backhand.");
        }

        /// <summary>
        /// Tests a case where the ball approaches a corner region but actually 
        /// stays outside the bounding box (i.e. just 'grazes' it) so no collision is detected.
        /// </summary>
        [Test]
        public void Collision_ForehandCorner_Misses_NoCollision()
        {
            // Arrange
            _paddleState.Position = Vector3.zero;
            _paddleState.Rotation = Quaternion.identity;

            // We'll place the ball near the forehand corner, but not enough to intersect.
            var prevBall = _ballState.Clone();
            prevBall.Position = new Vector3(-0.16f, 0.02f, 0.08f);

            var currBall = _ballState.Clone();
            // Move slightly differently so we *do not* actually cross into the bounding box
            currBall.Position = new Vector3(-0.16f, 0.02f, 0.075f);

            float deltaTime = 0.016f;

            // Act
            CollisionData collisionData = _collisionSystem.DetectCollision(
                prevBall, currBall,
                _paddleState, _paddleState,
                deltaTime
            );

            // Assert
            Debug.Log($"[ForehandCornerMiss] collisionData={collisionData.Detected}, Tag={collisionData.CollisionTag}");
            Assert.IsFalse(collisionData.Detected, "Should NOT detect collision if the ball path stays outside the corner.");
        }
    }
}
