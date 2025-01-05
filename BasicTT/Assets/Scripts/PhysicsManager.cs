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
        // If the ball is being held, skip physics updates
        if (_ballController.IsHeld())
        {
            return;
        }

        // Get current paddle state
        var paddleState = _paddleController.GetCurrentState();

        // Collision detection with paddle
        var paddleCollision = CollisionDetectionSystem.DetectCollision(_currentBallState, paddleState, dt);

        if (paddleCollision.Detected)
        {
            // Collision resolution with paddle
            CollisionResolutionSystem.ResolveCollision(ref _currentBallState, paddleState, paddleCollision);
        }

        // Collision detection with environment (e.g., table)
        var environmentCollision = _collisionDetector.DetectEnvironmentCollision(_currentBallState, dt);

        if (environmentCollision.Detected)
        {
            // Collision resolution with the environment
            CollisionResolutionSystem.ResolveCollision(ref _currentBallState, null, environmentCollision);
        }

        // Integrate ball physics
        BallPhysics.Integrate(ref _currentBallState, dt);
    }
}