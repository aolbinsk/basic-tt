using System.Diagnostics;
using UnityEngine;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Logic;
using Infrastructure.DependencyInjection;
using Infrastructure.XRInput;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

namespace Infrastructure.Bridging
{
    /// <summary>
    /// Bridges the Unity environment and the domain simulation.
    /// Manages the physics loop, input handling, and updates the renderer.
    /// </summary>
    public class SimulationBridge : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool useCustomPhysics = true;

        [Header("Dependencies")]
        [SerializeField] private XROrigin xrOrigin;
        
        [Header("Right Controller Input Settings")]
        [SerializeField] private InputActionReference rightControllerPositionAction;
        [SerializeField] private InputActionReference rightControllerRotationAction;
        [SerializeField] private InputActionReference rightControllerGripAction;
        [SerializeField] private string rightFilterType = "None"; // Options: "MovingAverage", "Kalman", "None"

        [Header("Left Controller Input Settings")]
        [SerializeField] private InputActionReference leftControllerPositionAction;
        [SerializeField] private InputActionReference leftControllerRotationAction;
        [SerializeField] private InputActionReference leftControllerGripAction;
        [SerializeField] private string leftFilterType = "None"; // Options: "MovingAverage", "Kalman", "None"

        private TableTennisSimulation _simulation;
        private IInputManager _inputManager;
        private IRenderer _renderer;
        private XROrigin _xrOrigin;
        private GameObject _ballGameObject;
        private GameObject _paddleGameObject;
        private BoxCollider _forehandPaddleCollider;
        private BoxCollider _backhandPaddleCollider;
        private IPhysicsConfig _physicsConfig;

        private PaddleState _currentPaddleState;
        private BallState _currentBallState;

        private float _accumulatedTime;
        private Transform _leftController;
        private Transform _rightController;
        private const float SubStepInterval = 1.0f / (3*120);

        private const string LOG_PREFIX = "[SimulationBridge] ";

        private void Awake()
        {
            InitializeDependencies();
        }

        private void FixedUpdate()
        {
            UpdateInput();

            // Set the updated paddle and ball states in the simulation
            _simulation.SetCurrentPaddleState(_currentPaddleState);
            _simulation.SetCurrentBallState(_currentBallState);

            ProcessSimulationSteps();
            
            _currentBallState = _simulation.GetCurrentBallState();
            _currentPaddleState = _simulation.GetCurrentPaddleState();

            UpdateVisuals();
        }

        /// <summary>
        /// Initializes all dependencies required for the simulation.
        /// </summary>
        private void InitializeDependencies()
        {
            var coreInputManager = new UnityXRInputManager(
                rightControllerPositionAction,
                rightControllerRotationAction,
                rightControllerGripAction,
                leftControllerPositionAction,
                leftControllerRotationAction,
                leftControllerGripAction,
                rightFilterType,
                leftFilterType
            );

            _xrOrigin = xrOrigin;

            _inputManager = new WorldSpaceAdapterInputManager(
                coreInputManager, 
                _xrOrigin.CameraFloorOffsetObject.transform);

            // Get dependencies from installer
            var installer = new SimulationBuilder();
            installer.Build(useCustomPhysics, xrOrigin.transform);

            _physicsConfig = installer.GetPhysicsConfig();
            _simulation = installer.GetSimulation();
            _renderer = installer.GetRenderer();
            _ballGameObject = installer.GetBallGameObject();
            _paddleGameObject = installer.GetPaddleGameObject();
            Debug.Assert(_ballGameObject != null, LOG_PREFIX + "Ball GameObject not found.");
            Debug.Assert(_paddleGameObject != null, LOG_PREFIX + "Paddle GameObject not found.");
            
            // Get colliders by name, ForehandSide, BackhandSide
            _forehandPaddleCollider = _paddleGameObject.transform.Find("PaddleHead/ForehandSide")?.GetComponent<BoxCollider>();
            _backhandPaddleCollider = _paddleGameObject.transform.Find("PaddleHead/BackhandSide")?.GetComponent<BoxCollider>();
            Debug.Assert(_forehandPaddleCollider != null, LOG_PREFIX + "Forehand paddle collider not found.");
            Debug.Assert(_backhandPaddleCollider != null, LOG_PREFIX + "Backhand paddle collider not found.");
            
            _currentBallState = _simulation.GetCurrentBallState();
            _currentPaddleState = _simulation.GetCurrentPaddleState();
            
            _currentBallState.Collider = _ballGameObject.GetComponent<SphereCollider>();
            _currentPaddleState.ForehandCollider = _forehandPaddleCollider;
            _currentPaddleState.BackhandCollider = _backhandPaddleCollider;
        }

        /// <summary>
        /// Processes simulation steps based on accumulated time.
        /// </summary>
        private void ProcessSimulationSteps()
        {
            _accumulatedTime += Time.fixedDeltaTime;
            while (_accumulatedTime >= SubStepInterval)
            {
                _simulation.UpdateSimulation(SubStepInterval);
                _accumulatedTime -= SubStepInterval;
            }
        }

        /// <summary>
        /// Updates input for both paddle and ball.
        /// </summary>
        private void UpdateInput()
        {
            // Update paddle state
            _currentPaddleState.Position = _inputManager.ReadFilteredRightPosition();
            _currentPaddleState.Rotation = _inputManager.ReadFilteredRightRotation();
            _currentPaddleState.Velocity = _inputManager.GetRightControllerVelocity();
            _currentPaddleState.AngularVelocity = _inputManager.GetRightControllerAngularVelocity();

            // Update ball holding logic
            if (leftControllerGripAction.action.IsPressed())
            {
                if (!_currentBallState.IsHeld)
                {
                    _currentBallState.IsHeld = true;
                }

                // Update ball position and rotation to follow the left controller
                // TODO: Add a new state, leftHandState, to let the simulation do this update.
                _currentBallState.Position = _inputManager.ReadFilteredLeftPosition();
                _currentBallState.Rotation = _inputManager.ReadFilteredLeftRotation();
            }
            else
            {
                if (_currentBallState.IsHeld)
                {
                    _currentBallState.IsHeld = false;

                    // Calculate release velocity
                    var releaseVelocity = _inputManager.GetRightControllerVelocity();
                    var angularVelocity = _inputManager.GetRightControllerAngularVelocity();

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
        }

        /// <summary>
        /// Updates the visuals of the ball and paddle based on the simulation state.
        /// </summary>
        private void UpdateVisuals()
        {
            _renderer.UpdateBallVisuals(_simulation.GetCurrentBallState());
            _renderer.UpdatePaddleVisuals(_simulation.GetCurrentPaddleState());
        }
    }
}