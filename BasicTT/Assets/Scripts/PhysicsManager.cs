using UnityEngine;

/// <summary>
/// Manages the physics loop, including sub-stepping, collision detection, and resolution.
/// </summary>
public class PhysicsManager : MonoBehaviour
{
    private const float SubStepInterval = 1.0f / 1000.0f; // 150 Hz physics update rate
    private float _accumulatedTime;

    private BallPhysics _ballPhysics;
    private CollisionDetectionSystem _collisionDetector;
    private CollisionResolutionSystem _collisionResolver;
    private BallController _ballController;
    private PaddleController _paddleController;

    private BallState _currentBallState;
    private BallState _previousBallState;
    private PaddleState _currentPaddleState;
    private PaddleState _previousPaddleState;

    private void Awake()
    {
        InitializeSystems();
        Debug.Log("PhysicsManager initialized");
    }

    private void Start()
    {
        InitializeBallState();
    }

    private void InitializeSystems()
    {
        _ballPhysics = new BallPhysics();
        _collisionDetector = new CollisionDetectionSystem();
        _collisionResolver = new CollisionResolutionSystem();
        _ballController = FindFirstObjectByType<BallController>();
        _paddleController = FindFirstObjectByType<PaddleController>();

        if (_ballController == null)
        {
            Debug.LogError("BallController not found in the scene!");
        }
        if (_paddleController == null)
        {
            Debug.LogError("PaddleController not found in the scene!");
        }
    }

    private void InitializeBallState()
    {
        _currentBallState = _ballController != null ? _ballController.GetCurrentBallState() : new BallState
        {
            Position = Vector3.zero,
            Velocity = Vector3.zero,
            Rotation = Quaternion.identity,
            AngularVelocity = Vector3.zero
        };
        _previousBallState = _currentBallState;
        Debug.Log("Ball state initialized with: " + _currentBallState);
    }

    private void FixedUpdate()
    {
        _accumulatedTime += Time.fixedDeltaTime;
        while (_accumulatedTime >= SubStepInterval)
        {
            ProcessPhysicsSubStep(SubStepInterval);
            _accumulatedTime -= SubStepInterval;
        }
    }

    private void Update()
    {
        // Update the visual position of the ball
        _ballController?.UpdateVisuals(_currentBallState);
    }

    private void ProcessPhysicsSubStep(float dt)
    {
        if (_ballController && _ballController.IsHeld())
        {
            return;
        }

        // Store previous states
        _previousBallState = _currentBallState;
        _previousPaddleState = _currentPaddleState;

        // Get current paddle state
        _currentPaddleState = _paddleController?.GetCurrentState();

        float remainingTime = dt;

        while (remainingTime > 0f)
        {
            // Detect collision with paddle
            CollisionData paddleCollision = _collisionDetector.DetectBallCollisionWithPaddle(
                _previousBallState, _currentBallState,
                _previousPaddleState, _currentPaddleState,
                remainingTime);

            // Detect collision with environment
            CollisionData environmentCollision = _collisionDetector.DetectEnvironmentCollision(_currentBallState, remainingTime);

            // Determine the earliest collision
            CollisionData earliestCollision = GetEarliestCollision(paddleCollision, environmentCollision);

            if (earliestCollision.Detected)
            {
                float timeToCollision = earliestCollision.TimeOfImpact;

                // Integrate up to collision time
                _ballPhysics.Integrate(ref _currentBallState, timeToCollision);

                // Resolve collision
                _collisionResolver.ResolveCollision(
                    ref _currentBallState,
                    earliestCollision.Collider == _currentPaddleState?.LeftCollider || earliestCollision.Collider == _currentPaddleState?.RightCollider
                        ? _currentPaddleState
                        : null,
                    earliestCollision);

                remainingTime -= timeToCollision;
            }
            else
            {
                // No collisions detected, integrate the remaining time
                _ballPhysics.Integrate(ref _currentBallState, remainingTime);
                remainingTime = 0f;
            }
        }
    }

    // Helper method to determine the earliest collision
    private CollisionData GetEarliestCollision(CollisionData first, CollisionData second)
    {
        if (!first.Detected)
            return second;
        if (!second.Detected)
            return first;
        return first.TimeOfImpact <= second.TimeOfImpact ? first : second;
    }
}