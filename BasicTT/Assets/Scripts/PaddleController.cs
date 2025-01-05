using UnityEngine;

/// <summary>
/// Updates the paddle's position and rotation based on filtered VR input.
/// Allows manual offset and rotation adjustment using the right controller grip button.
/// Ensures paddle follows the player when the XR Origin moves.
/// Uses an optimized box collider for efficient collision detection.
/// </summary>
public class PaddleController : MonoBehaviour
{
    [SerializeField] private GameObject rightControllerModel;

    private Vector3 _previousPosition;
    private Quaternion _previousRotation;
    private Vector3 _velocity;
    private Vector3 _angularVelocity;
    private Rigidbody _rigidbody;
    private VRInputManager _inputManager;
    private Transform _xrOriginTransform;
    private BoxCollider _boxCollider;

    private Vector3 _positionOffset = Vector3.zero;
    private Quaternion _rotationOffset = Quaternion.identity;
    private bool _isAdjusting = false;

    private const float COLLIDER_THICKNESS_MULTIPLIER = 1.1f; // Slightly larger than paddle for better contact

    private void Start()
    {
        InitializeRigidbody();
        InitializeBoxCollider();
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

    private void InitializeBoxCollider()
    {
        gameObject.layer = LayerMask.NameToLayer("Paddle");

        // Remove any existing colliders
        var existingColliders = GetComponents<Collider>();
        foreach (var collider in existingColliders)
        {
            DestroyImmediate(collider);
        }

        // Add and configure box collider
        _boxCollider = gameObject.AddComponent<BoxCollider>();
        
        // Set box collider size to match regulation paddle dimensions
        _boxCollider.size = new Vector3(
            TableTennisPhysicsConfig.PaddleWidthMeters,
            TableTennisPhysicsConfig.PaddleThicknessMeters,
            TableTennisPhysicsConfig.PaddleLengthMeters
        );

        // Adjust center to account for handle
        _boxCollider.center = new Vector3(
            0f, 
            0f,
            TableTennisPhysicsConfig.PaddleHandleLengthMeters / 2f
        );

        // Assign physics material
        _boxCollider.material = TableTennisPhysicsConfig.instance.paddleMaterial;

        Debug.Log($"Initialized paddle box collider with size: {_boxCollider.size}");
        Debug.Log($"Paddle collider center: {_boxCollider.center}");
    }

    private void InitializeInputAndTransforms()
    {
        _inputManager = VRInputManager.instance;
        _previousPosition = transform.position;
        _previousRotation = transform.rotation;

        var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            _xrOriginTransform = xrOrigin.transform;
        }
        else
        {
            Debug.LogError("XROrigin not found in the scene!");
        }

        if (rightControllerModel == null)
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
        if (!_xrOriginTransform) return;

        // Transform the controller position and rotation to world space relative to the XR Origin
        Vector3 localPosition = _inputManager.GetFilteredRightPosition() + _positionOffset;
        Quaternion localRotation = _inputManager.GetFilteredRightRotation() * _rotationOffset;

        Vector3 targetPosition = _xrOriginTransform.TransformPoint(localPosition);
        Quaternion targetRotation = _xrOriginTransform.rotation * localRotation;

        // Use Rigidbody to move position and rotation
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
        if (_inputManager.rightGripPressed)
        {
            if (!_isAdjusting)
            {
                _isAdjusting = true;
                // Toggle visibility of the right controller model based on the grip button
                if (rightControllerModel != null)
                {
                    rightControllerModel.SetActive(_inputManager.rightGripPressed);
                }
            }

            // Update offsets based on current positions
            _positionOffset = transform.position - _xrOriginTransform.TransformPoint(_inputManager.GetFilteredRightPosition());
            _rotationOffset = Quaternion.Inverse(_xrOriginTransform.rotation * _inputManager.GetFilteredRightRotation()) * transform.rotation;
        }
        else
        {
            if (_isAdjusting)
            {
                _isAdjusting = false;
                // Toggle visibility of the right controller model based on the grip button
                if (rightControllerModel != null)
                {
                    rightControllerModel.SetActive(_inputManager.rightGripPressed);
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
            Collider = _boxCollider
        };
    }
}