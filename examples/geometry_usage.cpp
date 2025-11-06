/**
 * @file geometry_usage.cpp
 * @brief Example showing how to use geometry builders for rendering setup
 *
 * This example demonstrates the intended usage of SceneGeometryBuilder
 * and PrimitiveGeometry when implementing a rendering backend.
 */

#include "infrastructure/rendering/SceneGeometryBuilder.h"
#include "infrastructure/rendering/PrimitiveGeometry.h"
#include "domain/config/BallConfig.h"
#include "domain/config/PaddleConfig.h"
#include "domain/config/TableConfig.h"
#include <iostream>

using namespace BasicTT;

/**
 * Example: Generate scene geometry and upload to rendering backend
 */
void ExampleCompleteSceneSetup() {
    std::cout << "=== Complete Scene Setup Example ===" << std::endl;

    // Load or create configurations
    BallConfig ballConfig = BallConfig::Default();
    PaddleConfig paddleConfig = PaddleConfig::Default();
    TableConfig tableConfig = TableConfig::Default();

    // Generate all scene geometry
    auto sceneObjects = SceneGeometryBuilder::BuildCompleteScene(
        ballConfig, paddleConfig, tableConfig, 10.0f);

    std::cout << "Generated " << sceneObjects.size() << " renderable objects:" << std::endl;

    // Example: Upload each object to rendering backend
    for (const auto& obj : sceneObjects) {
        std::cout << "  - " << obj.name
                  << " (" << obj.geometry.vertices.size() << " vertices, "
                  << obj.geometry.indices.size() << " indices)" << std::endl;

        // In actual renderer, you would:
        // 1. Create vertex buffer from obj.geometry.vertices
        // 2. Create index buffer from obj.geometry.indices
        // 3. Create material from obj.material
        // 4. Store position/rotation (obj.position, obj.rotation)
        // 5. Keep handle for later rendering
    }
}

/**
 * Example: Generate individual objects
 */
void ExampleIndividualObjects() {
    std::cout << "\n=== Individual Object Generation ===" << std::endl;

    BallConfig ballConfig = BallConfig::Default();

    // Generate ball with spin rings
    auto ballObjects = SceneGeometryBuilder::BuildBall(ballConfig, true);

    std::cout << "Ball object count: " << ballObjects.size() << std::endl;
    for (const auto& obj : ballObjects) {
        std::cout << "  - " << obj.name
                  << " (material: R=" << obj.material.r
                  << " G=" << obj.material.g
                  << " B=" << obj.material.b << ")" << std::endl;
    }

    // Generate table
    TableConfig tableConfig = TableConfig::Default();
    auto tableObjects = SceneGeometryBuilder::BuildTable(tableConfig);

    std::cout << "Table object count: " << tableObjects.size() << std::endl;
    for (const auto& obj : tableObjects) {
        std::cout << "  - " << obj.name
                  << " at position (" << obj.position.x
                  << ", " << obj.position.y
                  << ", " << obj.position.z << ")" << std::endl;
    }
}

/**
 * Example: Generate custom primitive geometry
 */
void ExampleCustomPrimitives() {
    std::cout << "\n=== Custom Primitive Generation ===" << std::endl;

    // Generate a sphere for custom use
    GeometryData sphere = PrimitiveGeometry::CreateSphere(0.5f, 32, 24);
    std::cout << "Custom sphere: " << sphere.vertices.size() << " vertices" << std::endl;

    // Generate a cube for custom use
    GeometryData cube = PrimitiveGeometry::CreateCube(1.0f, 2.0f, 1.0f);
    std::cout << "Custom cube: " << cube.vertices.size() << " vertices" << std::endl;

    // Access vertex data
    if (!sphere.vertices.empty()) {
        const auto& firstVertex = sphere.vertices[0];
        std::cout << "First vertex position: ("
                  << firstVertex.position.x << ", "
                  << firstVertex.position.y << ", "
                  << firstVertex.position.z << ")" << std::endl;
        std::cout << "First vertex normal: ("
                  << firstVertex.normal.x << ", "
                  << firstVertex.normal.y << ", "
                  << firstVertex.normal.z << ")" << std::endl;
        std::cout << "First vertex UV: ("
                  << firstVertex.u << ", "
                  << firstVertex.v << ")" << std::endl;
    }
}

/**
 * Example: Dynamic object updates (ball position during game)
 */
void ExampleDynamicUpdates() {
    std::cout << "\n=== Dynamic Update Pattern ===" << std::endl;

    // During game loop, update object transforms based on simulation
    BallConfig ballConfig = BallConfig::Default();
    auto ballObjects = SceneGeometryBuilder::BuildBall(ballConfig, true);

    // Simulate ball state from physics engine
    Vector3 simulatedBallPosition(0.5f, 1.2f, -0.3f);
    Quaternion simulatedBallRotation = Quaternion::FromAxisAngle(
        Vector3(0, 1, 0), 45.0f);

    std::cout << "Updating ball objects to simulated state..." << std::endl;

    for (auto& obj : ballObjects) {
        // Update object transform for rendering
        // (Geometry doesn't change, just position/rotation)
        obj.position = simulatedBallPosition;
        obj.rotation = simulatedBallRotation;

        std::cout << "  - " << obj.name
                  << " updated to (" << obj.position.x
                  << ", " << obj.position.y
                  << ", " << obj.position.z << ")" << std::endl;

        // In actual renderer, you would:
        // UpdateObjectTransform(obj.name, obj.position, obj.rotation);
    }
}

/**
 * Example: Renderer integration pseudocode
 */
void ExampleRendererIntegration() {
    std::cout << "\n=== Renderer Integration Pattern ===" << std::endl;

    std::cout << R"(
Typical rendering backend integration:

1. Scene Setup (once at startup):
   ----------------------------------------
   auto sceneObjects = SceneGeometryBuilder::BuildCompleteScene(...);

   for (const auto& obj : sceneObjects) {
       // Create GPU buffers
       VkBuffer vertexBuffer = CreateVertexBuffer(obj.geometry.vertices);
       VkBuffer indexBuffer = CreateIndexBuffer(obj.geometry.indices);

       // Create material/pipeline
       Pipeline pipeline = CreatePipeline(obj.material);

       // Store renderable
       renderables[obj.name] = {
           .vertexBuffer = vertexBuffer,
           .indexBuffer = indexBuffer,
           .pipeline = pipeline,
           .transform = CreateTransform(obj.position, obj.rotation)
       };
   }

2. Game Loop Updates:
   ----------------------------------------
   void UpdateFrame(const BallState& ballState) {
       // Update ball transform from simulation
       renderables["Ball"].transform = CreateTransform(
           ballState.position,
           ballState.rotation
       );

       // Spin rings rotate with ball
       renderables["SpinRing_Horizontal"].rotation = ballState.rotation;
       renderables["SpinRing_Vertical"].rotation = ballState.rotation;
   }

3. Render Frame:
   ----------------------------------------
   void RenderFrame() {
       for (const auto& [name, renderable] : renderables) {
           BindPipeline(renderable.pipeline);
           BindVertexBuffer(renderable.vertexBuffer);
           BindIndexBuffer(renderable.indexBuffer);
           UpdateTransformUniform(renderable.transform);
           DrawIndexed(renderable.indexCount);
       }
   }
)" << std::endl;
}

int main() {
    std::cout << "Geometry Builder Usage Examples\n" << std::endl;

    ExampleCompleteSceneSetup();
    ExampleIndividualObjects();
    ExampleCustomPrimitives();
    ExampleDynamicUpdates();
    ExampleRendererIntegration();

    std::cout << "\nNote: This example demonstrates API usage." << std::endl;
    std::cout << "Actual rendering requires implementing a graphics backend." << std::endl;

    return 0;
}
