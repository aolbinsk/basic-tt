using UnityEngine;

namespace Domain.Filters
{
    public class KalmanFilterVector3 : IFilter<Vector3>
    {
        private Vector3 _stateEstimate;
        private Vector3 _errorCovariance;
        private readonly float _processNoise;
        private readonly float _measurementNoise;

        public KalmanFilterVector3(float processNoise = 1e-5f, float measurementNoise = 1e-2f)
        {
            _processNoise = processNoise;
            _measurementNoise = measurementNoise;
            Reset();
        }

        public Vector3 Update(Vector3 measurement)
        {
            // Prediction phase
            _errorCovariance += Vector3.one * _processNoise;

            // Kalman gain
            Vector3 kalmanGain = new Vector3(
                _errorCovariance.x / (_errorCovariance.x + _measurementNoise),
                _errorCovariance.y / (_errorCovariance.y + _measurementNoise),
                _errorCovariance.z / (_errorCovariance.z + _measurementNoise)
            );

            // Update estimate with measurement
            _stateEstimate += Vector3.Scale(kalmanGain, (measurement - _stateEstimate));

            // Update error covariance
            _errorCovariance = Vector3.Scale(Vector3.one - kalmanGain, _errorCovariance);

            return _stateEstimate;
        }

        public void Reset()
        {
            _stateEstimate = Vector3.zero;
            _errorCovariance = Vector3.one;
        }
    }
}