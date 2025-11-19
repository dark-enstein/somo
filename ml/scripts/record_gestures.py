"""
Gesture data recorder using MediaPipe hand tracking.

Captures hand landmark data for gesture classification training.
Records samples with visual feedback and saves to CSV.

Usage:
    python record_gestures.py --gesture open_hand --samples 200
    python record_gestures.py --gesture fist --samples 200
    python record_gestures.py --gesture pinch --samples 200
    python record_gestures.py --gesture point --samples 200
    python record_gestures.py --gesture thumbs_up --samples 200
"""

import argparse
import csv
import os
import time
from datetime import datetime
from pathlib import Path

import cv2
import mediapipe as mp
import numpy as np

from extract_features import extract_features_from_mediapipe


class GestureRecorder:
    """Records gesture samples with MediaPipe hand tracking."""

    GESTURE_LABELS = ['open_hand', 'fist', 'pinch', 'point', 'thumbs_up']

    def __init__(self, output_dir: str = '../data/raw'):
        """
        Initialize recorder.

        Args:
            output_dir: Directory to save CSV files
        """
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)

        # Initialize MediaPipe
        self.mp_hands = mp.solutions.hands
        self.mp_drawing = mp.solutions.drawing_utils
        self.hands = self.mp_hands.Hands(
            static_image_mode=False,
            max_num_hands=1,
            min_detection_confidence=0.7,
            min_tracking_confidence=0.5
        )

        # Data storage
        self.samples = []
        self.gesture_name = None

    def record(self, gesture: str, num_samples: int = 200, countdown: int = 3):
        """
        Record gesture samples from webcam.

        Args:
            gesture: Gesture name (must be in GESTURE_LABELS)
            num_samples: Number of samples to collect
            countdown: Countdown seconds before recording starts
        """
        if gesture not in self.GESTURE_LABELS:
            raise ValueError(f"Invalid gesture. Must be one of {self.GESTURE_LABELS}")

        self.gesture_name = gesture
        self.samples = []

        cap = cv2.VideoCapture(0)
        if not cap.isOpened():
            raise RuntimeError("Could not open webcam")

        print(f"\n{'='*60}")
        print(f"Recording gesture: {gesture.upper()}")
        print(f"Target samples: {num_samples}")
        print(f"{'='*60}\n")

        # Countdown phase
        print(f"Get ready! Recording starts in {countdown} seconds...")
        start_time = time.time()
        recording = False

        while True:
            ret, frame = cap.read()
            if not ret:
                break

            frame = cv2.flip(frame, 1)  # Mirror for natural interaction
            h, w, _ = frame.shape

            # Convert to RGB for MediaPipe
            rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            results = self.hands.process(rgb_frame)

            # Countdown logic
            elapsed = time.time() - start_time
            if not recording and elapsed >= countdown:
                recording = True
                print("Recording started!")

            # Display countdown or recording status
            if not recording:
                countdown_text = f"Starting in {countdown - int(elapsed)}..."
                cv2.putText(frame, countdown_text, (w//2 - 150, h//2),
                           cv2.FONT_HERSHEY_SIMPLEX, 1.5, (0, 255, 255), 3)
            else:
                status_text = f"Recording: {len(self.samples)}/{num_samples}"
                cv2.putText(frame, status_text, (10, 50),
                           cv2.FONT_HERSHEY_SIMPLEX, 1.2, (0, 255, 0), 3)

            # Display gesture name
            cv2.putText(frame, f"Gesture: {gesture.upper()}", (10, h - 20),
                       cv2.FONT_HERSHEY_SIMPLEX, 0.8, (255, 255, 255), 2)

            # Process hand landmarks
            if results.multi_hand_landmarks and recording:
                for hand_landmarks in results.multi_hand_landmarks:
                    # Draw landmarks
                    self.mp_drawing.draw_landmarks(
                        frame, hand_landmarks, self.mp_hands.HAND_CONNECTIONS)

                    # Extract features
                    try:
                        features = extract_features_from_mediapipe(hand_landmarks)

                        # Store sample
                        self.samples.append({
                            'gesture': gesture,
                            'features': features,
                            'timestamp': datetime.now().isoformat()
                        })

                        # Visual feedback
                        cv2.circle(frame, (w - 50, 50), 20, (0, 255, 0), -1)

                    except Exception as e:
                        print(f"Error extracting features: {e}")

            # Show frame
            cv2.imshow('Gesture Recorder', frame)

            # Check for exit or completion
            key = cv2.waitKey(1) & 0xFF
            if key == ord('q'):
                print("\nRecording cancelled by user.")
                break
            elif len(self.samples) >= num_samples:
                print(f"\n✓ Collected {num_samples} samples!")
                break

        cap.release()
        cv2.destroyAllWindows()

        # Save data
        if self.samples:
            self._save_samples()

    def _save_samples(self):
        """Save collected samples to CSV file."""
        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
        filename = f"{self.gesture_name}_{timestamp}.csv"
        filepath = self.output_dir / filename

        print(f"\nSaving {len(self.samples)} samples to {filepath}...")

        with open(filepath, 'w', newline='') as f:
            writer = csv.writer(f)

            # Header: gesture, feature_0, feature_1, ..., feature_30, timestamp
            header = ['gesture'] + [f'feature_{i}' for i in range(31)] + ['timestamp']
            writer.writerow(header)

            # Data rows
            for sample in self.samples:
                row = [sample['gesture']] + list(sample['features']) + [sample['timestamp']]
                writer.writerow(row)

        print(f"✓ Saved successfully!")
        print(f"  File: {filepath}")
        print(f"  Samples: {len(self.samples)}")


def main():
    """Main entry point for gesture recorder."""
    parser = argparse.ArgumentParser(description='Record gesture training data')
    parser.add_argument('--gesture', type=str, required=True,
                       choices=GestureRecorder.GESTURE_LABELS,
                       help='Gesture to record')
    parser.add_argument('--samples', type=int, default=200,
                       help='Number of samples to collect (default: 200)')
    parser.add_argument('--countdown', type=int, default=3,
                       help='Countdown before recording starts (default: 3)')
    parser.add_argument('--output', type=str, default='../data/raw',
                       help='Output directory for CSV files')

    args = parser.parse_args()

    recorder = GestureRecorder(output_dir=args.output)
    recorder.record(
        gesture=args.gesture,
        num_samples=args.samples,
        countdown=args.countdown
    )


if __name__ == "__main__":
    main()
