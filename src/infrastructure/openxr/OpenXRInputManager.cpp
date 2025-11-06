#include "../../../include/infrastructure/openxr/OpenXRInputManager.h"
#include <iostream>
#include <cstring>
#include <cmath>

namespace BasicTT {

OpenXRInputManager::OpenXRInputManager(XrInstance instance, XrSession session)
    : m_instance(instance)
    , m_session(session)
    , m_leftHistory(10)   // Keep last 10 samples for velocity estimation
    , m_rightHistory(10) {
    m_leftController = ControllerState(ControllerHand::Left);
    m_rightController = ControllerState(ControllerHand::Right);
}

OpenXRInputManager::~OpenXRInputManager() {
    if (m_leftHandSpace != XR_NULL_HANDLE) xrDestroySpace(m_leftHandSpace);
    if (m_rightHandSpace != XR_NULL_HANDLE) xrDestroySpace(m_rightHandSpace);
    if (m_actionSet != XR_NULL_HANDLE) xrDestroyActionSet(m_actionSet);
}

bool OpenXRInputManager::Initialize() {
    if (!CreateActionSet()) return false;
    if (!CreateActions()) return false;
    if (!SuggestBindings()) return false;
    if (!AttachActionSet()) return false;

    return true;
}

bool OpenXRInputManager::CreateActionSet() {
    XrActionSetCreateInfo createInfo = {XR_TYPE_ACTION_SET_CREATE_INFO};
    std::strncpy(createInfo.actionSetName, "gameplay", XR_MAX_ACTION_SET_NAME_SIZE - 1);
    std::strncpy(createInfo.localizedActionSetName, "Gameplay", XR_MAX_LOCALIZED_ACTION_SET_NAME_SIZE - 1);
    createInfo.priority = 0;

    XrResult result = xrCreateActionSet(m_instance, &createInfo, &m_actionSet);
    if (XR_FAILED(result)) {
        std::cerr << "Failed to create action set" << std::endl;
        return false;
    }

    return true;
}

bool OpenXRInputManager::CreateActions() {
    // Hand pose action
    XrActionCreateInfo actionInfo = {XR_TYPE_ACTION_CREATE_INFO};
    actionInfo.actionType = XR_ACTION_TYPE_POSE_INPUT;
    std::strncpy(actionInfo.actionName, "hand_pose", XR_MAX_ACTION_NAME_SIZE - 1);
    std::strncpy(actionInfo.localizedActionName, "Hand Pose", XR_MAX_LOCALIZED_ACTION_NAME_SIZE - 1);

    XrPath handPaths[2];
    xrStringToPath(m_instance, "/user/hand/left", &handPaths[0]);
    xrStringToPath(m_instance, "/user/hand/right", &handPaths[1]);
    actionInfo.countSubactionPaths = 2;
    actionInfo.subactionPaths = handPaths;

    XrResult result = xrCreateAction(m_actionSet, &actionInfo, &m_poseAction);
    if (XR_FAILED(result)) {
        std::cerr << "Failed to create pose action" << std::endl;
        return false;
    }

    // Grip action
    actionInfo.actionType = XR_ACTION_TYPE_FLOAT_INPUT;
    std::strncpy(actionInfo.actionName, "grip", XR_MAX_ACTION_NAME_SIZE - 1);
    std::strncpy(actionInfo.localizedActionName, "Grip", XR_MAX_LOCALIZED_ACTION_NAME_SIZE - 1);

    result = xrCreateAction(m_actionSet, &actionInfo, &m_gripAction);
    if (XR_FAILED(result)) {
        std::cerr << "Failed to create grip action" << std::endl;
        return false;
    }

    // Trigger action
    std::strncpy(actionInfo.actionName, "trigger", XR_MAX_ACTION_NAME_SIZE - 1);
    std::strncpy(actionInfo.localizedActionName, "Trigger", XR_MAX_LOCALIZED_ACTION_NAME_SIZE - 1);

    result = xrCreateAction(m_actionSet, &actionInfo, &m_triggerAction);
    if (XR_FAILED(result)) {
        std::cerr << "Failed to create trigger action" << std::endl;
        return false;
    }

    // Create action spaces for hands
    XrActionSpaceCreateInfo spaceInfo = {XR_TYPE_ACTION_SPACE_CREATE_INFO};
    spaceInfo.action = m_poseAction;
    spaceInfo.poseInActionSpace.orientation = {0.0f, 0.0f, 0.0f, 1.0f};
    spaceInfo.poseInActionSpace.position = {0.0f, 0.0f, 0.0f};

    XrPath leftHandPath, rightHandPath;
    xrStringToPath(m_instance, "/user/hand/left", &leftHandPath);
    xrStringToPath(m_instance, "/user/hand/right", &rightHandPath);

    spaceInfo.subactionPath = leftHandPath;
    result = xrCreateActionSpace(m_session, &spaceInfo, &m_leftHandSpace);
    if (XR_FAILED(result)) {
        std::cerr << "Failed to create left hand space" << std::endl;
        return false;
    }

    spaceInfo.subactionPath = rightHandPath;
    result = xrCreateActionSpace(m_session, &spaceInfo, &m_rightHandSpace);
    if (XR_FAILED(result)) {
        std::cerr << "Failed to create right hand space" << std::endl;
        return false;
    }

    return true;
}

bool OpenXRInputManager::SuggestBindings() {
    // Simplified - suggest bindings for Oculus Touch controllers
    XrPath interactionProfilePath;
    xrStringToPath(m_instance, "/interaction_profiles/oculus/touch_controller", &interactionProfilePath);

    std::vector<XrActionSuggestedBinding> bindings;

    // Left hand pose
    XrPath path;
    xrStringToPath(m_instance, "/user/hand/left/input/grip/pose", &path);
    bindings.push_back({m_poseAction, path});

    // Right hand pose
    xrStringToPath(m_instance, "/user/hand/right/input/grip/pose", &path);
    bindings.push_back({m_poseAction, path});

    // Left grip
    xrStringToPath(m_instance, "/user/hand/left/input/squeeze/value", &path);
    bindings.push_back({m_gripAction, path});

    // Right grip
    xrStringToPath(m_instance, "/user/hand/right/input/squeeze/value", &path);
    bindings.push_back({m_gripAction, path});

    // Left trigger
    xrStringToPath(m_instance, "/user/hand/left/input/trigger/value", &path);
    bindings.push_back({m_triggerAction, path});

    // Right trigger
    xrStringToPath(m_instance, "/user/hand/right/input/trigger/value", &path);
    bindings.push_back({m_triggerAction, path});

    XrInteractionProfileSuggestedBinding suggestedBindings = {XR_TYPE_INTERACTION_PROFILE_SUGGESTED_BINDING};
    suggestedBindings.interactionProfile = interactionProfilePath;
    suggestedBindings.countSuggestedBindings = static_cast<uint32_t>(bindings.size());
    suggestedBindings.suggestedBindings = bindings.data();

    XrResult result = xrSuggestInteractionProfileBindings(m_instance, &suggestedBindings);
    if (XR_FAILED(result)) {
        std::cerr << "Failed to suggest bindings: " << result << std::endl;
        return false;
    }

    return true;
}

bool OpenXRInputManager::AttachActionSet() {
    XrSessionActionSetsAttachInfo attachInfo = {XR_TYPE_SESSION_ACTION_SETS_ATTACH_INFO};
    attachInfo.countActionSets = 1;
    attachInfo.actionSets = &m_actionSet;

    XrResult result = xrAttachSessionActionSets(m_session, &attachInfo);
    if (XR_FAILED(result)) {
        std::cerr << "Failed to attach action set" << std::endl;
        return false;
    }

    return true;
}

void OpenXRInputManager::Update(XrTime predictedDisplayTime, XrSpace playSpace) {
    // Sync actions
    XrActiveActionSet activeActionSet = {m_actionSet, XR_NULL_PATH};
    XrActionsSyncInfo syncInfo = {XR_TYPE_ACTIONS_SYNC_INFO};
    syncInfo.countActiveActionSets = 1;
    syncInfo.activeActionSets = &activeActionSet;

    XrResult result = xrSyncActions(m_session, &syncInfo);
    if (XR_FAILED(result)) {
        return;
    }

    // Store previous states for velocity calculation
    m_prevLeftController = m_leftController;
    m_prevRightController = m_rightController;

    // Update controllers
    UpdateController(ControllerHand::Left, predictedDisplayTime, playSpace);
    UpdateController(ControllerHand::Right, predictedDisplayTime, playSpace);
}

void OpenXRInputManager::UpdateController(ControllerHand hand, XrTime displayTime, XrSpace playSpace) {
    ControllerState& controller = (hand == ControllerHand::Left) ? m_leftController : m_rightController;
    const ControllerState& prevController = (hand == ControllerHand::Left) ? m_prevLeftController : m_prevRightController;
    XrSpace handSpace = (hand == ControllerHand::Left) ? m_leftHandSpace : m_rightHandSpace;

    // Get Kalman filters for this hand
    KalmanFilterVector3& posFilter = (hand == ControllerHand::Left) ? m_leftPosFilter : m_rightPosFilter;
    KalmanFilterQuaternion& rotFilter = (hand == ControllerHand::Left) ? m_leftRotFilter : m_rightRotFilter;
    CircularBuffer<ControllerState>& history = (hand == ControllerHand::Left) ? m_leftHistory : m_rightHistory;

    XrPath subactionPath;
    const char* pathStr = (hand == ControllerHand::Left) ? "/user/hand/left" : "/user/hand/right";
    xrStringToPath(m_instance, pathStr, &subactionPath);

    // Get pose
    XrSpaceLocation location = {XR_TYPE_SPACE_LOCATION};
    XrResult result = xrLocateSpace(handSpace, playSpace, displayTime, &location);

    if (XR_SUCCEEDED(result) && (location.locationFlags & XR_SPACE_LOCATION_POSITION_VALID_BIT)) {
        controller.isActive = true;

        // RAW position from OpenXR
        Vector3 rawPosition(
            location.pose.position.x,
            location.pose.position.y,
            location.pose.position.z
        );

        // RAW rotation from OpenXR
        Quaternion rawRotation(
            location.pose.orientation.x,
            location.pose.orientation.y,
            location.pose.orientation.z,
            location.pose.orientation.w
        );

        // FILTER position and rotation using Kalman filters
        controller.position = posFilter.Update(rawPosition);
        controller.rotation = rotFilter.Update(rawRotation);

        // Store in history for velocity estimation
        controller.timestamp = displayTime / 1000000000.0f;  // Convert nanoseconds to seconds
        history.Push(controller);

        // Calculate SMOOTH velocity from filtered history
        if (history.Size() >= 2) {
            const ControllerState& prev = history.Get(history.Size() - 2);
            float dt = controller.timestamp - prev.timestamp;
            if (dt > 0.001f) {  // At least 1ms
                controller.velocity = (controller.position - prev.position) / dt;

                // Estimate angular velocity from quaternion difference
                // This is a simplified approach - proper method would use quaternion logarithm
                Quaternion deltaRot = controller.rotation * prev.rotation.Conjugate();
                Vector3 axis(deltaRot.x, deltaRot.y, deltaRot.z);
                float angle = 2.0f * std::atan2(axis.Magnitude(), deltaRot.w);
                if (axis.Magnitude() > 1e-6f) {
                    controller.angularVelocity = axis.Normalized() * (angle / dt);
                } else {
                    controller.angularVelocity = Vector3::Zero();
                }
            }
        }

    } else {
        controller.isActive = false;
    }

    // Get grip value
    XrActionStateGetInfo getInfo = {XR_TYPE_ACTION_STATE_GET_INFO};
    getInfo.action = m_gripAction;
    getInfo.subactionPath = subactionPath;

    XrActionStateFloat gripState = {XR_TYPE_ACTION_STATE_FLOAT};
    result = xrGetActionStateFloat(m_session, &getInfo, &gripState);
    if (XR_SUCCEEDED(result) && gripState.isActive) {
        controller.gripValue = gripState.currentState;
    }

    // Get trigger value
    getInfo.action = m_triggerAction;
    XrActionStateFloat triggerState = {XR_TYPE_ACTION_STATE_FLOAT};
    result = xrGetActionStateFloat(m_session, &getInfo, &triggerState);
    if (XR_SUCCEEDED(result) && triggerState.isActive) {
        controller.triggerValue = triggerState.currentState;
    }

    controller.timestamp = displayTime / 1000000000.0f; // Convert nanoseconds to seconds
}

} // namespace BasicTT
