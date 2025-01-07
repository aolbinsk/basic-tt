using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Updates the paddle's position and rotation based on filtered VR input.
/// Allows manual offset and rotation adjustment using the right controller grip button.
/// Ensures paddle follows the player when the XR Origin moves.
/// Uses two thin colliders for each paddle side to detect side-specific hits.
/// </summary>
public class PaddleController : MonoBehaviour
{
    [SerializeField] public VRInputManager inputManager;
    [SerializeField] private GameObject rightControllerVisual;

    private Vector3 _previousPosition;
    private Quaternion _previousRotation;
    private Vector3 _velocity;
    private Vector3 _angularVelocity;
    private Rigidbody _rigidbody;
    private Transform _xrOriginTransform;
    private BoxCollider _leftCollider;
    private BoxCollider _rightCollider;

    private Vector3 _positionOffset = Vector3.zero;
    private Quaternion _rotationOffset = Quaternion.identity;
    private bool _isAdjusting = false;

    private const float SmoothingFactor = 0.1f; // Smoothing factor for position and rotation updates

    private void Awake()
    {
        InitializeRigidbody();
        InitializeBoxColliders();
        InitializeInputAndTransforms();
    }

    private void InitializeRigidbody()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            _rigidbody = gameObject.AddComponent<Rigidbody>();
        }
        _rigidbody.isKinematic = true;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void InitializeBoxColliders()
    {
        gameObject.layer = LayerMask.NameToLayer("Paddle");

        // Remove existing colliders
        var existingColliders = GetComponents<BoxCollider>();
        foreach (var collider in existingColliders)
        {
            Destroy(collider);
        }

        // Left side collider
        _leftCollider = gameObject.AddComponent<BoxCollider>();
        _leftCollider.size = new Vector3(
            TableTennisPhysicsConfig.PaddleWidthMeters,
            TableTennisPhysicsConfig.PaddleThicknessMeters / 2f,
            TableTennisPhysicsConfig.PaddleLengthMeters
        );
        _leftCollider.center = new Vector3(
            0f,
            -TableTennisPhysicsConfig.PaddleThicknessMeters / 4f,
            0f
        );

        // Right side collider
        _rightCollider = gameObject.AddComponent<BoxCollider>();
        _rightCollider.size = new Vector3(
            TableTennisPhysicsConfig.PaddleWidthMeters,
            TableTennisPhysicsConfig.PaddleThicknessMeters / 2f,
            TableTennisPhysicsConfig.PaddleLengthMeters
        );
        _rightCollider.center = new Vector3(
            0f,
            TableTennisPhysicsConfig.PaddleThicknessMeters / 4f,
            0f
        );

        Debug.Log("Initialized paddle with left and right colliders.");
    }

    private void InitializeInputAndTransforms()
    {
        _previousPosition = transform.position;
        _previousRotation = transform.rotation;

        if (inputManager == null)
        {
            Debug.LogError("VRInputManager instance not found!");
        }

        var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            _xrOriginTransform = xrOrigin.transform;
        }
        else
        {
            Debug.LogError("XROrigin not found in the scene!");
        }

        if (rightControllerVisual == null)
        {
            Debug.LogError("Right Controller Model is not assigned!");
        }
    }

    private void FixedUpdate()
    {
        UpdateTransform();
        CalculateVelocities();
    }

    private void Update()
    {
        HandleManualAdjustment();
    }

    private void UpdateTransform()
    {
        if (!_xrOriginTransform || inputManager == null) return;

        // Transform the controller position and rotation to world space relative to the XR Origin
        Vector3 localPosition = inputManager.GetFilteredRightPosition() + _positionOffset;
        Quaternion localRotation = inputManager.GetFilteredRightRotation() * _rotationOffset;

        Vector3 targetPosition = _xrOriginTransform.TransformPoint(localPosition);
        Quaternion targetRotation = _xrOriginTransform.rotation * localRotation;

        _rigidbody.MovePosition(targetPosition);
        _rigidbody.MoveRotation(targetRotation.normalized);
    }

    private void CalculateVelocities()
    {
        float deltaTime = Time.fixedDeltaTime;

        // Calculate linear velocity
        _velocity = (transform.position - _previousPosition) / deltaTime;

        // Calculate angular velocity
        Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(_previousRotation);
        deltaRotation.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);

        // Handle angle wrap-around
        if (angleInDegrees > 180f)
            angleInDegrees -= 360f;

        // Convert to angular velocity in radians per second
        _angularVelocity = rotationAxis * (angleInDegrees * Mathf.Deg2Rad) / deltaTime;

        // Update previous position and rotation
        _previousPosition = transform.position;
        _previousRotation = transform.rotation;
    }

    private void HandleManualAdjustment()
    {
        if (inputManager.rightGripPressed)
        {
            if (!_isAdjusting)
            {
                _isAdjusting = true;
                // Toggle visibility of the right controller model based on the grip button
                if (rightControllerVisual != null)
                {
                    rightControllerVisual.SetActive(inputManager.rightGripPressed);
                }
            }

            // Update offsets based on current positions
            _positionOffset = transform.position - _xrOriginTransform.TransformPoint(inputManager.GetFilteredRightPosition());
            _rotationOffset = Quaternion.Inverse(_xrOriginTransform.rotation * inputManager.GetFilteredRightRotation()) * transform.rotation;
        }
        else
        {
            if (_isAdjusting)
            {
                _isAdjusting = false;
                // Toggle visibility of the right controller model based on the grip button
                if (rightControllerVisual != null)
                {
                    rightControllerVisual.SetActive(inputManager.rightGripPressed);
                }
                // Print the offset values
                Debug.Log($"Paddle Offset Position: {_positionOffset}");
                Debug.Log($"Paddle Offset Rotation (Euler angles): {_rotationOffset.eulerAngles}");
            }
        }
    }

    public PaddleState GetCurrentState()
    {
        return new PaddleState
        {
            Position = transform.position,
            Rotation = transform.rotation,
            Velocity = _velocity,
            AngularVelocity = _angularVelocity,
            LeftCollider = _leftCollider,
            RightCollider = _rightCollider
        };
    }

    /// <summary>
    /// Sets the paddle GameObject and initializes its components.
    /// </summary>
    /// <param name="paddle">The paddle GameObject to be controlled.</param>
    public void SetPaddle(GameObject paddle)
    {
        if (paddle == null)
        {
            Debug.LogError("Paddle GameObject is null!");
            return;
        }

        transform.SetPositionAndRotation(paddle.transform.position, paddle.transform.rotation);
        InitializeRigidbody();
        InitializeBoxColliders();
        Debug.Log("Paddle assigned and initialized.");
    }
}