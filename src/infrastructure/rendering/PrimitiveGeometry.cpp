#include "../../../include/infrastructure/rendering/PrimitiveGeometry.h"
#include <cmath>

namespace BasicTT {

GeometryData PrimitiveGeometry::CreateSphere(float radius, int segments, int rings) {
    GeometryData geometry;

    // Calculate vertex count
    int vertexCount = (rings + 1) * (segments + 1);
    geometry.vertices.reserve(vertexCount);

    // Generate vertices
    for (int ring = 0; ring <= rings; ring++) {
        float phi = static_cast<float>(ring) / static_cast<float>(rings) * 3.14159265359f;
        float sinPhi = std::sin(phi);
        float cosPhi = std::cos(phi);

        for (int segment = 0; segment <= segments; segment++) {
            float theta = static_cast<float>(segment) / static_cast<float>(segments) * 2.0f * 3.14159265359f;
            float sinTheta = std::sin(theta);
            float cosTheta = std::cos(theta);

            // Position
            Vector3 position(
                radius * sinPhi * cosTheta,
                radius * cosPhi,
                radius * sinPhi * sinTheta
            );

            // Normal (normalized position for sphere)
            Vector3 normal = position.Normalized();

            // UV coordinates
            float u = static_cast<float>(segment) / static_cast<float>(segments);
            float v = static_cast<float>(ring) / static_cast<float>(rings);

            geometry.vertices.emplace_back(position, normal, u, v);
        }
    }

    // Generate indices
    for (int ring = 0; ring < rings; ring++) {
        for (int segment = 0; segment < segments; segment++) {
            int current = ring * (segments + 1) + segment;
            int next = current + segments + 1;

            // First triangle
            geometry.indices.push_back(current);
            geometry.indices.push_back(next);
            geometry.indices.push_back(current + 1);

            // Second triangle
            geometry.indices.push_back(current + 1);
            geometry.indices.push_back(next);
            geometry.indices.push_back(next + 1);
        }
    }

    return geometry;
}

GeometryData PrimitiveGeometry::CreateCube(float width, float height, float depth) {
    GeometryData geometry;

    float hw = width * 0.5f;
    float hh = height * 0.5f;
    float hd = depth * 0.5f;

    // Front face (+Z)
    geometry.vertices.emplace_back(Vector3(-hw, -hh, hd), Vector3(0, 0, 1), 0, 0);
    geometry.vertices.emplace_back(Vector3(hw, -hh, hd), Vector3(0, 0, 1), 1, 0);
    geometry.vertices.emplace_back(Vector3(hw, hh, hd), Vector3(0, 0, 1), 1, 1);
    geometry.vertices.emplace_back(Vector3(-hw, hh, hd), Vector3(0, 0, 1), 0, 1);

    // Back face (-Z)
    geometry.vertices.emplace_back(Vector3(hw, -hh, -hd), Vector3(0, 0, -1), 0, 0);
    geometry.vertices.emplace_back(Vector3(-hw, -hh, -hd), Vector3(0, 0, -1), 1, 0);
    geometry.vertices.emplace_back(Vector3(-hw, hh, -hd), Vector3(0, 0, -1), 1, 1);
    geometry.vertices.emplace_back(Vector3(hw, hh, -hd), Vector3(0, 0, -1), 0, 1);

    // Left face (-X)
    geometry.vertices.emplace_back(Vector3(-hw, -hh, -hd), Vector3(-1, 0, 0), 0, 0);
    geometry.vertices.emplace_back(Vector3(-hw, -hh, hd), Vector3(-1, 0, 0), 1, 0);
    geometry.vertices.emplace_back(Vector3(-hw, hh, hd), Vector3(-1, 0, 0), 1, 1);
    geometry.vertices.emplace_back(Vector3(-hw, hh, -hd), Vector3(-1, 0, 0), 0, 1);

    // Right face (+X)
    geometry.vertices.emplace_back(Vector3(hw, -hh, hd), Vector3(1, 0, 0), 0, 0);
    geometry.vertices.emplace_back(Vector3(hw, -hh, -hd), Vector3(1, 0, 0), 1, 0);
    geometry.vertices.emplace_back(Vector3(hw, hh, -hd), Vector3(1, 0, 0), 1, 1);
    geometry.vertices.emplace_back(Vector3(hw, hh, hd), Vector3(1, 0, 0), 0, 1);

    // Top face (+Y)
    geometry.vertices.emplace_back(Vector3(-hw, hh, hd), Vector3(0, 1, 0), 0, 0);
    geometry.vertices.emplace_back(Vector3(hw, hh, hd), Vector3(0, 1, 0), 1, 0);
    geometry.vertices.emplace_back(Vector3(hw, hh, -hd), Vector3(0, 1, 0), 1, 1);
    geometry.vertices.emplace_back(Vector3(-hw, hh, -hd), Vector3(0, 1, 0), 0, 1);

    // Bottom face (-Y)
    geometry.vertices.emplace_back(Vector3(-hw, -hh, -hd), Vector3(0, -1, 0), 0, 0);
    geometry.vertices.emplace_back(Vector3(hw, -hh, -hd), Vector3(0, -1, 0), 1, 0);
    geometry.vertices.emplace_back(Vector3(hw, -hh, hd), Vector3(0, -1, 0), 1, 1);
    geometry.vertices.emplace_back(Vector3(-hw, -hh, hd), Vector3(0, -1, 0), 0, 1);

    // Indices for all faces (2 triangles per face)
    for (uint32_t face = 0; face < 6; face++) {
        uint32_t base = face * 4;
        geometry.indices.push_back(base + 0);
        geometry.indices.push_back(base + 1);
        geometry.indices.push_back(base + 2);

        geometry.indices.push_back(base + 0);
        geometry.indices.push_back(base + 2);
        geometry.indices.push_back(base + 3);
    }

    return geometry;
}

GeometryData PrimitiveGeometry::CreateCylinder(float radius, float height, int segments) {
    GeometryData geometry;

    float halfHeight = height * 0.5f;

    // Top and bottom cap centers
    int topCenterIdx = 0;
    int bottomCenterIdx = 1;

    geometry.vertices.emplace_back(Vector3(0, halfHeight, 0), Vector3(0, 1, 0), 0.5f, 0.5f);
    geometry.vertices.emplace_back(Vector3(0, -halfHeight, 0), Vector3(0, -1, 0), 0.5f, 0.5f);

    // Generate side vertices (duplicated for top/bottom caps)
    for (int i = 0; i <= segments; i++) {
        float angle = static_cast<float>(i) / static_cast<float>(segments) * 2.0f * 3.14159265359f;
        float cosAngle = std::cos(angle);
        float sinAngle = std::sin(angle);

        float u = static_cast<float>(i) / static_cast<float>(segments);

        Vector3 position(radius * cosAngle, 0, radius * sinAngle);
        Vector3 normal(cosAngle, 0, sinAngle);

        // Top side vertex
        geometry.vertices.emplace_back(
            Vector3(position.x, halfHeight, position.z),
            normal, u, 0.0f
        );

        // Bottom side vertex
        geometry.vertices.emplace_back(
            Vector3(position.x, -halfHeight, position.z),
            normal, u, 1.0f
        );
    }

    int sideStartIdx = 2;

    // Side indices
    for (int i = 0; i < segments; i++) {
        int topCurrent = sideStartIdx + i * 2;
        int bottomCurrent = topCurrent + 1;
        int topNext = topCurrent + 2;
        int bottomNext = bottomCurrent + 2;

        geometry.indices.push_back(topCurrent);
        geometry.indices.push_back(bottomCurrent);
        geometry.indices.push_back(topNext);

        geometry.indices.push_back(topNext);
        geometry.indices.push_back(bottomCurrent);
        geometry.indices.push_back(bottomNext);
    }

    // Cap vertices (separate from sides for proper normals)
    int topCapStartIdx = static_cast<int>(geometry.vertices.size());
    for (int i = 0; i <= segments; i++) {
        float angle = static_cast<float>(i) / static_cast<float>(segments) * 2.0f * 3.14159265359f;
        float cosAngle = std::cos(angle);
        float sinAngle = std::sin(angle);

        geometry.vertices.emplace_back(
            Vector3(radius * cosAngle, halfHeight, radius * sinAngle),
            Vector3(0, 1, 0), 0, 0
        );
    }

    int bottomCapStartIdx = static_cast<int>(geometry.vertices.size());
    for (int i = 0; i <= segments; i++) {
        float angle = static_cast<float>(i) / static_cast<float>(segments) * 2.0f * 3.14159265359f;
        float cosAngle = std::cos(angle);
        float sinAngle = std::sin(angle);

        geometry.vertices.emplace_back(
            Vector3(radius * cosAngle, -halfHeight, radius * sinAngle),
            Vector3(0, -1, 0), 0, 0
        );
    }

    // Top cap indices
    for (int i = 0; i < segments; i++) {
        geometry.indices.push_back(topCenterIdx);
        geometry.indices.push_back(topCapStartIdx + i);
        geometry.indices.push_back(topCapStartIdx + i + 1);
    }

    // Bottom cap indices (reversed winding)
    for (int i = 0; i < segments; i++) {
        geometry.indices.push_back(bottomCenterIdx);
        geometry.indices.push_back(bottomCapStartIdx + i + 1);
        geometry.indices.push_back(bottomCapStartIdx + i);
    }

    return geometry;
}

GeometryData PrimitiveGeometry::CreatePlane(float width, float depth, int widthSegments, int depthSegments) {
    GeometryData geometry;

    float halfWidth = width * 0.5f;
    float halfDepth = depth * 0.5f;

    // Generate vertices
    for (int z = 0; z <= depthSegments; z++) {
        for (int x = 0; x <= widthSegments; x++) {
            float xPos = (static_cast<float>(x) / widthSegments - 0.5f) * width;
            float zPos = (static_cast<float>(z) / depthSegments - 0.5f) * depth;

            float u = static_cast<float>(x) / widthSegments;
            float v = static_cast<float>(z) / depthSegments;

            geometry.vertices.emplace_back(
                Vector3(xPos, 0, zPos),
                Vector3(0, 1, 0),  // Normal pointing up
                u, v
            );
        }
    }

    // Generate indices
    for (int z = 0; z < depthSegments; z++) {
        for (int x = 0; x < widthSegments; x++) {
            int topLeft = z * (widthSegments + 1) + x;
            int topRight = topLeft + 1;
            int bottomLeft = topLeft + (widthSegments + 1);
            int bottomRight = bottomLeft + 1;

            geometry.indices.push_back(topLeft);
            geometry.indices.push_back(bottomLeft);
            geometry.indices.push_back(topRight);

            geometry.indices.push_back(topRight);
            geometry.indices.push_back(bottomLeft);
            geometry.indices.push_back(bottomRight);
        }
    }

    return geometry;
}

GeometryData PrimitiveGeometry::CreateTorus(float majorRadius, float minorRadius, int majorSegments, int minorSegments) {
    GeometryData geometry;

    // Generate vertices
    for (int i = 0; i <= majorSegments; i++) {
        float theta = static_cast<float>(i) / static_cast<float>(majorSegments) * 2.0f * 3.14159265359f;
        float cosTheta = std::cos(theta);
        float sinTheta = std::sin(theta);

        for (int j = 0; j <= minorSegments; j++) {
            float phi = static_cast<float>(j) / static_cast<float>(minorSegments) * 2.0f * 3.14159265359f;
            float cosPhi = std::cos(phi);
            float sinPhi = std::sin(phi);

            // Position
            float x = (majorRadius + minorRadius * cosPhi) * cosTheta;
            float y = minorRadius * sinPhi;
            float z = (majorRadius + minorRadius * cosPhi) * sinTheta;

            Vector3 position(x, y, z);

            // Normal
            Vector3 center(majorRadius * cosTheta, 0, majorRadius * sinTheta);
            Vector3 normal = (position - center).Normalized();

            // UV coordinates
            float u = static_cast<float>(i) / static_cast<float>(majorSegments);
            float v = static_cast<float>(j) / static_cast<float>(minorSegments);

            geometry.vertices.emplace_back(position, normal, u, v);
        }
    }

    // Generate indices
    for (int i = 0; i < majorSegments; i++) {
        for (int j = 0; j < minorSegments; j++) {
            int current = i * (minorSegments + 1) + j;
            int next = current + minorSegments + 1;

            geometry.indices.push_back(current);
            geometry.indices.push_back(next);
            geometry.indices.push_back(current + 1);

            geometry.indices.push_back(current + 1);
            geometry.indices.push_back(next);
            geometry.indices.push_back(next + 1);
        }
    }

    return geometry;
}

} // namespace BasicTT
