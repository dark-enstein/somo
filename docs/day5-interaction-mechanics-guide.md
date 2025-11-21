# Day 5 — Interaction Mechanics Guide

Complete guide for implementing gesture-driven VR interactions.

---

## Overview

Day 5 implements full gesture-based interaction mechanics:
1. ✓ Pinch grab/release objects
2. ✓ Wrist roll rotation while grabbed
3. ✓ Two-hand pinch distance scaling
4. ✓ Radial menu toggle (open hand)
5. ✓ Thumbs-up confirmation

---

## Architecture

```
GestureInteractionController (orchestrator)
    ├── Subscribes to GestureClassifier events
    ├── Manages grab/release logic
    ├── Detects nearby GestureInteractable objects
    └── Coordinates two-hand interactions
           ↓
GestureInteractable (on objects)
    ├── Grab/release mechanics
    ├── Wrist roll rotation
    ├── Two-hand scaling
    └── Visual feedback
           ↓
GestureRadialMenu
    ├── Open hand → toggle menu
    ├── Point → hover items
    └── Thumbs up → confirm selection
```

---

## Scene Setup

### Step 1: Create Interactable Cube

1. **Create cube GameObject**:
   - `GameObject > 3D Object > Cube`
   - Name: "InteractableCube"
   - Position: (0, 1.5, 2) — in front of camera

2. **Add components**:
   - Add Component → `GestureInteractable`
   - Ensure `Rigidbody` is present (added automatically)

3. **Configure GestureInteractable**:
   - **Can Grab**: ☑
   - **Can Rotate**: ☑
   - **Can Scale**: ☑
   - **Use Physics**: ☑
   - **Throw Multiplier**: 1.5
   - **Normal Color**: White
   - **Hover Color**: Yellow
   - **Grabbed Color**: Green

4. **Optional: Add visual material**:
   - Create material in Project
   - Drag to cube's Mesh Renderer

### Step 2: Setup Interaction Controller

1. **Create empty GameObject**:
   - Name: "InteractionController"
   - Position: (0, 0, 0)

2. **Add component**:
   - Add Component → `GestureInteractionController`

3. **Configure**:
   - **Left Hand Classifier**: Drag GestureClassifier from scene
   - **Right Hand Classifier**: Same (or separate if two-hand setup)
   - **Left Hand Tracker**: Drag HandTrackingProvider
   - **Right Hand Tracker**: Same
   - **Left Hand Transform**: Drag hand tracking GameObject
   - **Right Hand Transform**: Same
   - **Grab Range**: 0.5 (meters)
   - **Grabbable Layer**: Default (or custom layer)
   - **Rotation Sensitivity**: 1.0
   - **Enable Two Hand Scaling**: ☑
   - **Min Scale**: 0.5
   - **Max Scale**: 3.0

### Step 3: Setup Radial Menu

1. **Create Canvas**:
   - `GameObject > UI > Canvas`
   - Name: "RadialMenuCanvas"
   - **Render Mode**: World Space
   - **Canvas Scaler**: Scale with Screen Size

2. **Create menu GameObject**:
   - Right-click Canvas → `Create Empty`
   - Name: "RadialMenu"
   - Add Component → `GestureRadialMenu`

3. **Configure GestureRadialMenu**:
   - **Gesture Classifier**: Drag from scene
   - **Hand Transform**: Hand tracking GameObject
   - **Menu Canvas**: Drag the Canvas
   - **Menu Radius**: 0.3
   - **Distance From Hand**: 0.5
   - **Face Camera**: ☑

4. **Add menu items** (in Inspector):
   - Click "+" to add items
   - Example items:
     - **Label**: "Teleport", **Icon**: null, **On Selected**: Add event
     - **Label**: "Reset", **Icon**: null, **On Selected**: Add event
     - **Label**: "Exit", **Icon**: null, **On Selected**: Add event

---

## Interaction Workflows

### Workflow 1: Pinch Grab

**Steps**:
1. User makes **pinch** gesture near cube
2. `GestureInteractionController` detects pinch
3. Searches for nearest `GestureInteractable` within grab range
4. Calls `interactable.TryGrab(handTransform, grabPoint)`
5. Cube turns green (grabbed color)
6. Cube follows hand movement

**Code flow**:
```csharp
OnGestureChanged("pinch")
  → TryGrabNearestObject()
  → Physics.OverlapSphere(grabRange)
  → interactable.TryGrab()
  → object.isKinematic = true
```

**Release**:
- Any gesture except pinch → `ReleaseObject()`
- Object physics re-enabled
- Throw velocity applied based on hand movement

### Workflow 2: Wrist Roll Rotation

**Trigger**: While pinching grabbed object

**Steps**:
1. User rotates wrist (roll axis)
2. `GestureInteractionController.Update()` calls `ApplyWristRollRotation()`
3. Calculates wrist roll delta from initial
4. Rotates object around hand forward axis

**Code flow**:
```csharp
Update()
  → if (grabbedObject != null)
  → grabbedObject.ApplyWristRollRotation(handTransform, sensitivity)
  → GetWristRoll() → calculate angle
  → transform.Rotate(forward, delta)
```

**Note**: Rotation only works while **not** two-hand scaling

### Workflow 3: Two-Hand Pinch Scaling

**Trigger**: Both hands pinch simultaneously

**Steps**:
1. User pinches with both hands
2. `IsTwoHandScaling()` returns true
3. Initial distance between hands recorded
4. Current distance compared to initial
5. Scale ratio applied to grabbed object

**Code flow**:
```csharp
Update()
  → UpdateTwoHandScaling()
  → if (leftGesture == "pinch" && rightGesture == "pinch")
  → currentDistance = Vector3.Distance(leftHand, rightHand)
  → scaleRatio = currentDistance / initialDistance
  → transform.localScale = Vector3.one * scaleRatio
```

**Constraints**:
- Min scale: 0.5x (configurable)
- Max scale: 3.0x (configurable)
- Rotation disabled during scaling

### Workflow 4: Radial Menu

**Show menu**:
1. User makes **open hand** gesture
2. `GestureRadialMenu.OnGestureChanged("open_hand")`
3. Menu appears in front of hand
4. Faces camera (or hand, configurable)

**Navigate menu**:
1. User makes **point** gesture
2. Hand direction determines hovered item
3. Hovered item turns yellow

**Confirm selection**:
1. User makes **thumbs up** gesture
2. Hovered item's `onSelected` event fires
3. Item turns green briefly
4. Menu hides after 0.5s

**Close menu**:
- **Fist** gesture → immediate close
- **Open hand** again → toggle off

---

## Script Reference

### GestureInteractable.cs

**Purpose**: Makes GameObject grabbable and manipulable.

**Key methods**:
```csharp
bool TryGrab(Transform handTransform, Vector3 grabPoint);
void Release();
void ApplyWristRollRotation(Transform hand, float sensitivity);
void ApplyTwoHandScale(float distance, float initialDistance, float min, float max);
void SetHighlight(bool highlighted);
bool IsGrabbable();
```

**Events**:
```csharp
event InteractionHandler OnGrabbed;  // Fired when grabbed
event InteractionHandler OnReleased; // Fired when released
```

**Inspector fields**:
- `canGrab`, `canRotate`, `canScale`: Enable/disable features
- `usePhysics`: Enable physics-based throwing
- `throwMultiplier`: Throw velocity multiplier
- `normalColor`, `hoverColor`, `grabbedColor`: Visual feedback

---

### GestureRadialMenu.cs

**Purpose**: Gesture-controlled radial menu UI.

**Key methods**:
```csharp
void ShowMenu();
void HideMenu();
void AddMenuItem(string label, Sprite icon, UnityAction callback);
```

**Menu item structure**:
```csharp
[Serializable]
public class RadialMenuItem
{
    public string label;
    public Sprite icon;
    public UnityEvent onSelected;
}
```

**Inspector fields**:
- `menuRadius`: Size of menu circle
- `distanceFromHand`: How far in front of hand
- `faceCamera`: Orient toward camera vs. hand
- `menuItems`: List of menu items

---

### GestureInteractionController.cs

**Purpose**: Orchestrates all gesture interactions.

**Key methods**:
```csharp
GestureInteractable GetGrabbedObject();
void ReleaseAll();
```

**Inspector fields**:
- Hand references (classifiers, trackers, transforms)
- `grabRange`: Proximity distance for grabbing
- `grabbableLayer`: Filter which objects can be grabbed
- `rotationSensitivity`: Wrist roll multiplier
- `enableTwoHandScaling`: Enable/disable two-hand scaling
- `minScale`, `maxScale`: Scaling constraints

---

## Common Issues & Solutions

### Issue: "Object not grabbable"

**Symptoms**: Pinch gesture doesn't grab object

**Solutions**:
1. Check `GestureInteractable` component is on object
2. Ensure `Rigidbody` is present
3. Verify object is within `grabRange` (check gizmos)
4. Check `grabbableLayer` matches object's layer
5. Ensure object's `canGrab` is enabled

**Debug**:
```csharp
// Add to GestureInteractionController.TryGrabNearestObject()
Debug.Log($"Found {colliders.Length} colliders in range");
```

### Issue: "Rotation not working"

**Symptoms**: Wrist roll doesn't rotate object

**Solutions**:
1. Check `canRotate` is enabled on `GestureInteractable`
2. Verify not in two-hand scaling mode
3. Increase `rotationSensitivity` in controller
4. Ensure hand transform is updating

**Debug**:
```csharp
// Add to GestureInteractable.ApplyWristRollRotation()
Debug.Log($"Wrist roll: {currentWristRoll} (delta: {rollDelta})");
```

### Issue: "Menu doesn't appear"

**Symptoms**: Open hand gesture doesn't show menu

**Solutions**:
1. Check `GestureRadialMenu.menuCanvas` is assigned
2. Verify Canvas is in World Space render mode
3. Ensure gesture classifier is assigned
4. Check hand transform is assigned
5. Verify Canvas is not disabled in hierarchy

**Debug**:
```csharp
// In GestureRadialMenu.ShowMenu()
Debug.Log($"Menu visible: {isMenuVisible}, Canvas: {menuCanvas != null}");
```

### Issue: "Two-hand scaling not working"

**Symptoms**: Pinching with both hands doesn't scale

**Solutions**:
1. Enable `enableTwoHandScaling` in controller
2. Ensure both hands have gesture classifiers
3. Verify both hand transforms are assigned
4. Check that one hand has grabbed an object first

**Debug**:
```csharp
// In GestureInteractionController.IsTwoHandScaling()
Debug.Log($"Left: {leftHandGesture}, Right: {rightHandGesture}, Grabbed: {leftHandGrabbedObject != null}");
```

---

## Testing Checklist

### Basic Interactions
- [ ] Pinch grabs nearest cube
- [ ] Cube turns green when grabbed
- [ ] Cube follows hand smoothly
- [ ] Release restores cube color
- [ ] Cube has physics when released

### Rotation
- [ ] Wrist roll rotates grabbed cube
- [ ] Rotation feels natural (not too fast/slow)
- [ ] Rotation stops when released

### Two-Hand Scaling
- [ ] Both hands pinching activates scaling
- [ ] Moving hands apart scales up
- [ ] Moving hands together scales down
- [ ] Scale respects min/max limits
- [ ] Rotation disabled during scaling

### Radial Menu
- [ ] Open hand shows menu
- [ ] Menu appears in front of hand
- [ ] Menu faces camera correctly
- [ ] Point gesture highlights items
- [ ] Thumbs up selects item
- [ ] Fist closes menu
- [ ] Menu items trigger callbacks

---

## Advanced Customization

### Custom Grab Logic

```csharp
public class CustomGrabInteractable : GestureInteractable
{
    protected override bool TryGrab(Transform hand, Vector3 point)
    {
        // Custom grab validation
        if (/* custom condition */)
        {
            return base.TryGrab(hand, point);
        }
        return false;
    }
}
```

### Multi-Object Menu Actions

```csharp
public class MenuActions : MonoBehaviour
{
    [SerializeField] private GestureRadialMenu menu;
    [SerializeField] private GameObject[] objectsToToggle;

    private void Start()
    {
        menu.AddMenuItem("Toggle Objects", null, ToggleObjects);
    }

    private void ToggleObjects()
    {
        foreach (var obj in objectsToToggle)
        {
            obj.SetActive(!obj.activeSelf);
        }
    }
}
```

### Distance-Based Scaling Sensitivity

```csharp
// In GestureInteractable.ApplyTwoHandScale()
float sensitivity = Mathf.Lerp(0.5f, 2.0f, distance / 2.0f);
scaleRatio *= sensitivity;
```

---

## Performance Optimization

### Grab Range Optimization

- Use reasonable `grabRange` (0.3-0.5m typical)
- Use Physics layers to filter grabbables
- Consider using `Physics.OverlapSphereNonAlloc` for allocation-free checks

```csharp
// Optimized version
private Collider[] colliderBuffer = new Collider[32];

private void TryGrab()
{
    int count = Physics.OverlapSphereNonAlloc(
        handPosition, grabRange, colliderBuffer, grabbableLayer
    );

    for (int i = 0; i < count; i++)
    {
        // Process colliderBuffer[i]
    }
}
```

### Menu Update Optimization

- Only update menu when visible
- Cache menu item references
- Use object pooling for menu items if dynamic

---

## Integration Examples

### Example 1: Teleport on Menu Selection

```csharp
public class TeleportAction : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private GestureRadialMenu menu;

    private void Start()
    {
        menu.AddMenuItem("Teleport Forward", null, TeleportForward);
    }

    private void TeleportForward()
    {
        player.position += player.forward * 2.0f;
    }
}
```

### Example 2: Reset Object Transform

```csharp
public class ResetAction : MonoBehaviour
{
    [SerializeField] private GestureInteractable interactable;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private void Start()
    {
        initialPosition = interactable.transform.position;
        initialRotation = interactable.transform.rotation;
    }

    public void ResetTransform()
    {
        interactable.Release();
        interactable.transform.position = initialPosition;
        interactable.transform.rotation = initialRotation;
        interactable.transform.localScale = Vector3.one;
    }
}
```

---

## Next Steps

After Day 5 completion:

**Day 6**: Robustness improvements
- Handle tracking loss gracefully
- Personal calibration system
- False-trigger suppression
- Left/right hand differentiation
- Multi-object interaction rules

**Day 7**: Final polish and demo
- Build executable
- Record demo video
- Performance profiling
- Documentation finalization

---

## Expected Deliverables

By end of Day 5:
- [ ] Cube grabbable with pinch gesture
- [ ] Wrist roll rotation working smoothly
- [ ] Two-hand scaling functional
- [ ] Radial menu appears/disappears correctly
- [ ] Menu navigation with point gesture
- [ ] Thumbs-up confirmation working
- [ ] All interactions feel natural and responsive
- [ ] No console errors during interactions

---

## References

- Unity Physics docs: https://docs.unity3d.com/Manual/PhysicsSection.html
- UI World Space Canvas: https://docs.unity3d.com/Manual/UICanvas.html
- Hand tracking best practices: See `docs/feature-extraction-spec.md`
