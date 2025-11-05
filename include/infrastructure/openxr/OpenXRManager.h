#pragma once

#define XR_USE_PLATFORM_WIN32
#define XR_USE_GRAPHICS_API_VULKAN

#include <openxr/openxr.h>
#include <openxr/openxr_platform.h>
#include <string>
#include <vector>

namespace BasicTT {

struct OpenXRConfig {
    std::string applicationName = "BasicTT OpenXR";
    uint32_t applicationVersion = 1;
    bool useVulkan = true;
    bool enableDebug = true;
};

class OpenXRManager {
public:
    OpenXRManager();
    ~OpenXRManager();

    // Initialization
    bool Initialize(const OpenXRConfig& config);
    void Shutdown();

    // Session management
    bool CreateSession();
    bool BeginSession();
    void EndSession();
    bool IsSessionRunning() const { return m_sessionRunning; }

    // Frame management
    bool WaitFrame();
    bool BeginFrame();
    bool EndFrame();

    // Poll events
    bool PollEvents();

    // Getters
    XrInstance GetInstance() const { return m_instance; }
    XrSession GetSession() const { return m_session; }
    XrSpace GetPlaySpace() const { return m_playSpace; }
    XrSystemId GetSystemId() const { return m_systemId; }

    // View information
    uint32_t GetViewCount() const { return m_viewCount; }
    const std::vector<XrView>& GetViews() const { return m_views; }
    const std::vector<XrViewConfigurationView>& GetViewConfigs() const { return m_viewConfigs; }

private:
    // Helper methods
    bool CreateInstance();
    bool GetSystem();
    bool CreateReferenceSpaces();
    void LogInstanceInfo();
    void LogSystemInfo();

    // OpenXR handles
    XrInstance m_instance = XR_NULL_HANDLE;
    XrSession m_session = XR_NULL_HANDLE;
    XrSystemId m_systemId = XR_NULL_SYSTEM_ID;
    XrSpace m_playSpace = XR_NULL_HANDLE;
    XrSpace m_viewSpace = XR_NULL_HANDLE;

    // Frame state
    XrFrameState m_frameState = {XR_TYPE_FRAME_STATE};
    bool m_sessionRunning = false;
    bool m_shouldRender = false;

    // View configuration
    uint32_t m_viewCount = 0;
    std::vector<XrView> m_views;
    std::vector<XrViewConfigurationView> m_viewConfigs;

    // Configuration
    OpenXRConfig m_config;
};

} // namespace BasicTT
