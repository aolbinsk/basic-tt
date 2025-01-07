using UnityEngine;

/// <summary>
/// Handles the physics integration of the ball using Velocity Verlet integration for improved stability.
/// Includes position, velocity, spin, air resistance, and Magnus force calculations.
/// </summary>
public class BallPhysics
{
    private Vector3 _previousAcceleration;

    public void Integrate(ref BallState state, float dt)
    {
        // 1) Current state
        Vector3 currentPosition = state.Position;
        Vector3 currentVelocity = state.Velocity;

        // 2) Forces -> Acceleration
        Vector3 currentAcceleration = CalculateAcceleration(state);

        // Velocity Verlet Steps
        // x(t+dt) = x(t) + v(t)*dt + 0.5*a(t)*dt^2
        state.Position = currentPosition 
            + currentVelocity * dt 
            + 0.5f * currentAcceleration * dt * dt;

        // v_mid = v(t) + 0.5*a(t)*dt
        Vector3 midVel = currentVelocity + 0.5f * currentAcceleration * dt;

        // Update temp velocity to compute new acceleration
        state.Velocity = midVel;
        Vector3 newAcceleration = CalculateAcceleration(state);

        // v(t+dt) = v(t) + 0.5*(a(t) + a(t+dt))*dt
        state.Velocity = currentVelocity + 0.5f * (currentAcceleration + newAcceleration) * dt;

        // --- Rotational motion (Semi-Implicit Euler as an example) ---
        // Optionally compute aerodynamic torque for spin
        // Vector3 torque = CalculateTorque(state);
        // Vector3 angularAccel = torque / momentOfInertia;
        // state.AngularVelocity += angularAccel * dt; // If you have moment of inertia for the ball

        // Just apply angular drag exponentially
        float angularDragCoefficient = 0.1f; // or config-based
        float dragFactor = Mathf.Exp(-angularDragCoefficient * dt);
        state.AngularVelocity *= dragFactor;

        // Update orientation
        Quaternion deltaRotation = Quaternion.Euler(state.AngularVelocity * (dt * Mathf.Rad2Deg));
        state.Rotation = deltaRotation * state.Rotation;

        _previousAcceleration = newAcceleration;
    }

    private Vector3 CalculateAcceleration(BallState state)
    {
        Vector3 v = state.Velocity;

        // Possibly speed-dependent drag coefficient
        float speed = v.magnitude;
        float dynamicCd = TableTennisPhysicsConfig.BallDragCoefficient; 
        // Could do dynamicCd = SomeDragFunction(speed); // if you want advanced modeling

        // Drag
        Vector3 dragForce = -0.5f * TableTennisPhysicsConfig.AirDensity 
                                  * dynamicCd 
                                  * TableTennisPhysicsConfig.BallCrossSectionalArea
                                  * speed * v;

        // Magnus
        float magnusCoeff = TableTennisPhysicsConfig.MagnusCoefficient;
        Vector3 magnusForce = magnusCoeff * Vector3.Cross(state.AngularVelocity, v);

        Vector3 totalForce = dragForce + magnusForce;

        float ballMass = TableTennisPhysicsConfig.BallMassGrams / 1000f;
        Vector3 accel = Physics.gravity + (totalForce / ballMass);

        return accel;
    }
}