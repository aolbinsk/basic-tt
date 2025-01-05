using UnityEngine;

namespace Filters
{
    public class KalmanFilterQuaternion : IFilter<Quaternion>
    {
        private Quaternion _stateEstimate;
        private float _errorCovariance;
        private readonly float _processNoise;
        private readonly float _measurementNoise;

        public KalmanFilterQuaternion(float processNoise = 1e-5f, float measurementNoise = 1e-2f)
        {
            _processNoise = processNoise;
            _measurementNoise = measurementNoise;
            Reset();
        }

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

        public void Reset()
        {
            _stateEstimate = Quaternion.identity;
            _errorCovariance = 1f;
        }
    }
}