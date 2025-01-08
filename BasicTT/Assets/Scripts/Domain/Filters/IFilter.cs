namespace Domain.Filters
{
    /// <summary>
    /// Interface for implementing filters that process and update input data.
    /// </summary>
    /// <typeparam name="T">The type of data the filter processes.</typeparam>
    public interface IFilter<T>
    {
        /// <summary>
        /// Updates the filter with new input data and returns the processed output.
        /// </summary>
        /// <param name="input">The input data to process.</param>
        /// <returns>The processed output data.</returns>
        T Update(T input);

        /// <summary>
        /// Resets the filter to its initial state.
        /// </summary>
        void Reset();
    }
}