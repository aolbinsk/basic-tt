#include "../../../include/infrastructure/game/GameLoop.h"
#include <iostream>
#include <thread>

namespace BasicTT {

GameLoop::GameLoop()
    : m_accumulatedTime(0.0f), m_running(false) {

    // Initialize configs with defaults
    m_ballConfig = BallConfig::Default();
    m_paddleConfig = PaddleConfig::Default();
    m_tableConfig = TableConfig::Default();
    m_physicsConfig = PhysicsConfig::Default();
}

GameLoop::~GameLoop() {
    Shutdown();
}

bool GameLoop::Initialize() {
    std::cout << "Initializing BasicTT OpenXR Game..." << std::endl;

    // Create simulation
    m_simulation = std::make_unique<TableTennisSimulation>(
        m_ballConfig, m_paddleConfig, m_tableConfig, m_physicsConfig);
    m_simulation->Initialize();

    // Initialize OpenXR
    std::cout << "\nInitializing OpenXR..." << std::endl;
    m_openxr = std::make_unique<OpenXRManager>();

    OpenXRConfig xrConfig;
    xrConfig.applicationName = "BasicTT OpenXR";
    xrConfig.useVulkan = true;

    if (!m_openxr->Initialize(xrConfig)) {
        std::cerr << "Failed to initialize OpenXR" << std::endl;
        std::cerr << "\nNote: This application requires an OpenXR runtime (e.g., SteamVR, Oculus)" << std::endl;
        std::cerr << "Running in simulation mode without VR..." << std::endl;

        // Continue without VR for testing
        m_lastFrameTime = std::chrono::steady_clock::now();
        return true;
    }

    // Create session
    if (!m_openxr->CreateSession()) {
        std::cerr << "Failed to create OpenXR session" << std::endl;
        std::cerr << "Note: Session creation requires graphics API binding" << std::endl;
        return true; // Continue anyway for demonstration
    }

    // Create input manager
    m_input = std::make_unique<OpenXRInputManager>(
        m_openxr->GetInstance(),
        m_openxr->GetSession());

    if (!m_input->Initialize()) {
        std::cerr << "Failed to initialize input" << std::endl;
        return false;
    }

    m_lastFrameTime = std::chrono::steady_clock::now();
    std::cout << "Initialization complete!" << std::endl;

    return true;
}

void GameLoop::Run() {
    m_running = true;
    std::cout << "\nStarting game loop..." << std::endl;
    std::cout << "Press Ctrl+C to exit\n" << std::endl;

    // Simple frame counter for demo
    int frameCount = 0;

    while (m_running && frameCount < 1000) { // Limit frames for demo
        ProcessFrame();

        frameCount++;
        if (frameCount % 60 == 0) {
            std::cout << "Frame " << frameCount << " - Ball position: ("
                      << m_simulation->GetBallState().position.x << ", "
                      << m_simulation->GetBallState().position.y << ", "
                      << m_simulation->GetBallState().position.z << ")" << std::endl;
        }

        // Limit to ~60 FPS for demo
        std::this_thread::sleep_for(std::chrono::milliseconds(16));
    }

    std::cout << "\nSimulation complete. Ran " << frameCount << " frames." << std::endl;
}

void GameLoop::ProcessFrame() {
    // Calculate delta time
    auto currentTime = std::chrono::steady_clock::now();
    std::chrono::duration<float> elapsed = currentTime - m_lastFrameTime;
    float deltaTime = elapsed.count();
    m_lastFrameTime = currentTime;

    // Cap delta time to prevent spiral of death
    if (deltaTime > 0.1f) deltaTime = 0.1f;

    // Poll OpenXR events (if available)
    if (m_openxr && m_openxr->IsSessionRunning()) {
        if (!m_openxr->PollEvents()) {
            m_running = false;
            return;
        }

        // Wait for frame
        if (!m_openxr->WaitFrame()) {
            return;
        }

        // Begin frame
        if (!m_openxr->BeginFrame()) {
            return;
        }

        // Update input
        if (m_input) {
            m_input->Update(0, m_openxr->GetPlaySpace());

            // Update paddles from controllers
            m_simulation->UpdatePaddleFromController(
                m_input->GetLeftController(),
                m_input->GetRightController());
        }
    }

    // Update simulation
    UpdateSimulation();

    // Render (simplified)
    Render();

    // End OpenXR frame
    if (m_openxr && m_openxr->IsSessionRunning()) {
        m_openxr->EndFrame();
    }
}

void GameLoop::UpdateSimulation() {
    // Fixed timestep update
    m_accumulatedTime += 0.016f; // ~60 FPS

    const float fixedDelta = m_physicsConfig.fixedTimestep;

    while (m_accumulatedTime >= fixedDelta) {
        m_simulation->FixedUpdate(fixedDelta);
        m_accumulatedTime -= fixedDelta;
    }

    // Check if ball fell below floor - reset
    if (m_simulation->GetBallState().position.y < -1.0f) {
        m_simulation->ResetBall();
    }
}

void GameLoop::Render() {
    // Rendering would go here
    // In a real implementation, this would:
    // 1. Get swapchain images from OpenXR
    // 2. Render scene to each eye
    // 3. Submit frames to OpenXR

    // For now, this is a placeholder
}

void GameLoop::Shutdown() {
    std::cout << "Shutting down..." << std::endl;

    m_simulation.reset();
    m_input.reset();
    m_openxr.reset();

    std::cout << "Shutdown complete." << std::endl;
}

} // namespace BasicTT
