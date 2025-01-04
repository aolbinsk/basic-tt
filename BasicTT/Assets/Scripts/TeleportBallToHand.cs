using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class TeleportBallToController : MonoBehaviour
{
    [SerializeField] 
    public Transform leftController;

    [SerializeField]
    private InputActionReference gripButtonAction;

    // Constants for physics simulation
    private const float BALL_MASS_GRAMS = 2.7f;  // Standard table tennis ball mass
    private const float AIR_DENSITY = 1.225f;  // kg/m^3 at sea level
    private const float BALL_RADIUS_METERS = 0.02f;  // 20mm standard size
    private const float DRAG_COEFFICIENT = 0.47f;  // Sphere drag coefficient
    private const float UNITY_TO_METERS = 1.0f;  // Scale factor for Unity units to meters
    
    // Constants for ball behavior
    private const float HOLD_OFFSET_Y = 0.1f;  // Vertical offset while held
    private const float MAX_THROW_VELOCITY = 8.0f;  // Increased from 3.0 for more realistic throws
    private const float MIN_THROW_VELOCITY = 0.1f;  // Minimum velocity for throw
    private const float VELOCITY_SMOOTHING_FACTOR = 0.15f;  // Increased from 0.05 for more responsive throws
    private const float THROW_FORCE_MULTIPLIER = 1.5f;  // Additional multiplier for throw force

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
        // Calculate drag based on physics formula
        const float crossSectionalArea = Mathf.PI * BALL_RADIUS_METERS * BALL_RADIUS_METERS;
        const float dragForce = 0.5f * AIR_DENSITY * DRAG_COEFFICIENT * crossSectionalArea;
        
        ballRigidbody.mass = BALL_MASS_GRAMS / 1000f;  // Convert to kg
        ballRigidbody.linearDamping = dragForce;
        ballRigidbody.angularDamping = 0.05f;
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
        
        // Enable physics and apply release velocity
        ballRigidbody.isKinematic = false;

        if (!(controllerVelocity.magnitude > MIN_THROW_VELOCITY)) return;
        
        var throwVelocity = CalculateReleaseVelocity();
        Debug.Log($"Calculated throw velocity: {throwVelocity.magnitude} m/s");
            
        // Reset and apply velocity
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
        
        // Apply throw force with upward bias for serves
        var throwDirection = throwVelocity.normalized;
        throwDirection.y = Mathf.Max(throwDirection.y, 0.3f); // Ensure minimum upward component
        throwDirection = throwDirection.normalized;
        
        var finalThrowVelocity = throwDirection * throwVelocity.magnitude * THROW_FORCE_MULTIPLIER;
        ballRigidbody.AddForce(finalThrowVelocity, ForceMode.Impulse);
        
        Debug.Log($"Final ball velocity: {ballRigidbody.linearVelocity.magnitude} m/s");
    }

    private Vector3 CalculateReleaseVelocity()
    {
        // Calculate average velocity from recent history
        var averageVelocity = velocityHistory.Aggregate(Vector3.zero, (current, velocity) => current + velocity);
        averageVelocity /= velocityHistory.Length;
        
        // Combine current and average velocity
        var combinedVelocity = Vector3.Lerp(controllerVelocity, averageVelocity, 0.3f);
        
        // Scale the velocity based on mass and desired throw speed
        var scaledVelocity = combinedVelocity * (BALL_MASS_GRAMS / 1000f);
        Debug.Log($"Scaled velocity: {scaledVelocity.magnitude} m/s");

        // Clamp the magnitude to the maximum allowed velocity
        var magnitude = Mathf.Min(scaledVelocity.magnitude, MAX_THROW_VELOCITY);
        var throwVelocity = scaledVelocity.normalized * magnitude;

        Debug.Log($"Final throw velocity: {throwVelocity.magnitude} m/s");
        return throwVelocity;
    }

    private void Update()
    {
        if (!isGripped) return;

        // Update position with offset
        transform.position = leftController.position + leftController.up * HOLD_OFFSET_Y;

        // Calculate raw controller velocity
        var newControllerVelocity = (leftController.position - previousControllerPosition) / Time.deltaTime;
        
        // Apply additional smoothing for extreme values
        if (newControllerVelocity.magnitude > MAX_THROW_VELOCITY * 2f)
        {
            newControllerVelocity = newControllerVelocity.normalized * (MAX_THROW_VELOCITY * 2f);
            Debug.Log($"Clamping extreme controller velocity: {newControllerVelocity.magnitude} m/s");
        }

        // Update velocity history
        velocityHistory[velocityHistoryIndex] = newControllerVelocity;
        velocityHistoryIndex = (velocityHistoryIndex + 1) % velocityHistory.Length;

        // Apply smoothing to controller velocity
        controllerVelocity = Vector3.Lerp(controllerVelocity, newControllerVelocity, VELOCITY_SMOOTHING_FACTOR);

        previousControllerPosition = leftController.position;
    }

    private void OnDestroy()
    {
        if (gripButtonAction == null) return;
        gripButtonAction.action.performed -= OnGripPressed;
        gripButtonAction.action.canceled -= OnGripReleased;
    }
}