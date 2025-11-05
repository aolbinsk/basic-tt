#pragma once

#include "../../domain/logic/TableTennisSimulation.h"
#include "../../domain/physics/BallPhysicsOptimizedVerlet.h"
#include "../../domain/physics/BallPhysicsRK4.h"
#include "../../domain/physics/CollisionDetectionSystem.h"
#include "../../domain/physics/CollisionResolutionSystem.h"
#include <memory>

namespace BasicTT {

/**
 * @brief Builder for creating and wiring up simulation components
 *
 * Simple dependency injection pattern - creates all components
 * and wires them together properly. Nothing fancy.
 */
class SimulationBuilder {
public:
    SimulationBuilder() = default;

    /**
     * @brief Build complete simulation with given configs
     * @return Fully configured simulation ready to use
     */
    std::unique_ptr<TableTennisSimulation> Build(
        const BallConfig& ballConfig = BallConfig::Default(),
        const PaddleConfig& paddleConfig = PaddleConfig::Default(),
        const TableConfig& tableConfig = TableConfig::Default(),
        const PhysicsConfig& physicsConfig = PhysicsConfig::Default()) {

        return std::make_unique<TableTennisSimulation>(
            ballConfig,
            paddleConfig,
            tableConfig,
            physicsConfig
        );
    }

    /**
     * @brief Build simulation with specific physics engine type
     */
    std::unique_ptr<TableTennisSimulation> BuildWithPhysicsEngine(
        PhysicsEngineType engineType,
        const BallConfig& ballConfig = BallConfig::Default(),
        const PaddleConfig& paddleConfig = PaddleConfig::Default(),
        const TableConfig& tableConfig = TableConfig::Default(),
        PhysicsConfig physicsConfig = PhysicsConfig::Default()) {

        physicsConfig.engineType = engineType;
        return Build(ballConfig, paddleConfig, tableConfig, physicsConfig);
    }

    /**
     * @brief Build simulation for testing with no air resistance
     */
    std::unique_ptr<TableTennisSimulation> BuildForTesting() {
        BallConfig ballCfg = BallConfig::Default();
        PhysicsConfig physicsCfg = PhysicsConfig::Default();

        // Zero out air effects for predictable tests
        physicsCfg.airDensity = 0.0f;
        ballCfg.dragCoefficient = 0.0f;
        ballCfg.magnusCoefficient = 0.0f;

        return Build(ballCfg, PaddleConfig::Default(),
                    TableConfig::Default(), physicsCfg);
    }

private:
    // No state needed - just a factory
};

} // namespace BasicTT
