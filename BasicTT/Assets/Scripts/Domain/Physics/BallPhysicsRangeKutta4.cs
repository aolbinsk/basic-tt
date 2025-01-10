using Domain.Entities;
using Domain.Interfaces;
using UnityEngine;

namespace Domain.Physics
{
    /// <summary>
    /// Provides an improved physics integrator for the ball, accounting for precise force calculations and spin dynamics.
    /// Utilizes Runge-Kutta 4th order integration for enhanced accuracy.
    /// </summary>
    public class BallPhysicsRangeKutta4 : IBallPhysics
    {
        private readonly IPhysicsConfig _config;
        private readonly float _dragCoefficient;
        private readonly float _magnusCoefficient;

        /// <summary>
        /// Initializes a new instance of the BallPhysicsRangeKutta4 class with the specified physics configuration.
        /// </summary>
        /// <param name="config">The physics configuration parameters.</param>
        public BallPhysicsRangeKutta4(IPhysicsConfig config)
        {
            _config = config;
            _dragCoefficient = 0.5f * _config.Air.Density * _config.Ball.DragCoefficient * _config.Ball.CrossSectionalArea;
            _magnusCoefficient = _config.Air.MagnusCoefficient;
        }

        /// <summary>
        /// Integrates the ball's state over a time step using Runge-Kutta 4th order (RK4) integration.
        /// </summary>
        /// <param name="state">The current state of the ball.</param>
        /// <param name="dt">The time step for integration.</param>
        public void Integrate(ref BallState state, float dt)
        {
            Vector3 position = state.Position;
            Vector3 velocity = state.Velocity;
            Vector3 angularVelocity = state.AngularVelocity;

            // RK4 Integration Steps
            var k1 = ComputeDerivatives(position, velocity, angularVelocity);
            var k2 = ComputeDerivatives(
                position + 0.5f * dt * k1.PositionDerivative,
                velocity + 0.5f * dt * k1.VelocityDerivative,
                angularVelocity);
            var k3 = ComputeDerivatives(
                position + 0.5f * dt * k2.PositionDerivative,
                velocity + 0.5f * dt * k2.VelocityDerivative,
                angularVelocity);
            var k4 = ComputeDerivatives(
                position + dt * k3.PositionDerivative,
                velocity + dt * k3.VelocityDerivative,
                angularVelocity);

            // Update position and velocity
            state.Position += (dt / 6f) * (k1.PositionDerivative + 2f * k2.PositionDerivative + 2f * k3.PositionDerivative + k4.PositionDerivative);
            state.Velocity += (dt / 6f) * (k1.VelocityDerivative + 2f * k2.VelocityDerivative + 2f * k3.VelocityDerivative + k4.VelocityDerivative);

            // Update rotation
            UpdateRotation(ref state, dt);
        }

        private (Vector3 PositionDerivative, Vector3 VelocityDerivative) ComputeDerivatives(Vector3 position, Vector3 velocity, Vector3 angularVelocity)
        {
            Vector3 acceleration = CalculateAcceleration(velocity, angularVelocity);
            return (velocity, acceleration);
        }

        /// <summary>
        /// Calculates the acceleration of the ball based on gravity, aerodynamic drag, and the Magnus effect.
        /// </summary>
        /// <param name="velocity">The current velocity of the ball.</param>
        /// <param name="angularVelocity">The current angular velocity of the ball.</param>
        /// <returns>The acceleration vector.</returns>
        private Vector3 CalculateAcceleration(Vector3 velocity, Vector3 angularVelocity)
        {
            Vector3 dragForce = -_dragCoefficient * velocity.magnitude * velocity;
            Vector3 magnusForce = _magnusCoefficient * Vector3.Cross(angularVelocity, velocity);
            Vector3 totalForce = _config.Gravity * _config.Ball.MassKg + dragForce + magnusForce;
            return totalForce / _config.Ball.MassKg;
        }

        /// <summary>
        /// Updates the rotational state of the ball.
        /// </summary>
        /// <param name="state">The current state of the ball.</param>
        /// <param name="dt">The time step for integration.</param>
        private void UpdateRotation(ref BallState state, float dt)
        {
            // Apply angular drag exponentially
            state.AngularVelocity *= Mathf.Exp(-_config.Air.AngularDragCoefficient * dt);

            // Update orientation using Quaternion integration
            Quaternion deltaRotation = Quaternion.Euler(state.AngularVelocity * dt * Mathf.Rad2Deg);
            state.Rotation = deltaRotation * state.Rotation;
        }
    }
}