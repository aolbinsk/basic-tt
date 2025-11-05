#pragma once

#include "../entities/BallState.h"
#include "../entities/PaddleState.h"
#include "../entities/ControllerState.h"
#include "../entities/CollisionData.h"
#include "../config/BallConfig.h"
#include "../config/PaddleConfig.h"
#include "../config/TableConfig.h"
#include "../config/PhysicsConfig.h"
#include "../physics/IBallPhysicsEngine.h"
#include "../physics/CollisionDetectionSystem.h"
#include "../physics/CollisionResolutionSystem.h"
#include <memory>

namespace BasicTT {

class TableTennisSimulation {
public:
    TableTennisSimulation(const BallConfig& ballConfig,
                         const PaddleConfig& paddleConfig,
                         const TableConfig& tableConfig,
                         const PhysicsConfig& physicsConfig);

    ~TableTennisSimulation();

    // Initialize simulation
    void Initialize();

    // Update simulation by fixed timestep
    void FixedUpdate(float deltaTime);

    // Update paddle from controller input
    void UpdatePaddleFromController(const ControllerState& leftController,
                                    const ControllerState& rightController);

    // Reset ball to serve position
    void ResetBall();

    // Getters
    const BallState& GetBallState() const { return m_ballState; }
    const PaddleState& GetLeftPaddle() const { return m_leftPaddle; }
    const PaddleState& GetRightPaddle() const { return m_rightPaddle; }

    // Setters for configs
    void SetPhysicsEngine(PhysicsEngineType type);

private:
    // Internal update methods
    void SubstepPhysics(float deltaTime);
    void IntegrateBall(float deltaTime);
    void DetectCollisions();
    void ResolveCollisions();

    // Ball state
    BallState m_ballState;
    BallState m_previousBallState;

    // Paddle states
    PaddleState m_leftPaddle;
    PaddleState m_rightPaddle;
    PaddleState m_prevLeftPaddle;
    PaddleState m_prevRightPaddle;

    // Configurations
    BallConfig m_ballConfig;
    PaddleConfig m_paddleConfig;
    TableConfig m_tableConfig;
    PhysicsConfig m_physicsConfig;

    // Physics systems
    std::unique_ptr<IBallPhysicsEngine> m_physicsEngine;
    std::unique_ptr<CollisionDetectionSystem> m_collisionDetection;
    std::unique_ptr<CollisionResolutionSystem> m_collisionResolution;

    // Collision state
    CollisionData m_lastCollision;
};

} // namespace BasicTT
