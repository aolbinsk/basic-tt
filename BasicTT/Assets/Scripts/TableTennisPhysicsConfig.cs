using UnityEngine;

/// <summary>
/// Central configuration for all table tennis physics and dimensions
/// </summary>
public class TableTennisPhysicsConfig : MonoBehaviour
{
    // Singleton instance
    public static TableTennisPhysicsConfig instance { get; private set; }

    [Header("Table Dimensions")]
    public const float TableLengthMeters = 2.74f;  // Regulation length
    public const float TableWidthMeters = 1.525f;  // Regulation width
    public const float TableHeightMeters = 0.76f;  // Regulation height
    public const float TableThicknessMeters = 0.02f;  // Standard table thickness
    public const float NetHeightMeters = 0.1525f;  // Regulation net height

    [Header("Paddle Dimensions")]
    public const float PaddleLengthMeters = 0.2525f;  // Total length including handle
    public const float PaddleWidthMeters = 0.1525f;   // Width at widest point
    public const float PaddleThicknessMeters = 0.02f;  // Standard paddle thickness
    public const float PaddleHandleLengthMeters = 0.10f;  // Handle length

    [Header("Ball Properties")]
    public const float BallDiameterMm = 40f;  // Regulation size
    public const float BallMassGrams = 2.7f;  // Regulation mass
    public const float BallMaxThrowVelocity = 8.0f;
    public const float BallMinThrowVelocity = 0.1f;

    [Header("Physics Materials")]
    [SerializeField] public PhysicsMaterial tableMaterial;
    [SerializeField] public PhysicsMaterial paddleMaterial;
    [SerializeField] public PhysicsMaterial ballMaterial;
    [SerializeField] public PhysicsMaterial netMaterial; // Added net material

    [Header("Bounce Properties")]
    [Range(0.1f, 1.0f)]
    public float tableBounceRestitution = 0.85f;
    [Range(0.1f, 1.0f)]
    public float paddleBounceRestitution = 0.95f;
    [Range(0.1f, 1.0f)]
    public float netBounceRestitution = 0.3f; // Added net bounce restitution

    [Header("Paddle Rubber Properties")]
    [Range(0.1f, 1.0f)]
    public float paddleRubberBounciness = 0.95f;

    [Range(0.5f, 2.0f)]
    public float paddleThrowMultiplier = 1.0f;

    [Range(0.5f, 2.0f)]
    public float paddleSpinMultiplier = 1.0f;

    [Header("Room Dimensions")]
    public const float RoomSizeMeters = 20f; // Size of the room (length and width)
    public const float WallHeightMeters = 5f; // Height of the walls

    [Header("Player Setup")]
    public const float PlayerDistanceFromTable = 1.0f;
    public const float PlayerHeightMeters = 1.8f;

    [Header("Physics Settings")]
    public const float PhysicsFixedTimestep = 0.0067f; // 150 Hz physics update rate

    [Header("Aerodynamics")]
    public const float AirDensity = 1.225f; // kg/m^3 at sea level
    public const float BallDragCoefficient = 0.47f; // Drag coefficient for a sphere
    public static readonly float BallCrossSectionalArea = Mathf.PI * Mathf.Pow(BallDiameterMm / 2000f, 2); // Cross-sectional area in m^2
    public const float MagnusCoefficient = 0.0001f; // Coefficient for Magnus force calculation

    [Header("Spin and Friction")]
    [Range(0f, 1f)]
    public float tableFriction = 0.2f;
    public const float SpinTransferCoefficient = 0.5f; // Coefficient for spin transfer during collisions

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
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
        if (tableMaterial != null)
        {
            tableMaterial.bounciness = tableBounceRestitution;
            tableMaterial.dynamicFriction = 0.2f;
            tableMaterial.staticFriction = 0.2f;
        }

        // Paddle material  
        if (paddleMaterial != null)
        {
            paddleMaterial.bounciness = paddleRubberBounciness;
            paddleMaterial.dynamicFriction = 0.6f;
            paddleMaterial.staticFriction = 0.6f;
        }

        // Ball material
        if (ballMaterial != null)
        {
            ballMaterial.bounciness = 0.9f;
            ballMaterial.dynamicFriction = 0.3f;
            ballMaterial.staticFriction = 0.3f;
        }

        // Net material
        if (netMaterial != null)
        {
            netMaterial.bounciness = netBounceRestitution;
            netMaterial.dynamicFriction = 0.5f;
            netMaterial.staticFriction = 0.5f;
        }
    }
}