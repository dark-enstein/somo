"""
Merge multiple gesture recording sessions into a single dataset.

Combines all CSV files from ml/data/raw/ into a single training dataset
in ml/data/processed/. Useful for aggregating multiple recording sessions.

Usage:
    python merge_dataset.py
    python merge_dataset.py --shuffle --output ../data/processed/gestures_full.csv
"""

import argparse
from pathlib import Path
import pandas as pd
import numpy as np


def merge_gesture_datasets(input_dir: str = '../data/raw',
                          output_file: str = '../data/processed/gestures_merged.csv',
                          shuffle: bool = True,
                          verbose: bool = True):
    """
    Merge all CSV files in input directory into single dataset.

    Args:
        input_dir: Directory containing individual CSV files
        output_file: Path to save merged dataset
        shuffle: Whether to shuffle rows (recommended for training)
        verbose: Print statistics
    """
    input_path = Path(input_dir)
    output_path = Path(output_file)
    output_path.parent.mkdir(parents=True, exist_ok=True)

    # Find all CSV files
    csv_files = list(input_path.glob('*.csv'))

    if not csv_files:
        print(f"No CSV files found in {input_path}")
        return

    if verbose:
        print(f"\nMerging {len(csv_files)} CSV files from {input_path}")
        print("=" * 60)

    # Load and concatenate all CSV files
    dataframes = []
    gesture_counts = {}

    for csv_file in csv_files:
        try:
            df = pd.read_csv(csv_file)
            dataframes.append(df)

            # Count samples per gesture
            gesture = df['gesture'].iloc[0] if len(df) > 0 else 'unknown'
            gesture_counts[gesture] = gesture_counts.get(gesture, 0) + len(df)

            if verbose:
                print(f"✓ {csv_file.name}: {len(df)} samples ({gesture})")

        except Exception as e:
            print(f"✗ Error loading {csv_file.name}: {e}")

    # Concatenate all dataframes
    merged_df = pd.concat(dataframes, ignore_index=True)

    # Shuffle if requested
    if shuffle:
        merged_df = merged_df.sample(frac=1, random_state=42).reset_index(drop=True)
        if verbose:
            print("\n✓ Dataset shuffled")

    # Save merged dataset
    merged_df.to_csv(output_path, index=False)

    if verbose:
        print(f"\n{'='*60}")
        print(f"Merged dataset saved to: {output_path}")
        print(f"Total samples: {len(merged_df)}")
        print(f"\nSamples per gesture:")
        for gesture, count in sorted(gesture_counts.items()):
            print(f"  {gesture:15s}: {count:4d} samples")

        # Check for class imbalance
        counts = list(gesture_counts.values())
        if len(counts) > 1:
            imbalance_ratio = max(counts) / min(counts)
            if imbalance_ratio > 1.5:
                print(f"\n⚠ Warning: Class imbalance detected (ratio: {imbalance_ratio:.2f})")
                print("  Consider collecting more samples for underrepresented gestures.")
            else:
                print(f"\n✓ Classes are balanced (ratio: {imbalance_ratio:.2f})")


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(description='Merge gesture datasets')
    parser.add_argument('--input', type=str, default='../data/raw',
                       help='Input directory with CSV files')
    parser.add_argument('--output', type=str, default='../data/processed/gestures_merged.csv',
                       help='Output merged CSV file')
    parser.add_argument('--shuffle', action='store_true', default=True,
                       help='Shuffle dataset (default: True)')
    parser.add_argument('--no-shuffle', dest='shuffle', action='store_false',
                       help='Do not shuffle dataset')

    args = parser.parse_args()

    merge_gesture_datasets(
        input_dir=args.input,
        output_file=args.output,
        shuffle=args.shuffle,
        verbose=True
    )


if __name__ == "__main__":
    main()
