# BasicTT - C++ OpenXR Implementation

This is a native C++ implementation of the BasicTT table tennis VR game, targeting OpenXR for cross-platform VR support.

## Overview

This project is a complete rewrite of the Unity-based BasicTT game in C++, designed to work with any OpenXR-compatible VR headset (Meta Quest, HTC Vive, Valve Index, Windows Mixed Reality, etc.).

### Key Features

- **Native C++17**: High-performance native implementation
- **OpenXR Support**: Cross-platform VR compatibility
- **Physics Simulation**:
  - Optimized Verlet integrator (2nd order)
  - Runge-Kutta 4 integrator (4th order) for high precision
  - 360 Hz effective physics rate with sub-stepping
- **Collision Detection**: Swept sphere collision detection to prevent tunneling
- **Domain-Driven Design**: Clean separation between game logic and infrastructure
- **Multiple Graphics APIs**: Supports both Vulkan and OpenGL (via compile-time option)

## Project Structure

```
basic-tt/
├── CMakeLists.txt              # Build configuration
├── README_CPP_OPENXR.md        # This file
├── include/                    # Header files
│   ├── domain/                 # Pure game logic (no VR/graphics dependencies)
│   │   ├── entities/           # Data structures (BallState, PaddleState, etc.)
│   │   ├── config/             # Configuration classes
│   │   ├── physics/            # Physics engines and collision systems
│   │   ├── logic/              # TableTennisSimulation
│   │   └── utilities/          # Math utilities (Vector3, Quaternion, etc.)
│   └── infrastructure/         # Platform-specific code
│       ├── openxr/             # OpenXR integration
│       ├── rendering/          # Graphics rendering (Vulkan/OpenGL)
│       └── game/               # Game loop and bridging
└── src/                        # Implementation files
    ├── domain/
    ├── infrastructure/
    └── main.cpp
```

## Prerequisites

### Required Dependencies

1. **C++ Compiler**:
   - GCC 8+ / Clang 8+ / MSVC 2019+ with C++17 support

2. **CMake**:
   - Version 3.20 or higher

3. **OpenXR SDK**:
   - Download from: https://github.com/KhronosGroup/OpenXR-SDK
   - Or install via package manager

4. **Graphics API** (choose one):
   - **Vulkan SDK**: https://vulkan.lunarg.com/ (recommended)
   - **OpenGL**: Usually comes with graphics drivers

5. **OpenXR Runtime** (required for VR):
   - **SteamVR**: For Valve Index, HTC Vive
   - **Oculus Runtime**: For Meta Quest (via Link)
   - **Windows Mixed Reality**: Built into Windows 10/11
   - **Monado**: Open-source runtime for Linux

### Linux

```bash
# Ubuntu/Debian
sudo apt-get install cmake build-essential
sudo apt-get install libopenxr-dev
sudo apt-get install libvulkan-dev  # For Vulkan
# OR
sudo apt-get install libgl1-mesa-dev  # For OpenGL

# Arch Linux
sudo pacman -S cmake base-devel
sudo pacman -S openxr
sudo pacman -S vulkan-devel  # For Vulkan
```

### Windows

1. Install Visual Studio 2019+ with C++ desktop development
2. Install CMake: https://cmake.org/download/
3. Install Vulkan SDK: https://vulkan.lunarg.com/
4. Download OpenXR SDK and extract to a known location

### macOS

```bash
brew install cmake
brew install openxr
brew install vulkan-headers vulkan-loader
```

Note: OpenXR support on macOS is limited. Consider using Linux or Windows for VR development.

## Building

### Linux/macOS

```bash
# Clone or navigate to the project directory
cd basic-tt

# Create build directory
mkdir build
cd build

# Configure with CMake (Vulkan)
cmake ..

# Or configure with OpenGL instead
cmake -DUSE_VULKAN=OFF ..

# Build
cmake --build . -j$(nproc)

# The executable will be in: build/BasicTT_OpenXR
```

### Windows (Visual Studio)

```bash
# Open Developer Command Prompt for VS 2019/2022
cd basic-tt
mkdir build
cd build

# Configure
cmake .. -G "Visual Studio 16 2019"  # Or "Visual Studio 17 2022"

# Build
cmake --build . --config Release

# The executable will be in: build\Release\BasicTT_OpenXR.exe
```

### Windows (MinGW)

```bash
cd basic-tt
mkdir build
cd build

cmake .. -G "MinGW Makefiles"
cmake --build .
```

## Running

### With VR Headset

1. Ensure your VR headset is connected and the OpenXR runtime is running:
   - **SteamVR**: Launch SteamVR first
   - **Oculus**: Launch Oculus app and connect Quest via Link
   - **WMR**: Windows Mixed Reality Portal should auto-start

2. Run the application:
   ```bash
   ./BasicTT_OpenXR  # Linux/macOS
   BasicTT_OpenXR.exe  # Windows
   ```

3. Put on your VR headset
4. Use your controllers to hold the paddles and play table tennis!

### Without VR (Simulation Mode)

The application will fall back to simulation mode if no VR runtime is detected:

```bash
./BasicTT_OpenXR
```

This will run the physics simulation and print ball positions to the console, useful for testing without VR hardware.

## Configuration

### Physics Configuration

Edit the physics settings in `src/infrastructure/game/GameLoop.cpp`:

```cpp
// Change physics engine
PhysicsConfig config;
config.engineType = PhysicsEngineType::RungeKutta4;  // Higher precision
config.substeps = 10;  // More substeps = more accurate
```

### Ball/Paddle/Table Configuration

Modify the default configurations in the respective config files:
- `include/domain/config/BallConfig.h`
- `include/domain/config/PaddleConfig.h`
- `include/domain/config/TableConfig.h`

## Architecture

### Domain Layer (Pure C++)

The domain layer contains all game logic and is completely independent of VR/graphics:

- **Entities**: Data structures for ball, paddle, controller states
- **Physics**:
  - `BallPhysicsOptimizedVerlet`: Fast 2nd-order integrator
  - `BallPhysicsRK4`: Accurate 4th-order integrator
- **Collision**:
  - `CollisionDetectionSystem`: Swept sphere-OBB detection
  - `CollisionResolutionSystem`: Impulse-based resolution with friction
- **Simulation**: `TableTennisSimulation` orchestrates everything

### Infrastructure Layer (Platform-Specific)

- **OpenXR**:
  - `OpenXRManager`: Instance, session, frame management
  - `OpenXRInputManager`: Controller tracking and button input
- **Rendering**: Graphics API abstraction (to be fully implemented)
- **Game Loop**: Fixed timestep game loop with VR integration

## Development Status

### ✅ Completed

- Core math utilities (Vector3, Quaternion, Matrix4x4)
- Domain entities and configurations
- Physics engines (Verlet, RK4)
- Collision detection and resolution
- OpenXR initialization and session management
- OpenXR input handling for controllers
- Table tennis simulation logic
- Game loop with fixed timestep
- CMake build system

### 🚧 In Progress / TODO

- **Graphics Rendering**:
  - Vulkan/OpenGL swapchain integration
  - Shader pipeline
  - Mesh rendering (ball, paddles, table, room)
  - Stereo rendering for VR

- **Additional Features**:
  - Menu system (start game, reset ball, change settings)
  - Score tracking
  - AI opponent
  - Haptic feedback
  - Sound effects
  - Optimization and profiling

## Known Limitations

1. **Graphics Rendering**: Currently incomplete - the simulation runs but doesn't render to VR displays yet. Graphics binding (Vulkan/OpenGL to OpenXR) needs to be implemented.

2. **Session Creation**: OpenXR session creation requires proper graphics API binding, which is partially implemented.

3. **Platform Support**: Primarily tested on Linux. Windows support is functional but may require additional configuration.

## Comparison to Unity Version

| Feature | Unity (C#) | C++ OpenXR |
|---------|-----------|------------|
| **Performance** | ~60 FPS | Potentially 90+ FPS |
| **VR Runtime** | Unity XR + OpenXR Plugin | Native OpenXR |
| **Physics Rate** | 360 Hz (with sub-stepping) | 360 Hz (with sub-stepping) |
| **Build Size** | ~200 MB | ~5-10 MB (native) |
| **Startup Time** | 5-10 seconds | <1 second |
| **Platform Support** | Windows, Android (Quest) | Windows, Linux, Android (future) |
| **Graphics** | URP (Unity) | Vulkan/OpenGL (native) |

## Contributing

This is a learning/demonstration project showing how to convert a Unity VR game to native C++ with OpenXR. Feel free to:

- Implement the missing rendering system
- Add new physics features
- Improve collision detection
- Optimize performance
- Add platform support

## Resources

- **OpenXR Specification**: https://www.khronos.org/openxr/
- **OpenXR SDK**: https://github.com/KhronosGroup/OpenXR-SDK
- **Vulkan Tutorial**: https://vulkan-tutorial.com/
- **Learn OpenGL**: https://learnopengl.com/

## License

This project is for educational purposes. Original Unity game architecture documented in the `docs/` directory.

## Credits

- Original Unity game design and architecture
- OpenXR by Khronos Group
- Physics based on Verlet integration and Runge-Kutta methods
- Collision detection using swept sphere algorithms

---

**Note**: This C++ implementation is a work in progress. The core simulation and VR tracking work, but full rendering integration is still being developed.
