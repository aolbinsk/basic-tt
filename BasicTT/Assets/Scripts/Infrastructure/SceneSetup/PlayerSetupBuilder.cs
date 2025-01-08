using UnityEngine;
using Domain.Interfaces;

namespace Infrastructure.SceneSetup
{
    /// <summary>
    /// Handles player and VR setup including paddle configuration.
    /// </summary>
    public class PlayerSetupBuilder
    {
        private readonly IPhysicsConfig _config;
        private readonly GameObject _ball;
        private readonly GameObject _paddle;
        private readonly Transform _xrOriginTransform;

        /// <summary>
        /// Initializes a new instance of the PlayerSetupBuilder class.
        /// </summary>
        /// <param name="config">The physics configuration to use.</param>
        /// <param name="xrOriginTransform">The player XROrigin transform.</param>
        /// <param name="ball">The ball GameObject.</param>
        /// <param name="paddle">The paddle GameObject.</param>
        public PlayerSetupBuilder(
            IPhysicsConfig config, 
            Transform xrOriginTransform, 
            GameObject ball, 
            GameObject paddle)
        {
            _config = config;
            _xrOriginTransform = xrOriginTransform;
            _ball = ball;
            _paddle = paddle;
        }

        /// <summary>
        /// Sets up the player, including positioning and attaching the paddle.
        /// </summary>
        public void SetupPlayer()
        {
            PositionPlayer();
            AttachPaddleToController();
            PositionBall();
        }

        /// <summary>
        /// Positions the player relative to the table.
        /// </summary>
        private void PositionPlayer()
        {
            var tableHalfLength = _config.Table.LengthMeters / 2f;
            var zOffset = -tableHalfLength - _config.Player.DistanceFromTable;

            _xrOriginTransform.position = new Vector3(0f, 0f, zOffset);
        }

        /// <summary>
        /// Attaches the paddle to the right controller.
        /// </summary>
        private void AttachPaddleToController()
        {
            var rightControllerTransform = _xrOriginTransform.Find("Camera Offset/Right Controller");
            if (rightControllerTransform != null)
            {
                _paddle.transform.SetParent(rightControllerTransform, false);
                //_paddle.transform.localPosition = Vector3.zero;
                //_paddle.transform.localRotation = Quaternion.identity;
            }
            else
            {
                Debug.LogError("RightHand transform not found under XR Origin.");
            }
        }

        /// <summary>
        /// Positions the ball above the table for serving.
        /// </summary>
        private void PositionBall()
        {
            if (_ball)
            {
                var ballHeight = _config.Table.HeightMeters + 0.2f;
                var ballPositionZ = -_config.Table.LengthMeters / 4f;
                _ball.transform.position = new Vector3(0f, ballHeight, ballPositionZ);
            }
        }
    }
}