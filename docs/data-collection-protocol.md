# Data Collection Protocol — Day 2

## Overview

This document outlines the procedure for capturing high-quality gesture training data using MediaPipe and the `record_gestures.py` script.

## Goals

- Collect **100-200 samples per gesture** (5 gestures × 150 samples = 750+ total)
- Ensure **variation** in hand position, rotation, lighting, and distance
- Maintain **label accuracy** (correct gesture performed during recording)
- Achieve **class balance** (similar sample counts per gesture)

---

## Gesture Definitions

Ensure you understand each gesture before recording:

| Gesture | Description | Key Characteristics |
|---------|-------------|---------------------|
| **open_hand** | All fingers extended, palm facing camera | Flat hand, fingers spread naturally |
| **fist** | All fingers curled into palm | Tight fist, thumb outside or inside |
| **pinch** | Thumb and index fingertips touching | Small gap between tips (~5-10mm) |
| **point** | Index finger extended, others curled | Clear pointing gesture, thumb relaxed |
| **thumbs_up** | Thumb extended upward, others curled | Classic "thumbs up" pose |

---

## Recording Environment

### Lighting
- **Good lighting** is critical for MediaPipe tracking quality
- Avoid backlighting (window behind you)
- Prefer diffuse lighting (no harsh shadows)
- Test: Can you clearly see your hand on the webcam feed?

### Background
- **Uncluttered background** improves tracking
- Avoid skin-toned backgrounds (reduces hand detection accuracy)
- Plain wall or neutral background recommended

### Camera Position
- Webcam at **eye level** or slightly above
- Distance: **40-80 cm** from camera
- Frame should show hand + forearm (not just fingertips)

---

## Recording Procedure

### Step 1: Setup Environment
```bash
cd ml/scripts
python -m venv ../venv  # Create virtual environment (first time only)
source ../venv/bin/activate  # Activate (macOS/Linux)
# or: ..\venv\Scripts\activate  # Windows

pip install -r ../requirements.txt
```

### Step 2: Record Each Gesture

Run the recorder for each gesture class:

```bash
# Open hand
python record_gestures.py --gesture open_hand --samples 150

# Fist
python record_gestures.py --gesture fist --samples 150

# Pinch
python record_gestures.py --gesture pinch --samples 150

# Point
python record_gestures.py --gesture point --samples 150

# Thumbs up
python record_gestures.py --gesture thumbs_up --samples 150
```

### Step 3: During Recording

**Variation is key!** For each gesture, vary:

1. **Hand Position**:
   - Left, center, right of frame
   - Top, middle, bottom of frame
   - Near, far from camera

2. **Hand Rotation**:
   - Palm facing camera
   - Palm tilted left/right
   - Slight wrist rotation

3. **Hand Size**:
   - Move closer (hand appears larger)
   - Move farther (hand appears smaller)

4. **Speed**:
   - Hold gesture steady (5-10 frames)
   - Move smoothly between variations
   - Avoid jerky movements

**Important**: Keep the gesture **recognizable**. Don't distort it beyond what a human would call that gesture.

### Step 4: Verify Recordings

After each gesture:
```bash
ls -lh ../data/raw/
# Should see: open_hand_YYYYMMDD_HHMMSS.csv, etc.
```

Check file size:
- 150 samples ≈ 15-25 KB per CSV
- If much smaller, recording may have failed

---

## Quality Checklist

Before moving to Day 3 training, verify:

- [ ] 5 CSV files in `ml/data/raw/` (one per gesture)
- [ ] Each file has 100-200 samples
- [ ] No corrupted files (can open in text editor or Excel)
- [ ] Feature values are numeric (not NaN or "error")
- [ ] Total dataset: 500-1000 samples

---

## Troubleshooting

### "Could not open webcam"
- Check if another app is using the camera (Zoom, Skype, etc.)
- Try different camera index: modify `cv2.VideoCapture(0)` → `VideoCapture(1)`

### "No hand detected" (no green circle appearing)
- Improve lighting
- Move hand closer to camera
- Ensure palm is visible (not just fingertips)
- Check MediaPipe confidence thresholds in code

### "Recording too fast/slow"
- Adjust by holding gesture steady longer
- MediaPipe processes at ~30 FPS, so 150 samples ≈ 5 seconds of recording
- Recording should feel natural, not rushed

### "Features look wrong" (e.g., all zeros)
- Check for division by zero in normalization
- Verify hand is fully visible (all 21 landmarks detected)

---

## Next Steps

After collecting data for all 5 gestures:

1. **Merge datasets**:
   ```bash
   python merge_dataset.py --output ../data/processed/gestures_merged.csv
   ```

2. **Inspect merged dataset**:
   - Open `ml/data/processed/gestures_merged.csv` in Excel/pandas
   - Verify class distribution is balanced
   - Check for outliers or corrupted rows

3. **Proceed to Day 3**: Train classifier using merged dataset

---

## Data Storage

```
ml/data/
├── raw/                              # Individual recording sessions
│   ├── open_hand_20241119_103045.csv
│   ├── fist_20241119_103201.csv
│   ├── pinch_20241119_103318.csv
│   ├── point_20241119_103442.csv
│   └── thumbs_up_20241119_103559.csv
│
└── processed/                        # Merged and ready for training
    └── gestures_merged.csv
```

---

## Tips for High-Quality Data

1. **Record in multiple sessions**: Don't do all 5 gestures in one sitting. Take breaks to avoid hand fatigue.

2. **Use both hands**: If you want the model to work for both hands, record sessions with left and right hands separately.

3. **Multiple participants**: Ask friends/family to contribute samples (increases generalization).

4. **Edge cases**: Include "messy" versions of gestures (slightly curled fingers on open_hand, loose fist, etc.).

5. **Re-record if needed**: If a recording session feels wrong (hand kept moving out of frame, poor lighting), delete the CSV and re-record.

---

## Expected Outcome

By end of Day 2, you should have:
- ✓ 5 gesture classes recorded
- ✓ 500-1000 total samples
- ✓ Merged dataset in `ml/data/processed/`
- ✓ Ready to train classifier on Day 3
