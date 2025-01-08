namespace Domain.Interfaces
{
    using Domain.Entities;

    /// <summary>
    /// Interface for collision detection and resolution systems.
    /// </summary>
    public interface ICollisionSystem
    {
        /// <summary>
        /// Detects collisions between the ball and paddle, as well as the ball and the environment.
        /// </summary>
        /// <param name="previousBallState">The previous state of the ball.</param>
        /// <param name="currentBallState">The current state of the ball.</param>
        /// <param name="previousPaddleState">The previous state of the paddle.</param>
        /// <param name="currentPaddleState">The current state of the paddle.</param>
        /// <param name="deltaTime">The time step for the physics update.</param>
        /// <returns>Collision data if a collision is detected, otherwise an empty collision data object.</returns>
        CollisionData DetectCollision(
            BallState previousBallState, BallState currentBallState,
            PaddleState previousPaddleState, PaddleState currentPaddleState,
            float deltaTime);

        /// <summary>
        /// Resolves a detected collision and updates the ball's state.
        /// </summary>
        /// <param name="ballState">The current state of the ball to be updated.</param>
        /// <param name="paddleState">The state of the paddle involved in the collision, if any.</param>
        /// <param name="collisionData">The collision data to resolve.</param>
        void ResolveCollision(ref BallState ballState, PaddleState paddleState, CollisionData collisionData);
    }
}