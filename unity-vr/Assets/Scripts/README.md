# Unity Scripts — Gesture Recognition System

C# implementation of the gesture recognition pipeline for Unity.

---

## Scripts Overview

| Script | Purpose | Dependencies |
|--------|---------|--------------|
| `HandFeatureExtractor.cs` | Feature extraction (31 features from 21 landmarks) | None |
| `HandTrackingProvider.cs` | Abstract base + Mock provider for hand tracking | None |
| `GestureClassifier.cs` | ONNX model inference with smoothing | Unity.Barracuda |
| `GestureDebugUI.cs` | On-screen debug overlay | TextMeshPro |
| `HandVisualizer.cs` | 3D landmark visualization | None |

---

## Architecture

```
HandTrackingProvider (abstract)
    ├── MockHandTrackingProvider (testing)
    ├── OculusHandTrackingProvider (Day 5)
    └── UltraleapHandTrackingProvider (future)
           ↓
    Provides 21 landmarks (Vector3[])
           ↓
HandFeatureExtractor (static utility)
    → Extracts 31 features
           ↓
GestureClassifier (MonoBehaviour)
    → ONNX inference
    → Smoothing
    → Events
           ↓
    Game Logic / UI
```

---

## Quick Start

### 1. Basic Setup (Mock Hand for Testing)

```csharp
// On empty GameObject:
GameObject gestureSystem = new GameObject("GestureSystem");
gestureSystem.AddComponent<MockHandTrackingProvider>();
GestureClassifier classifier = gestureSystem.AddComponent<GestureClassifier>();

// Assign ONNX model in Inspector:
// classifier.modelAsset = gesture_classifier.onnx
```

### 2. Subscribe to Gesture Changes

```csharp
public class MyGestureHandler : MonoBehaviour
{
    [SerializeField] private GestureClassifier classifier;

    private void Start()
    {
        classifier.OnGestureChanged += HandleGesture;
    }

    private void HandleGesture(string gesture, float confidence)
    {
        switch (gesture)
        {
            case "open_hand":
                Debug.Log("Open hand detected!");
                break;
            case "fist":
                Debug.Log("Fist detected!");
                break;
            case "pinch":
                Debug.Log("Pinch detected!");
                break;
            case "point":
                Debug.Log("Point detected!");
                break;
            case "thumbs_up":
                Debug.Log("Thumbs up detected!");
                break;
        }
    }

    private void OnDestroy()
    {
        classifier.OnGestureChanged -= HandleGesture;
    }
}
```

### 3. Add Visualization

```csharp
gestureSystem.AddComponent<HandVisualizer>();
```

### 4. Add Debug UI

```csharp
// Create Canvas with TextMeshPro elements first
GameObject uiSystem = new GameObject("DebugUI");
GestureDebugUI debugUI = uiSystem.AddComponent<GestureDebugUI>();

// Assign TextMeshPro references in Inspector
```

---

## Script Details

### HandFeatureExtractor.cs

**Static utility class** for feature extraction.

**Usage**:
```csharp
Vector3[] landmarks = handTracker.GetLandmarks();
float[] features = HandFeatureExtractor.Extract(landmarks);

Debug.Log($"Extracted {features.Length} features"); // 31
```

**Features** (total: 31):
- Inter-joint distances: 20 (4 per finger)
- Fingertip-to-wrist distances: 5
- Finger angles (PIP joints): 5
- Pinch distance (thumb-index): 1

**Critical**: Must match Python implementation in `ml/scripts/extract_features.py`.

---

### HandTrackingProvider.cs

**Abstract base class** for hand tracking backends.

**Key members**:
```csharp
public abstract bool IsTracking { get; }
public abstract Handedness TrackedHand { get; }
public abstract Vector3[] GetLandmarks(); // Returns 21 Vector3
public virtual float TrackingConfidence => IsTracking ? 1.0f : 0.0f;
```

**MockHandTrackingProvider**:
- Testing without hardware
- Generates synthetic landmarks for all 5 gestures
- Change gesture in Inspector at runtime
- Always returns IsTracking = true

**Implementing custom provider**:
```csharp
public class MyTrackingProvider : HandTrackingProvider
{
    public override bool IsTracking => /* your logic */;
    public override Handedness TrackedHand => Handedness.Right;

    public override Vector3[] GetLandmarks()
    {
        // Return 21 landmarks in local space
        // Order must match MediaPipe (see docs/feature-extraction-spec.md)
    }
}
```

---

### GestureClassifier.cs

**Main classifier** using Unity Barracuda for ONNX inference.

**Inspector fields**:
- `modelAsset`: ONNX model (from ml/models/)
- `workerType`: CSharpBurst (fastest), CSharp (fallback), ComputePrecompiled (GPU)
- `handTracker`: Reference to HandTrackingProvider
- `useMajorityVote`: Smooth predictions over recent frames
- `smoothingWindow`: Number of frames to vote (5 = good default)
- `confidenceThreshold`: Minimum confidence (0.7 = 70%)
- `dwellTime`: Delay before switching gestures (0.25s)

**Public API**:
```csharp
string CurrentGesture { get; }    // "open_hand", "fist", etc.
float CurrentConfidence { get; }  // 0.0 - 1.0
bool IsModelLoaded { get; }       // Model ready?

event GestureChangedHandler OnGestureChanged;
// Signature: void OnGestureChanged(string gesture, float confidence)

static int GetGestureIndex(string name);  // "pinch" → 2
static string GetGestureName(int index);  // 2 → "pinch"
```

**Smoothing pipeline**:
1. Raw model prediction
2. Majority vote (if enabled) over recent N frames
3. Confidence threshold check
4. Dwell time enforcement (prevents rapid switching)
5. OnGestureChanged event fired

---

### GestureDebugUI.cs

**Debug overlay** for development and testing.

**Inspector fields**:
- Text references (TextMeshPro):
  - `gestureText`: Current gesture name
  - `confidenceText`: Confidence percentage (color-coded)
  - `trackingStatusText`: Hand tracking status
  - `fpsText`: FPS counter
  - `featuresText`: Optional feature vector display
- `showFeatures`: Display raw features (verbose)
- `showFPS`: Display FPS counter

**Color coding**:
- Green: High confidence (≥80%)
- Yellow: Medium confidence (60-80%)
- Red: Low confidence (<60%)
- Gray: No tracking

**Runtime controls**:
```csharp
debugUI.ToggleFeatureDisplay();
debugUI.ToggleFPSDisplay();
```

---

### HandVisualizer.cs

**3D visualization** of hand landmarks and connections.

**Inspector fields**:
- `handTracker`: Reference to HandTrackingProvider
- `showLandmarks`: Draw spheres for joints
- `showConnections`: Draw lines between joints
- `landmarkSize`: Sphere size (0.01 default)
- `visualizationScale`: Overall scale multiplier

**Landmark colors**:
- Wrist: Yellow
- Thumb: Red
- Index: Green
- Middle: Blue
- Ring: Cyan
- Pinky: Magenta

**Runtime controls**:
```csharp
visualizer.ToggleLandmarks();
visualizer.ToggleConnections();
visualizer.SetVisualizationScale(2.0f);
```

---

## Common Patterns

### Pattern 1: Menu Toggle

```csharp
public class MenuController : MonoBehaviour
{
    [SerializeField] private GestureClassifier classifier;
    [SerializeField] private GameObject menu;

    private void Start()
    {
        classifier.OnGestureChanged += OnGestureChanged;
    }

    private void OnGestureChanged(string gesture, float confidence)
    {
        if (gesture == "open_hand")
            menu.SetActive(true);
        else if (gesture == "fist")
            menu.SetActive(false);
    }
}
```

### Pattern 2: Pinch Grab

```csharp
public class PinchGrab : MonoBehaviour
{
    [SerializeField] private GestureClassifier classifier;
    private Transform grabbedObject;

    private void Start()
    {
        classifier.OnGestureChanged += OnGestureChanged;
    }

    private void OnGestureChanged(string gesture, float confidence)
    {
        if (gesture == "pinch" && grabbedObject == null)
            TryGrab();
        else if (gesture != "pinch" && grabbedObject != null)
            Release();
    }

    private void TryGrab()
    {
        // Raycast or proximity check
        // grabbedObject = FindNearestGrabbable();
    }

    private void Release()
    {
        // grabbedObject = null;
    }
}
```

### Pattern 3: Gesture-Specific Actions

```csharp
public class GestureActions : MonoBehaviour
{
    [SerializeField] private GestureClassifier classifier;

    private void Start()
    {
        classifier.OnGestureChanged += OnGestureChanged;
    }

    private void OnGestureChanged(string gesture, float confidence)
    {
        switch (gesture)
        {
            case "point":
                // Highlight UI element under ray
                break;
            case "thumbs_up":
                // Confirm action
                break;
            case "pinch":
                // Grab object
                break;
        }
    }
}
```

---

## Performance Notes

### Benchmarks (Unity Editor, M1 MacBook Pro)

| Component | Time per Frame |
|-----------|----------------|
| Feature Extraction | < 0.5ms |
| ONNX Inference | 1-3ms |
| Smoothing | < 0.1ms |
| Visualization | 0.5-1ms |
| **Total** | **~2-5ms** |

**Target FPS**: 60+ (16.67ms per frame budget)
**Gesture recognition overhead**: ~5-10% of frame budget

### Optimization Tips

1. Use `WorkerType.CSharpBurst` (fastest)
2. Reduce `smoothingWindow` if FPS drops
3. Disable `HandVisualizer` in production builds
4. Use smaller model (fewer trees/depth) if needed

---

## Testing

### Unit Testing (Editor)

```csharp
[Test]
public void TestFeatureExtraction()
{
    Vector3[] landmarks = GenerateMockLandmarks();
    float[] features = HandFeatureExtractor.Extract(landmarks);

    Assert.AreEqual(31, features.Length);
    Assert.IsTrue(features.All(f => !float.IsNaN(f)));
}
```

### Integration Testing

1. Add `MockHandTrackingProvider` to scene
2. Set test gesture in Inspector
3. Run Play mode
4. Verify correct gesture displayed in debug UI
5. Change gesture, verify ~0.25s delay
6. Try all 5 gestures

---

## Troubleshooting

**"NullReferenceException in GestureClassifier"**
- Check `modelAsset` is assigned
- Check `handTracker` reference exists
- Verify HandTrackingProvider component is active

**"Model output is wrong size"**
- Ensure ONNX model is compatible with Barracuda 3.0
- Check input tensor shape: should be [1, 31]
- Try different worker type

**"Gestures not recognized correctly"**
- Test model in Python first: `test_model.py --live`
- Compare C# features with Python features (same input)
- Check confusion matrix: which gestures are confused?

**"Low FPS"**
- Change worker type to CSharpBurst
- Reduce smoothing window
- Disable visualization in build

---

## Next Steps

- **Day 5**: Implement full interaction mechanics (see Integration examples above)
- **Day 6**: Add robustness (tracking loss, calibration, false-trigger suppression)
- **Day 7**: Polish and demo

---

## References

- Feature extraction spec: `docs/feature-extraction-spec.md`
- Unity integration guide: `docs/day4-unity-integration-guide.md`
- Unity Barracuda: https://docs.unity3d.com/Packages/com.unity.barracuda@3.0
