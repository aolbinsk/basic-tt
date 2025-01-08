using UnityEngine;

namespace Domain.Filters
{
    /// <summary>
    /// Implements a Kalman filter for smoothing Quaternion data.
    /// </summary>
    public class KalmanFilterQuaternion : IFilter<Quaternion>
    {
        private Quaternion _stateEstimate;
        private float _errorCovariance;
        private readonly float _processNoise;
        private readonly float _measurementNoise;

        /// <summary>
        /// Initializes a new instance of the KalmanFilterQuaternion class.
        /// </summary>
        /// <param name="processNoise">The process noise parameter.</param>
        /// <param name="measurementNoise">The measurement noise parameter.</param>
        public KalmanFilterQuaternion(float processNoise = 1e-5f, float measurementNoise = 1e-2f)
        {
            _processNoise = processNoise;
            _measurementNoise = measurementNoise;
            Reset();
        }

        /// <summary>
        /// Updates the filter with a new measurement and returns the filtered Quaternion.
        /// </summary>
        /// <param name="measurement">The new Quaternion measurement.</param>
        /// <returns>The filtered Quaternion.</returns>
        public Quaternion Update(Quaternion measurement)
        {
            // Prediction phase
            _errorCovariance += _processNoise;

            // Kalman gain
            float kalmanGain = _errorCovariance / (_errorCovariance + _measurementNoise);

            // Update estimate with measurement
            _stateEstimate = Quaternion.Slerp(_stateEstimate, measurement, kalmanGain);

            // Update error covariance
            _errorCovariance = (1f - kalmanGain) * _errorCovariance;

            return _stateEstimate;
        }

        /// <summary>
        /// Resets the filter to its initial state.
        /// </summary>
        public void Reset()
        {
            _stateEstimate = Quaternion.identity;
            _errorCovariance = 1f;
        }
    }
}