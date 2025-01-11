using UnityEngine;
using Domain.Interfaces;

namespace Infrastructure.SceneSetup
{
    /// <summary>
    /// Handles setup of table tennis specific equipment (table, net, ball).
    /// </summary>
    public class TableTennisEquipmentBuilder
    {
        private readonly IPhysicsConfig _config;
        private readonly BallBuilder _ballBuilder;
        private readonly PaddleBuilder _paddleBuilder;

        /// <summary>
        /// Initializes a new instance of the TableTennisEquipmentBuilder class with the specified physics configuration.
        /// </summary>
        /// <param name="config">The physics configuration to use for equipment setup.</param>
        public TableTennisEquipmentBuilder(IPhysicsConfig config)
        {
            _config = config;
            _ballBuilder = new BallBuilder(_config);
            _paddleBuilder = new PaddleBuilder(_config.Paddle);
        }

        /// <summary>
        /// Builds the table tennis ball.
        /// </summary>
        public GameObject BuildBall()
        {
            var ball = _ballBuilder.BuildBall();
            
            // Position the ball above the table
            float ballHeight = _config.Table.HeightMeters + 0.2f; // e.g., 20cm above table
            ball.transform.position = new Vector3(0f, ballHeight, -_config.Table.LengthMeters / 4f);
            ball.tag = "Ball";
            
            return ball;
        }

        /// <summary>
        /// Builds the table tennis paddle.
        /// </summary>
        public GameObject BuildPaddle()
        {
            var paddle = _paddleBuilder.BuildPaddle();
            paddle.tag = "Paddle";
            return paddle;
        }

        /// <summary>
        /// Builds the table tennis table and net.
        /// </summary>
        public void BuildTableAndNet()
        {
            BuildTableTop();
            BuildNet();
        }

        private void BuildTableTop()
        {
            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "TableTop";
            table.layer = LayerMask.NameToLayer("Table");

            ConfigureTableDimensions(table);
            ConfigureTablePhysics(table);
        }

        private void ConfigureTableDimensions(GameObject table)
        {
            float tableWidth = _config.Table.WidthMeters;
            float tableLength = _config.Table.LengthMeters;

            table.transform.localScale = new Vector3(
                tableWidth,
                _config.Table.ThicknessMeters,
                tableLength);

            table.transform.position = new Vector3(
                0f,
                _config.Table.HeightMeters - _config.Table.ThicknessMeters / 2f,
                0f);
        }

        private void ConfigureTablePhysics(GameObject table)
        {
            var tableMaterial = _config.Table.TableMaterial;
            var tableCollider = table.GetComponent<Collider>();
            tableCollider.material = tableMaterial;
        }

        private void BuildNet()
        {
            var net = GameObject.CreatePrimitive(PrimitiveType.Cube);
            net.name = "TableTennisNet";
            net.layer = LayerMask.NameToLayer("Net");

            ConfigureNetDimensions(net);
            ConfigureNetPhysics(net);
            ConfigureNetVisuals(net);
        }

        private void ConfigureNetDimensions(GameObject net)
        {
            float netWidth = _config.Table.WidthMeters + _config.Table.NetHeightMeters;
            float netHeight = _config.Table.NetHeightMeters;
            const float netThickness = 0.001f;

            net.transform.localScale = new Vector3(netWidth, netHeight, netThickness);

            const float surfaceOffset = 0.001f;
            float netYPosition = _config.Table.HeightMeters
                               + _config.Table.NetHeightMeters / 2f
                               + surfaceOffset;
            net.transform.position = new Vector3(0f, netYPosition, 0f);
        }

        private void ConfigureNetPhysics(GameObject net)
        {
            var boxCollider = net.GetComponent<BoxCollider>();
            boxCollider.material = _config.Table.NetMaterial;
            boxCollider.isTrigger = false;

            var rb = net.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private void ConfigureNetVisuals(GameObject net)
        {
            var netRenderer = net.GetComponent<MeshRenderer>();
            if (netRenderer != null)
            {
                netRenderer.material.color = Color.white;
            }
        }
    }
}