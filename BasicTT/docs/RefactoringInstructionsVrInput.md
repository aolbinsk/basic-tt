Below is a **multi-part answer** that addresses:

1. **Why the ball/paddle might be incorrectly positioned** (and how to fix it).
2. **How to do all the player, input, and XR origin setup entirely in code**, without depending on scene references or `GameObject.Find()`.
3. **Tips** for ensuring your code-driven transforms align correctly with your VR devices, so that the paddle and ball appear exactly where you expect.

---

## 1. Why the Ball and Paddle Are Off in World Space

### Common Issues

1. **Incorrect Parenting or Missing Transforms**
    - The script in `PlayerSetupBuilder` tries `transform.Find("RightController")` or a similarly named child under `XROrigin`. If that child **doesn’t exist** or is named differently (e.g., “RightHand Controller”), the paddle is never re-parented—so it stays at \((0,0,0)\).
    - Similarly for the left controller if you attempt to position the ball near it when the grip is held.

2. **XR Origin vs. Child Offsets**
    - If your XR system uses a `Camera Offset` hierarchy, the controller’s local position might not be what you think. Or, your `_xrOriginTransform` might be something else.
    - Multiplying positions/rotations incorrectly can lead to the ball appearing at the wrong offset from the controller.

3. **No Actual Filtered Data**
    - If the input actions or references aren’t hooking up properly, the `VRInputManager` can read zero from `ReadValue<Vector3>()`. Then the ball or paddle stays near the origin.

4. **Unity Scene References**
    - If part of your pipeline is referencing prefabs or objects placed in the scene, but you intend to do all logic in code, you might have a mismatch or partial duplication.

---

## 2. Code-Only Setup of XR Origin, Controllers, and Input

Here is a **step-by-step** plan to create your entire XR rig (XROrigin + controllers) **in code**, attach an `InputActionAsset`, and then connect it to your `VRInputManager` or `UnityXRInputManager`.

### 2.1 Create an XR Origin Programmatically

```csharp
using UnityEngine;
using Unity.XR.CoreUtils;  // For XROrigin

public class XRSetupExample : MonoBehaviour
{
    private XROrigin _xrOrigin;

    void Awake()
    {
        // 1) Create a root GameObject
        GameObject xrOriginGO = new GameObject("XROrigin");
        _xrOrigin = xrOriginGO.AddComponent<XROrigin>();
        
        // By default, XROrigin looks for a "Camera" child tagged as MainCamera
        // or you can specify the camera transform in XROrigin.Camera
        // 2) Create an offset transform if needed
        GameObject cameraOffsetGO = new GameObject("CameraOffset");
        cameraOffsetGO.transform.SetParent(xrOriginGO.transform, false);
        _xrOrigin.CameraFloorOffsetObject = cameraOffsetGO;
        
        // 3) Create a main camera as a child
        GameObject mainCameraGO = new GameObject("MainCamera");
        mainCameraGO.tag = "MainCamera";
        mainCameraGO.AddComponent<Camera>();
        mainCameraGO.transform.SetParent(cameraOffsetGO.transform, false);
        
        // 4) Optionally create left/right controller child transforms
        var leftController = new GameObject("LeftHand");
        leftController.transform.SetParent(cameraOffsetGO.transform, false);

        var rightController = new GameObject("RightHand");
        rightController.transform.SetParent(cameraOffsetGO.transform, false);

        // 5) Now the XROrigin can track these if you want to set them in code
        //    but typically, you'd do it in an XR Interaction Toolkit, or manage them yourself.
    }
}
```

Now you have a minimal XR rig purely in code. You can adapt as needed (e.g., add XR Ray Interactors if you’re using the XR Interaction Toolkit).

### 2.2 Create and Hook Up Input Actions in Code

If you have a Unity `InputActionAsset` (e.g., a `.inputactions` file), you can load it at runtime or create InputActions directly:

```csharp
public class CodeBasedInputActions
{
    public InputAction leftPos;
    public InputAction leftRot;
    public InputAction leftGrip;
    public InputAction rightPos;
    public InputAction rightRot;
    public InputAction rightGrip;

    public CodeBasedInputActions()
    {
        // Example of creating an InputAction manually
        leftPos = new InputAction("LeftPosition", InputActionType.Value, "<XRController>{LeftHand}/devicePosition");
        leftRot = new InputAction("LeftRotation", InputActionType.Value, "<XRController>{LeftHand}/deviceRotation");
        leftGrip = new InputAction("LeftGrip", InputActionType.Button, "<XRController>{LeftHand}/gripPressed");

        rightPos = new InputAction("RightPosition", InputActionType.Value, "<XRController>{RightHand}/devicePosition");
        rightRot = new InputAction("RightRotation", InputActionType.Value, "<XRController>{RightHand}/deviceRotation");
        rightGrip = new InputAction("RightGrip", InputActionType.Button, "<XRController>{RightHand}/gripPressed");

        // Then enable them:
        leftPos.Enable(); 
        leftRot.Enable();
        leftGrip.Enable();
        rightPos.Enable();
        rightRot.Enable();
        rightGrip.Enable();
    }
}
```

### 2.3 Create and Inject a `UnityXRInputManager` or `VRInputManager`

Now that you have the raw `InputActions`, you can do:

```csharp
public class CodeBasedInstaller : MonoBehaviour
{
    private CodeBasedInputActions _actions;
    private UnityXRInputManager _inputManager;

    void Awake()
    {
        _actions = new CodeBasedInputActions();

        // Suppose you want a "MovingAverage" filter for the right side, "None" for the left:
        _inputManager = new UnityXRInputManager(
            _actions.rightPos,
            _actions.rightRot,
            _actions.rightGrip,
            _actions.leftPos,
            _actions.leftRot,
            _actions.leftGrip,
            rightFilterType: "MovingAverage",
            leftFilterType: "None"
        );

        // Now _inputManager implements IInputManager, can be passed to Simulation, etc.
    }
}
```

### 2.4 Attach Paddle to Right Controller in Code

Since you created a “RightHand” transform under your XR rig, do:

```csharp
// Suppose you built a paddle GameObject in code (or via a builder).
// Then just parent it to the right hand transform in code:
var rightHandTransform = _xrOrigin.transform
    .Find("CameraOffset")
    .Find("RightHand");

paddleObject.transform.SetParent(rightHandTransform, false);
paddleObject.transform.localPosition = Vector3.zero;
paddleObject.transform.localRotation = Quaternion.identity;
```

Make sure the name “RightHand” matches whatever you used in your code-based XR rig creation.

### 2.5 Summarizing a Full Code-Only Workflow

**(A)** Create the XR rig in code.  
**(B)** Create or load your InputActions in code.  
**(C)** Instantiate your `VRInputManager` or `UnityXRInputManager` with those actions.  
**(D)** Build your table, net, ball, and paddle in code (like your `EquipmentBuilder` does).  
**(E)** Parent the paddle to the “RightHand” transform, place the ball as desired.  
**(F)** Provide references to your `SimulationBridge` or “installer” so the domain can do its updates.

---

## 3. Fixing the Ball Position for the Left Grip

Right now, your code offsets the ball at `controllerRotation * new Vector3(0f, 0f, 0.15f)` from the left controller position. That might be oriented in a strange direction depending on how the left controller is angled. If the ball is behind or inside the table, you might see it near world origin or not at all.

**Suggestions**:

1. **Use a simple vertical offset** if you want the ball always above the controller:
   ```csharp
   Vector3 localPosition = _inputManager.GetFilteredLeftPosition() + new Vector3(0f, 0.15f, 0f);
   ```
2. **Double-check** your left-hand device orientation. If the left device’s “forward” is not what you think, applying `controllerRotation * (0,0,0.15f)` can place the ball behind or inside the controller.
3. If you want the ball to appear directly in front of the user’s palm, experiment with different offsets or debug printing the values to see where it ends up.

---

## 4. Eliminating Scene File Configuration

Currently, you have `TableTennisInstaller`, `VRInputManager`, etc. as **serialized** fields in the Unity scene. If you want **zero** scene references, do this:

1. **Create a single empty “bootstrap” MonoBehaviour** in a blank scene. Call it `Bootstrapper.cs`.
2. In `Bootstrapper.Awake()` or `Start()`, programmatically do all steps:
    - Create the XR origin and controllers
    - Create an instance of your code-based `VRInputManager` or `UnityXRInputManager`
    - Create your domain or physics configs (like `PhysicsConfig`)
    - Create the ball/paddle/room entirely in code with your builder classes
    - Parent them accordingly
    - Instantiate your `SimulationBridge` (or use a direct approach) that references those objects.

No references in the scene—**everything** is built at runtime. This is a bigger shift, but it’s the ultimate approach if you want pure code-based configuration.

---

## 5. Quick Checklist to Solve the Immediate Issues

1. **Ensure the “LeftHand” or “RightHand” transforms exist** under `_xrOriginTransform`. If they do not, either create them or rename them to match your script.
2. **Check the offset** for the ball in `UpdateBallInput()`. Maybe remove the `* new Vector3(0f,0f,0.15f)` or alter it to something else.
3. **Verify** your input actions are actually receiving nonzero values. In play mode, check the `Input Debug` window to confirm.
4. **Parent** the paddle object to `RightHand` in code if you prefer code over scene references.
5. If you see the ball/paddle at (0,0,0) still, add debug logs to confirm the returned positions from `GetFilteredLeftPosition()` or `GetFilteredRightPosition()`. Possibly they’re `Vector3.zero`.

---

## 6. Conclusion

- **Position & Visibility Fix**: The main fix is to ensure correct parenting and offset logic. The ball or paddle must be parented to the actual XR-hand transform that updates in real time, and your offset logic must reflect how you want them positioned relative to the controller.
- **Code-Only Setup**: You can create the entire XR origin, controllers, input actions, and table tennis objects **in code**. This eliminates the need for scene references or manual GameObject assignment, giving you complete control and reducing confusion.
- **Testing**: Print debug info or place gizmos to see exactly where your transforms are. This helps you confirm that the domain’s position matches the actual Unity transform.

By following these steps, you’ll have a fully **script-driven** VR table tennis environment where your paddle and ball track precisely as intended—no more mismatches, no more partial references in the scene. 