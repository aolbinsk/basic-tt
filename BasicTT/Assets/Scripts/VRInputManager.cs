using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

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
    [SerializeField] private int positionHistorySize = 5;

    [Header("Left Controller Input Settings")]
    [SerializeField] private InputActionReference leftControllerPositionAction;
    [SerializeField] private InputActionReference leftControllerRotationAction;
    [SerializeField] private InputActionReference leftControllerGripAction;

    private readonly Queue<Vector3> _rightPositionHistory = new();
    private readonly Queue<Quaternion> _rightRotationHistory = new();
    private Vector3 _filteredRightPosition;
    private Quaternion _filteredRightRotation;

    private readonly Queue<Vector3> _leftPositionHistory = new();
    private readonly Queue<Quaternion> _leftRotationHistory = new();
    private Vector3 _filteredLeftPosition;
    private Quaternion _filteredLeftRotation;

    public bool leftGripPressed { get; private set; }
    public bool RightGripPressed { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            InitializeInput();
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
        ApplyRightSmoothing();

        UpdateLeftControllerData();
        ApplyLeftSmoothing();
    }

    private void UpdateRightControllerData()
    {
        Vector3 currentPosition = rightControllerPositionAction.action.ReadValue<Vector3>();
        Quaternion currentRotation = rightControllerRotationAction.action.ReadValue<Quaternion>();

        _rightPositionHistory.Enqueue(currentPosition);
        _rightRotationHistory.Enqueue(currentRotation);

        if (_rightPositionHistory.Count > positionHistorySize)
            _rightPositionHistory.Dequeue();

        if (_rightRotationHistory.Count > positionHistorySize)
            _rightRotationHistory.Dequeue();
    }

    private void ApplyRightSmoothing()
    {
        // Average positions
        Vector3 sumPositions = Vector3.zero;
        foreach (var pos in _rightPositionHistory)
            sumPositions += pos;
        _filteredRightPosition = sumPositions / _rightPositionHistory.Count;

        // Average rotations using quaternion averaging
        _filteredRightRotation = AverageQuaternions(_rightRotationHistory);
    }

    public Vector3 GetFilteredRightPosition()
    {
        return _filteredRightPosition;
    }

    public Quaternion GetFilteredRightRotation()
    {
        return _filteredRightRotation;
    }

    private void UpdateLeftControllerData()
    {
        Vector3 currentPosition = leftControllerPositionAction.action.ReadValue<Vector3>();
        Quaternion currentRotation = leftControllerRotationAction.action.ReadValue<Quaternion>();

        _leftPositionHistory.Enqueue(currentPosition);
        _leftRotationHistory.Enqueue(currentRotation);

        if (_leftPositionHistory.Count > positionHistorySize)
            _leftPositionHistory.Dequeue();

        if (_leftRotationHistory.Count > positionHistorySize)
            _leftRotationHistory.Dequeue();
    }

    private void ApplyLeftSmoothing()
    {
        // Average positions
        Vector3 sumPositions = Vector3.zero;
        foreach (var pos in _leftPositionHistory)
            sumPositions += pos;
        _filteredLeftPosition = sumPositions / _leftPositionHistory.Count;

        // Average rotations using quaternion averaging
        _filteredLeftRotation = AverageQuaternions(_leftRotationHistory);
    }

    public Vector3 GetFilteredLeftPosition()
    {
        return _filteredLeftPosition;
    }

    public Quaternion GetFilteredLeftRotation()
    {
        return _filteredLeftRotation;
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
        RightGripPressed = true;
    }

    private void OnRightGripReleased(InputAction.CallbackContext context)
    {
        RightGripPressed = false;
    }

    public Vector3 GetLeftControllerVelocity()
    {
        // Compute velocity based on position history
        if (_leftPositionHistory.Count < 2)
            return Vector3.zero;

        Vector3 firstPosition = _leftPositionHistory.Peek();
        Vector3 lastPosition = _filteredLeftPosition;
        float deltaTime = Time.deltaTime * (_leftPositionHistory.Count - 1);

        return (lastPosition - firstPosition) / deltaTime;
    }

    private Quaternion AverageQuaternions(IEnumerable<Quaternion> rotations)
    {
        Quaternion average = new Quaternion(0, 0, 0, 0);
        foreach (var rotation in rotations)
        {
            if (Quaternion.Dot(rotation, average) > 0)
            {
                average.x += rotation.x;
                average.y += rotation.y;
                average.z += rotation.z;
                average.w += rotation.w;
            }
            else
            {
                average.x -= rotation.x;
                average.y -= rotation.y;
                average.z -= rotation.z;
                average.w -= rotation.w;
            }
        }
        average = new Quaternion(
            average.x / rotations.Count(),
            average.y / rotations.Count(),
            average.z / rotations.Count(),
            average.w / rotations.Count()
        ).normalized;
        return average;
    }
}