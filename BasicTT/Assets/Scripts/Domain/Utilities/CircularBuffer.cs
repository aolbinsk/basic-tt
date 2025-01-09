namespace Domain.Utilities
{
    /// <summary>
    /// Fixed-size circular buffer for storing a history of objects.
    /// </summary>
    /// <typeparam name="T">Type of objects to store.</typeparam>
    public class CircularBuffer<T> where T : new()
    {
        private readonly T[] _buffer;
        private int _index;

        /// <summary>
        /// Initializes a new instance of the CircularBuffer class.
        /// </summary>
        /// <param name="size">The fixed size of the buffer.</param>
        public CircularBuffer(int size)
        {
            _buffer = new T[size];
            for (int i = 0; i < size; i++)
            {
                _buffer[i] = new T();
            }
            _index = 0;
        }

        /// <summary>
        /// Gets the next available object from the buffer.
        /// </summary>
        /// <returns>The next object.</returns>
        public T GetNext()
        {
            T item = _buffer[_index];
            _index = (_index + 1) % _buffer.Length;
            return item;
        }
    }
}