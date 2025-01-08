using UnityEngine;

namespace Domain.Logic
{
    using Entities;
    using Interfaces;

    /// <summary>
    /// High-level orchestrator for the table tennis simulation. Manages the ball and paddle states, physics integration, and collision handling.
    /// </summary>
    public class TableTennisSimulation
    {
        private readonly IPhysicsEngine _physicsEngine;
        private readonly ICollisionSystem _collisionSystem;
        private readonly IRenderer _renderer;
        private readonly IPhysicsConfig _physicsConfig;

        private BallState _currentBallState;
        private PaddleState _currentPaddleState;
        private BallState _previousBallState;
        private PaddleState _previousPaddleState;

        private const string LOG_PREFIX = "[TableTennisSimulation] ";

        /// <summary>
        /// Initializes a new instance of the TableTennisSimulation class.
        /// </summary>
        /// <param name="physicsEngine">The physics engine implementation.</param>
        /// <param name="collisionSystem">The collision system implementation.</param>
        /// <param name="renderer">The renderer implementation.</param>
        /// <param name="physicsConfig">The physics configuration.</param>
        public TableTennisSimulation(
            IPhysicsEngine physicsEngine,
            ICollisionSystem collisionSystem,
            IRenderer renderer,
            IPhysicsConfig physicsConfig)
        {
            _physicsEngine = physicsEngine;
            _collisionSystem = collisionSystem;
            _renderer = renderer;
            _physicsConfig = physicsConfig;

            InitializeStates();
        }

        /// <summary>
        /// Initializes the ball and paddle states.
        /// </summary>
        private void InitializeStates()
        {
            InitializeBallState();

            _currentPaddleState = new PaddleState
            {
                Position = Vector3.zero,
                Velocity = Vector3.zero,
                Rotation = Quaternion.identity,
                AngularVelocity = Vector3.zero
            };

            _previousBallState = _currentBallState;
            _previousPaddleState = _currentPaddleState;
        }

        /// <summary>
        /// Updates the simulation for the given time step.
        /// </summary>
        /// <param name="deltaTime">The time step for the update.</param>
        public void UpdateSimulation(float deltaTime)
        {
            // Store previous states
            _previousBallState = _currentBallState;
            _previousPaddleState = _currentPaddleState;

            if (!_currentBallState.IsHeld)
            {
                // Make new copy of state when change is expected. TODO: Improve allocation handling, pool?
                _currentBallState = new BallState
                {
                    Position = _currentBallState.Position,
                    Rotation = _currentBallState.Rotation,
                    Velocity = _currentBallState.Velocity,
                    AngularVelocity = _currentBallState.AngularVelocity,
                    GameObject = _currentBallState.GameObject,
                };
                
                // Physics integration
                _physicsEngine.Integrate(ref _currentBallState, deltaTime);

                // Collision detection
                CollisionData collisionData = _collisionSystem.DetectCollision(
                    _previousBallState, _currentBallState,
                    _previousPaddleState, _currentPaddleState,
                    deltaTime);

                // Collision resolution if needed
                if (collisionData.Detected)
                {
                    _collisionSystem.ResolveCollision(ref _currentBallState, _currentPaddleState, collisionData);
                }
            }
            else
            {
                Debug.Log($"{LOG_PREFIX}Ball is held. Skipping physics integration.");
            }

            // Update visuals
            //_renderer.UpdateBallVisuals(_currentBallState);
        }

        /// <summary>
        /// Gets the current state of the ball.
        /// </summary>
        /// <returns>The current ball state.</returns>
        public BallState GetCurrentBallState()
        {
            return _currentBallState;
        }

        /// <summary>
        /// Gets the current state of the paddle.
        /// </summary>
        /// <returns>The current paddle state.</returns>
        public PaddleState GetCurrentPaddleState()
        {
            return _currentPaddleState;
        }

        /// <summary>
        /// Sets the current paddle state.
        /// </summary>
        /// <param name="paddleState">The new paddle state.</param>
        public void SetCurrentPaddleState(PaddleState paddleState)
        {
            _currentPaddleState = paddleState;
        }

        /// <summary>
        /// Sets the current ball state.
        /// </summary>
        /// <param name="ballState">The new ball state.</param>
        public void SetCurrentBallState(BallState ballState)
        {
            _currentBallState = ballState;
        }

        /// <summary>
        /// Resets the ball state to its initial values.
        /// </summary>
        public void ResetBallState()
        {
            InitializeBallState();
        }

        /// <summary>
        /// Initializes the ball state to default values.
        /// </summary>
        private void InitializeBallState()
        {
            _currentBallState = new BallState
            {
                Position = Vector3.zero,
                Velocity = Vector3.zero,
                Rotation = Quaternion.identity,
                AngularVelocity = Vector3.zero,
                IsHeld = false
            };

            _previousBallState = _currentBallState;
        }
    }
}