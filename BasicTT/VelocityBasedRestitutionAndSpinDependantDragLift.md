Below is an **action plan** for modifying your existing simulation to incorporate **velocity-based restitution** (per the yield-strength formula) and **spin-dependent drag/lift**. Since you’re assuming only plastic balls, we’ll skip separate celluloid parameters. The plan references typical classes or code segments found in your codebase (e.g., `CollisionResolutionSystem`, `BallPhysics`, `IPhysicsConfig`, etc.). Adjust the naming to match your own project.

---

## 1. Add Yield-Velocity and Velocity-Based CoR Logic

1. **In `BallConfig`** (or wherever you store ball parameters):
   - Add fields for plastic-ball material properties:
     ```csharp
     public float YieldStrength = 51e6f;  // 51 MPa => 51e6 Pa
     public float ElasticModulus = 1197e6f; // 1197 MPa => 1197e6 Pa
     // Possibly Poisson’s ratio, if needed
     public float K = ...;  // material coefficient
     // ...
     ```
   - Add a field or method to compute the **yield velocity** \(V_y\):
     ```csharp
     public float ComputeYieldVelocity()
     {
         // R^*, E^*, m^* => you'd compute or approximate from ball dims
         // For simplicity, assume R^* ~ ball radius, E^* ~ ElasticModulus, m^* ~ ball mass
         float RStar = DiameterMeters * 0.5f; // or a function if contacting surfaces vary
         float EStar = ElasticModulus;  // combined elasticity for ball vs. table/paddle
         float mStar = MassKg;          // ball mass approx
         
         float term1 = (1.61f * K * YieldStrength);
         float inside = term1 * term1 * term1 * term1 * term1; // (term1)^{5}
         float outside = 3.194f * inside;
         float ratio = (RStar / (EStar * mStar));
         float Vy = outside * Mathf.Sqrt(ratio);
         return Vy;
     }
     ```
   - Add a function `ComputeRestitution(float impactSpeed)` using the formula:
     ```csharp
     public float ComputeRestitution(float impactSpeed)
     {
         float Vy = ComputeYieldVelocity();
         // If impactSpeed < some threshold => use a fallback or clamp
         // Then:
         // Cr = 1 - 0.1 * ln(impactSpeed / Vy) * (impactSpeed / Vy - 1)^{0.156}
         float ratio = impactSpeed / Vy;
         // If ratio <= 0 => handle or clamp
         float term = 0.1f * Mathf.Log(ratio) * Mathf.Pow(ratio - 1f, 0.156f);
         float Cr = 1f - term;
         // clamp or sanity-check (Cr not negative, etc.)
         return Mathf.Clamp(Cr, 0f, 1f);
     }
     ```

2. **In `PhysicsConfig`** or similar, reference these fields or store them in the ball’s config so you can easily call `ballConfig.ComputeRestitution(speed)` in collisions.

---

## 2. Modify Collision Resolution for Speed-Based CoR

1. **In `CollisionResolutionSystem`** (or wherever you detect and resolve collisions):
   - When you detect a collision, compute the ball’s **impact speed**. For a paddle or table, it’s typically the magnitude of the ball’s velocity **relative** to the surface:
     ```csharp
     float impactSpeed = (ball.Velocity - paddle.Velocity).magnitude;
     ```
   - Call your new `ComputeRestitution(impactSpeed)` to get `Cr`.
   - Replace the old `restitution` usage with `Cr`. For example:
     ```csharp
     float normalImpulseMag = -(1f + Cr) * Vector3.Dot(relativeVel, collisionNormal) * ballMass;
     ```
   - Keep tangential friction or spin logic as is, but the normal bounce factor is no longer a single constant—it’s now `Cr`.

2. **(Optional)** If you do multiple collisions with different surfaces (table vs. paddle), you might tweak the formula or provide different effective `E^*` for each contact. But for a simpler approach, keep one formula for any collision.

---

## 3. Integrate Spin-Dependent Drag and Lift in Ball Physics

1. **In `BallPhysics`** (e.g., `BallPhysicsBasicVervlet`, `BallPhysicsRangeKutta4`, etc.):
   - Each update, compute **spin ratio**:
     ```csharp
     float spinRatio = (state.AngularVelocity.magnitude * ballRadius) 
                       / Mathf.Max(1e-6f, state.Velocity.magnitude);
     ```
     - Avoid dividing by zero if velocity is nearly zero.
2. **Define a small table** for `(spinRatio -> C_D, C_L)` or use the provided discrete points. Implement interpolation logic:
   ```csharp
   (C_D, C_L) = LookupCoefficients(spinRatio);
   ```
   - `LookupCoefficients` can do simple linear interpolation between the known ratio breakpoints.
3. **Compute drag force** \(\mathbf{F}_D\):
   ```csharp
   Vector3 v = state.Velocity;
   float vMag = v.magnitude;
   if (vMag > 1e-6f)
   {
       Vector3 dragDir = v.normalized;
       float dragMag = 0.5f * airDensity * C_D * ballCrossSectionArea * vMag * vMag;
       Vector3 F_D = -dragMag * dragDir;
       // Add to net acceleration: a += F_D / mass
   }
   ```
4. **Compute Magnus (lift) force** \(\mathbf{F}_L\):
   ```csharp
   // direction ~ omega x v
   Vector3 F_L = 0.5f * airDensity * C_L * ballCrossSectionArea 
                 * Vector3.Cross(state.AngularVelocity, v);
   // net acceleration: a += F_L / mass
   ```
5. **Sum gravity** \(\mathbf{F}_G = m*g\), drag \(\mathbf{F}_D\), lift \(\mathbf{F}_L\), do your integration. E.g.:
   ```csharp
   Vector3 netForce = F_G + F_D + F_L;
   Vector3 acceleration = netForce / ballMass;
   state.Velocity += acceleration * dt;
   state.Position += state.Velocity * dt; // or your chosen integrator
   ```
6. Maintain or update spin damping if you have an `AngularDragCoefficient`.

---

## 4. Adjust Data Structures

1. **`BallConfig`**  
   - Ensure you have `YieldStrength`, `K`, `ElasticModulus` as fields.  
   - Provide `ComputeYieldVelocity()` & `ComputeRestitution(float speed)` methods.

2. **`PhysicsConfig`**  
   - Possibly store `Air.Density`, references to the ball’s `DragCoefficientTable` or a function `LookupCoefficients(spinRatio)`.

3. **Coefficient Tables**  
   - A small lookup or interpolation function:
     ```csharp
     private (float, float) LookupCoefficients(float spinRatio)
     {
         // e.g., if spinRatio <= -1 => (0.4, -0.1)
         // spinRatio ~ 0 => (0.5, 0.1)
         // spinRatio >= 1 => (0.6, 0.3)
         // interpolate between these breakpoints
     }
     ```

---

## 5. Testing and Tuning Steps

1. **Collision Test**: Fire the ball at a set speed (5, 10, 15, 20, 25 m/s) onto a motionless paddle or “wall,” measure rebound speed, confirm it approximates the data (~0.9 at 5 m/s, ~0.78 at 25 m/s).  
2. **Spin Test**: Launch the ball with a set angular velocity and linear velocity, see if the flight path bends realistically. Adjust your `(C_D, C_L)` table or `MagnusCoefficient` if the curve is too little or too large.  
3. **Yield Velocity**: Confirm that for moderate impact speeds, CoR remains near 0.9, but at higher speeds it drops (0.8–0.78). If results differ from your chart, tweak `K` or double-check your units.  

---

## 6. Example Pseudocode Flow

```csharp
// Each physics frame:
void UpdateSimulation(float dt)
{
    // 1) For each ball:
    //    a) Check collisions:
    CollisionData collision = DetectCollision(ball, paddle, dt);
    if (collision.Detected)
    {
        // i) Compute impactSpeed 
        float impactSpeed = (ball.Velocity - paddle.Velocity).magnitude;

        // ii) Get restitution:
        float Cr = ballConfig.ComputeRestitution(impactSpeed);

        // iii) Resolve collision using Cr 
        collisionSystem.ResolveCollision(ref ball, paddle, collision, Cr);
    }

    // b) If no collision or after collisions:
    //    Apply forces (drag, lift, gravity):
    //    spinRatio = ...
    //    (C_D, C_L) = LookupCoefficients(spinRatio);
    //    Vector3 netForce = gravity + drag + magnus;
    //    ball.Integrate(netForce, dt);
}
```

---

### Summary of Planned Code Changes

1. **New Fields/Methods in `BallConfig`:**
   - `YieldStrength`, `ElasticModulus`, `K`
   - `ComputeYieldVelocity()`
   - `ComputeRestitution(impactSpeed)`

2. **CollisionResolution**:
   - Modify existing restitution usage to call `ComputeRestitution(impactSpeed)` each time.

3. **Drag & Lift Implementation**:
   - In your ball physics integrator, compute spin ratio, look up `(C_D, C_L)`, then compute `F_D` and `F_L`.

4. **Testing**:
   - Validate bounce speed vs. known data.
   - Validate spin flight with known arcs.

With these additions, you’ll have **velocity-based CoR** for plastic balls plus a **spin-dependent drag/lift** model, significantly improving realism in your table tennis simulation.