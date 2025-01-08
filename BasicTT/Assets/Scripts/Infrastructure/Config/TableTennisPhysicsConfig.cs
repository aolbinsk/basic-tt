using UnityEngine;
using Domain.Config;
using Domain.Interfaces;

namespace Infrastructure.Config
{
    /// <summary>
    /// Unity-specific implementation of IPhysicsConfig.
    /// Provides centralized configuration for all physics-related parameters.
    /// </summary>
    public class TableTennisPhysicsConfig : IPhysicsConfig
    {
        private PhysicsConfig _config = PhysicsConfigFactory.Create();
        
        int _paddleLayerMask = LayerMask.GetMask("Paddle");
        int _ballLayerMask = LayerMask.GetMask("Ball");
        int _environmentLayerMask = LayerMask.GetMask("Table", "Floor", "Walls", "Ceiling", "Net");
    
        public BallConfig Ball => _config.Ball;
        public AirConfig Air => _config.Air;
        public PaddleConfig Paddle => _config.Paddle;
        public TableConfig Table => _config.Table;
        public PlayerConfig Player => _config.Player;
        public RoomConfig Room => _config.Room;
        public Vector3 Gravity => _config.Gravity;
        public int BallLayerMask => _ballLayerMask;
        public int EnvironmentLayerMask => _environmentLayerMask;
    }
}