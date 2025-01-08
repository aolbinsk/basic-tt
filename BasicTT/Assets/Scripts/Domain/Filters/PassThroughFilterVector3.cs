using UnityEngine;

namespace Domain.Filters
{
    /// <summary>
    /// A filter that directly passes through the input without any modification.
    /// </summary>
    public class PassThroughFilterVector3 : IFilter<Vector3>
    {
        /// <summary>
        /// Returns the input value as-is.
        /// </summary>
        /// <param name="input">The input Vector3 value.</param>
        /// <returns>The same input Vector3 value.</returns>
        public Vector3 Update(Vector3 input)
        {
            return input;
        }

        /// <summary>
        /// Resets the filter state. No operation is performed as this filter has no state.
        /// </summary>
        public void Reset()
        {
            // No state to reset
        }
    }
}