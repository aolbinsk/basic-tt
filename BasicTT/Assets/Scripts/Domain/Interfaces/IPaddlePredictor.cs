namespace Domain.Interfaces
{
    using Domain.Entities;

    /// <summary>
    /// Defines a contract for predicting (extrapolating or interpolating)
    /// a paddle's pose for sub-step times, given the current known
    /// paddle state and relevant motion data.
    /// </summary>
    public interface IPaddlePredictor
    {
        /// <summary>
        /// Predicts the paddle's state at a future sub-step time,
        /// typically from the last known or recorded sample.
        /// </summary>
        /// <param name="basePaddleState">The last known actual paddle state.</param>
        /// <param name="deltaTime">The sub-step duration into the future from that known state.</param>
        /// <returns>A newly predicted paddle state for the sub-step.</returns>
        PaddleState PredictPose(PaddleState basePaddleState, float deltaTime);
    }
}