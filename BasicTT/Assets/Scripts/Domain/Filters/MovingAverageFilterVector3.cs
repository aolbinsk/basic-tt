using System.Collections.Generic;
using UnityEngine;

namespace Domain.Filters
{
    /// <summary>
    /// Implements a moving average filter for Vector3 values.
    /// Smooths input data by averaging over a sliding window of a fixed size.
    /// </summary>
    public class MovingAverageFilterVector3 : IFilter<Vector3>
    {
        private readonly Queue<Vector3> _samples;
        private readonly int _windowSize;

        /// <summary>
        /// Initializes a new instance of the MovingAverageFilterVector3 class.
        /// </summary>
        /// <param name="windowSize">The size of the sliding window for averaging.</param>
        public MovingAverageFilterVector3(int windowSize)
        {
            _windowSize = windowSize;
            _samples = new Queue<Vector3>();
        }

        /// <summary>
        /// Updates the filter with a new input value and returns the filtered result.
        /// </summary>
        /// <param name="input">The new input value to filter.</param>
        /// <returns>The filtered Vector3 value.</returns>
        public Vector3 Update(Vector3 input)
        {
            _samples.Enqueue(input);
            if (_samples.Count > _windowSize)
                _samples.Dequeue();

            Vector3 sum = Vector3.zero;
            foreach (var sample in _samples)
                sum += sample;

            return sum / _samples.Count;
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