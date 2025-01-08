using Domain.Config;
using UnityEngine;

namespace Domain.Interfaces
{
    /// <summary>
    /// Interface for physics configuration parameters needed by the domain layer.
    /// </summary>
    public interface IPhysicsConfig
    {
        BallConfig Ball { get; }
        AirConfig Air { get; }
        PaddleConfig Paddle { get; }
        TableConfig Table { get; }
        PlayerConfig Player { get; }
        RoomConfig Room { get; }
        Vector3 Gravity { get; }
        int BallLayerMask { get; }
        int EnvironmentLayerMask { get; }
    }
}