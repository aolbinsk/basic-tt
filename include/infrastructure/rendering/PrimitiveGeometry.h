#pragma once

#include "GeometryData.h"
#include "domain/utilities/Vector3.h"

namespace BasicTT {

/**
 * @brief Utility for generating primitive mesh geometries
 *
 * Generates vertex and index data for common primitives (sphere, cube, cylinder, etc.)
 * Similar to Unity's CreatePrimitive but returns raw geometry data.
 */
class PrimitiveGeometry {
public:
    /**
     * @brief Generate a UV sphere
     * @param radius Sphere radius
     * @param segments Horizontal segments (longitude)
     * @param rings Vertical segments (latitude)
     * @return Geometry data for the sphere
     */
    static GeometryData CreateSphere(float radius = 0.5f, int segments = 24, int rings = 16);

    /**
     * @brief Generate a cube (box)
     * @param width Size in X dimension
     * @param height Size in Y dimension
     * @param depth Size in Z dimension
     * @return Geometry data for the cube
     */
    static GeometryData CreateCube(float width = 1.0f, float height = 1.0f, float depth = 1.0f);

    /**
     * @brief Generate a cylinder
     * @param radius Cylinder radius
     * @param height Cylinder height (along Y axis)
     * @param segments Number of radial segments
     * @return Geometry data for the cylinder
     */
    static GeometryData CreateCylinder(float radius = 0.5f, float height = 1.0f, int segments = 24);

    /**
     * @brief Generate a plane
     * @param width Size in X dimension
     * @param depth Size in Z dimension
     * @param widthSegments Subdivisions along X
     * @param depthSegments Subdivisions along Z
     * @return Geometry data for the plane (facing +Y)
     */
    static GeometryData CreatePlane(float width = 1.0f, float depth = 1.0f,
                                    int widthSegments = 1, int depthSegments = 1);

    /**
     * @brief Generate a torus (ring)
     * @param majorRadius Radius from center to tube center
     * @param minorRadius Tube radius
     * @param majorSegments Segments around major circle
     * @param minorSegments Segments around tube
     * @return Geometry data for the torus
     */
    static GeometryData CreateTorus(float majorRadius = 0.5f, float minorRadius = 0.1f,
                                    int majorSegments = 24, int minorSegments = 12);
};

} // namespace BasicTT
