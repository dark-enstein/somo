# Feature Extraction Mathematical Specification

## Overview

This document defines the mathematical transformations applied to raw hand-tracking data to create gesture-invariant features for ML classification.

## Input Data

**MediaPipe Hand Landmarks**: 21 3D points per hand

```
Raw input per frame:
  landmarks = [(x₀, y₀, z₀), (x₁, y₁, z₁), ..., (x₂₀, y₂₀, z₂₀)]

  Total: 21 points × 3 coords = 63 values
```

### Landmark Indices (MediaPipe Standard)
```
0:  Wrist
1-4:  Thumb (CMC, MCP, IP, Tip)
5-8:  Index (MCP, PIP, DIP, Tip)
9-12: Middle (MCP, PIP, DIP, Tip)
13-16: Ring (MCP, PIP, DIP, Tip)
17-20: Pinky (MCP, PIP, DIP, Tip)
```

---

## Feature Engineering Pipeline

### 1. Normalization (Translation Invariance)

**Goal**: Remove dependency on hand position in 3D space.

**Method**: Translate all landmarks relative to wrist (landmark 0).

```
For each landmark i:
  landmark'ᵢ = landmarkᵢ - landmark₀

Result: landmark'₀ = (0, 0, 0)  // wrist at origin
```

**Output**: 21 points, wrist-centered

---

### 2. Scale Normalization

**Goal**: Make features invariant to hand size and distance from camera.

**Method**: Normalize by palm size (distance from wrist to middle finger MCP).

```
palm_size = ||landmark₉ - landmark₀||
           = √((x₉ - x₀)² + (y₉ - y₀)² + (z₉ - z₀)²)

For each landmark i:
  landmark''ᵢ = landmark'ᵢ / palm_size
```

**Output**: 21 points, scale-normalized

---

### 3. Inter-Joint Distances (20 features)

**Goal**: Capture finger curl/extension.

**Method**: Compute Euclidean distance between consecutive joints on each finger.

```
For finger f ∈ {thumb, index, middle, ring, pinky}:
  For joint pair (j, j+1):
    dᵢ = ||landmark''ⱼ₊₁ - landmark''ⱼ||

Example (Index finger):
  d₁ = ||tip₈ - DIP₇||
  d₂ = ||DIP₇ - PIP₆||
  d₃ = ||PIP₆ - MCP₅||
  d₄ = ||MCP₅ - wrist₀||
```

**Output**: 20 distance features (4 per finger × 5 fingers)

---

### 4. Fingertip-to-Wrist Distances (5 features)

**Goal**: Measure overall finger extension.

```
For each finger f:
  tipDist_f = ||tip_f - wrist₀||

Example:
  thumbDist = ||landmark₄ - landmark₀||
  indexDist = ||landmark₈ - landmark₀||
  ...
```

**Output**: 5 distance features

---

### 5. Finger Angles (5 features)

**Goal**: Detect finger bending using joint angles.

**Method**: Compute angle at PIP joint using law of cosines.

```
For each finger f with joints (MCP, PIP, DIP):
  v₁ = MCP - PIP
  v₂ = DIP - PIP

  cos(θ) = (v₁ · v₂) / (||v₁|| × ||v₂||)

  angle_f = arccos(cos(θ))  // in radians
```

**Output**: 5 angle features (radians, range [0, π])

---

### 6. Pinch Detection (1 feature)

**Goal**: Measure thumb-index proximity for pinch gesture.

```
pinchDist = ||landmark₄ - landmark₈||
           = ||thumbTip - indexTip||
```

**Output**: 1 distance feature

---

## Final Feature Vector

**Total dimensions**: 20 + 5 + 5 + 1 = **31 features**

```python
features = [
    # Inter-joint distances (20)
    d_thumb_0, d_thumb_1, d_thumb_2, d_thumb_3,
    d_index_0, d_index_1, d_index_2, d_index_3,
    d_middle_0, d_middle_1, d_middle_2, d_middle_3,
    d_ring_0, d_ring_1, d_ring_2, d_ring_3,
    d_pinky_0, d_pinky_1, d_pinky_2, d_pinky_3,

    # Fingertip-to-wrist (5)
    thumb_tip_dist, index_tip_dist, middle_tip_dist,
    ring_tip_dist, pinky_tip_dist,

    # Finger angles (5)
    thumb_angle, index_angle, middle_angle,
    ring_angle, pinky_angle,

    # Pinch distance (1)
    pinch_dist
]
```

---

## Implementation Requirements

### Python (ML Training)
- Use NumPy for vector operations
- Apply feature extraction to entire dataset before training
- Store processed features as CSV: `ml/data/processed/features.csv`

### C# (Unity Runtime)
- Implement **identical** math using Unity's `Vector3` API
- Must produce bit-identical results to Python (for consistency)
- Real-time computation: target < 5ms per frame

---

## Validation

To ensure Python and C# implementations match:

```python
# Test vector
test_landmarks = [...] # 21 × 3 array

# Python output
features_py = extract_features(test_landmarks)

# C# output (via Unity debug log)
features_cs = extract_features_unity(test_landmarks)

# Assert
assert np.allclose(features_py, features_cs, rtol=1e-5)
```

---

## References

- MediaPipe Hand Landmarks: https://google.github.io/mediapipe/solutions/hands.html
- Feature engineering for gesture recognition: scale + rotation invariance is critical for robust classification
