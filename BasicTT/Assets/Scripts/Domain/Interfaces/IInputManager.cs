using Domain.Entities;

namespace Domain.Interfaces
{
    using UnityEngine;
    using System;

    /// <summary>
    /// Interface for input manager implementations.
    /// </summary>
    public interface IInputManager : IDisposable
    {
        public void ReadLeftControllerState(ref ControllerState controllerState);
        public void ReadRightControllerState(ref ControllerState controllerState);

        /// <summary>
        /// Indicates whether the left grip button is pressed.
        /// </summary>
        bool LeftGripPressed { get; }

        /// <summary>
        /// Indicates whether the right grip button is pressed.
        /// </summary>
        bool RightGripPressed { get; }

        /// <summary>
        /// Stores a sample of left controller input with timestamp.
        /// </summary>
        /// <param name="position">The position of the left controller.</param>
        /// <param name="rotation">The rotation of the left controller.</param>
        /// <param name="time">The timestamp of the sample.</param>
        void StoreLeftInputSample(Vector3 position, Quaternion rotation, float time);

        /// <summary>
        /// Calculates the velocity of the left controller based on stored samples.
        /// </summary>
        /// <returns>The calculated velocity as a Vector3.</returns>
        Vector3 CalculateLeftControllerVelocity();

        /// <summary>
        /// Clears all stored left controller input samples.
        /// </summary>
        void ClearLeftInputSamples();
    }
}