using UnityEngine;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Physics;
using Domain.Utilities;

namespace Domain.Logic
{
    /// <summary>
    /// High-level orchestrator for the table tennis simulation. Manages the ball, paddle, and hand states, physics integration, and collision handling.
    /// </summary>
    public class TableTennisSimulation
    {
        private readonly IPhysicsEngine _physicsEngine;
        private readonly ICollisionSystem _collisionSystem;
        private readonly IPhysicsConfig _physicsConfig;

        private readonly CircularBuffer<BallState> _ballStateBuffer = new(2);
        private readonly CircularBuffer<PaddleState> _paddleStateBuffer = new(2);
        private ControllerState _leftControllerState;
        private ControllerState _rightControllerState;

        private const string LOGPrefix = "[TableTennisSimulation] ";

        /// <summary>
        /// Initializes a new instance of the TableTennisSimulation class.
        /// </summary>
        /// <param name="physicsEngine">The physics engine implementation.</param>
        /// <param name="collisionSystem">The collision system implementation.</param>
        /// <param name="physicsConfig">The physics configuration.</param>
        public TableTennisSimulation(
            IPhysicsEngine physicsEngine,
            ICollisionSystem collisionSystem,
            IPhysicsConfig physicsConfig)
        {
            _physicsEngine = physicsEngine;
            _collisionSystem = collisionSystem;
            _physicsConfig = physicsConfig;

            InitializeStates();
        }

        /// <summary>
        /// Initializes the ball, paddle, and hand states.
        /// </summary>
        private void InitializeStates()
        {
            for (var i = 0; i < _ballStateBuffer.Capacity; i++)
            {
                var initialState = _ballStateBuffer.IncrementAndGetNext();
                initialState.Position = Vector3.zero;
                initialState.Velocity = Vector3.zero;
                initialState.Rotation = Quaternion.identity;
                initialState.AngularVelocity = Vector3.zero;
                initialState.IsHeld = false;
            }

            for (var i = 0; i < _paddleStateBuffer.Capacity; i++)
            {
                var paddleState = _paddleStateBuffer.IncrementAndGetNext();
                paddleState.Position = Vector3.zero;
                paddleState.Velocity = Vector3.zero;
                paddleState.Rotation = Quaternion.identity;
                paddleState.AngularVelocity = Vector3.zero;
            }

            _leftControllerState = new ControllerState
            {
                Position = Vector3.zero,
                Rotation = Quaternion.identity,
                Velocity = Vector3.zero,
                AngularVelocity = Vector3.zero,
                GripPressed = false
            };

            _rightControllerState = new ControllerState
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
            var previousBallState = _ballStateBuffer.PeekCurrent();
            var currentBallState = _ballStateBuffer.IncrementAndGetNext();
            CopyBallState(previousBallState, currentBallState);
            
            var previousPaddleState = _paddleStateBuffer.PeekPrevious();
            var currentPaddleState = _paddleStateBuffer.PeekCurrent();

            if (_leftControllerState.GripPressed)
            {
                BallThrowLogic.HoldBall(ref currentBallState, _leftControllerState);
            }
            else
            {
                if (currentBallState.IsHeld)
                {
                    // Ball was held and now is being released
                    BallThrowLogic.ReleaseBall(ref currentBallState, _leftControllerState, _physicsConfig);
                }
            }

            if (!currentBallState.IsHeld)
            {
                _physicsEngine.Integrate(ref currentBallState, deltaTime);

                CollisionData collisionData = _collisionSystem.DetectCollision(
                    previousBallState, currentBallState,
                    previousPaddleState, currentPaddleState,
                    deltaTime);

                if (collisionData.Detected)
                {
                    _collisionSystem.ResolveCollision(ref currentBallState, currentPaddleState, collisionData);
                }
            }
        }

        /// <summary>
        /// Gets the current state of the ball.
        /// </summary>
        /// <returns>The current ball state.</returns>
        public BallState GetBallState(ref BallState targetBallState)
        {
            CopyBallState(_ballStateBuffer.PeekCurrent(), targetBallState);
            return targetBallState;
        }

        /// <summary>
        /// Gets the current state of the paddle.
        /// </summary>
        /// <returns>The current paddle state.</returns>
        public void GetPaddleState(ref PaddleState targetPaddleState)
        {
            CopyPaddleState(_paddleStateBuffer.PeekCurrent(), targetPaddleState);
        }

        /// <summary>
        /// Sets the current paddle state.
        /// </summary>
        /// <param name="paddleState">The new paddle state.</param>
        public void SetCurrentPaddleState(PaddleState paddleState)
        {
            var currentPaddleState = _paddleStateBuffer.IncrementAndGetNext();
            CopyPaddleState(paddleState, currentPaddleState);
        }

        /// <summary>
        /// Sets the current ball state.
        /// </summary>
        /// <param name="ballState">The new ball state.</param>
        public void SetCurrentBallState(BallState ballState)
        {
            var currentBallState = _ballStateBuffer.IncrementAndGetNext();
            CopyBallState(ballState, currentBallState);
        }

        /// <summary>
        /// Gets the current state of the left hand.
        /// </summary>
        /// <returns>The current left hand state.</returns>
        public ControllerState GetLeftHandState()
        {
            return _leftControllerState;
        }

        /// <summary>
        /// Gets the current state of the right hand.
        /// </summary>
        /// <returns>The current right hand state.</returns>
        public ControllerState GetRightHandState()
        {
            return _rightControllerState;
        }

        /// <summary>
        /// Sets the current state of the left hand.
        /// </summary>
        /// <param name="controllerState">The new left hand state.</param>
        public void SetLeftControllerState(ControllerState controllerState)
        {
            _leftControllerState = controllerState;
        }

        /// <summary>
        /// Sets the current state of the right hand.
        /// </summary>
        /// <param name="controllerState">The new right hand state.</param>
        public void SetRightControllerState(ControllerState controllerState)
        {
            _rightControllerState = controllerState;
        }

        /// <summary>
        /// Copies the state data from one BallState to another.
        /// </summary>
        /// <param name="source">The source BallState.</param>
        /// <param name="target">The target BallState.</param>
        private void CopyBallState(BallState source, BallState target)
        {
            target.Position = source.Position;
            target.Velocity = source.Velocity;
            target.Rotation = source.Rotation;
            target.AngularVelocity = source.AngularVelocity;
            target.IsHeld = source.IsHeld;
        }

        /// <summary>
        /// Copies the state data from one PaddleState to another.
        /// </summary>
        /// <param name="source">The source PaddleState.</param>
        /// <param name="target">The target PaddleState.</param>
        private void CopyPaddleState(PaddleState source, PaddleState target)
        {
            target.Position = source.Position;
            target.Velocity = source.Velocity;
            target.Rotation = source.Rotation;
            target.AngularVelocity = source.AngularVelocity;
        }
    }
}