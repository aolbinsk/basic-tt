using UnityEngine;
using UnityEngine.InputSystem;
using Domain.Filters;
using Domain.Interfaces;
using System.Collections.Generic;
using Domain.Entities;

namespace Infrastructure.XRInput
{
    /// <summary>
    /// Unity XR implementation of the IInputManager interface.
    /// Handles input from XR controllers and applies filters if needed.
    /// </summary>
    public class UnityXRInputManager : IInputManager
    {
        private readonly InputAction _rightPositionAction;
        private readonly InputAction _rightRotationAction;
        private readonly InputAction _leftPositionAction;
        private readonly InputAction _leftRotationAction;
        private readonly InputAction _leftGripAction;
        private readonly InputAction _rightGripAction;

        private IFilter<Vector3> _rightPositionFilter;
        private IFilter<Quaternion> _rightRotationFilter;
        private IFilter<Vector3> _leftPositionFilter;
        private IFilter<Quaternion> _leftRotationFilter;

        private Vector3 _previousRightPosition;
        private Quaternion _previousRightRotation;
        private Vector3 _rightControllerVelocity;
        private Vector3 _rightControllerAngularVelocity;

        private Vector3 _previousLeftPosition;
        private Quaternion _previousLeftRotation;
        private Vector3 _leftControllerVelocity;
        private Vector3 _leftControllerAngularVelocity;

        private readonly List<InputSample> _leftInputSamples = new();

        public bool LeftGripPressed { get; private set; }
        public bool RightGripPressed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the UnityXRInputManager class.
        /// </summary>
        /// <param name="rightControllerPositionAction">Input action for the right controller's position.</param>
        /// <param name="rightControllerRotationAction">Input action for the right controller's rotation.</param>
        /// <param name="rightControllerGripAction">Input action for the right controller's grip.</param>
        /// <param name="leftControllerPositionAction">Input action for the left controller's position.</param>
        /// <param name="leftControllerRotationAction">Input action for the left controller's rotation.</param>
        /// <param name="leftControllerGripAction">Input action for the left controller's grip.</param>
        /// <param name="rightFilterType">Filter type for the right controller (e.g., "None", "Kalman", "MovingAverage").</param>
        /// <param name="leftFilterType">Filter type for the left controller (e.g., "None", "Kalman", "MovingAverage").</param>
        public UnityXRInputManager(
            InputAction rightControllerPositionAction,
            InputAction rightControllerRotationAction,
            InputAction rightControllerGripAction,
            InputAction leftControllerPositionAction,
            InputAction leftControllerRotationAction,
            InputAction leftControllerGripAction,
            string rightFilterType = "None",
            string leftFilterType = "None"
        )
        {
            _rightPositionAction = rightControllerPositionAction;
            _rightRotationAction = rightControllerRotationAction;
            _leftPositionAction = leftControllerPositionAction;
            _leftRotationAction = leftControllerRotationAction;
            _leftGripAction = leftControllerGripAction;
            _rightGripAction = rightControllerGripAction;

            InitializeFilters(rightFilterType, leftFilterType);
            InitializeInput();
        }

        private void InitializeFilters(string rightFilterType, string leftFilterType)
        {
            const int filterWindowSize = 5;

            // Right controller filters
            _rightPositionFilter = CreateFilter<Vector3>(rightFilterType, filterWindowSize);
            _rightRotationFilter = CreateFilter<Quaternion>(rightFilterType, filterWindowSize);

            // Left controller filters
            _leftPositionFilter = CreateFilter<Vector3>(leftFilterType, filterWindowSize);
            _leftRotationFilter = CreateFilter<Quaternion>(leftFilterType, filterWindowSize);
        }

        private IFilter<T> CreateFilter<T>(string filterType, int windowSize)
        {
            return filterType switch
            {
                "Kalman" => typeof(T) == typeof(Vector3)
                    ? (IFilter<T>)new KalmanFilterVector3()
                    : (IFilter<T>)new KalmanFilterQuaternion(),
                "MovingAverage" => typeof(T) == typeof(Vector3)
                    ? (IFilter<T>)new MovingAverageFilterVector3(windowSize)
                    : (IFilter<T>)new MovingAverageFilterQuaternion(windowSize),
                _ => typeof(T) == typeof(Vector3)
                    ? (IFilter<T>)new PassThroughFilterVector3()
                    : (IFilter<T>)new PassThroughFilterQuaternion()
            };
        }

        private void InitializeInput()
        {
            _rightPositionAction.Enable();
            _rightRotationAction.Enable();
            _rightGripAction.Enable();

            _leftPositionAction.Enable();
            _leftRotationAction.Enable();
            _leftGripAction.Enable();

            _leftGripAction.performed += OnLeftGripPressed;
            _leftGripAction.canceled += OnLeftGripReleased;

            _rightGripAction.performed += OnRightGripPressed;
            _rightGripAction.canceled += OnRightGripReleased;
        }

        public void ReadLeftControllerState(ref ControllerState controllerState)
        {
            UpdateLeftControllerData();
            controllerState.Position = _previousLeftPosition;
            controllerState.Rotation = _previousLeftRotation;
            controllerState.Velocity = _leftControllerVelocity;
            controllerState.AngularVelocity = _leftControllerAngularVelocity;
            controllerState.GripPressed = LeftGripPressed;
        }

        public void ReadRightControllerState(ref ControllerState controllerState)
        {
            UpdateRightControllerData();
            controllerState.Position = _previousRightPosition;
            controllerState.Rotation = _previousRightRotation;
            controllerState.Velocity = _rightControllerVelocity;
            controllerState.AngularVelocity = _rightControllerAngularVelocity;
            controllerState.GripPressed = RightGripPressed;
        }
        
        private void UpdateRightControllerData()
        {
            Vector3 currentPosition = _rightPositionAction.ReadValue<Vector3>();
            Quaternion currentRotation = _rightRotationAction.ReadValue<Quaternion>();

            float deltaTime = Time.deltaTime;
            _rightControllerVelocity = (currentPosition - _previousRightPosition) / deltaTime;
            _rightControllerAngularVelocity = CalculateAngularVelocity(
                _previousRightRotation, currentRotation, deltaTime);

            _previousRightPosition = currentPosition;
            _previousRightRotation = currentRotation;
        }

        private void UpdateLeftControllerData()
        {
            Vector3 currentPosition = _leftPositionAction.ReadValue<Vector3>();
            Quaternion currentRotation = _leftRotationAction.ReadValue<Quaternion>();

            float deltaTime = Time.deltaTime;
            _leftControllerVelocity = (currentPosition - _previousLeftPosition) / deltaTime;
            _leftControllerAngularVelocity = CalculateAngularVelocity(
                _previousLeftRotation, currentRotation, deltaTime);

            _previousLeftPosition = currentPosition;
            _previousLeftRotation = currentRotation;
        }

        private void OnLeftGripPressed(InputAction.CallbackContext context)
        {
            LeftGripPressed = true;
        }

        private void OnLeftGripReleased(InputAction.CallbackContext context)
        {
            LeftGripPressed = false;
            ClearLeftInputSamples();
        }

        private void OnRightGripPressed(InputAction.CallbackContext context)
        {
            RightGripPressed = true;
        }

        private void OnRightGripReleased(InputAction.CallbackContext context)
        {
            RightGripPressed = false;
        }

        private Vector3 CalculateAngularVelocity(Quaternion previousRotation, Quaternion currentRotation, float deltaTime)
        {
            Quaternion deltaRotation = currentRotation * Quaternion.Inverse(previousRotation);
            deltaRotation.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);

            if (angleInDegrees > 180f)
                angleInDegrees -= 360f;

            return rotationAxis * (angleInDegrees * Mathf.Deg2Rad) / deltaTime;
        }

        public void StoreLeftInputSample(Vector3 position, Quaternion rotation, float time)
        {
            _leftInputSamples.Add(new InputSample { Time = time, Position = position, Rotation = rotation });

            // Limit buffer size to last 10 samples
            if (_leftInputSamples.Count > 10)
            {
                _leftInputSamples.RemoveAt(0);
            }
        }

        public Vector3 CalculateLeftControllerVelocity()
        {
            if (_leftInputSamples.Count >= 2)
            {
                int n = _leftInputSamples.Count;
                InputSample s1 = _leftInputSamples[n - 2];
                InputSample s2 = _leftInputSamples[n - 1];

                float dt = s2.Time - s1.Time;
                if (dt < 0.0001f)
                {
                    dt = Time.fixedDeltaTime; // Fallback to fixed delta time
                }
                Vector3 velocity = (s2.Position - s1.Position) / dt;
                return velocity;
            }
            else
            {
                return Vector3.zero;
            }
        }

        public void ClearLeftInputSamples()
        {
            _leftInputSamples.Clear();
        }

        public void Dispose()
        {
            _leftGripAction.performed -= OnLeftGripPressed;
            _leftGripAction.canceled -= OnLeftGripReleased;

            _rightGripAction.performed -= OnRightGripPressed;
            _rightGripAction.canceled -= OnRightGripReleased;

            _leftPositionAction.Disable();
            _leftRotationAction.Disable();
            _leftGripAction.Disable();

            _rightPositionAction.Disable();
            _rightRotationAction.Disable();
            _rightGripAction.Disable();
        }

        private struct InputSample
        {
            public float Time;
            public Vector3 Position;
            public Quaternion Rotation;
        }
    }
}