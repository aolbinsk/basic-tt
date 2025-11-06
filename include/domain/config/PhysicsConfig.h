#pragma once

#include "../utilities/Vector3.h"

namespace BasicTT {

enum class PhysicsEngineType {
    OptimizedVerlet,    // 2nd-order Verlet integrator (default)
    RungeKutta4,        // 4th-order Runge-Kutta (high precision)
    BasicVerlet         // 1st-order Verlet (testing only)
};

struct PhysicsConfig {
    // Simulation parameters
    float fixedTimestep;        // Fixed timestep for physics (seconds)
    int substeps;               // Number of substeps per fixed update
    PhysicsEngineType engineType;

    // Global physics constants
    Vector3 gravity;            // Gravity vector (m/s²)
    float airDensity;           // Air density (kg/m³) at sea level

    // Numerical stability
    float minVelocity;          // Minimum velocity threshold (m/s)
    float maxVelocity;          // Maximum velocity clamp (m/s)
    float minAngularVelocity;   // Minimum angular velocity (rad/s)
    float maxAngularVelocity;   // Maximum angular velocity (rad/s)

    // Collision parameters
    float collisionTolerance;   // Collision detection tolerance (meters)
    int maxCollisionIterations; // Maximum collision resolution iterations

    // Default values
    PhysicsConfig()
        : fixedTimestep(0.02f),             // 50 Hz
          substeps(7),                       // 360 Hz effective rate
          engineType(PhysicsEngineType::OptimizedVerlet),
          gravity(Vector3(0.0f, -9.81f, 0.0f)),
          airDensity(1.225f),                // Sea level
          minVelocity(0.001f),               // 1mm/s
          maxVelocity(100.0f),               // 100 m/s
          minAngularVelocity(0.01f),         // ~0.57 degrees/s
          maxAngularVelocity(628.3f),        // ~100 rev/s
          collisionTolerance(0.0001f),       // 0.1mm
          maxCollisionIterations(4) {}

    float GetSubstepDeltaTime() const {
        return fixedTimestep / static_cast<float>(substeps);
    }

    static PhysicsConfig Default() {
        return PhysicsConfig();
    }

    static PhysicsConfig HighPrecision() {
        PhysicsConfig config;
        config.engineType = PhysicsEngineType::RungeKutta4;
        config.substeps = 10;
        config.collisionTolerance = 0.00001f; // 0.01mm
        return config;
    }
};

} // namespace BasicTT
