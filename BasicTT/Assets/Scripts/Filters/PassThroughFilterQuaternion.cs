using UnityEngine;

namespace Filters
{
    public class PassThroughFilterQuaternion : IFilter<Quaternion>
    {
        public Quaternion Update(Quaternion input)
        {
            return input;
        }

        public void Reset()
        {
            // No state to reset
        }
    }
}