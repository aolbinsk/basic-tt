using Domain.Entities;

namespace Domain.Interfaces
{
    /// <summary>
    /// Interface for renderer implementations.
    /// </summary>
    public interface IRenderer
    {
        void UpdateBallVisuals(BallState ballState);
        void UpdatePaddleVisuals(PaddleState paddleState);
    }
}
