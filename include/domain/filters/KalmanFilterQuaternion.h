#pragma once

#include "../utilities/Quaternion.h"

namespace BasicTT {

/**
 * @brief Kalman filter for Quaternion orientation smoothing
 *
 * Filters noisy VR controller rotation measurements.
 * Uses the same per-component approach as Vector3 filter.
 */
class KalmanFilterQuaternion {
public:
    KalmanFilterQuaternion(float processNoise = 1e-5f, float measurementNoise = 1e-2f);

    /**
     * @brief Update filter with new measurement
     * @param measurement Noisy orientation from VR tracking
     * @return Filtered orientation estimate
     */
    Quaternion Update(const Quaternion& measurement);

    void Reset();
    const Quaternion& GetEstimate() const { return m_stateEstimate; }

    void SetProcessNoise(float noise) { m_processNoise = noise; }
    void SetMeasurementNoise(float noise) { m_measurementNoise = noise; }

private:
    Quaternion m_stateEstimate;
    Quaternion m_errorCovariance;  // Treating each component independently
    float m_processNoise;
    float m_measurementNoise;
};

} // namespace BasicTT
