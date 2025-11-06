#pragma once

namespace BasicTT {

struct BallConfig {
    float radius;               // Ball radius (meters) - standard is 0.02m (40mm diameter)
    float mass;                 // Ball mass (kg) - standard is 0.0027kg (2.7g)
    float dragCoefficient;      // Air drag coefficient
    float magnusCoefficient;    // Magnus effect coefficient for spin
    float restitution;          // Coefficient of restitution (bounciness) 0-1
    float friction;             // Friction coefficient with surfaces

    // Default values based on standard table tennis ball
    BallConfig()
        : radius(0.02f),                    // 40mm diameter
          mass(0.0027f),                    // 2.7 grams
          dragCoefficient(0.45f),           // Typical for sphere
          magnusCoefficient(0.29f),         // Tuned for table tennis spin
          restitution(0.89f),               // Table tennis ball bounce
          friction(0.4f) {}                 // Moderate friction

    static BallConfig Default() {
        return BallConfig();
    }
};

} // namespace BasicTT
