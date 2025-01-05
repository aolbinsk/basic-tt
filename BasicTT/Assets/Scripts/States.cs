using UnityEngine;

/// <summary>
/// Represents the state of the paddle at a given time.
/// </summary>
public class PaddleState
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
    public Vector3 AngularVelocity;
    public Collider Collider; // Reference to the paddle's collider
}

/// <summary>
/// Represents the state of the ball at a given time.
/// </summary>
public class BallState
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
    public Vector3 AngularVelocity;
    public Collider Collider; // Reference to the ball's collider
}

/// <summary>
/// Represents collision data between two objects.
/// </summary>
public struct CollisionData
{
    public bool Detected;
    public Vector3 Point;
    public Vector3 Normal;
    public float TimeOfImpact;
    public Collider Collider;
}