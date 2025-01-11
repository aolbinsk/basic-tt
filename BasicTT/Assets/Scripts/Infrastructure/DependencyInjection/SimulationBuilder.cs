using System.Collections.Generic;
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
        private PaddleCalibration _paddleCalibration;

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
        /// Provides the paddle calibration data.
        /// </summary>
        /// <returns></returns>
        public PaddleCalibration GetPaddleCalibration() => _paddleCalibration;

        /// <summary>
        /// Installs all required dependencies for the table tennis simulation.
        /// </summary>
        public void Build(bool useCustomPhysics, Transform xrOriginTransform)
        {
            // Create configurations
            _physicsConfig = PhysicsConfigFactory.Create();

            if (useCustomPhysics)
            {
                _physicsEngine = new CustomPhysicsEngine(_physicsConfig);
            }
            else
            {
                _physicsEngine = new UnityPhysicsEngine();
            }

            var environmentShapes = BuildEnvironmentShapes();
            _collisionSystem = new CollisionDetectionSystem(_physicsConfig, environmentShapes);

            // Initialize scene builders
            _roomBuilder = new RoomBuilder(_physicsConfig);
            _equipmentBuilder = new TableTennisEquipmentBuilder(_physicsConfig);

            // Build scene
            _roomBuilder.BuildRoom();
            _ball = _equipmentBuilder.BuildBall();
            _paddle = _equipmentBuilder.BuildPaddle();
            _equipmentBuilder.BuildTableAndNet();

            // Load paddle calibration data
            string calibrationFilePath = "Assets/PaddleCalibrations/BonwasylViscaria.json";
            _paddleCalibration = PaddleCalibration.LoadFromFile(calibrationFilePath);

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

        private List<OrientedBox> BuildEnvironmentShapes()
        {
            List<OrientedBox> environmentShapes = new List<OrientedBox>();

            // Floor
            OrientedBox floorBox = new OrientedBox(
                new Vector3(0f, -_physicsConfig.Room.WallHeightMeters / 2f, 0f),
                Quaternion.identity,
                new Vector3(_physicsConfig.Room.WidthMeters / 2f, _physicsConfig.Room.WallHeightMeters / 2f,
                    _physicsConfig.Room.LengthMeters / 2f)
            );
            environmentShapes.Add(floorBox);

            // Table
            OrientedBox tableBox = new OrientedBox(
                new Vector3(0f, _physicsConfig.Table.HeightMeters - (_physicsConfig.Table.ThicknessMeters / 2f), 0f),
                Quaternion.identity,
                new Vector3(_physicsConfig.Table.WidthMeters / 2f, _physicsConfig.Table.ThicknessMeters / 2f,
                    _physicsConfig.Table.LengthMeters / 2f)
            );
            environmentShapes.Add(tableBox);

            // Net
            OrientedBox netBox = new OrientedBox(
                new Vector3(0f, _physicsConfig.Table.HeightMeters + (_physicsConfig.Table.NetHeightMeters / 2f), 0f),
                Quaternion.identity,
                new Vector3((_physicsConfig.Table.WidthMeters + _physicsConfig.Table.NetHeightMeters) / 2f,
                    _physicsConfig.Table.NetHeightMeters / 2f, 0.001f)
            );
            environmentShapes.Add(netBox);

            // Walls and Ceiling (simplified examples)
            float wallHeight = _physicsConfig.Room.WallHeightMeters;
            float roomSize = _physicsConfig.Room.SizeMeters;

            // Left Wall
            OrientedBox leftWall = new OrientedBox(
                new Vector3(-roomSize / 2f, wallHeight / 2f, 0f),
                Quaternion.identity,
                new Vector3(0.05f, wallHeight / 2f, roomSize / 2f)
            );
            environmentShapes.Add(leftWall);

            // Right Wall
            OrientedBox rightWall = new OrientedBox(
                new Vector3(roomSize / 2f, wallHeight / 2f, 0f),
                Quaternion.identity,
                new Vector3(0.05f, wallHeight / 2f, roomSize / 2f)
            );
            environmentShapes.Add(rightWall);

            // Back Wall
            OrientedBox backWall = new OrientedBox(
                new Vector3(0f, wallHeight / 2f, -roomSize / 2f),
                Quaternion.identity,
                new Vector3(roomSize / 2f, wallHeight / 2f, 0.05f)
            );
            environmentShapes.Add(backWall);

            // Front Wall
            OrientedBox frontWall = new OrientedBox(
                new Vector3(0f, wallHeight / 2f, roomSize / 2f),
                Quaternion.identity,
                new Vector3(roomSize / 2f, wallHeight / 2f, 0.05f)
            );
            environmentShapes.Add(frontWall);

            // Ceiling
            OrientedBox ceiling = new OrientedBox(
                new Vector3(0f, wallHeight, 0f),
                Quaternion.identity,
                new Vector3(roomSize / 2f, 0.05f, roomSize / 2f)
            );
            environmentShapes.Add(ceiling);

            return environmentShapes;
        }
    }
}