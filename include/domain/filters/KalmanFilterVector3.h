#pragma once

#include "../utilities/Vector3.h"

namespace BasicTT {

/**
 * @brief Kalman filter for Vector3 position smoothing
 *
 * Implements a simple 1D Kalman filter applied independently to each axis.
 * This filter smooths noisy VR controller position measurements while
 * maintaining responsiveness to actual movement.
 *
 * Process model: x(k+1) = x(k) + w(k)  where w ~ N(0, processNoise)
 * Measurement model: z(k) = x(k) + v(k)  where v ~ N(0, measurementNoise)
 */
class KalmanFilterVector3 {
public:
    /**
     * @param processNoise How much the true value is expected to change (higher = more responsive)
     * @param measurementNoise How noisy the measurements are (higher = more smoothing)
     */
    KalmanFilterVector3(float processNoise = 1e-5f, float measurementNoise = 1e-2f);

    /**
     * @brief Update filter with new measurement
     * @param measurement Noisy position from VR tracking
     * @return Filtered position estimate
     */
    Vector3 Update(const Vector3& measurement);

    /**
     * @brief Reset filter to initial state
     */
    void Reset();

    /**
     * @brief Get current filtered estimate without updating
     */
    const Vector3& GetEstimate() const { return m_stateEstimate; }

    /**
     * @brief Get current uncertainty in estimate
     */
    const Vector3& GetErrorCovariance() const { return m_errorCovariance; }

    /**
     * @brief Set process noise (how much position can change per update)
     */
    void SetProcessNoise(float noise) { m_processNoise = noise; }

    /**
     * @brief Set measurement noise (how noisy the sensors are)
     */
    void SetMeasurementNoise(float noise) { m_measurementNoise = noise; }

private:
    Vector3 m_stateEstimate;      // Current filtered position
    Vector3 m_errorCovariance;    // Uncertainty in estimate (per axis)
    float m_processNoise;         // Expected process variation
    float m_measurementNoise;     // Expected measurement variation
};

} // namespace BasicTT
