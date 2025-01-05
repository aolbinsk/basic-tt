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

    private void Start()
    {
        _vrInputManager = VRInputManager.instance;
        ConfigureBallPhysics();
        InitializeBallState();

        // Get the XR Origin's transform to track player movement
        var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
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
        var ballCollider = GetComponent<SphereCollider>();
        if (ballCollider == null)
        {
            ballCollider = gameObject.AddComponent<SphereCollider>();
        }
        ballCollider.radius = TableTennisPhysicsConfig.BallDiameterMm / 2000f;

        // Only add if missing 
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
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
                _currentBallState.Velocity = Vector3.zero;
                _currentBallState.AngularVelocity = Vector3.zero;
                _previousLeftControllerPosition = _vrInputManager.GetFilteredLeftPosition();
            }

            // Move ball to left controller position with offset in world space
            Vector3 holdOffset = _vrInputManager.GetFilteredLeftRotation() * Vector3.up * (TableTennisPhysicsConfig.BallDiameterMm / 1000f * 2.5f);
            Vector3 localPosition = _vrInputManager.GetFilteredLeftPosition() + holdOffset;
            transform.position = _xrOriginTransform.TransformPoint(localPosition);
            transform.rotation = _xrOriginTransform.rotation * _vrInputManager.GetFilteredLeftRotation();
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