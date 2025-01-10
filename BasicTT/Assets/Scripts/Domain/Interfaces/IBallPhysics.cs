namespace Domain.Interfaces
{
    using Domain.Entities;

    /// <summary>
    /// Interface for ball physics integrator implementations.
    /// </summary>
    public interface IBallPhysics
    {
        /// <summary>
        /// Integrates the ball's state over a given time step.
        /// </summary>
        /// <param name="state">The current state of the ball to integrate.</param>
        /// <param name="deltaTime">The time step for the integration.</param>
        void Integrate(ref BallState state, float deltaTime);
    }
}