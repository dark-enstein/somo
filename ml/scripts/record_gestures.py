"""
Gesture data recorder using MediaPipe hand tracking.

Captures hand landmark data for gesture classification training.
Records samples with visual feedback and saves to CSV.

Controls:
    - Click "RECORD" button or press 'r' to start/stop recording
    - Press 'q' to quit
    - Press 's' to save current samples

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


# Global variable for mouse callback
mouse_clicked = False
click_x, click_y = 0, 0


def mouse_callback(event, x, y, flags, param):
    """Handle mouse click events."""
    global mouse_clicked, click_x, click_y
    if event == cv2.EVENT_LBUTTONDOWN:
        mouse_clicked = True
        click_x, click_y = x, y


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

    def draw_button(self, frame, text, x, y, w, h, color, text_color=(255, 255, 255)):
        """Draw a button on the frame."""
        cv2.rectangle(frame, (x, y), (x + w, y + h), color, -1)
        cv2.rectangle(frame, (x, y), (x + w, y + h), (0, 0, 0), 2)

        # Center text in button
        font = cv2.FONT_HERSHEY_SIMPLEX
        font_scale = 0.7
        thickness = 2
        text_size = cv2.getTextSize(text, font, font_scale, thickness)[0]
        text_x = x + (w - text_size[0]) // 2
        text_y = y + (h + text_size[1]) // 2
        cv2.putText(frame, text, (text_x, text_y), font, font_scale, text_color, thickness)

        return (x, y, x + w, y + h)

    def is_button_clicked(self, btn_bounds, click_x, click_y):
        """Check if a button was clicked."""
        x1, y1, x2, y2 = btn_bounds
        return x1 <= click_x <= x2 and y1 <= click_y <= y2

    def record(self, gesture: str, num_samples: int = 200):
        """
        Record gesture samples from webcam.

        Args:
            gesture: Gesture name (must be in GESTURE_LABELS)
            num_samples: Number of samples to collect
        """
        global mouse_clicked, click_x, click_y

        if gesture not in self.GESTURE_LABELS:
            raise ValueError(f"Invalid gesture. Must be one of {self.GESTURE_LABELS}")

        self.gesture_name = gesture
        self.samples = []

        cap = cv2.VideoCapture(1)
        frameWidth = 640
        frameHeight = 480
        cap.set(cv2.CAP_PROP_FRAME_WIDTH, frameWidth)
        cap.set(cv2.CAP_PROP_FRAME_HEIGHT, frameHeight)
        cap.set(cv2.CAP_PROP_BRIGHTNESS, 150)
        cap.set(cv2.CAP_PROP_FPS, 60)

        if not cap.isOpened():
            raise RuntimeError("Could not open webcam")

        # Create window and set mouse callback
        window_name = 'Gesture Recorder'
        cv2.namedWindow(window_name)
        cv2.setMouseCallback(window_name, mouse_callback)

        print(f"\n{'='*60}")
        print(f"Recording gesture: {gesture.upper()}")
        print(f"Target samples: {num_samples}")
        print(f"{'='*60}\n")
        print("Click 'RECORD' button or press 'r' to start/stop recording")
        print("Press 's' to save, 'q' to quit\n")

        recording = False

        while cap.isOpened():
            ret, frame = cap.read()
            if not ret:
                continue

            frame = cv2.flip(frame, 1)  # Mirror for natural interaction
            h, w, _ = frame.shape

            # Convert to RGB for MediaPipe
            rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            results = self.hands.process(rgb_frame)

            # Draw UI elements
            # Record button
            if recording:
                record_btn = self.draw_button(frame, "STOP", 10, 10, 100, 40, (0, 0, 255))
            else:
                record_btn = self.draw_button(frame, "RECORD", 10, 10, 100, 40, (0, 200, 0))

            # Save button
            save_btn = self.draw_button(frame, "SAVE", 120, 10, 80, 40, (200, 150, 0))

            # Status display
            status_color = (0, 255, 0) if recording else (128, 128, 128)
            status_text = "RECORDING" if recording else "PAUSED"
            cv2.putText(frame, status_text, (220, 40), cv2.FONT_HERSHEY_SIMPLEX, 0.8, status_color, 2)

            # Sample count
            cv2.putText(frame, f"Samples: {len(self.samples)}/{num_samples}", (10, 80),
                       cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2)

            # Display gesture name
            cv2.putText(frame, f"Gesture: {gesture.upper()}", (10, h - 20),
                       cv2.FONT_HERSHEY_SIMPLEX, 0.8, (255, 255, 255), 2)

            # Instructions
            cv2.putText(frame, "R: Record | S: Save | Q: Quit", (w - 280, h - 20),
                       cv2.FONT_HERSHEY_SIMPLEX, 0.5, (200, 200, 200), 1)

            # Check for button clicks
            if mouse_clicked:
                if self.is_button_clicked(record_btn, click_x, click_y):
                    recording = not recording
                    if recording:
                        print("Recording started!")
                    else:
                        print(f"Recording paused. Samples: {len(self.samples)}")
                elif self.is_button_clicked(save_btn, click_x, click_y):
                    if self.samples:
                        self._save_samples()
                        print(f"Saved {len(self.samples)} samples")
                    else:
                        print("No samples to save")
                mouse_clicked = False

            # Draw hand landmarks (always show for preview)
            if results.multi_hand_landmarks:
                for hand_landmarks in results.multi_hand_landmarks:
                    self.mp_drawing.draw_landmarks(
                        frame, hand_landmarks, self.mp_hands.HAND_CONNECTIONS)

                    # Only record if recording is active
                    if recording:
                        try:
                            features = extract_features_from_mediapipe(hand_landmarks)
                            self.samples.append({
                                'gesture': gesture,
                                'features': features,
                                'timestamp': datetime.now().isoformat()
                            })
                            # Visual feedback - green circle when recording
                            cv2.circle(frame, (w - 30, 30), 15, (0, 255, 0), -1)
                        except Exception as e:
                            print(f"Error extracting features: {e}")
            else:
                # No hand detected indicator
                cv2.putText(frame, "No hand detected", (w//2 - 80, h//2),
                           cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 0, 255), 2)

            # Show frame
            cv2.imshow(window_name, frame)

            # Check for keyboard input
            key = cv2.waitKey(1) & 0xFF
            if key == ord('q'):
                print("\nQuitting...")
                break
            elif key == ord('r'):
                recording = not recording
                if recording:
                    print("Recording started!")
                else:
                    print(f"Recording paused. Samples: {len(self.samples)}")
            elif key == ord('s'):
                if self.samples:
                    self._save_samples()
                else:
                    print("No samples to save")
            elif len(self.samples) >= num_samples:
                print(f"\n✓ Collected {num_samples} samples!")
                recording = False

        cap.release()
        cv2.destroyAllWindows()

        # Auto-save if there are unsaved samples
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
    parser.add_argument('--output', type=str, default='../data/raw',
                       help='Output directory for CSV files')

    args = parser.parse_args()

    recorder = GestureRecorder(output_dir=args.output)
    recorder.record(
        gesture=args.gesture,
        num_samples=args.samples
    )


if __name__ == "__main__":
    main()
