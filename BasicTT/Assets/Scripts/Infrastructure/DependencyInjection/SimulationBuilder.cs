using UnityEngine;
using Domain.Config;
using Domain.Interfaces;
using Domain.Logic;
using Domain.Physics;
using Infrastructure.Config;
using Infrastructure.CustomPhysics;
using Infrastructure.Rendering;
using Infrastructure.SceneSetup;
using Infrastructure.UnityPhysics;

namespace Infrastructure.DependencyInjection
{
    /// <summary>
    /// Installer for table tennis dependencies.
    /// </summary>
    public class SimulationBuilder
    {
        private PhysicsConfig _physicsConfig;
        private IPhysicsEngine _physicsEngine;
        private ICollisionSystem _collisionSystem;
        private IRenderer _renderer;
        private TableTennisSimulation _simulation;

        // Scene setup components
        private TableTennisEquipmentBuilder _equipmentBuilder;
        private PlayerSetupBuilder _playerSetupBuilder;
        private RoomBuilder _roomBuilder;
        private GameObject _ball;
        private GameObject _paddle;

        /// <summary>
        /// Provides the initialized renderer.
        /// </summary>
        public IRenderer GetRenderer() => _renderer;

        /// <summary>
        /// Provides the initialized table tennis simulation.
        /// </summary>
        public TableTennisSimulation GetSimulation() => _simulation;

        /// <summary>
        /// Provides the physics configuration.
        /// </summary>
        public PhysicsConfig GetPhysicsConfig() => _physicsConfig;

        /// <summary>
        /// Provides the ball GameObject.
        /// </summary>
        public GameObject GetBallGameObject() => _ball;

        /// <summary>
        /// Provides the paddle GameObject.
        /// </summary>
        public GameObject GetPaddleGameObject() => _paddle;

        /// <summary>
        /// Installs all required dependencies for the table tennis simulation.
        /// </summary>
        public void Build(bool useCustomPhysics, Transform xrOriginTransform)
        {
            // Create configurations
            _physicsConfig = PhysicsConfigFactory.Create();

            if (useCustomPhysics)
            {
                // Create physics engine and collision system based on configuration
                _physicsEngine = new CustomPhysicsEngine(_physicsConfig);
                _collisionSystem = new CollisionDetectionSystem(_physicsConfig);
            }
            else
            {
                _physicsEngine = new UnityPhysicsEngine();
                _collisionSystem = new UnityCollisionSystem();
            }

            // Initialize scene builders
            _roomBuilder = new RoomBuilder(_physicsConfig);
            _equipmentBuilder = new TableTennisEquipmentBuilder(_physicsConfig);

            // Build scene
            _roomBuilder.BuildRoom();
            _ball = _equipmentBuilder.BuildBall();
            _paddle = _equipmentBuilder.BuildPaddle();
            _equipmentBuilder.BuildTableAndNet();

            // Initialize PlayerSetupBuilder after _ball and _paddle are assigned
            _playerSetupBuilder = new PlayerSetupBuilder(_physicsConfig, xrOriginTransform, _ball, _paddle);
            _playerSetupBuilder.SetupPlayer();

            _renderer = new UnityRenderer(_ball, _paddle);

            // Create simulation
            _simulation = new TableTennisSimulation(
                _physicsEngine,
                _collisionSystem,
                _renderer,
                _physicsConfig);
        }
    }
}