#!/bin/bash
#
# Complete ML pipeline for gesture classification
# Generates synthetic data, merges it, and trains a model
#
# Usage:
#   bash run_pipeline.sh
#   bash run_pipeline.sh --real-data  # Skip synthetic generation

set -e  # Exit on error

echo "=========================================="
echo "Gesture Classification ML Pipeline"
echo "=========================================="
echo ""

# Parse arguments
USE_REAL_DATA=false
if [[ "$1" == "--real-data" ]]; then
    USE_REAL_DATA=true
fi

# Step 1: Generate or verify data
if [ "$USE_REAL_DATA" = false ]; then
    echo "[Step 1/3] Generating synthetic gesture data..."
    python generate_synthetic_data.py --samples 150
    echo ""
else
    echo "[Step 1/3] Using existing real data (skipping generation)..."
    echo ""
fi

# Step 2: Merge datasets
echo "[Step 2/3] Merging datasets..."
python merge_dataset.py --output ../data/processed/gestures_merged.csv
echo ""

# Step 3: Train model
echo "[Step 3/3] Training classifier..."
python train_classifier.py \
    --model rf \
    --trees 100 \
    --depth 10 \
    --test-size 0.2 \
    --output ../models/gesture_classifier.onnx
echo ""

echo "=========================================="
echo "Pipeline Complete!"
echo "=========================================="
echo ""
echo "Outputs:"
echo "  - Dataset: ml/data/processed/gestures_merged.csv"
echo "  - ONNX Model: ml/models/gesture_classifier.onnx"
echo "  - Pickle Model: ml/models/gesture_classifier.pkl"
echo "  - Confusion Matrix: ml/models/confusion_matrix_*.png"
echo ""
echo "Next: Integrate ONNX model into Unity (Day 4)"
