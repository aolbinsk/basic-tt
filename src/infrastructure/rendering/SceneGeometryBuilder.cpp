#include "../../../include/infrastructure/rendering/SceneGeometryBuilder.h"

namespace BasicTT {

std::vector<RenderableObject> SceneGeometryBuilder::BuildBall(
    const BallConfig& config,
    bool withSpinRings) {

    std::vector<RenderableObject> objects;

    // Main ball sphere
    RenderableObject ball;
    ball.name = "Ball";
    ball.geometry = PrimitiveGeometry::CreateSphere(config.radius, 24, 16);
    ball.material = MaterialProperties::White();
    ball.position = Vector3::Zero();
    ball.rotation = Quaternion::Identity();
    objects.push_back(ball);

    // Spin visualization rings (similar to Unity's CreateSpinRing)
    if (withSpinRings) {
        // Horizontal ring
        RenderableObject ring1 = CreateSpinRing(config.radius, Vector3(90, 0, 0));
        ring1.name = "SpinRing_Horizontal";
        objects.push_back(ring1);

        // Vertical ring
        RenderableObject ring2 = CreateSpinRing(config.radius, Vector3(0, 0, 0));
        ring2.name = "SpinRing_Vertical";
        objects.push_back(ring2);
    }

    return objects;
}

std::vector<RenderableObject> SceneGeometryBuilder::BuildPaddle(const PaddleConfig& config) {
    std::vector<RenderableObject> objects;

    // Blade (wooden core)
    RenderableObject blade;
    blade.name = "PaddleBlade";
    blade.geometry = PrimitiveGeometry::CreateCube(
        config.bladeWidth,
        config.bladeHeight,
        config.bladeThickness
    );
    blade.material = MaterialProperties::Gray(0.6f);  // Wood-like gray
    blade.position = Vector3::Zero();
    blade.rotation = Quaternion::Identity();
    objects.push_back(blade);

    // Forehand rubber (red, slightly offset from blade)
    RenderableObject forehandRubber;
    forehandRubber.name = "ForehandRubber";
    forehandRubber.geometry = PrimitiveGeometry::CreateCube(
        config.bladeWidth * 0.9f,
        config.bladeHeight * 0.9f,
        0.002f  // Thin rubber layer
    );
    forehandRubber.material = MaterialProperties::Red();
    forehandRubber.position = Vector3(0, 0, config.bladeThickness * 0.5f + 0.001f);
    forehandRubber.rotation = Quaternion::Identity();
    objects.push_back(forehandRubber);

    // Backhand rubber (black, opposite side)
    RenderableObject backhandRubber;
    backhandRubber.name = "BackhandRubber";
    backhandRubber.geometry = PrimitiveGeometry::CreateCube(
        config.bladeWidth * 0.9f,
        config.bladeHeight * 0.9f,
        0.002f
    );
    backhandRubber.material = MaterialProperties::Black();
    backhandRubber.position = Vector3(0, 0, -(config.bladeThickness * 0.5f + 0.001f));
    backhandRubber.rotation = Quaternion::Identity();
    objects.push_back(backhandRubber);

    // Handle (cylindrical)
    RenderableObject handle;
    handle.name = "PaddleHandle";
    handle.geometry = PrimitiveGeometry::CreateCylinder(
        config.handleRadius,
        config.handleLength,
        16
    );
    handle.material = MaterialProperties::Gray(0.5f);

    // Position handle below blade (along -Y)
    float handleOffset = config.bladeHeight * 0.5f + config.handleLength * 0.5f;
    handle.position = Vector3(0, -handleOffset, 0);

    // Rotate 90 degrees so cylinder extends along Y axis (Unity cylinder is Y-aligned)
    handle.rotation = Quaternion::FromAxisAngle(Vector3(1, 0, 0), 90.0f);
    objects.push_back(handle);

    return objects;
}

std::vector<RenderableObject> SceneGeometryBuilder::BuildTable(const TableConfig& config) {
    std::vector<RenderableObject> objects;

    // Table surface
    RenderableObject tableSurface;
    tableSurface.name = "TableSurface";
    tableSurface.geometry = PrimitiveGeometry::CreateCube(
        config.length,
        0.02f,  // Thin table top
        config.width
    );
    tableSurface.material = MaterialProperties::Color(0.0f, 0.5f, 0.0f);  // Dark green
    tableSurface.position = Vector3(0, config.height, 0);
    tableSurface.rotation = Quaternion::Identity();
    objects.push_back(tableSurface);

    // Net (thin vertical surface at x=0)
    RenderableObject net;
    net.name = "Net";
    net.geometry = PrimitiveGeometry::CreateCube(
        0.01f,  // Thin net
        config.netHeight,
        config.width
    );
    net.material = MaterialProperties::Gray(0.2f);  // Dark gray net
    net.position = Vector3(0, config.height + config.netHeight * 0.5f, 0);
    net.rotation = Quaternion::Identity();
    objects.push_back(net);

    // Center line (white marking)
    RenderableObject centerLine;
    centerLine.name = "CenterLine";
    centerLine.geometry = PrimitiveGeometry::CreateCube(
        config.length,
        0.001f,  // Very thin line
        0.003f   // 3mm wide line
    );
    centerLine.material = MaterialProperties::White();
    centerLine.position = Vector3(0, config.height + 0.011f, 0);  // Slightly above surface
    centerLine.rotation = Quaternion::Identity();
    objects.push_back(centerLine);

    return objects;
}

std::vector<RenderableObject> SceneGeometryBuilder::BuildRoom(float roomSize, float wallHeight) {
    std::vector<RenderableObject> objects;

    float halfRoom = roomSize * 0.5f;
    const float wallThickness = 0.1f;

    // Floor
    RenderableObject floor;
    floor.name = "Floor";
    floor.geometry = PrimitiveGeometry::CreateCube(roomSize, 0.1f, roomSize);
    floor.material = MaterialProperties::Color(0.8f, 0.1f, 0.1f);  // Red-tinted
    floor.position = Vector3(0, -0.05f, 0);
    floor.rotation = Quaternion::Identity();
    objects.push_back(floor);

    // Back wall (-Z)
    RenderableObject backWall;
    backWall.name = "BackWall";
    backWall.geometry = PrimitiveGeometry::CreateCube(roomSize, wallHeight, wallThickness);
    backWall.material = MaterialProperties::Color(0.1f, 0.1f, 0.8f);  // Blue-tinted
    backWall.position = Vector3(0, wallHeight * 0.5f, -halfRoom);
    backWall.rotation = Quaternion::Identity();
    objects.push_back(backWall);

    // Front wall (+Z)
    RenderableObject frontWall;
    frontWall.name = "FrontWall";
    frontWall.geometry = PrimitiveGeometry::CreateCube(roomSize, wallHeight, wallThickness);
    frontWall.material = MaterialProperties::Color(0.1f, 0.1f, 0.8f);
    frontWall.position = Vector3(0, wallHeight * 0.5f, halfRoom);
    frontWall.rotation = Quaternion::Identity();
    objects.push_back(frontWall);

    // Left wall (-X)
    RenderableObject leftWall;
    leftWall.name = "LeftWall";
    leftWall.geometry = PrimitiveGeometry::CreateCube(wallThickness, wallHeight, roomSize);
    leftWall.material = MaterialProperties::Color(0.1f, 0.1f, 0.8f);
    leftWall.position = Vector3(-halfRoom, wallHeight * 0.5f, 0);
    leftWall.rotation = Quaternion::Identity();
    objects.push_back(leftWall);

    // Right wall (+X)
    RenderableObject rightWall;
    rightWall.name = "RightWall";
    rightWall.geometry = PrimitiveGeometry::CreateCube(wallThickness, wallHeight, roomSize);
    rightWall.material = MaterialProperties::Color(0.1f, 0.1f, 0.8f);
    rightWall.position = Vector3(halfRoom, wallHeight * 0.5f, 0);
    rightWall.rotation = Quaternion::Identity();
    objects.push_back(rightWall);

    // Ceiling (using plane)
    RenderableObject ceiling;
    ceiling.name = "Ceiling";
    ceiling.geometry = PrimitiveGeometry::CreatePlane(roomSize, roomSize, 1, 1);
    ceiling.material = MaterialProperties::White();
    ceiling.position = Vector3(0, wallHeight, 0);
    // Rotate plane to face downward (Unity plane faces +Y, we want -Y)
    ceiling.rotation = Quaternion::FromAxisAngle(Vector3(1, 0, 0), 180.0f);
    objects.push_back(ceiling);

    return objects;
}

std::vector<RenderableObject> SceneGeometryBuilder::BuildCompleteScene(
    const BallConfig& ballConfig,
    const PaddleConfig& paddleConfig,
    const TableConfig& tableConfig,
    float roomSize) {

    std::vector<RenderableObject> scene;

    // Add room
    auto room = BuildRoom(roomSize, 3.0f);
    scene.insert(scene.end(), room.begin(), room.end());

    // Add table
    auto table = BuildTable(tableConfig);
    scene.insert(scene.end(), table.begin(), table.end());

    // Add ball (positioned above table)
    auto ball = BuildBall(ballConfig, true);
    for (auto& obj : ball) {
        obj.position = Vector3(0, tableConfig.height + 0.5f, 0);
    }
    scene.insert(scene.end(), ball.begin(), ball.end());

    // Add two paddles (left and right sides of table)
    auto leftPaddle = BuildPaddle(paddleConfig);
    for (auto& obj : leftPaddle) {
        obj.position = obj.position + Vector3(-1.0f, tableConfig.height + 0.3f, 0.5f);
    }
    scene.insert(scene.end(), leftPaddle.begin(), leftPaddle.end());

    auto rightPaddle = BuildPaddle(paddleConfig);
    for (auto& obj : rightPaddle) {
        obj.position = obj.position + Vector3(1.0f, tableConfig.height + 0.3f, -0.5f);
    }
    scene.insert(scene.end(), rightPaddle.begin(), rightPaddle.end());

    return scene;
}

RenderableObject SceneGeometryBuilder::CreateSpinRing(
    float ballRadius,
    const Vector3& rotationEuler) {

    RenderableObject ring;
    ring.name = "SpinRing";

    // Create thin torus for spin visualization
    float ringRadius = ballRadius + 0.0005f;  // Slightly larger than ball
    float tubeRadius = 0.001f;  // Very thin tube

    ring.geometry = PrimitiveGeometry::CreateTorus(ringRadius, tubeRadius, 24, 8);
    ring.material = MaterialProperties::Black();
    ring.position = Vector3::Zero();

    // Apply rotation (Unity uses Euler angles for spin rings)
    // Convert Euler angles (degrees) to quaternion
    float pitch = rotationEuler.x * 3.14159265359f / 180.0f;
    float yaw = rotationEuler.y * 3.14159265359f / 180.0f;
    float roll = rotationEuler.z * 3.14159265359f / 180.0f;

    Quaternion qx = Quaternion::FromAxisAngle(Vector3(1, 0, 0), pitch);
    Quaternion qy = Quaternion::FromAxisAngle(Vector3(0, 1, 0), yaw);
    Quaternion qz = Quaternion::FromAxisAngle(Vector3(0, 0, 1), roll);

    ring.rotation = qy * qx * qz;  // YXZ order (typical Euler order)

    return ring;
}

} // namespace BasicTT
