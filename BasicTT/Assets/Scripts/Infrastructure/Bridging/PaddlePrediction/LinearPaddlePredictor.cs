using Domain.Entities;
using Domain.Interfaces;
using UnityEngine;

namespace Infrastructure.Bridging.PaddlePrediction
{
    /// <summary>
    /// A simple IPaddlePredictor implementation that linearly extrapolates
    /// the paddle's position and rotation from the last known state.
    /// It also uses angular velocity for a naive rotation advance.
    /// </summary>
    public class LinearPaddlePredictor : IPaddlePredictor
    {
        public PaddleState PredictPose(PaddleState basePaddleState, float deltaTime)
        {
            PaddleState predictedState = basePaddleState.Clone();

            // Linear extrapolation of position
            predictedState.Position += basePaddleState.Velocity * deltaTime;

            // Semi-linear approach to rotation with angular velocity
            Quaternion deltaRot = Quaternion.Euler(basePaddleState.AngularVelocity * (deltaTime * Mathf.Rad2Deg));
            predictedState.Rotation = deltaRot * predictedState.Rotation;

            return predictedState;
        }
    }
}