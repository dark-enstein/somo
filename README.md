# SOMO — Hand-Gesture VR Interaction System

Real-time controller-free VR interactions using hand-gesture recognition.

## Overview

SOMO enables intuitive VR interactions without physical controllers by combining lightweight ML gesture classification with Unity XR. Using hand-tracking data from MediaPipe (or Oculus/Meta/Ultraleap), the system recognizes five core gestures and maps them to VR actions like menu toggling, object manipulation, and confirmation.

**Key Features**:
- 5 gesture classes: Open hand, Fist, Pinch, Point, Thumbs-up
- Real-time classification with < 50ms latency
- Interaction mechanics: radial menu, grab/rotate/scale objects
- Hardware-agnostic ML model (31-feature vector)

---

## Project Structure

```
somo/
├── unity-vr/              # Unity XR project
│   ├── Assets/
│   │   ├── Scenes/        # MainScene (XR Origin + interaction objects)
│   │   ├── Scripts/       # C# gesture classifier + interaction logic
│   │   └── Prefabs/       # Radial menu, interactable cube
│   ├── Packages/          # XR Interaction Toolkit, Barracuda
│   └── ProjectSettings/
│
├── ml/                    # Machine learning pipeline
│   ├── data/
│   │   ├── raw/           # Captured gesture samples (CSV)
│   │   └── processed/     # Feature-engineered datasets
│   ├── models/            # Trained models (ONNX, pickle)
│   ├── notebooks/         # Jupyter exploration
│   └── scripts/           # Feature extraction, training
│
└── docs/                  # Documentation
    ├── tracking-stack-decision.md
    ├── feature-extraction-spec.md
    └── ... (day-by-day implementation notes)
```

---

## Tech Stack

| Component | Technology |
|-----------|------------|
| **Hand Tracking** | MediaPipe Hands (webcam-based, 21 landmarks) |
| **ML Framework** | scikit-learn (kNN / Random Forest baseline) |
| **Model Export** | ONNX (for Unity Barracuda inference) |
| **VR Engine** | Unity 2022.3 LTS + XR Interaction Toolkit |
| **Runtime Inference** | Unity Barracuda (ONNX runtime) |

---

## 7-Day Implementation Plan

### ✅ Day 1 — Setup & Architecture
- [x] Choose tracking stack (MediaPipe)
- [x] Define folder structure
- [x] Write feature extraction math spec
- [x] Unity XR project scaffolding

### Day 2 — Data Capture
- [ ] Build gesture recorder (Python + MediaPipe)
- [ ] Capture 100–200 samples per gesture
- [ ] Store as CSV in `ml/data/raw/`

### Day 3 — Feature Engineering & ML Model
- [ ] Implement feature extractor (31 features)
- [ ] Train kNN / Random Forest classifier
- [ ] Evaluate with confusion matrix
- [ ] Export model to ONNX

### Day 4 — Unity Runtime Integration
- [ ] Re-implement feature extraction in C#
- [ ] Load ONNX model via Barracuda
- [ ] Add smoothing (majority vote + dwell time)
- [ ] Debug overlay: predicted gesture + confidence

### Day 5 — Interaction Mechanics
- [ ] Radial menu toggle (open hand)
- [ ] Pinch → grab/release cube
- [ ] Wrist roll → rotate cube
- [ ] Two-hand pinch distance → scale cube
- [ ] Thumbs-up → confirm selection

### Day 6 — Robustness + Documentation
- [ ] Personal calibration scene
- [ ] False-trigger suppression
- [ ] Handle left/right hands + lost tracking
- [ ] Complete documentation

### Day 7 — Packaging & Demo
- [ ] Build final demo executable
- [ ] Record 60–90s demo video
- [ ] Performance evaluation
- [ ] V2 roadmap

---

## Gesture Classes

| Gesture | Action | Detection Logic |
|---------|--------|-----------------|
| **Open Hand** | Toggle radial menu | All fingers extended |
| **Fist** | Idle / No action | All fingers curled |
| **Pinch** | Grab/release cube | Thumb-index distance < threshold |
| **Point** | Menu hover | Index extended, others curled |
| **Thumbs-up** | Confirm selection | Thumb extended, others curled |

---

## Getting Started

### Prerequisites
```bash
# Python environment
python -m venv ml/venv
source ml/venv/bin/activate  # or `ml\venv\Scripts\activate` on Windows
pip install mediapipe numpy scikit-learn onnx

# Unity
# Install Unity 2022.3 LTS via Unity Hub
# Open unity-vr/ folder as Unity project
```

### Running the Recorder (Day 2+)
```bash
python ml/scripts/record_gestures.py --gesture open_hand --samples 200
```

### Training the Model (Day 3+)
```bash
python ml/scripts/train_classifier.py --output ml/models/gesture_classifier.onnx
```

### Unity VR Scene (Day 4+)
1. Open `unity-vr/` in Unity Hub
2. Load `Assets/Scenes/MainScene.unity`
3. Enter Play mode (requires VR headset or simulator)

---

## Performance Targets

- **Inference Latency**: < 50ms (Unity runtime)
- **Accuracy**: > 90% on test set
- **Frame Rate**: 30+ FPS (VR rendering + gesture classification)
- **False Positive Rate**: < 5% (robust to hand movements)

---

## Future Enhancements (V2 Roadmap)

- **More Gestures**: Swipe, rotate, peace sign
- **Continuous Tracking**: LSTM for temporal patterns
- **Multi-Hand**: Simultaneous two-hand gestures
- **Haptic Feedback**: Vibration via controller-free haptics
- **Production VR**: Migrate to Oculus/Meta native hand tracking

---

## License

MIT License - see LICENSE file for details.

---

## Contributors

Built with incremental commits and modular architecture for educational and demonstration purposes.

---

**Current Status**: Day 1 Complete ✅
**Next**: Run `continue` to proceed to Day 2 (Data Capture)
