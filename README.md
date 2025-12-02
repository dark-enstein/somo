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
    ├── ml
    └── unity-vr (in-progress)
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

### Running the Recorder
```bash
python ml/scripts/record_gestures.py --gesture open_hand --samples 200
```

### Training the Model
```bash
python ml/scripts/train_classifier.py --output ml/models/gesture_classifier.onnx
```

### Unity VR Scene
1. Open `unity-vr/` in Unity Hub
2. Load `Assets/Scenes/MainScene.unity`
3. Enter Play mode (requires VR headset or simulator)

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