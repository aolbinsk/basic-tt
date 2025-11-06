cmake_minimum_required(VERSION 3.20)
project(BasicTT_OpenXR VERSION 1.0.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# Build options
option(BUILD_TESTS "Build unit tests" OFF)
option(USE_VULKAN "Use Vulkan for rendering (otherwise OpenGL)" ON)

# Find packages
find_package(OpenXR REQUIRED)
find_package(spdlog CONFIG REQUIRED)
find_package(nlohmann_json CONFIG REQUIRED)

if(BUILD_TESTS)
    find_package(GTest CONFIG REQUIRED)
    enable_testing()
endif()

if(USE_VULKAN)
    find_package(Vulkan REQUIRED)
    add_compile_definitions(USE_VULKAN)
else()
    find_package(OpenGL REQUIRED)
    add_compile_definitions(USE_OPENGL)
endif()

# Include directories
include_directories(
    ${CMAKE_CURRENT_SOURCE_DIR}/src
    ${CMAKE_CURRENT_SOURCE_DIR}/include
    ${OPENXR_INCLUDE_DIRS}
)

# Source files
set(DOMAIN_SOURCES
    src/domain/entities/BallState.cpp
    src/domain/entities/PaddleState.cpp
    src/domain/entities/CollisionData.cpp
    src/domain/entities/ControllerState.cpp
    src/domain/config/BallConfig.cpp
    src/domain/config/PaddleConfig.cpp
    src/domain/config/TableConfig.cpp
    src/domain/config/PhysicsConfig.cpp
    src/domain/physics/BallPhysicsOptimizedVerlet.cpp
    src/domain/physics/BallPhysicsRK4.cpp
    src/domain/physics/CollisionDetectionSystem.cpp
    src/domain/physics/CollisionResolutionSystem.cpp
    src/domain/physics/RestitutionModel.cpp
    src/domain/logic/TableTennisSimulation.cpp
    src/domain/utilities/CircularBuffer.cpp
    src/domain/utilities/Vector3.cpp
    src/domain/utilities/Quaternion.cpp
    src/domain/utilities/Matrix4x4.cpp
    src/domain/filters/KalmanFilterVector3.cpp
    src/domain/filters/KalmanFilterQuaternion.cpp
)

set(INFRASTRUCTURE_SOURCES
    src/infrastructure/openxr/OpenXRManager.cpp
    src/infrastructure/openxr/OpenXRInputManager.cpp
    src/infrastructure/openxr/OpenXRSwapchain.cpp
    src/infrastructure/rendering/Renderer.cpp
    src/infrastructure/rendering/Mesh.cpp
    src/infrastructure/rendering/Shader.cpp
    src/infrastructure/rendering/Material.cpp
    src/infrastructure/rendering/PrimitiveGeometry.cpp
    src/infrastructure/rendering/SceneGeometryBuilder.cpp
    src/infrastructure/game/GameLoop.cpp
    src/infrastructure/game/SimulationBridge.cpp
    src/infrastructure/utilities/Logger.cpp
    src/infrastructure/utilities/ConfigManager.cpp
)

set(MAIN_SOURCES
    src/main.cpp
)

# Create executable
add_executable(${PROJECT_NAME}
    ${DOMAIN_SOURCES}
    ${INFRASTRUCTURE_SOURCES}
    ${MAIN_SOURCES}
)

# Link libraries
if(USE_VULKAN)
    target_link_libraries(${PROJECT_NAME}
        PRIVATE
        OpenXR::openxr_loader
        Vulkan::Vulkan
        spdlog::spdlog
        nlohmann_json::nlohmann_json
    )
else()
    target_link_libraries(${PROJECT_NAME}
        PRIVATE
        OpenXR::openxr_loader
        OpenGL::GL
        spdlog::spdlog
        nlohmann_json::nlohmann_json
    )
endif()

# Platform-specific settings
if(WIN32)
    target_link_libraries(${PROJECT_NAME} PRIVATE advapi32)
elseif(UNIX AND NOT APPLE)
    target_link_libraries(${PROJECT_NAME} PRIVATE pthread dl)
endif()

# Compiler warnings
if(MSVC)
    target_compile_options(${PROJECT_NAME} PRIVATE /W4 /WX-)
else()
    target_compile_options(${PROJECT_NAME} PRIVATE -Wall -Wextra -pedantic)
endif()

# Install targets
install(TARGETS ${PROJECT_NAME}
    RUNTIME DESTINATION bin
)

# Tests
if(BUILD_TESTS)
    add_subdirectory(tests)
endif()
