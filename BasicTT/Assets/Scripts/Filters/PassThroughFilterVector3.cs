using UnityEngine;

namespace Filters
{
    public class PassThroughFilterVector3 : IFilter<Vector3>
    {
        public Vector3 Update(Vector3 input)
        {
            return input;
        }

        public void Reset()
        {
            // No state to reset
        }
    }
}