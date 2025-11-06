#pragma once

#include "../../domain/logic/TableTennisSimulation.h"
#include "../openxr/OpenXRManager.h"
#include "../openxr/OpenXRInputManager.h"
#include <memory>
#include <chrono>

namespace BasicTT {

class GameLoop {
public:
    GameLoop();
    ~GameLoop();

    bool Initialize();
    void Run();
    void Shutdown();

private:
    void ProcessFrame();
    void UpdateSimulation();
    void Render();

    // OpenXR components
    std::unique_ptr<OpenXRManager> m_openxr;
    std::unique_ptr<OpenXRInputManager> m_input;

    // Game simulation
    std::unique_ptr<TableTennisSimulation> m_simulation;

    // Configs
    BallConfig m_ballConfig;
    PaddleConfig m_paddleConfig;
    TableConfig m_tableConfig;
    PhysicsConfig m_physicsConfig;

    // Timing
    std::chrono::steady_clock::time_point m_lastFrameTime;
    float m_accumulatedTime;
    bool m_running;
};

} // namespace BasicTT
