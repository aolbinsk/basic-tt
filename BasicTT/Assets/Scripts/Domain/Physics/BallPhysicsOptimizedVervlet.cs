using Domain.Entities;
using Domain.Interfaces;
using UnityEngine;

namespace Domain.Physics
{
    /// <summary>
    /// Provides an improved physics integrator for the ball, accounting for precise force calculations and spin dynamics.
    /// Utilizes improved Verlet integration with higher-order corrections for enhanced accuracy.
    /// </summary>
    public class BallPhysicsOptimizedVervlet : IBallPhysics
    {
        private readonly IPhysicsConfig _config;
        private readonly float _dragFactor;
        private readonly float _magnusCoefficient;

        /// <summary>
        /// Initializes a new instance of the BallPhysicsOptimizedVervlet class with the specified physics configuration.
        /// </summary>
        /// <param name="config">The physics configuration parameters.</param>
        public BallPhysicsOptimizedVervlet(IPhysicsConfig config)
        {
            _config = config;
            _dragFactor = 0.5f * _config.Air.Density * _config.Ball.DragCoefficient * _config.Ball.CrossSectionalArea;
            _magnusCoefficient = _config.Air.MagnusCoefficient;
        }

        /// <summary>
        /// Integrates the ball's state over a time step using improved Verlet integration with higher-order corrections.
        /// </summary>
        /// <param name="state">The current state of the ball.</param>
        /// <param name="dt">The time step for integration.</param>
        public void Integrate(ref BallState state, float dt)
        {
            // 1. Current acceleration
            Vector3 currentAcceleration = CalculateAcceleration(state);

            // 2. Update position
            Vector3 newPosition = state.Position
                                  + state.Velocity * dt
                                  + 0.5f * currentAcceleration * dt * dt;

            // 3. Estimate new acceleration at the new position
            BallState tempState = state;
            tempState.Position = newPosition;
            Vector3 newAcceleration = CalculateAcceleration(tempState);

            // 4. Update velocity with average acceleration
            Vector3 newVelocity = state.Velocity
                                  + 0.5f * (currentAcceleration + newAcceleration) * dt;

            // 5. Apply updates to the state
            state.Position = newPosition;
            state.Velocity = newVelocity;

            // Update rotational dynamics
            ApplyRotationalDynamics(ref state, dt);
        }

        /// <summary>
        /// Calculates the acceleration of the ball based on gravity, aerodynamic drag, and the Magnus effect.
        /// </summary>
        /// <param name="state">The current state of the ball.</param>
        /// <returns>The acceleration vector.</returns>
        private Vector3 CalculateAcceleration(BallState state)
        {
            Vector3 velocity = state.Velocity;

            // Air drag
            Vector3 dragForce = -_dragFactor * velocity.magnitude * velocity;

            // Magnus force
            Vector3 magnusForce = _magnusCoefficient * Vector3.Cross(state.AngularVelocity, velocity);

            // Combined forces
            Vector3 totalForce = _config.Gravity * _config.Ball.MassKg + dragForce + magnusForce;

            // Acceleration
            return totalForce / _config.Ball.MassKg;
        }

        /// <summary>
        /// Applies rotational dynamics to the ball, including angular drag and rotation update.
        /// </summary>
        /// <param name="state">The current state of the ball.</param>
        /// <param name="dt">The time step for integration.</param>
        private void ApplyRotationalDynamics(ref BallState state, float dt)
        {
            // Apply angular drag
            float angularDragFactor = Mathf.Exp(-_config.Air.AngularDragCoefficient * dt);
            state.AngularVelocity *= angularDragFactor;

            // Update rotation
            Quaternion deltaRotation = Quaternion.Euler(state.AngularVelocity * dt * Mathf.Rad2Deg);
            state.Rotation = deltaRotation * state.Rotation;
        }
    }
}