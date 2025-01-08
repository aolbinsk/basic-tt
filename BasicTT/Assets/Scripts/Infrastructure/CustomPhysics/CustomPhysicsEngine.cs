using Domain.Entities;
using Domain.Interfaces;
using Domain.Physics;

namespace Infrastructure.CustomPhysics
{
    /// <summary>
    /// Custom implementation of the IPhysicsEngine interface.
    /// Uses a custom physics integrator for ball and paddle states.
    /// </summary>
    public class CustomPhysicsEngine : IPhysicsEngine
    {
        private readonly BallPhysics _ballPhysics;

        /// <summary>
        /// Initializes a new instance of the CustomPhysicsEngine class.
        /// </summary>
        /// <param name="config">The physics configuration parameters.</param>
        public CustomPhysicsEngine(IPhysicsConfig config)
        {
            _ballPhysics = new BallPhysics(config);
        }

        /// <summary>
        /// Integrates the state of the ball over a given time step.
        /// </summary>
        /// <param name="ballState">The current state of the ball to integrate.</param>
        /// <param name="deltaTime">The time step for the integration.</param>
        public void Integrate(ref BallState ballState, float deltaTime)
        {
            _ballPhysics.Integrate(ref ballState, deltaTime);
        }

        /// <summary>
        /// Integrates the state of the paddle over a given time step.
        /// </summary>
        /// <param name="paddleState">The current state of the paddle to integrate.</param>
        /// <param name="deltaTime">The time step for the integration.</param>
        public void Integrate(ref PaddleState paddleState, float deltaTime)
        {
            // Paddle integration is not required as paddles are directly controlled by input.
        }
    }
}