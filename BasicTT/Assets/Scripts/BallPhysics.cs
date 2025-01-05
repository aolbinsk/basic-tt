using UnityEngine;

/// <summary>
/// Handles the physics integration of the ball, including position, velocity, spin, air resistance, and Magnus force.
/// </summary>
public class BallPhysics
{
    /// <summary>
    /// Integrates the ball's physics state over a time step.
    /// </summary>
    /// <param name="state">The current state of the ball.</param>
    /// <param name="dt">The time step for the physics update.</param>
    public void Integrate(ref BallState state, float dt)
    {
        // Calculate air resistance (drag)
        float airDensity = TableTennisPhysicsConfig.AirDensity;
        float dragCoefficient = TableTennisPhysicsConfig.BallDragCoefficient;
        float ballArea = TableTennisPhysicsConfig.BallCrossSectionalArea;
        Vector3 velocity = state.Velocity;
        Vector3 airResistance = -0.5f * airDensity * dragCoefficient * ballArea * velocity.magnitude * velocity;

        // Calculate Magnus force due to spin
        float magnusCoefficient = TableTennisPhysicsConfig.MagnusCoefficient;
        Vector3 magnusForce = magnusCoefficient * Vector3.Cross(state.AngularVelocity, velocity);

        // Apply forces to velocity
        Vector3 totalForces = Physics.gravity + airResistance + magnusForce;
        state.Velocity += totalForces * dt;

        // Update position
        state.Position += state.Velocity * dt;

        // Update rotation based on angular velocity
        Quaternion deltaRotation = Quaternion.Euler(state.AngularVelocity * (dt * Mathf.Rad2Deg));
        state.Rotation = deltaRotation * state.Rotation;
    }
}