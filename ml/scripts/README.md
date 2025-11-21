# ML Scripts

Python scripts for gesture data collection, training, and evaluation.

---

## Scripts Overview

| Script | Purpose | Usage |
|--------|---------|-------|
| `extract_features.py` | Feature extraction from hand landmarks | Library (imported by other scripts) |
| `record_gestures.py` | Record gesture data from webcam | `python record_gestures.py --gesture open_hand --samples 150` |
| `generate_synthetic_data.py` | Generate synthetic training data | `python generate_synthetic_data.py --samples 150` |
| `merge_dataset.py` | Merge multiple CSV files into one | `python merge_dataset.py` |
| `train_classifier.py` | Train ML model and export to ONNX | `python train_classifier.py --model rf` |
| `test_model.py` | Test trained model (dataset or live) | `python test_model.py --model ../models/gesture_classifier.pkl --live` |
| `run_pipeline.sh` | Run complete pipeline end-to-end | `bash run_pipeline.sh` |

---

## Typical Workflows

### Workflow 1: Using Synthetic Data (Testing)
```bash
# 1. Generate synthetic data
python generate_synthetic_data.py --samples 150

# 2. Merge datasets
python merge_dataset.py

# 3. Train model
python train_classifier.py --model rf

# 4. Test model
python test_model.py --model ../models/gesture_classifier.pkl --samples 10
```

**Or use the one-liner:**
```bash
bash run_pipeline.sh
```

---

### Workflow 2: Using Real Data (Production)
```bash
# 1. Record each gesture (Day 2)
python record_gestures.py --gesture open_hand --samples 150
python record_gestures.py --gesture fist --samples 150
python record_gestures.py --gesture pinch --samples 150
python record_gestures.py --gesture point --samples 150
python record_gestures.py --gesture thumbs_up --samples 150

# 2. Merge datasets
python merge_dataset.py

# 3. Train model
python train_classifier.py --model rf --trees 100 --depth 10

# 4. Test model (live webcam)
python test_model.py --model ../models/gesture_classifier.pkl --live
```

**Or use:**
```bash
# After recording gestures manually
bash run_pipeline.sh --real-data
```

---

## Script Details

### extract_features.py
**Purpose**: Convert 21 hand landmarks (63 values) into 31 gesture-invariant features.

**Features extracted**:
- 20 inter-joint distances
- 5 fingertip-to-wrist distances
- 5 finger angles
- 1 pinch distance

**Usage** (as library):
```python
from extract_features import extract_features_from_mediapipe

features = extract_features_from_mediapipe(hand_landmarks)
# Returns: np.ndarray of shape (31,)
```

---

### record_gestures.py
**Purpose**: Record gesture samples using webcam + MediaPipe.

**Arguments**:
- `--gesture`: Gesture name (open_hand, fist, pinch, point, thumbs_up)
- `--samples`: Number of samples to collect (default: 200)
- `--countdown`: Seconds before recording starts (default: 3)
- `--output`: Output directory (default: ../data/raw)

**Output**: CSV file with columns:
- `gesture` (label)
- `feature_0` ... `feature_30` (31 features)
- `timestamp`

**Example**:
```bash
python record_gestures.py --gesture pinch --samples 200 --countdown 5
```

---

### generate_synthetic_data.py
**Purpose**: Generate synthetic training data for testing pipeline.

**Arguments**:
- `--samples`: Samples per gesture (default: 150)
- `--output`: Output directory (default: ../data/raw)
- `--seed`: Random seed (default: 42)

**Output**: 5 CSV files (one per gesture) with synthetic feature vectors.

**Note**: Use for pipeline testing only. Replace with real data for production.

---

### merge_dataset.py
**Purpose**: Combine all CSV files from `ml/data/raw/` into single dataset.

**Arguments**:
- `--input`: Input directory (default: ../data/raw)
- `--output`: Output file (default: ../data/processed/gestures_merged.csv)
- `--shuffle`: Shuffle dataset (default: True)
- `--no-shuffle`: Disable shuffling

**Output**:
- Merged CSV file
- Class distribution report
- Imbalance warning if ratio > 1.5

**Example**:
```bash
python merge_dataset.py --output ../data/processed/my_dataset.csv
```

---

### train_classifier.py
**Purpose**: Train ML classifier and export to ONNX.

**Arguments**:
- `--data`: Path to dataset CSV (default: ../data/processed/gestures_merged.csv)
- `--model`: Model type: `knn` or `rf` (default: rf)
- `--k`: k for k-NN (default: 5)
- `--trees`: Number of trees for Random Forest (default: 100)
- `--depth`: Max depth for Random Forest (default: 10)
- `--test-size`: Test set fraction (default: 0.2)
- `--output`: Output ONNX file (default: ../models/gesture_classifier.onnx)

**Output**:
- ONNX model (for Unity)
- Pickle model (for Python testing)
- Confusion matrix PNG
- Training metrics (accuracy, classification report)

**Example**:
```bash
# Random Forest
python train_classifier.py --model rf --trees 150 --depth 12

# k-NN
python train_classifier.py --model knn --k 7
```

---

### test_model.py
**Purpose**: Test trained model on dataset or live webcam.

**Arguments**:
- `--model`: Path to pickle model (required)
- `--live`: Enable live webcam mode
- `--data`: Path to test dataset (default: ../data/processed/gestures_merged.csv)
- `--samples`: Number of samples to test (default: 10)

**Example**:
```bash
# Test on dataset
python test_model.py --model ../models/gesture_classifier.pkl --samples 20

# Live webcam test
python test_model.py --model ../models/gesture_classifier.pkl --live
```

---

### run_pipeline.sh
**Purpose**: Run complete ML pipeline in one command.

**Usage**:
```bash
# With synthetic data
bash run_pipeline.sh

# With real recorded data
bash run_pipeline.sh --real-data
```

**Steps executed**:
1. Generate synthetic data (or skip if --real-data)
2. Merge datasets
3. Train Random Forest classifier
4. Export to ONNX

---

## Requirements

Install dependencies:
```bash
cd ml
python -m venv venv
source venv/bin/activate  # macOS/Linux
# or: venv\Scripts\activate  # Windows

pip install -r requirements.txt
```

**Required packages**:
- mediapipe
- numpy
- opencv-python
- scikit-learn
- pandas
- matplotlib
- onnx
- skl2onnx

---

## Outputs

After running pipeline, you'll have:

```
ml/
├── data/
│   ├── raw/
│   │   ├── open_hand_*.csv
│   │   ├── fist_*.csv
│   │   ├── pinch_*.csv
│   │   ├── point_*.csv
│   │   └── thumbs_up_*.csv
│   └── processed/
│       └── gestures_merged.csv
│
└── models/
    ├── gesture_classifier.onnx      # For Unity
    ├── gesture_classifier.pkl       # For Python
    └── confusion_matrix_*.png       # Evaluation
```

---

## Troubleshooting

**Import errors**:
```bash
# Make sure virtual environment is activated
source venv/bin/activate

# Reinstall dependencies
pip install -r requirements.txt
```

**Webcam not found**:
- Check if another app is using camera
- Try different camera index: modify `cv2.VideoCapture(1)` in code

**ONNX export fails**:
- Check scikit-learn and skl2onnx compatibility
- Try: `pip install --upgrade skl2onnx`

---

## Next Steps

After training model on Day 3:
- **Day 4**: Integrate ONNX into Unity
- **Day 5**: Build VR interaction mechanics
- **Day 6**: Robustness improvements
- **Day 7**: Final demo and packaging
