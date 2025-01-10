namespace Domain.Interfaces
{
    using Domain.Entities;

    /// <summary>
    /// Interface for physics engine implementations.
    /// </summary>
    public interface IPhysicsEngine
    {
        /// <summary>
        /// Integrates the state of the ball over a given time step.
        /// </summary>
        /// <param name="ballState">The current state of the ball to integrate.</param>
        /// <param name="deltaTime">The time step for the integration.</param>
        void Integrate(ref BallState ballState, float deltaTime);
    }
}