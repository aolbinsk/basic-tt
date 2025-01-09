using UnityEngine;

namespace Domain.Logic
{
    using Entities;
    using Interfaces;

    /// <summary>
    /// High-level orchestrator for the table tennis simulation. Manages the ball, paddle, and hand states, physics integration, and collision handling.
    /// </summary>
    public class TableTennisSimulation
    {
        private readonly IPhysicsEngine _physicsEngine;
        private readonly ICollisionSystem _collisionSystem;
        private readonly IRenderer _renderer;
        private readonly IPhysicsConfig _physicsConfig;

        private BallState _currentBallState;
        private PaddleState _currentPaddleState;
        private HandState _leftHandState;
        private HandState _rightHandState;
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
        /// Initializes the ball, paddle, and hand states.
        /// </summary>
        private void InitializeStates()
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

            _currentPaddleState = new PaddleState
            {
                Position = Vector3.zero,
                Velocity = Vector3.zero,
                Rotation = Quaternion.identity,
                AngularVelocity = Vector3.zero
            };
            _previousPaddleState = _currentPaddleState;

            _leftHandState = new HandState
            {
                Position = Vector3.zero,
                Rotation = Quaternion.identity,
                Velocity = Vector3.zero,
                AngularVelocity = Vector3.zero,
                GripPressed = false
            };

            _rightHandState = new HandState
            {
                Position = Vector3.zero,
                Rotation = Quaternion.identity,
                Velocity = Vector3.zero,
                AngularVelocity = Vector3.zero,
                GripPressed = false
            };
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

            // Ball holding logic
            if (_leftHandState.GripPressed)
            {
                if (!_currentBallState.IsHeld)
                {
                    _currentBallState.IsHeld = true;
                }

                // Update ball position and rotation to follow the left hand
                _currentBallState.Position = _leftHandState.Position;
                _currentBallState.Rotation = _leftHandState.Rotation;
            }
            else
            {
                if (_currentBallState.IsHeld)
                {
                    _currentBallState.IsHeld = false;

                    // Calculate release velocity
                    Vector3 releaseVelocity = _leftHandState.Velocity;
                    Vector3 angularVelocity = _leftHandState.AngularVelocity;

                    // Ensure a minimum upward velocity
                    if (releaseVelocity.y < _physicsConfig.Ball.MinThrowVelocity)
                    {
                        releaseVelocity.y = _physicsConfig.Ball.MinThrowVelocity;
                    }

                    // Clamp to maximum throw velocity
                    float maxVelocity = _physicsConfig.Ball.MaxThrowVelocity;
                    if (releaseVelocity.magnitude > maxVelocity)
                    {
                        releaseVelocity = releaseVelocity.normalized * maxVelocity;
                    }

                    _currentBallState.Velocity = releaseVelocity;
                    _currentBallState.AngularVelocity = angularVelocity;
                }
            }

            if (!_currentBallState.IsHeld)
            {
                // Make new copy of state when change is expected. TODO: Improve allocation handling, pool?
                _currentBallState = new BallState
                {
                    Position = _currentBallState.Position,
                    Rotation = _currentBallState.Rotation,
                    Velocity = _currentBallState.Velocity,
                    AngularVelocity = _currentBallState.AngularVelocity,
                    IsHeld = _currentBallState.IsHeld,
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
            _renderer.UpdateBallVisuals(_currentBallState);
            _renderer.UpdatePaddleVisuals(_currentPaddleState);
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
        /// Gets the current state of the left hand.
        /// </summary>
        /// <returns>The current left hand state.</returns>
        public HandState GetLeftHandState()
        {
            return _leftHandState;
        }

        /// <summary>
        /// Gets the current state of the right hand.
        /// </summary>
        /// <returns>The current right hand state.</returns>
        public HandState GetRightHandState()
        {
            return _rightHandState;
        }

        /// <summary>
        /// Sets the current state of the left hand.
        /// </summary>
        /// <param name="handState">The new left hand state.</param>
        public void SetLeftHandState(HandState handState)
        {
            _leftHandState = handState;
        }

        /// <summary>
        /// Sets the current state of the right hand.
        /// </summary>
        /// <param name="handState">The new right hand state.</param>
        public void SetRightHandState(HandState handState)
        {
            _rightHandState = handState;
        }
    }
}