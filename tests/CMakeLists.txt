cmake_minimum_required(VERSION 3.20)

# Test executable
add_executable(BasicTT_Tests
    # Domain tests
    domain/physics/BallPhysicsTests.cpp
    domain/physics/CollisionDetectionTests.cpp
    domain/physics/RestitutionModelTests.cpp
    domain/filters/KalmanFilterTests.cpp
)

# Link against main project sources (create library first)
target_link_libraries(BasicTT_Tests
    PRIVATE
    GTest::gtest
    GTest::gtest_main
    spdlog::spdlog
    nlohmann_json::nlohmann_json
)

# Include directories
target_include_directories(BasicTT_Tests
    PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_SOURCE_DIR}/include
)

# Add domain sources directly (simpler than creating a library)
target_sources(BasicTT_Tests PRIVATE
    ${CMAKE_SOURCE_DIR}/src/domain/entities/BallState.cpp
    ${CMAKE_SOURCE_DIR}/src/domain/entities/PaddleState.cpp
    ${CMAKE_SOURCE_DIR}/src/domain/entities/CollisionData.cpp
    ${CMAKE_SOURCE_DIR}/src/domain/config/BallConfig.cpp
    ${CMAKE_SOURCE_DIR}/src/domain/config/PaddleConfig.cpp
    ${CMAKE_SOURCE_DIR}/src/domain/config/TableConfig.cpp
    ${CMAKE_SOURCE_DIR}/src/domain/config/PhysicsConfig.cpp
    ${CMAKE_SOURCE_DIR}/src/domain/physics/BallPhysicsOptimizedVerlet.cpp
    ${CMAKE_SOURCE_DIR}/src/domain/physics/BallPhysicsRK4.cpp
    ${CMAKE_SOURCE_DIR}/src/domain/physics/CollisionDetectionSystem.cpp
    ${CMAKE_SOURCE_DIR}/src/domain/physics/CollisionResolutionSystem.cpp
    ${CMAKE_SOURCE_DIR}/src/domain/physics/RestitutionModel.cpp
    ${CMAKE_SOURCE_DIR}/src/domain/logic/TableTennisSimulation.cpp
    ${CMAKE_SOURCE_DIR}/src/domain/utilities/Vector3.cpp
    ${CMAKE_SOURCE_DIR}/src/domain/utilities/Quaternion.cpp
    ${CMAKE_SOURCE_DIR}/src/domain/utilities/Matrix4x4.cpp
    ${CMAKE_SOURCE_DIR}/src/domain/utilities/CircularBuffer.cpp
    ${CMAKE_SOURCE_DIR}/src/domain/filters/KalmanFilterVector3.cpp
    ${CMAKE_SOURCE_DIR}/src/domain/filters/KalmanFilterQuaternion.cpp
)

# Discover tests
include(GoogleTest)
gtest_discover_tests(BasicTT_Tests)
