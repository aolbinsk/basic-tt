#include "../../../include/domain/logic/TableTennisSimulation.h"
#include "../../../include/domain/physics/BallPhysicsOptimizedVerlet.h"
#include "../../../include/domain/physics/BallPhysicsRK4.h"
#include <iostream>

namespace BasicTT {

TableTennisSimulation::TableTennisSimulation(
    const BallConfig& ballConfig,
    const PaddleConfig& paddleConfig,
    const TableConfig& tableConfig,
    const PhysicsConfig& physicsConfig)
    : m_ballConfig(ballConfig),
      m_paddleConfig(paddleConfig),
      m_tableConfig(tableConfig),
      m_physicsConfig(physicsConfig) {

    // Create collision systems
    m_collisionDetection = std::make_unique<CollisionDetectionSystem>(
        ballConfig, tableConfig, physicsConfig);

    m_collisionResolution = std::make_unique<CollisionResolutionSystem>(
        ballConfig, paddleConfig, tableConfig, physicsConfig);

    // Create physics engine based on config
    SetPhysicsEngine(physicsConfig.engineType);
}

TableTennisSimulation::~TableTennisSimulation() = default;

void TableTennisSimulation::Initialize() {
    // Initialize ball at center of table, slightly above surface
    m_ballState.position = Vector3(0.0f, m_tableConfig.height + 0.5f, 0.0f);
    m_ballState.velocity = Vector3(0.0f, 0.0f, 0.0f);
    m_ballState.spin = Vector3::Zero();
    m_ballState.angularVelocity = Vector3::Zero();
    m_ballState.timestamp = 0.0f;

    // Initialize paddles
    m_leftPaddle.position = Vector3(-1.0f, m_tableConfig.height + 0.2f, 0.0f);
    m_leftPaddle.rotation = Quaternion::Identity();

    m_rightPaddle.position = Vector3(1.0f, m_tableConfig.height + 0.2f, 0.0f);
    m_rightPaddle.rotation = Quaternion::Identity();

    std::cout << "TableTennisSimulation initialized" << std::endl;
}

void TableTennisSimulation::FixedUpdate(float deltaTime) {
    // Perform substep integration
    float substepDelta = m_physicsConfig.GetSubstepDeltaTime();

    for (int i = 0; i < m_physicsConfig.substeps; i++) {
        SubstepPhysics(substepDelta);
    }
}

void TableTennisSimulation::SubstepPhysics(float deltaTime) {
    m_previousBallState = m_ballState;

    // Integrate ball physics
    IntegrateBall(deltaTime);

    // Detect collisions
    DetectCollisions();

    // Resolve collisions
    ResolveCollisions();
}

void TableTennisSimulation::IntegrateBall(float deltaTime) {
    m_ballState = m_physicsEngine->Step(m_ballState, deltaTime);
}

void TableTennisSimulation::DetectCollisions() {
    m_lastCollision.Reset();

    // Check paddle collisions
    CollisionData leftPaddleCollision = m_collisionDetection->DetectPaddleCollision(
        m_previousBallState, m_ballState, m_prevLeftPaddle, m_leftPaddle);

    if (leftPaddleCollision.hasCollision) {
        m_lastCollision = leftPaddleCollision;
        return;
    }

    CollisionData rightPaddleCollision = m_collisionDetection->DetectPaddleCollision(
        m_previousBallState, m_ballState, m_prevRightPaddle, m_rightPaddle);

    if (rightPaddleCollision.hasCollision) {
        m_lastCollision = rightPaddleCollision;
        return;
    }

    // Check table collision
    CollisionData tableCollision = m_collisionDetection->DetectTableCollision(
        m_previousBallState, m_ballState);

    if (tableCollision.hasCollision) {
        m_lastCollision = tableCollision;
        return;
    }

    // Check net collision
    CollisionData netCollision = m_collisionDetection->DetectNetCollision(
        m_previousBallState, m_ballState);

    if (netCollision.hasCollision) {
        m_lastCollision = netCollision;
        return;
    }

    // Check floor collision
    CollisionData floorCollision = m_collisionDetection->DetectFloorCollision(
        m_previousBallState, m_ballState);

    if (floorCollision.hasCollision) {
        m_lastCollision = floorCollision;
        return;
    }

    // Check wall collision
    CollisionData wallCollision = m_collisionDetection->DetectWallCollision(
        m_previousBallState, m_ballState);

    if (wallCollision.hasCollision) {
        m_lastCollision = wallCollision;
    }
}

void TableTennisSimulation::ResolveCollisions() {
    if (!m_lastCollision.hasCollision) {
        return;
    }

    // Determine which paddle to pass (if any)
    const PaddleState* paddle = nullptr;
    if (m_lastCollision.type == CollisionType::Paddle) {
        // Determine which paddle based on collision position
        // Simple heuristic: left side = left paddle
        if (m_lastCollision.collisionPoint.x < 0) {
            paddle = &m_leftPaddle;
        } else {
            paddle = &m_rightPaddle;
        }
    }

    // Resolve collision and update ball state
    m_ballState = m_collisionResolution->ResolveCollision(m_ballState, m_lastCollision, paddle);

    // Log collision for debugging
    if (m_lastCollision.type == CollisionType::Paddle) {
        std::cout << "Paddle collision at " << m_lastCollision.collisionPoint.x << ", "
                  << m_lastCollision.collisionPoint.y << ", "
                  << m_lastCollision.collisionPoint.z << std::endl;
    }
}

void TableTennisSimulation::UpdatePaddleFromController(
    const ControllerState& leftController,
    const ControllerState& rightController) {

    // Store previous paddle states
    m_prevLeftPaddle = m_leftPaddle;
    m_prevRightPaddle = m_rightPaddle;

    // Update left paddle
    if (leftController.isActive) {
        m_leftPaddle.position = leftController.position;
        m_leftPaddle.rotation = leftController.rotation;
        m_leftPaddle.velocity = leftController.velocity;
        m_leftPaddle.angularVelocity = leftController.angularVelocity;
        m_leftPaddle.timestamp = leftController.timestamp;
    }

    // Update right paddle
    if (rightController.isActive) {
        m_rightPaddle.position = rightController.position;
        m_rightPaddle.rotation = rightController.rotation;
        m_rightPaddle.velocity = rightController.velocity;
        m_rightPaddle.angularVelocity = rightController.angularVelocity;
        m_rightPaddle.timestamp = rightController.timestamp;
    }
}

void TableTennisSimulation::ResetBall() {
    m_ballState.position = Vector3(0.0f, m_tableConfig.height + 0.5f, 0.0f);
    m_ballState.velocity = Vector3(2.0f, 1.0f, 0.0f); // Give it some initial velocity
    m_ballState.spin = Vector3::Zero();
    m_ballState.angularVelocity = Vector3::Zero();

    std::cout << "Ball reset" << std::endl;
}

void TableTennisSimulation::SetPhysicsEngine(PhysicsEngineType type) {
    switch (type) {
        case PhysicsEngineType::OptimizedVerlet:
            m_physicsEngine = std::make_unique<BallPhysicsOptimizedVerlet>(
                m_ballConfig, m_physicsConfig);
            std::cout << "Using Optimized Verlet physics engine" << std::endl;
            break;

        case PhysicsEngineType::RungeKutta4:
            m_physicsEngine = std::make_unique<BallPhysicsRK4>(
                m_ballConfig, m_physicsConfig);
            std::cout << "Using Runge-Kutta 4 physics engine" << std::endl;
            break;

        default:
            m_physicsEngine = std::make_unique<BallPhysicsOptimizedVerlet>(
                m_ballConfig, m_physicsConfig);
            std::cout << "Using default Optimized Verlet physics engine" << std::endl;
            break;
    }
}

} // namespace BasicTT
