#include "../../../include/infrastructure/openxr/OpenXRManager.h"
#include <iostream>
#include <cstring>

namespace BasicTT {

OpenXRManager::OpenXRManager() {}

OpenXRManager::~OpenXRManager() {
    Shutdown();
}

bool OpenXRManager::Initialize(const OpenXRConfig& config) {
    m_config = config;

    if (!CreateInstance()) {
        std::cerr << "Failed to create OpenXR instance" << std::endl;
        return false;
    }

    if (!GetSystem()) {
        std::cerr << "Failed to get OpenXR system" << std::endl;
        return false;
    }

    LogInstanceInfo();
    LogSystemInfo();

    return true;
}

void OpenXRManager::Shutdown() {
    if (m_sessionRunning) {
        EndSession();
    }

    if (m_playSpace != XR_NULL_HANDLE) {
        xrDestroySpace(m_playSpace);
        m_playSpace = XR_NULL_HANDLE;
    }

    if (m_viewSpace != XR_NULL_HANDLE) {
        xrDestroySpace(m_viewSpace);
        m_viewSpace = XR_NULL_HANDLE;
    }

    if (m_session != XR_NULL_HANDLE) {
        xrDestroySession(m_session);
        m_session = XR_NULL_HANDLE;
    }

    if (m_instance != XR_NULL_HANDLE) {
        xrDestroyInstance(m_instance);
        m_instance = XR_NULL_HANDLE;
    }
}

bool OpenXRManager::CreateInstance() {
    XrApplicationInfo appInfo = {};
    std::strncpy(appInfo.applicationName, m_config.applicationName.c_str(), XR_MAX_APPLICATION_NAME_SIZE - 1);
    appInfo.applicationVersion = m_config.applicationVersion;
    std::strncpy(appInfo.engineName, "BasicTT", XR_MAX_ENGINE_NAME_SIZE - 1);
    appInfo.engineVersion = 1;
    appInfo.apiVersion = XR_CURRENT_API_VERSION;

    std::vector<const char*> extensions;
    if (m_config.useVulkan) {
        extensions.push_back(XR_KHR_VULKAN_ENABLE_EXTENSION_NAME);
    } else {
        extensions.push_back(XR_KHR_OPENGL_ENABLE_EXTENSION_NAME);
    }

    XrInstanceCreateInfo createInfo = {XR_TYPE_INSTANCE_CREATE_INFO};
    createInfo.applicationInfo = appInfo;
    createInfo.enabledExtensionCount = static_cast<uint32_t>(extensions.size());
    createInfo.enabledExtensionNames = extensions.data();

    XrResult result = xrCreateInstance(&createInfo, &m_instance);
    if (XR_FAILED(result)) {
        std::cerr << "xrCreateInstance failed: " << result << std::endl;
        return false;
    }

    return true;
}

bool OpenXRManager::GetSystem() {
    XrSystemGetInfo systemInfo = {XR_TYPE_SYSTEM_GET_INFO};
    systemInfo.formFactor = XR_FORM_FACTOR_HEAD_MOUNTED_DISPLAY;

    XrResult result = xrGetSystem(m_instance, &systemInfo, &m_systemId);
    if (XR_FAILED(result)) {
        std::cerr << "xrGetSystem failed: " << result << std::endl;
        return false;
    }

    // Enumerate view configurations
    uint32_t viewCount = 0;
    xrEnumerateViewConfigurationViews(m_instance, m_systemId,
                                     XR_VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO,
                                     0, &viewCount, nullptr);

    m_viewConfigs.resize(viewCount, {XR_TYPE_VIEW_CONFIGURATION_VIEW});
    xrEnumerateViewConfigurationViews(m_instance, m_systemId,
                                     XR_VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO,
                                     viewCount, &viewCount, m_viewConfigs.data());

    m_viewCount = viewCount;
    m_views.resize(viewCount, {XR_TYPE_VIEW});

    return true;
}

bool OpenXRManager::CreateSession() {
    // This is simplified - actual implementation would need graphics binding
    // For Vulkan: XrGraphicsBindingVulkanKHR
    // For OpenGL: XrGraphicsBindingOpenGLWin32KHR

    std::cout << "Note: CreateSession() needs graphics API binding" << std::endl;
    std::cout << "This is a simplified implementation" << std::endl;

    // In a real implementation, you would:
    // 1. Create graphics binding struct (Vulkan/OpenGL)
    // 2. Pass it to xrCreateSession
    // 3. Create swapchains for each view

    XrSessionCreateInfo createInfo = {XR_TYPE_SESSION_CREATE_INFO};
    createInfo.systemId = m_systemId;
    // createInfo.next = &graphicsBinding; // Would point to graphics binding

    // Note: This will fail without proper graphics binding
    // XrResult result = xrCreateSession(m_instance, &createInfo, &m_session);

    if (!CreateReferenceSpaces()) {
        return false;
    }

    return true;
}

bool OpenXRManager::BeginSession() {
    XrSessionBeginInfo beginInfo = {XR_TYPE_SESSION_BEGIN_INFO};
    beginInfo.primaryViewConfigurationType = XR_VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO;

    XrResult result = xrBeginSession(m_session, &beginInfo);
    if (XR_FAILED(result)) {
        std::cerr << "xrBeginSession failed: " << result << std::endl;
        return false;
    }

    m_sessionRunning = true;
    return true;
}

void OpenXRManager::EndSession() {
    if (m_session != XR_NULL_HANDLE) {
        xrEndSession(m_session);
        m_sessionRunning = false;
    }
}

bool OpenXRManager::CreateReferenceSpaces() {
    // Create play space (local floor)
    XrReferenceSpaceCreateInfo spaceInfo = {XR_TYPE_REFERENCE_SPACE_CREATE_INFO};
    spaceInfo.referenceSpaceType = XR_REFERENCE_SPACE_TYPE_LOCAL;
    spaceInfo.poseInReferenceSpace.orientation = {0.0f, 0.0f, 0.0f, 1.0f};
    spaceInfo.poseInReferenceSpace.position = {0.0f, 0.0f, 0.0f};

    XrResult result = xrCreateReferenceSpace(m_session, &spaceInfo, &m_playSpace);
    if (XR_FAILED(result)) {
        std::cerr << "Failed to create play space: " << result << std::endl;
        return false;
    }

    // Create view space
    spaceInfo.referenceSpaceType = XR_REFERENCE_SPACE_TYPE_VIEW;
    result = xrCreateReferenceSpace(m_session, &spaceInfo, &m_viewSpace);
    if (XR_FAILED(result)) {
        std::cerr << "Failed to create view space: " << result << std::endl;
        return false;
    }

    return true;
}

bool OpenXRManager::WaitFrame() {
    XrFrameWaitInfo waitInfo = {XR_TYPE_FRAME_WAIT_INFO};
    XrResult result = xrWaitFrame(m_session, &waitInfo, &m_frameState);

    if (XR_FAILED(result)) {
        std::cerr << "xrWaitFrame failed: " << result << std::endl;
        return false;
    }

    m_shouldRender = m_frameState.shouldRender;
    return true;
}

bool OpenXRManager::BeginFrame() {
    XrFrameBeginInfo beginInfo = {XR_TYPE_FRAME_BEGIN_INFO};
    XrResult result = xrBeginFrame(m_session, &beginInfo);

    if (XR_FAILED(result)) {
        std::cerr << "xrBeginFrame failed: " << result << std::endl;
        return false;
    }

    return true;
}

bool OpenXRManager::EndFrame() {
    XrFrameEndInfo endInfo = {XR_TYPE_FRAME_END_INFO};
    endInfo.displayTime = m_frameState.predictedDisplayTime;
    endInfo.environmentBlendMode = XR_ENVIRONMENT_BLEND_MODE_OPAQUE;

    XrResult result = xrEndFrame(m_session, &endInfo);
    if (XR_FAILED(result)) {
        std::cerr << "xrEndFrame failed: " << result << std::endl;
        return false;
    }

    return true;
}

bool OpenXRManager::PollEvents() {
    XrEventDataBuffer eventData = {XR_TYPE_EVENT_DATA_BUFFER};

    while (true) {
        XrResult result = xrPollEvent(m_instance, &eventData);
        if (result == XR_EVENT_UNAVAILABLE) {
            return true; // No more events
        }

        if (XR_FAILED(result)) {
            return false;
        }

        switch (eventData.type) {
            case XR_TYPE_EVENT_DATA_SESSION_STATE_CHANGED: {
                auto* stateEvent = reinterpret_cast<XrEventDataSessionStateChanged*>(&eventData);
                std::cout << "Session state changed to: " << stateEvent->state << std::endl;
                // Handle state changes
                break;
            }
            case XR_TYPE_EVENT_DATA_INSTANCE_LOSS_PENDING:
                std::cout << "Instance loss pending" << std::endl;
                return false;
            default:
                break;
        }

        eventData.type = XR_TYPE_EVENT_DATA_BUFFER;
    }

    return true;
}

void OpenXRManager::LogInstanceInfo() {
    XrInstanceProperties properties = {XR_TYPE_INSTANCE_PROPERTIES};
    xrGetInstanceProperties(m_instance, &properties);

    std::cout << "OpenXR Runtime: " << properties.runtimeName << std::endl;
    std::cout << "Runtime Version: " << XR_VERSION_MAJOR(properties.runtimeVersion)
              << "." << XR_VERSION_MINOR(properties.runtimeVersion)
              << "." << XR_VERSION_PATCH(properties.runtimeVersion) << std::endl;
}

void OpenXRManager::LogSystemInfo() {
    XrSystemProperties systemProps = {XR_TYPE_SYSTEM_PROPERTIES};
    xrGetSystemProperties(m_instance, m_systemId, &systemProps);

    std::cout << "System: " << systemProps.systemName << std::endl;
    std::cout << "Vendor ID: " << systemProps.vendorId << std::endl;
    std::cout << "View Count: " << m_viewCount << std::endl;
}

} // namespace BasicTT
