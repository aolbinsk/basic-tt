using UnityEngine;
using Domain.Config;

namespace Infrastructure.Config
{
    /// <summary>
    /// Factory for creating and initializing physics configuration.
    /// </summary>
    public static class PhysicsConfigFactory
    {
        /// <summary>
        /// Creates a fully initialized physics configuration.
        /// </summary>
        /// <returns>The initialized physics configuration.</returns>
        public static PhysicsConfig Create()
        {
            var config = new PhysicsConfig();
            
            // Initialize layer masks
            config.BallLayerMask = LayerMask.GetMask("Ball");
            config.EnvironmentLayerMask = LayerMask.GetMask("Table", "Floor", "Walls", "Ceiling", "Net");

            // Initialize materials
            InitializeMaterials(config);

            return config;
        }

        private static void InitializeMaterials(PhysicsConfig config)
        {
            // Ball material
            var ballMaterial = new PhysicsMaterial("Ball")
            {
                bounciness = 0.9f,
                dynamicFriction = 0.3f,
                staticFriction = 0.3f
            };
            config.Ball.Material = ballMaterial;

            // Paddle material
            var paddleMaterial = new PhysicsMaterial("Paddle")
            {
                bounciness = config.Paddle.RubberBounciness,
                dynamicFriction = 0.6f,
                staticFriction = 0.6f
            };
            config.Paddle.Material = paddleMaterial;

            // Table material
            var tableMaterial = new PhysicsMaterial("Table")
            {
                bounciness = config.Table.BounceRestitution,
                dynamicFriction = config.Table.Friction,
                staticFriction = config.Table.Friction
            };
            config.Table.TableMaterial = tableMaterial;

            // Net material
            var netMaterial = new PhysicsMaterial("Net")
            {
                bounciness = 0.3f,
                dynamicFriction = 0.5f,
                staticFriction = 0.5f
            };
            config.Table.NetMaterial = netMaterial;
        }
    }
}