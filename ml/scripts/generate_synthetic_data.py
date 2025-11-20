"""
Generate synthetic gesture data for testing the ML pipeline.

Creates realistic-looking feature vectors for each gesture class
with appropriate characteristics. Useful for testing training pipeline
before real data collection is complete.

Usage:
    python generate_synthetic_data.py --samples 150
"""

import argparse
import csv
from datetime import datetime
from pathlib import Path
import numpy as np


class SyntheticGestureGenerator:
    """Generate synthetic gesture features based on gesture characteristics."""

    GESTURES = ['open_hand', 'fist', 'pinch', 'point', 'thumbs_up']

    def __init__(self, seed: int = 42):
        """Initialize generator with random seed for reproducibility."""
        np.random.seed(seed)

    def generate_open_hand(self, n_samples: int) -> np.ndarray:
        """
        Generate open hand features.
        - All fingers extended → large fingertip distances
        - Fingers spread → larger inter-joint distances
        - Wide pinch distance
        """
        features = []
        for _ in range(n_samples):
            # Inter-joint distances (20): extended fingers
            inter_joint = np.random.uniform(0.15, 0.25, 20)

            # Fingertip distances (5): far from wrist
            fingertip = np.random.uniform(0.7, 1.0, 5)

            # Finger angles (5): straighter (closer to π)
            angles = np.random.uniform(2.5, 3.0, 5)

            # Pinch distance (1): large (fingers not touching)
            pinch = np.random.uniform(0.4, 0.7, 1)

            features.append(np.concatenate([inter_joint, fingertip, angles, pinch]))

        return np.array(features)

    def generate_fist(self, n_samples: int) -> np.ndarray:
        """
        Generate fist features.
        - All fingers curled → small fingertip distances
        - Tight curl → smaller inter-joint distances
        - Small pinch distance (thumb close to index)
        """
        features = []
        for _ in range(n_samples):
            # Inter-joint distances (20): curled fingers
            inter_joint = np.random.uniform(0.08, 0.15, 20)

            # Fingertip distances (5): close to wrist
            fingertip = np.random.uniform(0.2, 0.4, 5)

            # Finger angles (5): more bent (smaller angles)
            angles = np.random.uniform(0.5, 1.5, 5)

            # Pinch distance (1): small (thumb near fingers)
            pinch = np.random.uniform(0.1, 0.3, 1)

            features.append(np.concatenate([inter_joint, fingertip, angles, pinch]))

        return np.array(features)

    def generate_pinch(self, n_samples: int) -> np.ndarray:
        """
        Generate pinch features.
        - Thumb and index extended, touching
        - Other fingers curled
        - Very small pinch distance
        """
        features = []
        for _ in range(n_samples):
            # Inter-joint distances (20): thumb+index extended, others curled
            thumb_index = np.random.uniform(0.15, 0.22, 8)  # Thumb + Index
            others = np.random.uniform(0.08, 0.14, 12)      # Middle, Ring, Pinky
            inter_joint = np.concatenate([thumb_index, others])

            # Fingertip distances (5): thumb+index far, others close
            fingertip = np.array([
                np.random.uniform(0.5, 0.7),   # Thumb
                np.random.uniform(0.5, 0.7),   # Index
                np.random.uniform(0.2, 0.4),   # Middle
                np.random.uniform(0.2, 0.4),   # Ring
                np.random.uniform(0.2, 0.4)    # Pinky
            ])

            # Finger angles (5): thumb+index straighter, others bent
            angles = np.array([
                np.random.uniform(2.0, 2.8),   # Thumb
                np.random.uniform(2.0, 2.8),   # Index
                np.random.uniform(0.5, 1.5),   # Middle
                np.random.uniform(0.5, 1.5),   # Ring
                np.random.uniform(0.5, 1.5)    # Pinky
            ])

            # Pinch distance (1): very small (touching)
            pinch = np.random.uniform(0.01, 0.08, 1)

            features.append(np.concatenate([inter_joint, fingertip, angles, pinch]))

        return np.array(features)

    def generate_point(self, n_samples: int) -> np.ndarray:
        """
        Generate point features.
        - Index finger extended
        - Thumb relaxed/out
        - Other fingers curled
        """
        features = []
        for _ in range(n_samples):
            # Inter-joint distances (20): only index extended
            thumb = np.random.uniform(0.10, 0.18, 4)       # Thumb (semi-extended)
            index = np.random.uniform(0.18, 0.24, 4)       # Index (extended)
            others = np.random.uniform(0.08, 0.14, 12)     # Others (curled)
            inter_joint = np.concatenate([thumb, index, others])

            # Fingertip distances (5): only index far
            fingertip = np.array([
                np.random.uniform(0.4, 0.6),   # Thumb
                np.random.uniform(0.7, 0.9),   # Index
                np.random.uniform(0.2, 0.4),   # Middle
                np.random.uniform(0.2, 0.4),   # Ring
                np.random.uniform(0.2, 0.4)    # Pinky
            ])

            # Finger angles (5): only index straight
            angles = np.array([
                np.random.uniform(1.5, 2.2),   # Thumb
                np.random.uniform(2.5, 3.0),   # Index
                np.random.uniform(0.5, 1.5),   # Middle
                np.random.uniform(0.5, 1.5),   # Ring
                np.random.uniform(0.5, 1.5)    # Pinky
            ])

            # Pinch distance (1): medium (index away from thumb)
            pinch = np.random.uniform(0.3, 0.5, 1)

            features.append(np.concatenate([inter_joint, fingertip, angles, pinch]))

        return np.array(features)

    def generate_thumbs_up(self, n_samples: int) -> np.ndarray:
        """
        Generate thumbs up features.
        - Thumb extended upward
        - All other fingers curled
        """
        features = []
        for _ in range(n_samples):
            # Inter-joint distances (20): only thumb extended
            thumb = np.random.uniform(0.18, 0.25, 4)       # Thumb (extended)
            others = np.random.uniform(0.08, 0.14, 16)     # All others (curled)
            inter_joint = np.concatenate([thumb, others])

            # Fingertip distances (5): only thumb far
            fingertip = np.array([
                np.random.uniform(0.7, 0.9),   # Thumb
                np.random.uniform(0.2, 0.4),   # Index
                np.random.uniform(0.2, 0.4),   # Middle
                np.random.uniform(0.2, 0.4),   # Ring
                np.random.uniform(0.2, 0.4)    # Pinky
            ])

            # Finger angles (5): thumb straight, others bent
            angles = np.array([
                np.random.uniform(2.5, 3.0),   # Thumb
                np.random.uniform(0.5, 1.5),   # Index
                np.random.uniform(0.5, 1.5),   # Middle
                np.random.uniform(0.5, 1.5),   # Ring
                np.random.uniform(0.5, 1.5)    # Pinky
            ])

            # Pinch distance (1): large (thumb far from index)
            pinch = np.random.uniform(0.5, 0.8, 1)

            features.append(np.concatenate([inter_joint, fingertip, angles, pinch]))

        return np.array(features)

    def generate_dataset(self, samples_per_gesture: int = 150,
                        output_dir: str = '../data/raw') -> dict:
        """
        Generate complete synthetic dataset for all gestures.

        Args:
            samples_per_gesture: Number of samples per gesture class
            output_dir: Directory to save CSV files

        Returns:
            Dictionary with gesture names and sample counts
        """
        output_path = Path(output_dir)
        output_path.mkdir(parents=True, exist_ok=True)

        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
        stats = {}

        generators = {
            'open_hand': self.generate_open_hand,
            'fist': self.generate_fist,
            'pinch': self.generate_pinch,
            'point': self.generate_point,
            'thumbs_up': self.generate_thumbs_up
        }

        for gesture, generator_func in generators.items():
            # Generate features
            features = generator_func(samples_per_gesture)

            # Save to CSV
            filename = f"{gesture}_synthetic_{timestamp}.csv"
            filepath = output_path / filename

            with open(filepath, 'w', newline='') as f:
                writer = csv.writer(f)

                # Header
                header = ['gesture'] + [f'feature_{i}' for i in range(31)] + ['timestamp']
                writer.writerow(header)

                # Data rows
                for feat in features:
                    row = [gesture] + list(feat) + [datetime.now().isoformat()]
                    writer.writerow(row)

            stats[gesture] = len(features)
            print(f"✓ Generated {gesture}: {len(features)} samples → {filename}")

        return stats


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(description='Generate synthetic gesture data')
    parser.add_argument('--samples', type=int, default=150,
                       help='Samples per gesture (default: 150)')
    parser.add_argument('--output', type=str, default='../data/raw',
                       help='Output directory')
    parser.add_argument('--seed', type=int, default=42,
                       help='Random seed for reproducibility')

    args = parser.parse_args()

    print(f"\n{'='*60}")
    print("Generating Synthetic Gesture Dataset")
    print(f"{'='*60}\n")

    generator = SyntheticGestureGenerator(seed=args.seed)
    stats = generator.generate_dataset(
        samples_per_gesture=args.samples,
        output_dir=args.output
    )

    print(f"\n{'='*60}")
    print(f"Total samples: {sum(stats.values())}")
    print(f"Saved to: {args.output}")
    print(f"{'='*60}\n")
    print("Next step: python merge_dataset.py")


if __name__ == "__main__":
    main()
