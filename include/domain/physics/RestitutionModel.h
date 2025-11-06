#pragma once

#include <cmath>
#include <algorithm>

namespace BasicTT {

/**
 * @brief Velocity-dependent restitution model for realistic bouncing
 *
 * Real table tennis balls don't have constant restitution - the coefficient
 * of restitution varies with impact velocity due to:
 * - Material non-linearity
 * - Compression losses at high speed
 * - Air pressure effects
 *
 * This model implements experimentally-derived relationships between
 * impact velocity and energy return.
 */
class RestitutionModel {
public:
    /**
     * @brief Calculate restitution for ball bouncing off table
     *
     * Table surface (wood/composite) interaction with celluloid/plastic ball.
     * Based on experimental data:
     * - Low speed (< 1 m/s): e ≈ 0.92 (nearly perfectly elastic)
     * - Medium speed (1-5 m/s): e ≈ 0.89 (typical play)
     * - High speed (> 5 m/s): e ≈ 0.85 (compression losses)
     *
     * @param impactSpeed Magnitude of impact velocity (m/s)
     * @return Coefficient of restitution (0-1)
     */
    static float CalculateBallTableRestitution(float impactSpeed) {
        if (impactSpeed < 1.0f) {
            // Low speed: nearly elastic
            return 0.92f - (0.02f * impactSpeed);
        } else if (impactSpeed < 5.0f) {
            // Medium speed: standard restitution with slight velocity dependence
            return 0.89f - (0.01f * (impactSpeed - 1.0f));
        } else {
            // High speed: increased compression losses
            float speedFactor = std::min(impactSpeed - 5.0f, 10.0f);
            return std::max(0.75f, 0.85f - (0.005f * speedFactor));
        }
    }

    /**
     * @brief Calculate restitution for ball-paddle collision
     *
     * Paddle restitution depends on:
     * - Sponge hardness (softer absorbs more energy)
     * - Impact velocity (higher speed = more compression loss)
     * - Rubber type (tensor vs tacky)
     *
     * @param impactSpeed Magnitude of impact velocity (m/s)
     * @param rubberHardness Sponge hardness 0-1 (0=soft, 1=hard)
     * @return Coefficient of restitution (0-1)
     */
    static float CalculateBallPaddleRestitution(float impactSpeed, float rubberHardness) {
        // Base restitution depends on rubber hardness
        // Soft sponge (30 degrees): ~0.78
        // Medium sponge (40 degrees): ~0.82
        // Hard sponge (50 degrees): ~0.87
        float baseRestitution = 0.78f + (rubberHardness * 0.09f);

        // Very fast hits lose more energy due to compression
        if (impactSpeed > 8.0f) {
            float penalty = (impactSpeed - 8.0f) * 0.01f;
            baseRestitution -= std::min(penalty, 0.10f);
        }

        // Clamp to reasonable range
        return std::max(0.70f, std::min(0.90f, baseRestitution));
    }

    /**
     * @brief Calculate restitution for ball hitting net
     *
     * Net is highly damping - absorbs most energy.
     *
     * @param impactSpeed Magnitude of impact velocity (m/s)
     * @return Coefficient of restitution (0-1)
     */
    static float CalculateBallNetRestitution(float impactSpeed) {
        // Net absorbs energy - very low restitution
        // Decreases with speed (net cord vibration absorbs energy)
        return std::max(0.20f, 0.35f - impactSpeed * 0.02f);
    }

    /**
     * @brief Calculate restitution for ball hitting floor
     *
     * Floor (wood/concrete) is similar to table but slightly less elastic.
     *
     * @param impactSpeed Magnitude of impact velocity (m/s)
     * @return Coefficient of restitution (0-1)
     */
    static float CalculateBallFloorRestitution(float impactSpeed) {
        // Floor is slightly less bouncy than table
        if (impactSpeed < 2.0f) {
            return 0.85f;
        } else if (impactSpeed < 6.0f) {
            return 0.82f - (impactSpeed - 2.0f) * 0.01f;
        } else {
            return std::max(0.70f, 0.78f - (impactSpeed - 6.0f) * 0.01f);
        }
    }

    /**
     * @brief Calculate restitution for ball hitting wall
     *
     * Walls are typically harder than floor but not as smooth as table.
     *
     * @param impactSpeed Magnitude of impact velocity (m/s)
     * @return Coefficient of restitution (0-1)
     */
    static float CalculateBallWallRestitution(float impactSpeed) {
        // Walls vary by material - assume painted drywall
        return std::max(0.60f, 0.75f - impactSpeed * 0.015f);
    }

private:
    // Constants for tuning (can be exposed as config later)
    static constexpr float LOW_SPEED_THRESHOLD = 1.0f;
    static constexpr float HIGH_SPEED_THRESHOLD = 5.0f;
    static constexpr float PADDLE_HIGH_SPEED_THRESHOLD = 8.0f;
};

} // namespace BasicTT
