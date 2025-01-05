using UnityEngine;

/// <summary>
/// Updates the paddle's position and rotation based on filtered VR input.
/// Allows manual offset and rotation adjustment using the right controller grip button.
/// Ensures paddle follows the player when the XR Origin moves.
/// </summary>
public class PaddleController : MonoBehaviour
{
    [SerializeField] private GameObject rightControllerModel;

    private Vector3 _previousPosition;
    private Vector3 _velocity;
    private Vector3 _angularVelocity;
    private Rigidbody _rigidbody;
    private VRInputManager _inputManager;
    private Transform _xrOriginTransform;

    private Vector3 _positionOffset = Vector3.zero;
    private Quaternion _rotationOffset = Quaternion.identity;
    private bool _isAdjusting = false;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            Debug.LogError("No Rigidbody found on paddle!");
            return;
        }

        _rigidbody.isKinematic = true;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        _inputManager = VRInputManager.instance;
        _previousPosition = transform.position;

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

        _velocity = (transform.position - _previousPosition) / deltaTime;

        // Calculate angular velocity using quaternions
        Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(_rigidbody.rotation);
        deltaRotation.ToAngleAxis(out var angleInDegrees, out var rotationAxis);

        // Convert to radians per second
        _angularVelocity = rotationAxis * (angleInDegrees * Mathf.Deg2Rad) / deltaTime;

        _previousPosition = transform.position;
    }

    private void HandleManualAdjustment()
    {
        if (_inputManager.RightGripPressed)
        {
            if (!_isAdjusting)
            {
                _isAdjusting = true;
                // Toggle visibility of the right controller model based on the grip button
                if (rightControllerModel != null)
                {
                    rightControllerModel.SetActive(_inputManager.RightGripPressed);
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
                    rightControllerModel.SetActive(_inputManager.RightGripPressed);
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
            AngularVelocity = _angularVelocity
        };
    }
}