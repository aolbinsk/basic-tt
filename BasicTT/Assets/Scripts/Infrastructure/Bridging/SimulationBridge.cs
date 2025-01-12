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
        private PaddleCalibration _paddleCalibration;

        private PaddleState _currentPaddleState;
        private BallState _currentBallState;
        private ControllerState _leftControllerState;
        private ControllerState _rightControllerState;

        private float _accumulatedTime;
        private const float TargetSimulationUpdateFrequencyWithSubStepping = 3 * 120;
        private const float SubStepInterval = 1.0f / TargetSimulationUpdateFrequencyWithSubStepping;

        private const string LOGPrefix = "[SimulationBridge] ";

        private void Awake()
        {
            InitializeDependencies();
        }

        private void FixedUpdate()
        {
            UpdateInput();

            _simulation.SetLeftControllerState(_leftControllerState);
            _simulation.SetRightControllerState(_rightControllerState);
            _simulation.SetCurrentPaddleState(_currentPaddleState);

            ProcessSimulationSteps();
            
            _simulation.GetBallState(ref _currentBallState);
        }

        private void Update()
        {
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

            _simulation = installer.GetSimulation();
            _renderer = installer.GetRenderer();
            _ballGameObject = installer.GetBallGameObject();
            _paddleGameObject = installer.GetPaddleGameObject();
            _paddleCalibration = installer.GetPaddleCalibration();
            Debug.Assert(_ballGameObject != null, LOGPrefix + "Ball GameObject not found.");
            Debug.Assert(_paddleGameObject != null, LOGPrefix + "Paddle GameObject not found.");

            _currentBallState = new BallState();
            _currentPaddleState = new PaddleState();
            _leftControllerState = new ControllerState();
            _rightControllerState = new ControllerState();
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
            _inputManager.ReadLeftControllerState(ref _leftControllerState);
            _inputManager.ReadRightControllerState(ref _rightControllerState);

            // Apply calibration
            var calibratedPaddlePositionAndRotation = CalibrationUtility.ApplyCalibration(
                _rightControllerState.Position, _rightControllerState.Rotation, _paddleCalibration);
            _currentPaddleState.Position = calibratedPaddlePositionAndRotation.Position;
            _currentPaddleState.Rotation = calibratedPaddlePositionAndRotation.Rotation;
            
            _currentPaddleState.Position = _rightControllerState.Position;
            _currentPaddleState.Rotation = _rightControllerState.Rotation;            
            _currentPaddleState.Velocity = _rightControllerState.Velocity;
            _currentPaddleState.AngularVelocity = _rightControllerState.AngularVelocity;
        }

        /// <summary>
        /// Updates the visuals of the ball and paddle based on the simulation state.
        /// </summary>
        private void UpdateVisuals()
        {
            _renderer.UpdateBallVisuals(_currentBallState);
            _renderer.UpdatePaddleVisuals(_currentPaddleState);
        }
    }
}