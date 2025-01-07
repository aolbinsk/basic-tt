using UnityEngine;

/// <summary>
/// Manages the ball's state, including pickup, throwing, and updating its visual position based on physics calculations.
/// </summary>
public class BallController : MonoBehaviour
{
    [SerializeField] private VRInputManager inputManager;
    
    private bool _isHeld;
    private Vector3 _previousLeftControllerPosition;
    private BallState _currentBallState;
    private Transform _xrOriginTransform;
    private Rigidbody _rigidbody;
    private SphereCollider _ballCollider;

    // Constants for ball positioning relative to the controller
    private static readonly Vector3 ControllerTipOffset = new(0f, 0f, 0.15f); // 15cm forward from controller
    private static readonly Vector3 ControllerHeightOffset = new(0f, 0.02f, 0f); // 2cm up to avoid clipping

    private void Awake()
    {
        ConfigureBallPhysics();
        InitializeBallState();

        // Get the XR Origin's transform to track player movement
        var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            _xrOriginTransform = xrOrigin.transform;
        }
        else
        {
            Debug.LogError("XROrigin not found in the scene!");
        }
    }

    private void ConfigureBallPhysics()
    {
        _ballCollider = GetComponent<SphereCollider>();
        if (_ballCollider == null)
        {
            _ballCollider = gameObject.AddComponent<SphereCollider>();
        }

        // Convert from mm to meters and set radius
        float radiusInMeters = TableTennisPhysicsConfig.BallDiameterMm / 2000f;
        _ballCollider.radius = radiusInMeters;
        Debug.Log($"Ball collider radius set to {radiusInMeters}m");

        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            _rigidbody = gameObject.AddComponent<Rigidbody>();
        }

        // Configure rigidbody
        _rigidbody.mass = TableTennisPhysicsConfig.BallMassGrams / 1000f; // Convert to kg
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Enable continuous collision detection
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        // Configure sleep settings
        _rigidbody.sleepThreshold = 0.005f; // Small energy threshold for sleeping
        _rigidbody.maxAngularVelocity = 50f; // Limit max rotation speed
        _rigidbody.solverIterations = 10; // Increase solver stability

        // Assign physics material
        //if (TableTennisPhysicsConfig.instance.ballMaterial != null)
        //{
        //    _ballCollider.material = TableTennisPhysicsConfig.instance.ballMaterial;
        //    Debug.Log("Ball physics configured with updated sleep parameters");
        //}

        // Scale the visual ball model to match the correct size
        transform.localScale = Vector3.one * (TableTennisPhysicsConfig.BallDiameterMm / 1000f);
        Debug.Log($"Ball visual model scaled to diameter {TableTennisPhysicsConfig.BallDiameterMm / 1000f}m");
    }

    private void InitializeBallState()
    {
        _currentBallState = new BallState
        {
            Position = transform.position,
            Velocity = Vector3.zero,
            Rotation = transform.rotation,
            AngularVelocity = Vector3.zero,
            Collider = _ballCollider
        };
        Debug.Log("Ball state initialized");
    }

    private void Update()
    {
        HandleBallPickupAndThrow();

        UpdateVisuals(_currentBallState);
    }

    private void HandleBallPickupAndThrow()
    {
        if (inputManager.leftGripPressed)
        {
            if (!_isHeld)
            {
                // Begin holding the ball
                _isHeld = true;

                // Set Rigidbody to kinematic and disable gravity
                _rigidbody.isKinematic = true;
                _rigidbody.useGravity = false;

                _currentBallState.Velocity = Vector3.zero;
                _currentBallState.AngularVelocity = Vector3.zero;
                _previousLeftControllerPosition = inputManager.GetFilteredLeftPosition();
            }

            // Calculate ball position at controller tip
            Quaternion controllerRotation = inputManager.GetFilteredLeftRotation();
            Vector3 tipOffset = controllerRotation * ControllerTipOffset;
            Vector3 heightOffset = controllerRotation * ControllerHeightOffset;

            // Position ball at controller tip in local space
            Vector3 localPosition = inputManager.GetFilteredLeftPosition() + tipOffset + heightOffset;

            // Transform to world space
            transform.position = _xrOriginTransform.TransformPoint(localPosition);
            transform.rotation = _xrOriginTransform.rotation * controllerRotation;
        }
        else
        {
            if (_isHeld)
            {
                // Release the ball
                _isHeld = false;

                // Calculate throw velocity
                Vector3 currentControllerPosition = inputManager.GetFilteredLeftPosition();
                Vector3 controllerVelocity = (currentControllerPosition - _previousLeftControllerPosition) / Time.deltaTime;

                // Use the raw controller velocity for throw
                var throwVelocity = controllerVelocity;

                // Apply upward bias for serves
                throwVelocity.y = Mathf.Max(throwVelocity.y, 0.3f);

                // Clamp to max throw velocity
                const float maxVelocity = TableTennisPhysicsConfig.BallMaxThrowVelocity;
                if (throwVelocity.magnitude > maxVelocity)
                {
                    throwVelocity = throwVelocity.normalized * maxVelocity;
                }

                // Update ball state
                _currentBallState.Position = transform.position;
                _currentBallState.Velocity = throwVelocity;
                _currentBallState.Rotation = transform.rotation;

                // Let PhysicsManager handle the ball from now on
            }
        }

        if (_isHeld)
        {
            _previousLeftControllerPosition = inputManager.GetFilteredLeftPosition();
        }
    }

    public void UpdateVisuals(BallState ballState)
    {
        if (!_isHeld && ballState != null)
        {
            transform.position = ballState.Position;
            transform.rotation = ballState.Rotation;
        }
    }

    public bool IsHeld()
    {
        return _isHeld;
    }

    public BallState GetCurrentBallState()
    {
        return _currentBallState;
    }
}