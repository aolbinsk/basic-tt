using UnityEngine;

/// <summary>
/// Central configuration for all table tennis physics and dimensions
/// </summary>
public class TableTennisPhysicsConfig : MonoBehaviour
{
    // Singleton instance
    public static TableTennisPhysicsConfig Instance { get; private set; }

    [Header("Table Dimensions")]
    public const float TABLE_LENGTH_METERS = 2.74f;  // Regulation length
    public const float TABLE_WIDTH_METERS = 1.525f;  // Regulation width
    public const float TABLE_HEIGHT_METERS = 0.76f;  // Regulation height
    public const float NET_HEIGHT_METERS = 0.1525f;  // Regulation net height

    [Header("Paddle Dimensions")]
    public const float PADDLE_LENGTH_METERS = 0.2525f;  // Total length including handle
    public const float PADDLE_WIDTH_METERS = 0.1525f;   // Width at widest point
    public const float PADDLE_HANDLE_LENGTH_METERS = 0.10f;  // Handle length

    [Header("Ball Properties")]
    public const float BALL_DIAMETER_MM = 40f;  // Regulation size
    public const float BALL_MASS_GRAMS = 2.7f;  // Regulation mass
    public const float BALL_MAX_THROW_VELOCITY = 8.0f;
    public const float BALL_MIN_THROW_VELOCITY = 0.1f;

    [Header("Physics Materials")]
    [SerializeField] private PhysicsMaterial tableMaterial;
    [SerializeField] private PhysicsMaterial paddleMaterial;
    [SerializeField] private PhysicsMaterial ballMaterial;

    [Header("Bounce Properties")]
    [Range(0.1f, 1.0f)]
    public float tableBounceRestitution = 0.85f;
    [Range(0.1f, 1.0f)]
    public float paddleBounceRestitution = 0.95f;

    [Header("Player Setup")]
    public const float PLAYER_DISTANCE_FROM_TABLE = 1.0f;
    public const float PLAYER_HEIGHT_METERS = 1.8f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePhysicsMaterials();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePhysicsMaterials()
    {
        // Table material
        tableMaterial.bounciness = tableBounceRestitution;
        tableMaterial.dynamicFriction = 0.2f;
        tableMaterial.staticFriction = 0.2f;

        // Paddle material  
        paddleMaterial.bounciness = paddleBounceRestitution;
        paddleMaterial.dynamicFriction = 0.6f;
        paddleMaterial.staticFriction = 0.6f;

        // Ball material
        ballMaterial.bounciness = 0.9f;
        ballMaterial.dynamicFriction = 0.3f;
        ballMaterial.staticFriction = 0.3f;
    }
}