using UnityEngine;
using UnityEngine.InputSystem;
using Filters;

/// <summary>
/// Manages VR input and provides filtered controller data.
/// </summary>
public class VRInputManager : MonoBehaviour
{
    public static VRInputManager instance { get; private set; }

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

    private IFilter<Vector3> _rightPositionFilter;
    private IFilter<Quaternion> _rightRotationFilter;
    private IFilter<Vector3> _leftPositionFilter;
    private IFilter<Quaternion> _leftRotationFilter;

    public bool leftGripPressed { get; private set; }
    public bool rightGripPressed { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            InitializeInput();
            InitializeFilters();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeInput()
    {
        rightControllerPositionAction.action.Enable();
        rightControllerRotationAction.action.Enable();
        rightControllerGripAction.action.Enable();

        leftControllerPositionAction.action.Enable();
        leftControllerRotationAction.action.Enable();
        leftControllerGripAction.action.Enable();

        leftControllerGripAction.action.performed += OnLeftGripPressed;
        leftControllerGripAction.action.canceled += OnLeftGripReleased;

        rightControllerGripAction.action.performed += OnRightGripPressed;
        rightControllerGripAction.action.canceled += OnRightGripReleased;

        Debug.Log("Input initialized");
    }

    private void InitializeFilters()
    {
        int filterWindowSize = 5; // Default window size for MovingAverage filters

        // Initialize right controller filters
        if (rightFilterType == "Kalman")
        {
            _rightPositionFilter = new KalmanFilterVector3();
            _rightRotationFilter = new KalmanFilterQuaternion();
        }
        else if (rightFilterType == "None")
        {
            _rightPositionFilter = new PassThroughFilterVector3();
            _rightRotationFilter = new PassThroughFilterQuaternion();
        }
        else // Default to Moving Average
        {
            _rightPositionFilter = new MovingAverageFilterVector3(filterWindowSize);
            _rightRotationFilter = new MovingAverageFilterQuaternion(filterWindowSize);
        }

        // Initialize left controller filters
        if (leftFilterType == "Kalman")
        {
            _leftPositionFilter = new KalmanFilterVector3();
            _leftRotationFilter = new KalmanFilterQuaternion();
        }
        else if (leftFilterType == "None")
        {
            _leftPositionFilter = new PassThroughFilterVector3();
            _leftRotationFilter = new PassThroughFilterQuaternion();
        }
        else // Default to Moving Average
        {
            _leftPositionFilter = new MovingAverageFilterVector3(filterWindowSize);
            _leftRotationFilter = new MovingAverageFilterQuaternion(filterWindowSize);
        }

        Debug.Log("Filters initialized");
    }

    private void OnDestroy()
    {
        if (leftControllerGripAction != null)
        {
            leftControllerGripAction.action.performed -= OnLeftGripPressed;
            leftControllerGripAction.action.canceled -= OnLeftGripReleased;
        }

        if (rightControllerGripAction != null)
        {
            rightControllerGripAction.action.performed -= OnRightGripPressed;
            rightControllerGripAction.action.canceled -= OnRightGripReleased;
        }
    }

    private void Update()
    {
        UpdateRightControllerData();
        UpdateLeftControllerData();
    }

    private void UpdateRightControllerData()
    {
        Vector3 currentPosition = rightControllerPositionAction.action.ReadValue<Vector3>();
        Quaternion currentRotation = rightControllerRotationAction.action.ReadValue<Quaternion>();

        _rightPositionFilter.Update(currentPosition);
        _rightRotationFilter.Update(currentRotation);
    }

    public Vector3 GetFilteredRightPosition()
    {
        return _rightPositionFilter.Update(rightControllerPositionAction.action.ReadValue<Vector3>());
    }

    public Quaternion GetFilteredRightRotation()
    {
        return _rightRotationFilter.Update(rightControllerRotationAction.action.ReadValue<Quaternion>());
    }

    private void UpdateLeftControllerData()
    {
        Vector3 currentPosition = leftControllerPositionAction.action.ReadValue<Vector3>();
        Quaternion currentRotation = leftControllerRotationAction.action.ReadValue<Quaternion>();

        _leftPositionFilter.Update(currentPosition);
        _leftRotationFilter.Update(currentRotation);
    }

    public Vector3 GetFilteredLeftPosition()
    {
        return _leftPositionFilter.Update(leftControllerPositionAction.action.ReadValue<Vector3>());
    }

    public Quaternion GetFilteredLeftRotation()
    {
        return _leftRotationFilter.Update(leftControllerRotationAction.action.ReadValue<Quaternion>());
    }

    private void OnLeftGripPressed(InputAction.CallbackContext context)
    {
        leftGripPressed = true;
    }

    private void OnLeftGripReleased(InputAction.CallbackContext context)
    {
        leftGripPressed = false;
    }

    private void OnRightGripPressed(InputAction.CallbackContext context)
    {
        rightGripPressed = true;
    }

    private void OnRightGripReleased(InputAction.CallbackContext context)
    {
        rightGripPressed = false;
    }
}