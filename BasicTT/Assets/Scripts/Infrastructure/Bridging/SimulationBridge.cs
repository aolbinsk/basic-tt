using System.Diagnostics;
using Domain.Config;
using UnityEngine;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Logic;
using Infrastructure.DependencyInjection;
using Infrastructure.Utilities;
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
        private PaddleCalibration _paddleCalibration;

        private PaddleState _currentPaddleState;
        private BallState _currentBallState;
        private HandState _leftHandState;
        private HandState _rightHandState;

        private float _accumulatedTime;
        private const float SubStepInterval = 1.0f / (3 * 120);

        private const string LOG_PREFIX = "[SimulationBridge] ";

        private void Awake()
        {
            InitializeDependencies();
        }

        private void FixedUpdate()
        {
            UpdateInput();

            // Set the updated hand states in the simulation
            _simulation.SetLeftHandState(_leftHandState);
            _simulation.SetRightHandState(_rightHandState);

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
            _paddleCalibration = installer.GetPaddleCalibration();
            Debug.Assert(_ballGameObject != null, LOG_PREFIX + "Ball GameObject not found.");
            Debug.Assert(_paddleGameObject != null, LOG_PREFIX + "Paddle GameObject not found.");
            
            // Get colliders by name, ForehandSide, BackhandSide
            _forehandPaddleCollider = _paddleGameObject.transform.Find("PaddleHead/ForehandSide")?.GetComponent<BoxCollider>();
            _backhandPaddleCollider = _paddleGameObject.transform.Find("PaddleHead/BackhandSide")?.GetComponent<BoxCollider>();
            Debug.Assert(_forehandPaddleCollider != null, LOG_PREFIX + "Forehand paddle collider not found.");
            Debug.Assert(_backhandPaddleCollider != null, LOG_PREFIX + "Backhand paddle collider not found.");
            
            _currentBallState = _simulation.GetCurrentBallState();
            _currentPaddleState = _simulation.GetCurrentPaddleState();
            
            _currentPaddleState.ForehandCollider = _forehandPaddleCollider;
            _currentPaddleState.BackhandCollider = _backhandPaddleCollider;

            _leftHandState = new HandState();
            _rightHandState = new HandState();
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
            // Update left hand state
            _leftHandState.Position = _inputManager.ReadFilteredLeftPosition();
            _leftHandState.Rotation = _inputManager.ReadFilteredLeftRotation();
            _leftHandState.Velocity = _inputManager.GetLeftControllerVelocity();
            _leftHandState.AngularVelocity = _inputManager.GetLeftControllerAngularVelocity();
            _leftHandState.GripPressed = _inputManager.LeftGripPressed;

            // Update right hand state
            _rightHandState.Position = _inputManager.ReadFilteredRightPosition();
            _rightHandState.Rotation = _inputManager.ReadFilteredRightRotation();
            _rightHandState.Velocity = _inputManager.GetRightControllerVelocity();
            _rightHandState.AngularVelocity = _inputManager.GetRightControllerAngularVelocity();
            _rightHandState.GripPressed = _inputManager.RightGripPressed;

            // Apply calibration
            var calibratedPaddlePositionAndRotation = CalibrationUtility.ApplyCalibration(
                _rightHandState.Position, _rightHandState.Rotation, _paddleCalibration);

            _currentPaddleState.Position = calibratedPaddlePositionAndRotation.Position;
            _currentPaddleState.Rotation = calibratedPaddlePositionAndRotation.Rotation;
            _currentPaddleState.Velocity = _rightHandState.Velocity;
            _currentPaddleState.AngularVelocity = _rightHandState.AngularVelocity;
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