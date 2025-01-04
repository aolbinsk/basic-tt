# VR Table Tennis - Requirements Document

## 1. Core Game Requirements

### 1.1 Game Setup
- Single scene with table tennis setup
- Dynamic scene setup based on code configurations
- Player positioned dynamically one meter from the table
- AI opponent on opposite side

### 1.2 Player Interaction
- Right hand controller controls paddle
  - Direct 1:1 mapping of controller to paddle
  - Physical collision with ball
  - Natural swing mechanics
- Left hand controller for ball handling
  - Ball follows the controller when grip is pressed
  - Realistic momentum accumulation until grip is released
  - Smooth transition to actual physics upon release
  - Smoothing of extreme controller values to prevent unrealistic behavior
- Serving Mechanics
  - On grip release, the ball should mimic the throw motion, moving upwards realistically
  - Ensure minimum upward component when serving
- Paddle Dimensions
  - Length: 15.25 cm (6 inches) from handle to tip
  - Width: 15.25 cm (6 inches) at widest point
  - Handle: 10 cm (4 inches) long
  - Total paddle length with handle: 25.25 cm (10 inches)

### 1.3 AI Opponent
- Basic reflection mechanics
  - Tracks ball position
  - Moves paddle to intercept
  - Simple return angle calculation
- Fixed position on opposite side
- Predictable behavior

### 1.4 Physics
- Realistic ball movement
  - Gravity effects
  - Bounce physics
  - Spin effects
- Paddle and Table Collision Detection
  - Ball bounces realistically on paddle and table
  - Adjustable bounce parameters
- Centralized Physics Configuration
  - All physical properties set in code for consistency and maintainability
  - Parameters include table size, paddle size, room scaling, material properties
- Configurable Parameters
  - Ball speed
  - Bounce properties
  - Paddle properties
  - Adjustable via code
- Accurate Equipment Scaling
  - Paddle dimensions must match regulation sizes
  - Scale imported models dynamically to match real-world dimensions
  - Handle model variations without requiring model changes

## 2. Technical Requirements

### 2.1 Performance
- Maintain 120 FPS for VR comfort
- Efficient physics calculations
- Optimized collision detection
- Minimal garbage collection

### 2.2 VR Integration
- OpenXR compatibility
- XR Interaction Toolkit usage

### 2.3 Input System
- Controller button mapping
  - Grip buttons for interactions
  - Haptic feedback on hit

### 2.4 Physics System
- Unity Physics engine
- Configurable parameters
  - Ball speed
  - Bounce properties
  - Paddle properties
- Reliable collision detection
- Smooth transitions between physics states (gripped vs released)
- Smoothing for extreme controller inputs

## 3. Gameplay Requirements

### 3.1 Basic Rules
- Standard table tennis scoring
- Ball must bounce on table
- Out of bounds detection
- Service mechanics

### 3.2 Game Flow
- Start position setup
- Ball reset after point
- Score tracking
- Game reset capability

### 3.3 Player Experience
- Responsive paddle movement
- Consistent physics behavior
- Pro-level simulation accuracy
- Realistic serving mechanics

### 3.4 AI Behavior
- Predictable returns
- Basic positioning
- No "impossible" moves
- Consistent difficulty level

## 4. Quality Requirements

### 4.1 Reliability
- No physics glitches
- Consistent frame rate
- Stable VR tracking
- Predictable behavior

### 4.2 Usability
- Clear visual feedback
- Intuitive controls
- Easy to start playing
- Simple reset mechanism

### 4.3 Maintainability
- Clean code structure
- Clear documentation
- Modular systems
- Easy to extend
- Centralized configuration for physical properties

### 4.4 Testing
- Unit tests for core systems
- Physics validation
- Input verification
- Performance benchmarks

## 5. Future Considerations

### 5.1 Planned Extensions
- Multiple difficulty levels
- Sound effects
- Multiplayer support
  - Play with other players over the internet
- AI Training Modes
  - Practice with AI opponent
  - Ball machine functionality

### 5.2 Optional Features
- Training mode
- Shot replays
- Statistics tracking
- Custom paddle designs

## 6. Development Guidelines

### 6.1 Code Standards
- C# coding standards
- XML documentation
- Clear naming conventions
- SOLID principles
- Centralized configuration of physics parameters

### 6.2 Version Control
- Clear commit messages
- Feature branches
- Regular merging
- Version tagging

### 6.3 Testing Process
- Regular playtesting
- Performance monitoring
- Bug tracking
- Feedback collection

### 6.4 Documentation
- Code documentation
- Setup instructions
- Maintenance guides
- Change logs
