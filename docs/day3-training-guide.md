# Day 3 — ML Training Guide

Complete guide for training the gesture classifier and exporting to ONNX.

---

## Overview

Day 3 focuses on:
1. ✓ Generating/collecting training data
2. ✓ Training k-NN and Random Forest classifiers
3. ✓ Evaluating model performance with confusion matrix
4. ✓ Exporting model to ONNX for Unity integration

---

## Quick Start (Full Pipeline)

Run the complete pipeline in one command:

```bash
cd ml/scripts

# With synthetic data (for testing)
bash run_pipeline.sh

# With real data (after recording gestures in Day 2)
bash run_pipeline.sh --real-data
```

This will:
- Generate/use existing data (750 samples)
- Merge into `ml/data/processed/gestures_merged.csv`
- Train Random Forest classifier
- Export to `ml/models/gesture_classifier.onnx`
- Save confusion matrix as PNG

---

## Step-by-Step Workflow

### Step 1: Prepare Data

#### Option A: Use Synthetic Data (for testing)
```bash
cd ml/scripts
python generate_synthetic_data.py --samples 150
```

Generates 150 samples × 5 gestures = 750 total samples with realistic feature distributions.

#### Option B: Use Real Data (recommended for production)
Follow Day 2 data collection protocol to record real gestures:
```bash
python record_gestures.py --gesture open_hand --samples 150
python record_gestures.py --gesture fist --samples 150
python record_gestures.py --gesture pinch --samples 150
python record_gestures.py --gesture point --samples 150
python record_gestures.py --gesture thumbs_up --samples 150
```

### Step 2: Merge Datasets

Combine all CSV files into single dataset:
```bash
python merge_dataset.py --output ../data/processed/gestures_merged.csv
```

**Output**: `ml/data/processed/gestures_merged.csv`
- Shuffled for training
- Class distribution report
- Warning if imbalanced

### Step 3: Train Classifier

#### Random Forest (Recommended)
```bash
python train_classifier.py \
    --model rf \
    --trees 100 \
    --depth 10 \
    --test-size 0.2 \
    --output ../models/gesture_classifier.onnx
```

**Why Random Forest?**
- Higher accuracy than k-NN (typically 95%+ vs 90%+)
- Robust to outliers
- Fast inference (< 5ms)
- Better generalization

#### k-Nearest Neighbors
```bash
python train_classifier.py \
    --model knn \
    --k 5 \
    --test-size 0.2 \
    --output ../models/gesture_knn.onnx
```

**Why k-NN?**
- Simpler model (easier to debug)
- No training time (just stores data)
- Good for small datasets
- Interpretable decisions

### Step 4: Evaluate Model

Training script automatically outputs:

1. **Accuracy Metrics**
   - Test set accuracy
   - Cross-validation score (5-fold)

2. **Classification Report**
   - Precision, Recall, F1-score per gesture
   - Support (samples per class)

3. **Confusion Matrix**
   - Saved as PNG: `ml/models/confusion_matrix_*.png`
   - Shows misclassification patterns

**Example Output**:
```
Test Accuracy: 96.67%
Cross-val Accuracy: 95.33% ± 2.11%

Classification Report:
              precision    recall  f1-score   support

   open_hand       0.97      0.97      0.97        30
        fist       0.97      1.00      0.98        30
       pinch       1.00      0.97      0.98        30
       point       0.93      0.97      0.95        30
  thumbs_up       0.97      0.93      0.95        30

    accuracy                           0.97       150
```

### Step 5: Test Model

#### Test on Dataset
```bash
python test_model.py --model ../models/gesture_classifier.pkl --samples 20
```

#### Live Webcam Test
```bash
python test_model.py --model ../models/gesture_classifier.pkl --live
```

Displays:
- Real-time gesture predictions
- Confidence score
- FPS counter
- MediaPipe hand landmarks

---

## Model Outputs

After training, you'll have:

```
ml/models/
├── gesture_classifier.onnx          # For Unity Barracuda (Day 4)
├── gesture_classifier.pkl           # For Python testing
└── confusion_matrix_rf_*.png        # Evaluation visualization
```

### ONNX Model Details
- **Input**: Float tensor `[1, 31]` (batch size 1, 31 features)
- **Output**: Class prediction (0-4) or probabilities
- **Opset**: 12 (compatible with Unity Barracuda 3.0)
- **Size**: ~50-200 KB depending on model type

---

## Interpreting Results

### Good Model (Target Metrics)
- **Test Accuracy**: > 90%
- **Cross-val Accuracy**: > 88% (within 2-3% of test)
- **Per-class F1**: > 0.85 for all gestures
- **Confusion Matrix**: Strong diagonal, minimal off-diagonal

### Warning Signs
1. **Low Accuracy (< 85%)**
   - Collect more data
   - Check feature extraction for bugs
   - Try different model hyperparameters

2. **High Variance (CV std > 5%)**
   - Dataset too small or imbalanced
   - Overfitting (reduce `max_depth` for RF, increase `k` for k-NN)

3. **Specific Gesture Fails**
   - Check confusion matrix: which gestures are confused?
   - Example: "pinch" confused with "point" → collect more varied pinch samples
   - Add more distinctive features for those gestures

4. **Overfitting (Train 99%, Test 85%)**
   - Reduce model complexity
   - Add more training data
   - Increase regularization

---

## Troubleshooting

### "FileNotFoundError: gestures_merged.csv"
- Run `python merge_dataset.py` first
- Check `ml/data/processed/` exists

### "ONNX conversion failed"
- Ensure `skl2onnx` is installed: `pip install skl2onnx`
- Check scikit-learn version compatibility
- Try different target opset: modify `target_opset=11` in code

### "All predictions are same class"
- Dataset imbalance: check class distribution
- Bug in feature extraction: verify features aren't all zeros/NaN
- Model didn't train: check for errors in training output

### "Low accuracy with synthetic data"
- Synthetic data is simplified—real data will differ
- Use synthetic for pipeline testing only
- Collect real data for production model

---

## Hyperparameter Tuning

### Random Forest
```bash
# More trees = better accuracy, slower inference
--trees 200  # Default: 100

# Deeper trees = more complex, risk of overfitting
--depth 15   # Default: 10

# Recommended ranges
--trees 50-200
--depth 5-15
```

### k-NN
```bash
# Smaller k = more flexible, may overfit
--k 3

# Larger k = smoother, may underfit
--k 9

# Recommended: odd numbers 3-9 to avoid ties
--k 5  # Default
```

---

## Feature Importance (Random Forest Only)

Add to `train_classifier.py` after training:

```python
# Print feature importance
importances = self.model.feature_importances_
for i, imp in enumerate(importances):
    if imp > 0.02:  # Only show important features
        print(f"  feature_{i}: {imp:.4f}")
```

Helps understand which features are most discriminative.

---

## Next Steps

After Day 3 completion:

1. ✓ Trained model exported to ONNX
2. ✓ Confusion matrix showing > 90% accuracy
3. ✓ Model tested on live webcam (optional but recommended)

**Ready for Day 4**: Unity runtime integration
- Load ONNX model in Unity via Barracuda
- Re-implement feature extraction in C#
- Real-time gesture recognition in VR scene

---

## Expected Deliverables

By end of Day 3:
- [ ] `ml/models/gesture_classifier.onnx` (< 200 KB)
- [ ] `ml/models/gesture_classifier.pkl`
- [ ] `ml/models/confusion_matrix_*.png`
- [ ] Test accuracy > 90%
- [ ] All 5 gestures have F1-score > 0.85

---

## Tips

1. **Start with synthetic data** to validate pipeline, then replace with real data
2. **Random Forest is recommended** for production (better accuracy + speed)
3. **Save confusion matrix** to identify which gestures need more training data
4. **Test live** before moving to Unity (easier to debug in Python)
5. **Document your accuracy** for comparison after Unity integration

---

## References

- scikit-learn Random Forest: https://scikit-learn.org/stable/modules/ensemble.html#forest
- ONNX conversion: https://github.com/onnx/sklearn-onnx
- Unity Barracuda: https://docs.unity3d.com/Packages/com.unity.barracuda@3.0
