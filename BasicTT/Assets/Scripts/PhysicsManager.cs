using UnityEngine;

/// <summary>
/// Manages the physics loop, including sub-stepping, collision detection, and resolution.
/// </summary>
public class PhysicsManager : MonoBehaviour
{
    [SerializeField] private float subStepInterval = 1.0f / 150.0f; // 150 Hz physics
    private float _accumulatedTime;

    private BallPhysics _ballPhysics;
    private CollisionDetectionSystem _collisionDetector;
    private CollisionResolutionSystem _collisionResolver;
    private BallController _ballController;
    private PaddleController _paddleController;

    private BallState _currentBallState;

    private void Start()
    {
        InitializeSystems();
        InitializeBallState();
        Debug.Log("PhysicsManager initialized");
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
        _currentBallState = _ballController.GetCurrentBallState();
    }

    private void FixedUpdate()
    {
        _accumulatedTime += Time.fixedDeltaTime;
        while (_accumulatedTime >= subStepInterval)
        {
            ProcessPhysicsSubStep(subStepInterval);
            _accumulatedTime -= subStepInterval;
        }

        // Update the visual position of the ball
        _ballController.UpdateVisuals(_currentBallState);
    }

    private void ProcessPhysicsSubStep(float dt)
    {
        if (_ballController.IsHeld())
        {
            return;
        }

        float remainingTime = dt;

        while (remainingTime > 0f)
        {
            // Get current paddle state
            var paddleState = _paddleController.GetCurrentState();

            // Detect collision with paddle
            CollisionData paddleCollision = _collisionDetector.DetectCollision(_currentBallState, paddleState, remainingTime);

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
                    earliestCollision.Collider == paddleState.Collider ? paddleState : null, 
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