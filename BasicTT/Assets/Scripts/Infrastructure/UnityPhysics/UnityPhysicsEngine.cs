using Domain.Entities;
using Domain.Interfaces;
using UnityEngine;

namespace Infrastructure.UnityPhysics
{
    /// <summary>
    /// Unity-based implementation of the IPhysicsEngine interface.
    /// Uses Unity's physics system to integrate ball and paddle states.
    /// </summary>
    public class UnityPhysicsEngine : IPhysicsEngine
    {
        public void Integrate(ref BallState ballState, float deltaTime)
        {
            // Assuming the ball GameObject has a Rigidbody component
            if (ballState.GameObject != null)
            {
                Rigidbody rb = ballState.GameObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Unity's physics engine handles integration, so we update the state from the Rigidbody
                    ballState.Position = rb.position;
                    ballState.Rotation = rb.rotation;
                    ballState.Velocity = rb.linearVelocity;
                    ballState.AngularVelocity = rb.angularVelocity;
                }
            }
        }

        public void Integrate(ref PaddleState paddleState, float deltaTime)
        {
            // Assuming the paddle is controlled directly by the player input
            // Update the paddle state based on its transform
            if (paddleState.GameObject != null)
            {
                Transform transform = paddleState.GameObject.transform;
                paddleState.Position = transform.position;
                paddleState.Rotation = transform.rotation;
                // Velocity and AngularVelocity can be calculated if needed
                // For simplicity, we can set them to zero or calculate based on previous frames
                paddleState.Velocity = Vector3.zero;
                paddleState.AngularVelocity = Vector3.zero;
            }
        }
    }
}