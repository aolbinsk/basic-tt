#pragma once

#include "domain/utilities/Vector3.h"
#include <vector>

namespace BasicTT {

/**
 * @brief Vertex data for rendering
 */
struct Vertex {
    Vector3 position;
    Vector3 normal;
    float u, v;  // Texture coordinates

    Vertex(const Vector3& pos, const Vector3& norm, float u_ = 0.0f, float v_ = 0.0f)
        : position(pos), normal(norm), u(u_), v(v_) {}
};

/**
 * @brief Mesh geometry data (vertices and indices)
 *
 * Generic mesh representation that can be used with any rendering backend
 * (Vulkan, OpenGL, etc.)
 */
struct GeometryData {
    std::vector<Vertex> vertices;
    std::vector<uint32_t> indices;

    void Clear() {
        vertices.clear();
        indices.clear();
    }

    void Reserve(size_t vertexCount, size_t indexCount) {
        vertices.reserve(vertexCount);
        indices.reserve(indexCount);
    }
};

/**
 * @brief Material properties for rendering
 */
struct MaterialProperties {
    float r, g, b, a;  // Color (RGBA)
    float metallic;
    float roughness;

    MaterialProperties()
        : r(1.0f), g(1.0f), b(1.0f), a(1.0f)
        , metallic(0.0f), roughness(0.5f) {}

    static MaterialProperties White() {
        MaterialProperties mat;
        mat.r = mat.g = mat.b = 1.0f;
        return mat;
    }

    static MaterialProperties Black() {
        MaterialProperties mat;
        mat.r = mat.g = mat.b = 0.0f;
        return mat;
    }

    static MaterialProperties Gray(float value = 0.5f) {
        MaterialProperties mat;
        mat.r = mat.g = mat.b = value;
        return mat;
    }

    static MaterialProperties Color(float r, float g, float b) {
        MaterialProperties mat;
        mat.r = r;
        mat.g = g;
        mat.b = b;
        return mat;
    }

    static MaterialProperties Red() { return Color(1.0f, 0.0f, 0.0f); }
    static MaterialProperties Green() { return Color(0.0f, 1.0f, 0.0f); }
    static MaterialProperties Blue() { return Color(0.0f, 0.0f, 1.0f); }
};

} // namespace BasicTT
