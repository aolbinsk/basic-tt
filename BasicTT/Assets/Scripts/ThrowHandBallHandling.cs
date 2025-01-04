using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles the throwing mechanics for the table tennis ball, including realistic momentum accumulation and smoothing.
/// </summary>
public class ThrowHandBallHandling : MonoBehaviour
{
    [SerializeField] 
    private Transform leftController;

    [SerializeField]
    private InputActionReference gripButtonAction;

    private Rigidbody ballRigidbody;
    private bool isGripped = false;
    private Vector3 previousControllerPosition;
    private Vector3 controllerVelocity;
    private Vector3[] velocityHistory = new Vector3[5];  // Store recent velocities for averaging
    private int velocityHistoryIndex = 0;

    private void Awake()
    {
        if (leftController == null)
        {
            Debug.LogError("LeftController Transform is not assigned!");
            return;
        }

        ballRigidbody = GetComponent<Rigidbody>();
        if (ballRigidbody == null)
        {
            Debug.LogError("No Rigidbody component found on ball!");
            return;
        }

        // Configure rigidbody for realistic physics
        ConfigureRigidbody();

        gripButtonAction.action.Enable();
        gripButtonAction.action.performed += OnGripPressed;
        gripButtonAction.action.canceled += OnGripReleased;
    }

    private void ConfigureRigidbody()
    {
        // Set mass based on regulation ball mass
        ballRigidbody.mass = TableTennisPhysicsConfig.BALL_MASS_GRAMS / 1000f;  // Convert to kg
        ballRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        ballRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void OnGripPressed(InputAction.CallbackContext context)
    {
        isGripped = true;
        ballRigidbody.isKinematic = true;
        previousControllerPosition = leftController.position;
        controllerVelocity = Vector3.zero;
        ClearVelocityHistory();
    }

    private void ClearVelocityHistory()
    {
        for (var i = 0; i < velocityHistory.Length; i++)
        {
            velocityHistory[i] = Vector3.zero;
        }
        velocityHistoryIndex = 0;
    }

    private void OnGripReleased(InputAction.CallbackContext context)
    {
        if (!isGripped) return;
        
        isGripped = false;
        ballRigidbody.isKinematic = false;

        if (!(controllerVelocity.magnitude > TableTennisPhysicsConfig.BALL_MIN_THROW_VELOCITY)) return;
        var throwVelocity = CalculateReleaseVelocity();
        ApplyThrowForce(throwVelocity);
    }

    private Vector3 CalculateReleaseVelocity()
    {
        // Calculate average velocity from recent history
        var averageVelocity = velocityHistory.Aggregate(Vector3.zero, (current, velocity) => current + velocity);
        averageVelocity /= velocityHistory.Length;
        
        // Combine current and average velocity
        var combinedVelocity = Vector3.Lerp(controllerVelocity, averageVelocity, 0.3f);
        
        // Scale the velocity based on mass and desired throw speed
        var scaledVelocity = combinedVelocity * (TableTennisPhysicsConfig.BALL_MASS_GRAMS / 1000f);

        // Clamp the magnitude to the maximum allowed velocity
        var magnitude = Mathf.Min(scaledVelocity.magnitude, TableTennisPhysicsConfig.BALL_MAX_THROW_VELOCITY);
        return scaledVelocity.normalized * magnitude;
    }

    private void ApplyThrowForce(Vector3 throwVelocity)
    {
        // Add upward bias for serves
        var throwDirection = throwVelocity.normalized;
        throwDirection.y = Mathf.Max(throwDirection.y, 0.3f); // Ensure minimum upward component
        throwDirection = throwDirection.normalized;
        
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
        ballRigidbody.AddForce(throwDirection * throwVelocity.magnitude, ForceMode.Impulse);
    }

    private void Update()
    {
        if (!isGripped) return;

        UpdateBallPosition();
        UpdateVelocityTracking();
    }

    private void UpdateBallPosition()
    {
        var holdOffset = leftController.up * (TableTennisPhysicsConfig.BALL_DIAMETER_MM / 1000f * 2.5f);
        transform.position = leftController.position + holdOffset;
    }

    private void UpdateVelocityTracking()
    {
        // Calculate raw controller velocity
        var newControllerVelocity = (leftController.position - previousControllerPosition) / Time.deltaTime;
        
        // Apply smoothing for extreme values
        if (newControllerVelocity.magnitude > TableTennisPhysicsConfig.BALL_MAX_THROW_VELOCITY * 2f)
        {
            newControllerVelocity = newControllerVelocity.normalized * (TableTennisPhysicsConfig.BALL_MAX_THROW_VELOCITY * 2f);
        }

        // Update velocity history
        velocityHistory[velocityHistoryIndex] = newControllerVelocity;
        velocityHistoryIndex = (velocityHistoryIndex + 1) % velocityHistory.Length;

        // Apply smoothing to controller velocity
        controllerVelocity = Vector3.Lerp(controllerVelocity, newControllerVelocity, 0.15f);
        previousControllerPosition = leftController.position;
    }

    private void OnDestroy()
    {
        if (gripButtonAction == null) return;
        gripButtonAction.action.performed -= OnGripPressed;
        gripButtonAction.action.canceled -= OnGripReleased;
    }
}