namespace Domain.Config
{
    /// <summary>
    /// Configuration for room dimensions.
    /// </summary>
    public class RoomConfig
    {
        // Constants
        private const float DEFAULT_SIZE_METERS = 20f;
        private const float DEFAULT_WALL_HEIGHT_METERS = 5f;

        // Properties
        public float SizeMeters => DEFAULT_SIZE_METERS;
        public float WallHeightMeters => DEFAULT_WALL_HEIGHT_METERS;
        public float WidthMeters => DEFAULT_SIZE_METERS;
        public float LengthMeters => DEFAULT_SIZE_METERS;
    }
}