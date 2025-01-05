using System.Collections.Generic;
using UnityEngine;

namespace Filters
{
    public class MovingAverageFilterVector3 : IFilter<Vector3>
    {
        private readonly Queue<Vector3> _samples;
        private readonly int _windowSize;

        public MovingAverageFilterVector3(int windowSize)
        {
            _windowSize = windowSize;
            _samples = new Queue<Vector3>();
        }

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

        public void Reset()
        {
            _samples.Clear();
        }
    }
}