using UnityEngine;

/// <summary>
/// Handles the physics integration of the ball using Velocity Verlet integration for improved stability.
/// Includes position, velocity, spin, air resistance, and Magnus force calculations.
/// </summary>
public class BallPhysics
{
    private Vector3 _previousAcceleration;

    /// <summary>
    /// Integrates the ball's physics state over a time step using Velocity Verlet integration.
    /// This method provides better energy conservation and stability compared to basic Euler integration.
    /// </summary>
    /// <param name="state">The current state of the ball.</param>
    /// <param name="dt">The time step for the physics update.</param>
    public void Integrate(ref BallState state, float dt)
    {
        // Store current position and velocity
        Vector3 currentPosition = state.Position;
        Vector3 currentVelocity = state.Velocity;

        // Calculate current acceleration from forces
        Vector3 currentAcceleration = CalculateAcceleration(state);

        // Step 1: Update position using current velocity and acceleration
        // x(t + dt) = x(t) + v(t)dt + (1/2)a(t)dt^2
        state.Position = currentPosition + 
                        currentVelocity * dt + 
                        0.5f * currentAcceleration * dt * dt;

        // Step 2: Calculate mid-point velocity using current acceleration
        // v_mid = v(t) + (1/2)a(t)dt
        Vector3 midPointVelocity = currentVelocity + 0.5f * currentAcceleration * dt;

        // Step 3: Calculate new acceleration at the new position
        state.Velocity = midPointVelocity; // Temporarily set for force calculation
        Vector3 newAcceleration = CalculateAcceleration(state);

        // Step 4: Update final velocity using average of accelerations
        // v(t + dt) = v(t) + (1/2)(a(t) + a(t + dt))dt
        state.Velocity = currentVelocity + 0.5f * (currentAcceleration + newAcceleration) * dt;

        // Update rotation based on angular velocity
        Quaternion deltaRotation = Quaternion.Euler(state.AngularVelocity * (dt * Mathf.Rad2Deg));
        state.Rotation = deltaRotation * state.Rotation;

        // Store acceleration for next step
        _previousAcceleration = newAcceleration;

        // Apply drag to angular velocity
        state.AngularVelocity *= (1f - CalculateAngularDrag(dt));
    }

    /// <summary>
    /// Calculates the total acceleration on the ball from all forces.
    /// </summary>
    private Vector3 CalculateAcceleration(BallState state)
    {
        // Calculate air resistance (drag force)
        float airDensity = TableTennisPhysicsConfig.AirDensity;
        float dragCoefficient = TableTennisPhysicsConfig.BallDragCoefficient;
        float ballArea = TableTennisPhysicsConfig.BallCrossSectionalArea;
        Vector3 velocity = state.Velocity;
        
        // Drag force = -0.5 * ρ * Cd * A * |v| * v
        Vector3 dragForce = -0.5f * airDensity * dragCoefficient * ballArea * 
                           velocity.magnitude * velocity;

        // Calculate Magnus force due to spin
        // F_magnus = S * (ω × v), where S is the Magnus coefficient
        float magnusCoefficient = TableTennisPhysicsConfig.MagnusCoefficient;
        Vector3 magnusForce = magnusCoefficient * Vector3.Cross(state.AngularVelocity, velocity);

        // Sum all forces
        Vector3 totalForce = dragForce + magnusForce;

        // Convert forces to acceleration (F = ma)
        float ballMass = TableTennisPhysicsConfig.BallMassGrams / 1000f; // Convert to kg
        Vector3 acceleration = Physics.gravity + (totalForce / ballMass);

        return acceleration;
    }

    /// <summary>
    /// Calculates the angular drag coefficient based on the time step.
    /// </summary>
    private float CalculateAngularDrag(float dt)
    {
        // Simple linear drag model for angular velocity
        const float angularDragCoefficient = 0.1f;
        return angularDragCoefficient * dt;
    }

    /// <summary>
    /// Resets the integrator state.
    /// </summary>
    public void Reset()
    {
        _previousAcceleration = Vector3.zero;
    }
}