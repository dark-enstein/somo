# Gesture Dataset

This directory contains hand gesture training data for the ML classifier.

## Directory Structure

```
data/
├── raw/                    # Individual recording sessions (CSV files)
│   ├── .gitkeep
│   └── [gesture]_[timestamp].csv
│
├── processed/              # Merged and preprocessed datasets
│   ├── .gitkeep
│   └── gestures_merged.csv
│
└── README.md               # This file
```

---

## Raw Data Format

Each CSV file in `raw/` contains samples from a single recording session:

**Filename**: `{gesture}_{timestamp}.csv`
- Example: `open_hand_20241119_103045.csv`

**Columns** (33 total):
1. `gesture` — Label (open_hand, fist, pinch, point, thumbs_up)
2. `feature_0` to `feature_30` — 31 extracted features
3. `timestamp` — ISO 8601 timestamp of capture

**Sample row**:
```csv
gesture,feature_0,feature_1,...,feature_30,timestamp
open_hand,0.523,0.412,...,0.098,2024-11-19T10:30:45.123456
```

---

## Processed Data Format

`processed/gestures_merged.csv` combines all raw CSV files:
- Same schema as raw data
- Shuffled for training
- Balanced class distribution (ideally)

---

## Data Collection Status

**Target**: 100-200 samples per gesture × 5 gestures = 500-1000 total samples

**Current Status** (update after recording):
- [ ] open_hand: 0 samples
- [ ] fist: 0 samples
- [ ] pinch: 0 samples
- [ ] point: 0 samples
- [ ] thumbs_up: 0 samples

**Total**: 0 samples

---

## How to Collect Data

Follow the [data collection protocol](../../docs/data-collection-protocol.md).

**Quick start**:
```bash
cd ml/scripts

# Record each gesture (150 samples each)
python record_gestures.py --gesture open_hand --samples 150
python record_gestures.py --gesture fist --samples 150
python record_gestures.py --gesture pinch --samples 150
python record_gestures.py --gesture point --samples 150
python record_gestures.py --gesture thumbs_up --samples 150

# Merge all recordings
python merge_dataset.py
```

---

## Data Quality Checklist

Before training (Day 3), verify:

- [ ] All 5 gesture classes have CSV files in `raw/`
- [ ] Each gesture has 100-200 samples
- [ ] `processed/gestures_merged.csv` exists
- [ ] No NaN or corrupted values in features
- [ ] Class distribution is balanced (±20%)

---

## Notes

- **Raw data is preserved**: Never delete `raw/` CSV files (allows re-merging if needed)
- **Git tracking**: Raw data CSVs are **not** committed to git (listed in `.gitignore`)
- **Processed data**: May be committed if small enough (<10 MB)
- **Privacy**: Do not share data containing identifiable information

---

## Troubleshooting

**"No CSV files found"**
- Check you're in the correct directory (`ml/data/raw/`)
- Verify recorder script ran successfully (check console output)

**"Class imbalance warning"**
- Record more samples for underrepresented gesture(s)
- Aim for ±10% balance (e.g., if one class has 200 samples, others should have 180-220)

**"Features are all zeros"**
- Check MediaPipe hand detection (green landmarks should be visible during recording)
- Verify camera is working and lighting is adequate
- Review feature extraction code for bugs

---

## Next Steps

After data collection:
1. ✓ Verify data quality using checklist above
2. → Proceed to **Day 3**: Train ML classifier
3. → Evaluate model accuracy with confusion matrix
4. → Export to ONNX for Unity integration
