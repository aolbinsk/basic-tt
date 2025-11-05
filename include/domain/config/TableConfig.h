#pragma once

namespace BasicTT {

struct TableConfig {
    // Table dimensions (meters) - ITTF regulation sizes
    float length;               // 2.74m
    float width;                // 1.525m
    float height;               // 0.76m from floor
    float thickness;            // Table surface thickness

    // Net dimensions (meters)
    float netHeight;            // 0.1525m (15.25cm)
    float netOverhang;          // Extends beyond table edges

    // Physical properties
    float restitution;          // Coefficient of restitution
    float friction;             // Friction coefficient

    // Default values based on ITTF regulations
    TableConfig()
        : length(2.74f),
          width(1.525f),
          height(0.76f),
          thickness(0.025f),
          netHeight(0.1525f),
          netOverhang(0.1525f),
          restitution(0.85f),
          friction(0.3f) {}

    static TableConfig Default() {
        return TableConfig();
    }

    // Utility methods for table bounds
    float GetHalfLength() const { return length * 0.5f; }
    float GetHalfWidth() const { return width * 0.5f; }
};

} // namespace BasicTT
