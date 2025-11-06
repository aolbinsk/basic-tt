#include "../../../include/domain/filters/KalmanFilterVector3.h"

namespace BasicTT {

KalmanFilterVector3::KalmanFilterVector3(float processNoise, float measurementNoise)
    : m_processNoise(processNoise)
    , m_measurementNoise(measurementNoise) {
    Reset();
}

Vector3 KalmanFilterVector3::Update(const Vector3& measurement) {
    // Prediction phase - uncertainty increases due to process noise
    m_errorCovariance.x += m_processNoise;
    m_errorCovariance.y += m_processNoise;
    m_errorCovariance.z += m_processNoise;

    // Kalman gain - how much to trust new measurement vs current estimate
    // K = P / (P + R)  where P is error covariance, R is measurement noise
    Vector3 kalmanGain(
        m_errorCovariance.x / (m_errorCovariance.x + m_measurementNoise),
        m_errorCovariance.y / (m_errorCovariance.y + m_measurementNoise),
        m_errorCovariance.z / (m_errorCovariance.z + m_measurementNoise)
    );

    // Update estimate with measurement
    // x = x + K * (z - x)
    m_stateEstimate.x += kalmanGain.x * (measurement.x - m_stateEstimate.x);
    m_stateEstimate.y += kalmanGain.y * (measurement.y - m_stateEstimate.y);
    m_stateEstimate.z += kalmanGain.z * (measurement.z - m_stateEstimate.z);

    // Update error covariance
    // P = (1 - K) * P
    m_errorCovariance.x *= (1.0f - kalmanGain.x);
    m_errorCovariance.y *= (1.0f - kalmanGain.y);
    m_errorCovariance.z *= (1.0f - kalmanGain.z);

    return m_stateEstimate;
}

void KalmanFilterVector3::Reset() {
    m_stateEstimate = Vector3::Zero();
    m_errorCovariance = Vector3::One();  // High initial uncertainty
}

} // namespace BasicTT
