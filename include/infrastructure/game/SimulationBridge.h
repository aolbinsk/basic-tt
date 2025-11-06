#pragma once

#include "../../domain/logic/TableTennisSimulation.h"
#include "../../domain/entities/ControllerState.h"
#include <memory>

namespace BasicTT {

/**
 * @brief Bridges between VR input and domain simulation
 *
 * Takes raw controller data and feeds it to the simulation,
 * handles fixed timestep updates.
 */
class SimulationBridge {
public:
    explicit SimulationBridge(std::unique_ptr<TableTennisSimulation> simulation)
        : m_simulation(std::move(simulation))
        , m_accumulatedTime(0.0f) {}

    /**
     * @brief Update with variable timestep, handles fixed step internally
     */
    void Update(float deltaTime,
                const ControllerState& leftController,
                const ControllerState& rightController) {

        // Update paddles from controllers
        m_simulation->UpdatePaddleFromController(leftController, rightController);

        // Accumulate time and run fixed updates
        m_accumulatedTime += deltaTime;
        const float fixedDelta = 0.02f;  // 50Hz

        while (m_accumulatedTime >= fixedDelta) {
            m_simulation->FixedUpdate(fixedDelta);
            m_accumulatedTime -= fixedDelta;
        }
    }

    TableTennisSimulation* GetSimulation() { return m_simulation.get(); }
    const TableTennisSimulation* GetSimulation() const { return m_simulation.get(); }

private:
    std::unique_ptr<TableTennisSimulation> m_simulation;
    float m_accumulatedTime;
};

} // namespace BasicTT
