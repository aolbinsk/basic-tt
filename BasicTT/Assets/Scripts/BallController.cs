using UnityEngine;

/// <summary>
/// Manages the ball's state, including pickup, throwing, and updating its visual position based on physics calculations.
/// </summary>
public class BallController : MonoBehaviour
{
    private VRInputManager _vrInputManager;
    private bool _isHeld;
    private Vector3 _previousLeftControllerPosition;
    private BallState _currentBallState;
    private Transform _xrOriginTransform;
    private Rigidbody _rigidbody;

    // Constants for ball positioning relative to the controller
    private static readonly Vector3 CONTROLLER_TIP_OFFSET = new(0f, 0f, 0.15f); // 15cm forward from controller
    private static readonly Vector3 CONTROLLER_HEIGHT_OFFSET = new(0f, 0.02f, 0f); // 2cm up to avoid clipping

    private void Start()
    {
        _vrInputManager = VRInputManager.instance;
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
        gameObject.layer = LayerMask.NameToLayer("Ball");

        var ballCollider = GetComponent<SphereCollider>();
        if (ballCollider == null)
        {
            ballCollider = gameObject.AddComponent<SphereCollider>();
        }

        // Convert from mm to meters and set radius
        float radiusInMeters = TableTennisPhysicsConfig.BallDiameterMm / 2000f;
        ballCollider.radius = radiusInMeters;
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
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        // Assign physics material
        ballCollider.material = TableTennisPhysicsConfig.instance.ballMaterial;
        Debug.Log("Ball physics configured");
    }

    private void InitializeBallState()
    {
        _currentBallState = new BallState
        {
            Position = transform.position,
            Velocity = Vector3.zero,
            Rotation = transform.rotation,
            AngularVelocity = Vector3.zero
        };
    }

    private void Update()
    {
        HandleBallPickupAndThrow();
    }

    private void HandleBallPickupAndThrow()
    {
        if (_vrInputManager.leftGripPressed)
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
                _previousLeftControllerPosition = _vrInputManager.GetFilteredLeftPosition();
            }

            // Calculate ball position at controller tip
            Quaternion controllerRotation = _vrInputManager.GetFilteredLeftRotation();
            Vector3 tipOffset = controllerRotation * CONTROLLER_TIP_OFFSET;
            Vector3 heightOffset = controllerRotation * CONTROLLER_HEIGHT_OFFSET;

            // Position ball at controller tip in local space
            Vector3 localPosition = _vrInputManager.GetFilteredLeftPosition() + tipOffset + heightOffset;

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
                Vector3 currentControllerPosition = _vrInputManager.GetFilteredLeftPosition();
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
            _previousLeftControllerPosition = _vrInputManager.GetFilteredLeftPosition();
        }
    }

    public void UpdateVisuals(BallState ballState)
    {
        if (!_isHeld)
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