namespace Domain.Config
{
    /// <summary>
    /// Configuration for player properties.
    /// </summary>
    public class PlayerConfig
    {
        // Constants
        private const float DEFAULT_DISTANCE_FROM_TABLE = 1.0f;
        private const float DEFAULT_SPIN_TRANSFER_COEFFICIENT = 0.5f;

        // Properties
        public float DistanceFromTable => DEFAULT_DISTANCE_FROM_TABLE;
        public float SpinTransferCoefficient => DEFAULT_SPIN_TRANSFER_COEFFICIENT;
    }
}