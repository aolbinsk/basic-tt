using Domain.Entities;
using Domain.Interfaces;
using UnityEngine;

namespace Infrastructure.Rendering
{
    /// <summary>
    /// Unity-based implementation of the IRenderer interface.
    /// Updates the visuals of the ball and paddle in the Unity scene.
    /// </summary>
    public class UnityRenderer : IRenderer
    {
        private readonly GameObject _ballGameObject;
        private readonly GameObject _paddleGameObject;

        /// <summary>
        /// Initializes a new instance of the UnityRenderer class.
        /// </summary>
        /// <param name="ballGameObject">The GameObject representing the ball.</param>
        /// <param name="paddleGameObject">The GameObject representing the paddle.</param>
        public UnityRenderer(GameObject ballGameObject, GameObject paddleGameObject)
        {
            _ballGameObject = ballGameObject;
            _paddleGameObject = paddleGameObject;
        }

        /// <summary>
        /// Updates the visuals of the ball based on its current state.
        /// </summary>
        /// <param name="ballState">The current state of the ball.</param>
        public void UpdateBallVisuals(BallState ballState)
        {
            if (_ballGameObject)
            {
                _ballGameObject.transform.position = ballState.Position;
                _ballGameObject.transform.rotation = ballState.Rotation;
            }
        }

        /// <summary>
        /// Updates the visuals of the paddle based on its current state.
        /// </summary>
        /// <param name="paddleState">The current state of the paddle.</param>
        public void UpdatePaddleVisuals(PaddleState paddleState)
        {
            if (_paddleGameObject)
            {
                _paddleGameObject.transform.position = paddleState.Position;
                _paddleGameObject.transform.rotation = paddleState.Rotation;
            }
        }
    }
}