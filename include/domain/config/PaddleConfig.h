#pragma once

namespace BasicTT {

struct PaddleConfig {
    // Blade dimensions (meters)
    float bladeWidth;
    float bladeHeight;
    float bladeThickness;

    // Handle dimensions (meters)
    float handleLength;
    float handleRadius;

    // Physical properties
    float mass;                 // Paddle mass (kg)
    float restitution;          // Coefficient of restitution
    float friction;             // Friction coefficient with ball

    // Rubber properties
    float rubberThickness;      // Thickness of rubber on each side (meters)
    float rubberFriction;       // Rubber friction coefficient (higher than wood)

    // Default values based on standard table tennis paddle
    PaddleConfig()
        : bladeWidth(0.15f),            // 150mm wide
          bladeHeight(0.15f),           // 150mm tall
          bladeThickness(0.006f),       // 6mm thick blade
          handleLength(0.1f),           // 100mm handle
          handleRadius(0.015f),         // 15mm radius
          mass(0.17f),                  // 170 grams typical
          restitution(0.82f),           // Less bouncy than ball
          friction(0.6f),               // Wood friction
          rubberThickness(0.002f),      // 2mm rubber
          rubberFriction(0.9f) {}       // High rubber friction

    static PaddleConfig Default() {
        return PaddleConfig();
    }
};

} // namespace BasicTT
