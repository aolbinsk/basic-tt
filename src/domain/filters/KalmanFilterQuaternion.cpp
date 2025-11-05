#include "../../../include/domain/filters/KalmanFilterQuaternion.h"

namespace BasicTT {

KalmanFilterQuaternion::KalmanFilterQuaternion(float processNoise, float measurementNoise)
    : m_processNoise(processNoise)
    , m_measurementNoise(measurementNoise) {
    Reset();
}

Quaternion KalmanFilterQuaternion::Update(const Quaternion& measurement) {
    // Ensure measurement is normalized
    Quaternion normalizedMeas = measurement.Normalized();

    // Choose shortest path (avoid 360-degree rotations)
    if (Quaternion::Dot(m_stateEstimate, normalizedMeas) < 0.0f) {
        normalizedMeas.x = -normalizedMeas.x;
        normalizedMeas.y = -normalizedMeas.y;
        normalizedMeas.z = -normalizedMeas.z;
        normalizedMeas.w = -normalizedMeas.w;
    }

    // Prediction phase
    m_errorCovariance.x += m_processNoise;
    m_errorCovariance.y += m_processNoise;
    m_errorCovariance.z += m_processNoise;
    m_errorCovariance.w += m_processNoise;

    // Kalman gain (per component)
    Quaternion kalmanGain(
        m_errorCovariance.x / (m_errorCovariance.x + m_measurementNoise),
        m_errorCovariance.y / (m_errorCovariance.y + m_measurementNoise),
        m_errorCovariance.z / (m_errorCovariance.z + m_measurementNoise),
        m_errorCovariance.w / (m_errorCovariance.w + m_measurementNoise)
    );

    // Update estimate
    m_stateEstimate.x += kalmanGain.x * (normalizedMeas.x - m_stateEstimate.x);
    m_stateEstimate.y += kalmanGain.y * (normalizedMeas.y - m_stateEstimate.y);
    m_stateEstimate.z += kalmanGain.z * (normalizedMeas.z - m_stateEstimate.z);
    m_stateEstimate.w += kalmanGain.w * (normalizedMeas.w - m_stateEstimate.w);

    // Re-normalize to ensure valid quaternion
    m_stateEstimate = m_stateEstimate.Normalized();

    // Update error covariance
    m_errorCovariance.x *= (1.0f - kalmanGain.x);
    m_errorCovariance.y *= (1.0f - kalmanGain.y);
    m_errorCovariance.z *= (1.0f - kalmanGain.z);
    m_errorCovariance.w *= (1.0f - kalmanGain.w);

    return m_stateEstimate;
}

void KalmanFilterQuaternion::Reset() {
    m_stateEstimate = Quaternion::Identity();
    m_errorCovariance = Quaternion(1.0f, 1.0f, 1.0f, 1.0f);  // High initial uncertainty
}

} // namespace BasicTT
