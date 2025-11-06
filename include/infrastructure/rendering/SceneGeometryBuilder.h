#pragma once

#include "GeometryData.h"
#include "PrimitiveGeometry.h"
#include "domain/config/BallConfig.h"
#include "domain/config/PaddleConfig.h"
#include "domain/config/TableConfig.h"
#include <vector>

namespace BasicTT {

/**
 * @brief Describes a complete renderable object with geometry and material
 */
struct RenderableObject {
    GeometryData geometry;
    MaterialProperties material;
    Vector3 position;
    Quaternion rotation;
    std::string name;

    RenderableObject() : position(Vector3::Zero()), rotation(Quaternion::Identity()) {}
};

/**
 * @brief Builder for table tennis scene geometry
 *
 * Creates configured geometry for all scene objects (ball, paddle, table, room, etc.)
 * Preserves Unity's builder pattern while generating renderer-agnostic geometry data.
 */
class SceneGeometryBuilder {
public:
    /**
     * @brief Build ball geometry with spin visualization rings
     *
     * Creates sphere mesh for ball with optional spin indicator rings
     * (similar to Unity's BallBuilder with spin rings)
     *
     * @param config Ball configuration
     * @param withSpinRings Include visual spin rings
     * @return Vector of renderable objects (ball + optional rings)
     */
    static std::vector<RenderableObject> BuildBall(
        const BallConfig& config,
        bool withSpinRings = true);

    /**
     * @brief Build paddle geometry with blade, rubber, and handle
     *
     * Creates paddle with:
     * - Blade (wooden core)
     * - Forehand rubber (red)
     * - Backhand rubber (black)
     * - Handle (cylindrical grip)
     *
     * @param config Paddle configuration
     * @return Vector of renderable objects (blade, rubbers, handle)
     */
    static std::vector<RenderableObject> BuildPaddle(const PaddleConfig& config);

    /**
     * @brief Build table geometry with net
     *
     * Creates standard table tennis table with:
     * - Table surface (green/blue)
     * - Net (black mesh)
     * - Center line marking
     *
     * @param config Table configuration
     * @return Vector of renderable objects (table, net, lines)
     */
    static std::vector<RenderableObject> BuildTable(const TableConfig& config);

    /**
     * @brief Build room geometry (floor, walls, ceiling)
     *
     * Creates play space environment:
     * - Floor (red-tinted)
     * - Four walls (blue-tinted)
     * - Ceiling (white)
     *
     * @param roomSize Room dimensions (meters)
     * @param wallHeight Height of walls (meters)
     * @return Vector of renderable objects (floor, walls, ceiling)
     */
    static std::vector<RenderableObject> BuildRoom(
        float roomSize = 10.0f,
        float wallHeight = 3.0f);

    /**
     * @brief Build complete scene geometry
     *
     * Generates all scene objects in a single call.
     * Useful for initial scene setup.
     *
     * @param ballConfig Ball configuration
     * @param paddleConfig Paddle configuration
     * @param tableConfig Table configuration
     * @param roomSize Room size (meters)
     * @return Vector of all scene objects
     */
    static std::vector<RenderableObject> BuildCompleteScene(
        const BallConfig& ballConfig,
        const PaddleConfig& paddleConfig,
        const TableConfig& tableConfig,
        float roomSize = 10.0f);

private:
    // Helper: Create spin ring geometry (thin torus)
    static RenderableObject CreateSpinRing(
        float ballRadius,
        const Vector3& rotationEuler);
};

} // namespace BasicTT
