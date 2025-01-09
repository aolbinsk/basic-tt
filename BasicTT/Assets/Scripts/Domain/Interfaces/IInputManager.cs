namespace Domain.Interfaces
{
    using UnityEngine;
    using System;

    /// <summary>
    /// Interface for input manager implementations.
    /// </summary>
    public interface IInputManager : IDisposable
    {
        /// <summary>
        /// Gets the filtered position of the right controller.
        /// </summary>
        /// <returns>The filtered position as a Vector3.</returns>
        Vector3 ReadFilteredRightPosition();

        /// <summary>
        /// Gets the filtered rotation of the right controller.
        /// </summary>
        /// <returns>The filtered rotation as a Quaternion.</returns>
        Quaternion ReadFilteredRightRotation();

        /// <summary>
        /// Gets the filtered position of the left controller.
        /// </summary>
        /// <returns>The filtered position as a Vector3.</returns>
        Vector3 ReadFilteredLeftPosition();

        /// <summary>
        /// Gets the filtered rotation of the left controller.
        /// </summary>
        /// <returns>The filtered rotation as a Quaternion.</returns>
        Quaternion ReadFilteredLeftRotation();

        /// <summary>
        /// Indicates whether the left grip button is pressed.
        /// </summary>
        bool LeftGripPressed { get; }

        /// <summary>
        /// Indicates whether the right grip button is pressed.
        /// </summary>
        bool RightGripPressed { get; }

        /// <summary>
        /// Gets the velocity of the right controller.
        /// </summary>
        /// <returns>The velocity as a Vector3.</returns>
        Vector3 GetRightControllerVelocity();

        /// <summary>
        /// Gets the angular velocity of the right controller.
        /// </summary>
        /// <returns>The angular velocity as a Vector3.</returns>
        Vector3 GetRightControllerAngularVelocity();

        /// <summary>
        /// Gets the velocity of the left controller.
        /// </summary>
        /// <returns>The velocity as a Vector3.</returns>
        Vector3 GetLeftControllerVelocity();

        /// <summary>
        /// Gets the angular velocity of the left controller.
        /// </summary>
        /// <returns>The angular velocity as a Vector3.</returns>
        Vector3 GetLeftControllerAngularVelocity();

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