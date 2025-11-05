#pragma once

#include <openxr/openxr.h>
#include "../../domain/entities/ControllerState.h"
#include <map>

namespace BasicTT {

class OpenXRInputManager {
public:
    OpenXRInputManager(XrInstance instance, XrSession session);
    ~OpenXRInputManager();

    bool Initialize();
    void Update(XrTime predictedDisplayTime, XrSpace playSpace);

    // Get controller states
    const ControllerState& GetLeftController() const { return m_leftController; }
    const ControllerState& GetRightController() const { return m_rightController; }

private:
    bool CreateActionSet();
    bool CreateActions();
    bool SuggestBindings();
    bool AttachActionSet();

    void UpdateController(ControllerHand hand, XrTime displayTime, XrSpace playSpace);

    XrInstance m_instance;
    XrSession m_session;

    // Action set and actions
    XrActionSet m_actionSet = XR_NULL_HANDLE;
    XrAction m_poseAction = XR_NULL_HANDLE;
    XrAction m_gripAction = XR_NULL_HANDLE;
    XrAction m_triggerAction = XR_NULL_HANDLE;

    // Spaces for controller tracking
    XrSpace m_leftHandSpace = XR_NULL_HANDLE;
    XrSpace m_rightHandSpace = XR_NULL_HANDLE;

    // Controller states
    ControllerState m_leftController;
    ControllerState m_rightController;
    ControllerState m_prevLeftController;
    ControllerState m_prevRightController;
};

} // namespace BasicTT
