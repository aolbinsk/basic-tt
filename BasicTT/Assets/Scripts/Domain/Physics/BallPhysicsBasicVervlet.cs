using Domain.Entities;
using Domain.Interfaces;
using UnityEngine;

namespace Domain.Physics
{
    /// <summary>
    /// Handles the physics integration of the ball using Velocity Verlet integration for improved stability.
    /// Includes position, velocity, spin, air resistance, and Magnus force calculations.
    /// </summary>
    public class BallPhysicsBasicVervlet : IBallPhysics
    {
        private readonly IPhysicsConfig _config;
        private readonly float _dragFactor;

        /// <summary>
        /// Initializes a new instance of the BallPhysicsBasicVervlet class with the specified physics configuration.
        /// </summary>
        /// <param name="config">The physics configuration parameters.</param>
        public BallPhysicsBasicVervlet(IPhysicsConfig config)
        {
            _config = config;
            _dragFactor = 0.5f * _config.Air.Density * _config.Ball.DragCoefficient * _config.Ball.CrossSectionalArea;
        }

        /// <summary>
        /// Integrates the ball's state over a time step using Velocity Verlet integration.
        /// </summary>
        /// <param name="state">The current state of the ball.</param>
        /// <param name="dt">The time step for integration.</param>
        public void Integrate(ref BallState state, float dt)
        {
            // 1) Current state
            Vector3 currentPosition = state.Position;
            Vector3 currentVelocity = state.Velocity;

            // 2) Forces -> Acceleration
            Vector3 currentAcceleration = CalculateAcceleration(state);

            // Velocity Verlet Steps
            // x(t+dt) = x(t) + v(t)*dt + 0.5*a(t)*dt^2
            Vector3 newPosition = currentPosition
                + currentVelocity * dt
                + currentAcceleration * (0.5f * dt * dt);
            state.Position = newPosition;

            // v_mid = v(t) + 0.5*a(t)*dt
            Vector3 midVel = currentVelocity + currentAcceleration * (0.5f * dt);

            // Update temp velocity to compute new acceleration
            state.Velocity = midVel;
            Vector3 newAcceleration = CalculateAcceleration(state);

            // v(t+dt) = v(t) + 0.5*(a(t) + a(t+dt))*dt
            Vector3 newVelocity = currentVelocity + (currentAcceleration + newAcceleration) * (0.5f * dt);
            state.Velocity = newVelocity;

            // Rotational motion (Semi-Implicit Euler as an example)
            // Apply angular drag exponentially
            float angularDragCoefficient = _config.Air.AngularDragCoefficient;
            float dragFactor = Mathf.Exp(-angularDragCoefficient * dt);
            state.AngularVelocity *= dragFactor;

            // Update orientation
            Quaternion deltaRotation = Quaternion.Euler(state.AngularVelocity * (dt * Mathf.Rad2Deg));
            state.Rotation = deltaRotation * state.Rotation;
        }

        /// <summary>
        /// Calculates the acceleration of the ball based on forces such as drag and Magnus effect.
        /// </summary>
        /// <param name="state">The current state of the ball.</param>
        /// <returns>The calculated acceleration vector.</returns>
        private Vector3 CalculateAcceleration(BallState state)
        {
            Vector3 v = state.Velocity;

            // Drag
            Vector3 dragForce = -_dragFactor * v.magnitude * v;

            // Magnus
            Vector3 magnusForce = _config.Air.MagnusCoefficient * Vector3.Cross(state.AngularVelocity, v);

            Vector3 totalForce = dragForce + magnusForce;
            Vector3 acceleration = _config.Gravity + (totalForce / _config.Ball.MassKg);
            
            return acceleration;
        }
    }
}