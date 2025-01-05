using System.Collections.Generic;
using UnityEngine;

namespace Filters
{
    public class MovingAverageFilterQuaternion : IFilter<Quaternion>
    {
        private readonly Queue<Quaternion> _samples;
        private readonly int _windowSize;

        public MovingAverageFilterQuaternion(int windowSize)
        {
            _windowSize = windowSize;
            _samples = new Queue<Quaternion>();
        }

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

        public void Reset()
        {
            _samples.Clear();
        }
    }
}