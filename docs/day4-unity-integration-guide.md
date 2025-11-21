# Day 4 — Unity Integration Guide

Complete guide for integrating the trained gesture classifier into Unity.

---

## Overview

Day 4 implements real-time gesture recognition in Unity:
1. ✓ C# feature extraction (matching Python)
2. ✓ ONNX model loading via Barracuda
3. ✓ Hand tracking provider system
4. ✓ Gesture classification with smoothing
5. ✓ Debug UI and visualization

---

## Prerequisites

### Unity Version
- Unity 2022.3 LTS or later
- Universal Render Pipeline (optional, but recommended for VR)

### Required Packages
Add these via Unity Package Manager (Window > Package Manager):

```json
{
  "com.unity.xr.interaction.toolkit": "2.3.2",
  "com.unity.barracuda": "3.0.0",
  "com.unity.textmeshpro": "3.0.6"
}
```

### Trained Model
Ensure you have completed Day 3 and have:
- `ml/models/gesture_classifier.onnx` (ONNX model file)
- Model accuracy > 90% on test set

---

## Project Setup

### Step 1: Import ONNX Model

1. Copy trained model to Unity:
   ```bash
   cp ml/models/gesture_classifier.onnx unity-vr/Assets/Models/
   ```

2. In Unity Editor:
   - Select `gesture_classifier.onnx` in Project window
   - Inspector should show "Model Asset" type
   - Verify "Model" dropdown shows structure

### Step 2: Create Test Scene

1. **Create new scene** or use `Assets/Scenes/MainScene.unity`

2. **Add GameObject hierarchy**:
   ```
   Scene
   ├── XR Origin (or Main Camera for non-VR testing)
   ├── GestureSystem (Empty GameObject)
   │   ├── MockHandTrackingProvider (component)
   │   ├── GestureClassifier (component)
   │   ├── HandVisualizer (component)
   │   └── GestureDebugUI (component)
   └── Canvas (for debug UI)
       └── DebugPanel
           ├── GestureText (TextMeshPro)
           ├── ConfidenceText (TextMeshPro)
           ├── TrackingStatusText (TextMeshPro)
           └── FPSText (TextMeshPro)
   ```

### Step 3: Configure Components

#### GestureSystem GameObject

**MockHandTrackingProvider:**
- **Handedness**: Right
- **Is Tracking**: ☑ (checked)
- **Test Gesture**: Open Hand (change at runtime to test)

**GestureClassifier:**
- **Model Asset**: Drag `gesture_classifier.onnx` here
- **Worker Type**: CSharpBurst (fastest)
- **Hand Tracker**: Auto-assigned (or drag GestureSystem)
- **Use Majority Vote**: ☑
- **Smoothing Window**: 5
- **Confidence Threshold**: 0.7
- **Dwell Time**: 0.25

**HandVisualizer:**
- **Hand Tracker**: Auto-assigned
- **Show Landmarks**: ☑
- **Show Connections**: ☑
- **Landmark Size**: 0.01
- **Visualization Scale**: 1.0

**GestureDebugUI:**
- **Gesture Classifier**: Auto-assigned
- **Hand Tracker**: Auto-assigned
- Drag TextMeshPro UI elements to corresponding fields:
  - **Gesture Text**: DebugPanel/GestureText
  - **Confidence Text**: DebugPanel/ConfidenceText
  - **Tracking Status Text**: DebugPanel/TrackingStatusText
  - **FPS Text**: DebugPanel/FPSText
- **Show Features**: ☐ (optional, verbose)
- **Show FPS**: ☑

### Step 4: Test in Editor

1. **Enter Play mode**
2. **Observe debug UI**:
   - Should show: "Gesture: Open Hand"
   - Confidence: ~70-100%
   - Tracking: Right hand (100%)
   - FPS: 60+

3. **Change test gesture**:
   - While playing, select GestureSystem
   - In Inspector, change `MockHandTrackingProvider > Test Gesture`
   - Try: Fist, Pinch, Point, Thumbs Up
   - Debug UI should update in ~0.25 seconds (dwell time)

4. **Verify hand visualization**:
   - Scene view should show colored spheres (hand landmarks)
   - Lines connecting joints
   - Hand pose should match selected gesture

---

## Script Reference

### HandFeatureExtractor.cs

**Purpose**: Extract 31 features from 21 hand landmarks.

**Usage**:
```csharp
Vector3[] landmarks = handTracker.GetLandmarks();
float[] features = HandFeatureExtractor.Extract(landmarks);
// Returns: 31-element array
```

**Features extracted**:
- Inter-joint distances (20)
- Fingertip-to-wrist distances (5)
- Finger angles at PIP joints (5)
- Pinch distance (1)

**Important**: Must match Python implementation exactly!

---

### HandTrackingProvider.cs

**Purpose**: Abstract interface for hand tracking backends.

**Implementations**:
1. **MockHandTrackingProvider**: Testing without hardware
2. **OculusHandTrackingProvider**: (Day 5) Oculus/Meta Quest
3. **UltraleapHandTrackingProvider**: (Future) Ultraleap sensor

**Key methods**:
```csharp
bool IsTracking { get; }             // Is hand visible?
Handedness TrackedHand { get; }      // Left or Right
Vector3[] GetLandmarks();            // 21 landmarks
float TrackingConfidence { get; }    // 0-1
```

**Custom implementation**:
```csharp
public class MyHandTracker : HandTrackingProvider
{
    public override bool IsTracking => /* your logic */;
    public override Handedness TrackedHand => Handedness.Right;

    public override Vector3[] GetLandmarks()
    {
        // Return 21 Vector3 positions in local space
        // Must match MediaPipe landmark order (see docs/feature-extraction-spec.md)
    }
}
```

---

### GestureClassifier.cs

**Purpose**: Real-time gesture classification with smoothing.

**Key properties**:
```csharp
string CurrentGesture { get; }       // "open_hand", "fist", etc.
float CurrentConfidence { get; }     // 0-1
bool IsModelLoaded { get; }          // Model ready?
```

**Events**:
```csharp
public event GestureChangedHandler OnGestureChanged;

// Subscribe:
gestureClassifier.OnGestureChanged += (gesture, confidence) => {
    Debug.Log($"New gesture: {gesture} ({confidence:P0})");
};
```

**Smoothing parameters**:
- **Majority Vote**: Reduces jitter by voting over recent frames
- **Smoothing Window**: Number of frames to consider (5 = ~83ms at 60 FPS)
- **Confidence Threshold**: Minimum confidence to accept prediction (0.7 = 70%)
- **Dwell Time**: Delay before switching gestures (0.25s prevents rapid switching)

---

### GestureDebugUI.cs

**Purpose**: On-screen debug overlay for development.

**Displays**:
- Current gesture name
- Confidence percentage (color-coded)
- Hand tracking status
- FPS counter
- Optional: Feature vector values

**Color coding**:
- **Green**: High confidence (≥80%)
- **Yellow**: Medium confidence (60-80%)
- **Red**: Low confidence (<60%)
- **Gray**: No tracking

---

### HandVisualizer.cs

**Purpose**: 3D visualization of hand landmarks and connections.

**Controls**:
```csharp
visualizer.ToggleLandmarks();        // Show/hide spheres
visualizer.ToggleConnections();      // Show/hide lines
visualizer.SetVisualizationScale(2.0f); // Make bigger
```

**Landmark colors**:
- Wrist: Yellow
- Thumb: Red
- Index: Green
- Middle: Blue
- Ring: Cyan
- Pinky: Magenta

---

## Troubleshooting

### "Model Asset not assigned"
- Ensure ONNX file is in `Assets/Models/` or similar
- Re-import ONNX file (right-click > Reimport)
- Check Unity console for Barracuda import errors

### "No hand detected" (always shows "none")
- Check `MockHandTrackingProvider.IsTracking` is enabled
- Verify `GestureClassifier.handTracker` is assigned
- Look at Console for errors in feature extraction

### "Gesture never changes from initial"
- Increase `Smoothing Window` or disable `Use Majority Vote`
- Reduce `Confidence Threshold` (try 0.5)
- Reduce `Dwell Time` (try 0.1s)

### "Low FPS (< 30)"
- Change `Worker Type` to `CSharpBurst` (fastest)
- Reduce `Smoothing Window`
- Check model size (should be < 200 KB)
- Disable `Show Features` in debug UI

### "Wrong gesture predicted"
- Verify model was trained correctly (Day 3)
- Check confusion matrix: which gestures are confused?
- Test with Python script first: `test_model.py --live`
- Ensure C# feature extraction matches Python (compare outputs)

### "Model output is all zeros"
- Check Barracuda worker type compatibility
- Try `WorkerType.CSharp` instead of `CSharpBurst`
- Verify ONNX model is compatible with Barracuda 3.0
- Check model inputs: should be `[1, 31]` float tensor

---

## Performance Optimization

### Target Metrics
- **FPS**: 60+ (VR target: 72-90)
- **Latency**: < 50ms (tracking → prediction → action)
- **Accuracy**: > 90% (matching Python test accuracy)

### Optimization Tips

1. **Worker Type**:
   - `CSharpBurst`: Fastest (Unity Burst compiler)
   - `ComputePrecompiled`: GPU inference (if supported)
   - `CSharp`: Fallback (slower, more compatible)

2. **Smoothing**:
   - Reduce `Smoothing Window` (3-5 is good balance)
   - Disable majority vote for instant response (less stable)

3. **Model**:
   - Use Random Forest over k-NN (faster inference)
   - Limit tree depth (10 is good balance)
   - Fewer trees = faster (but less accurate)

4. **Feature Extraction**:
   - Features are computed every frame
   - Already optimized (< 1ms)
   - No further optimization needed

---

## Integration with Game Logic

### Example: Menu Toggle

```csharp
public class RadialMenuController : MonoBehaviour
{
    [SerializeField] private GestureClassifier gestureClassifier;
    [SerializeField] private GameObject radialMenu;

    private void Start()
    {
        gestureClassifier.OnGestureChanged += OnGestureChanged;
    }

    private void OnGestureChanged(string gesture, float confidence)
    {
        if (gesture == "open_hand")
        {
            radialMenu.SetActive(true);
        }
        else if (gesture == "fist")
        {
            radialMenu.SetActive(false);
        }
    }
}
```

### Example: Object Manipulation

```csharp
public class GestureGrab : MonoBehaviour
{
    [SerializeField] private GestureClassifier gestureClassifier;
    [SerializeField] private Transform targetObject;

    private bool isGrabbing = false;

    private void Start()
    {
        gestureClassifier.OnGestureChanged += OnGestureChanged;
    }

    private void OnGestureChanged(string gesture, float confidence)
    {
        if (gesture == "pinch" && !isGrabbing)
        {
            StartGrab();
        }
        else if (gesture != "pinch" && isGrabbing)
        {
            ReleaseGrab();
        }
    }

    private void StartGrab()
    {
        isGrabbing = true;
        targetObject.SetParent(transform); // Attach to hand
    }

    private void ReleaseGrab()
    {
        isGrabbing = false;
        targetObject.SetParent(null); // Release
        // Add physics (e.g., rigidbody.velocity = hand velocity)
    }
}
```

---

## Testing Checklist

Before moving to Day 5:

- [ ] ONNX model loads without errors
- [ ] All 5 gestures are recognized correctly in Mock mode
- [ ] Confidence values are reasonable (>70% for correct gesture)
- [ ] FPS is stable at 60+ in Editor
- [ ] Debug UI displays correct information
- [ ] Hand visualizer shows landmarks in correct positions
- [ ] Gesture changes smoothly with ~0.25s dwell time
- [ ] No console errors or warnings

---

## Next Steps

After Day 4 completion:

**Day 5**: Implement interaction mechanics
- Radial menu toggle (open hand)
- Pinch grab/release
- Wrist rotation
- Two-hand scaling
- Thumbs-up confirmation

**Day 6**: Robustness improvements
- Handle tracking loss gracefully
- Left/right hand support
- Personal calibration
- False-trigger suppression

---

## References

- Unity Barracuda docs: https://docs.unity3d.com/Packages/com.unity.barracuda@3.0
- MediaPipe hand landmarks: https://google.github.io/mediapipe/solutions/hands.html
- Feature extraction spec: `docs/feature-extraction-spec.md`
