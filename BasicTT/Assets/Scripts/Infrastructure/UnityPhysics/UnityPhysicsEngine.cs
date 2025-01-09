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
        }

        public void Integrate(ref PaddleState paddleState, float deltaTime)
        {
        }
    }
}