using System.Collections.Generic;
using UnityEngine;

namespace Domain.Filters
{
    /// <summary>
    /// Implements a moving average filter for quaternions to smooth rotations over a specified window size.
    /// </summary>
    public class MovingAverageFilterQuaternion : IFilter<Quaternion>
    {
        private readonly Queue<Quaternion> _samples;
        private readonly int _windowSize;

        /// <summary>
        /// Initializes a new instance of the MovingAverageFilterQuaternion class.
        /// </summary>
        /// <param name="windowSize">The number of samples to include in the moving average.</param>
        public MovingAverageFilterQuaternion(int windowSize)
        {
            _windowSize = windowSize;
            _samples = new Queue<Quaternion>();
        }

        /// <summary>
        /// Updates the filter with a new quaternion input and returns the smoothed result.
        /// </summary>
        /// <param name="input">The new quaternion input.</param>
        /// <returns>The smoothed quaternion.</returns>
        public Quaternion Update(Quaternion input)
        {
            _samples.Enqueue(input);
            if (_samples.Count > _windowSize)
                _samples.Dequeue();

            // Averaging quaternions
            Quaternion average = Quaternion.identity;
            float weight = 1.0f / _samples.Count;
            foreach (var rotation in _samples)
            {
                average = Quaternion.Slerp(average, rotation, weight);
            }
            return average;
        }

        /// <summary>
        /// Resets the filter, clearing all stored samples.
        /// </summary>
        public void Reset()
        {
            _samples.Clear();
        }
    }
}