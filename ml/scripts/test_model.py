"""
Test trained gesture classifier model.

Loads a trained model and tests it on sample data or real-time webcam input.

Usage:
    # Test on existing dataset
    python test_model.py --model ../models/gesture_classifier.pkl

    # Live webcam test (requires real model)
    python test_model.py --model ../models/gesture_classifier.pkl --live
"""

import argparse
import pickle
import time
from pathlib import Path

import cv2
import mediapipe as mp
import numpy as np
import pandas as pd

from extract_features import extract_features_from_mediapipe


class GestureClassifierTester:
    """Test trained gesture classifier."""

    GESTURE_LABELS = ['open_hand', 'fist', 'pinch', 'point', 'thumbs_up']

    def __init__(self, model_path: str):
        """
        Initialize tester with trained model.

        Args:
            model_path: Path to pickle model file
        """
        self.model_path = Path(model_path)

        if not self.model_path.exists():
            raise FileNotFoundError(f"Model not found: {self.model_path}")

        # Load model
        with open(self.model_path, 'rb') as f:
            self.model = pickle.load(f)

        print(f"✓ Loaded model from {self.model_path}")
        print(f"  Type: {type(self.model).__name__}")

    def test_on_dataset(self, data_path: str = '../data/processed/gestures_merged.csv',
                       n_samples: int = 10):
        """
        Test model on random samples from dataset.

        Args:
            data_path: Path to test dataset
            n_samples: Number of samples to test
        """
        print(f"\nTesting on dataset: {data_path}")
        print("="*60)

        df = pd.read_csv(data_path)

        # Sample random rows
        samples = df.sample(n=min(n_samples, len(df)))

        feature_cols = [f'feature_{i}' for i in range(31)]
        X = samples[feature_cols].values
        y_true = samples['gesture'].values

        # Predict
        y_pred = self.model.predict(X)
        probas = None

        # Get probabilities if available
        if hasattr(self.model, 'predict_proba'):
            probas = self.model.predict_proba(X)

        # Display results
        correct = 0
        for i, (true, pred) in enumerate(zip(y_true, y_pred)):
            match = "✓" if true == pred else "✗"
            print(f"{match} Sample {i+1}: True={true:15s} | Pred={pred:15s}", end="")

            if probas is not None:
                max_prob = probas[i].max()
                print(f" | Confidence={max_prob*100:5.1f}%")
            else:
                print()

            if true == pred:
                correct += 1

        accuracy = correct / len(samples) * 100
        print("="*60)
        print(f"Accuracy: {correct}/{len(samples)} = {accuracy:.1f}%")

    def test_live_webcam(self, confidence_threshold: float = 0.7):
        """
        Test model on live webcam feed.

        Args:
            confidence_threshold: Minimum confidence to display prediction
        """
        print("\nStarting live webcam test...")
        print("Press 'q' to quit")
        print("="*60)

        # Initialize MediaPipe
        mp_hands = mp.solutions.hands
        mp_drawing = mp.solutions.drawing_utils
        hands = mp_hands.Hands(
            static_image_mode=False,
            max_num_hands=1,
            min_detection_confidence=0.7,
            min_tracking_confidence=0.5
        )

        cap = cv2.VideoCapture(0)
        if not cap.isOpened():
            raise RuntimeError("Could not open webcam")

        # For FPS calculation
        prev_time = time.time()
        fps = 0

        while True:
            ret, frame = cap.read()
            if not ret:
                break

            frame = cv2.flip(frame, 1)
            h, w, _ = frame.shape

            # Convert to RGB for MediaPipe
            rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            results = hands.process(rgb_frame)

            # Process hand landmarks
            predicted_gesture = "No hand detected"
            confidence = 0.0

            if results.multi_hand_landmarks:
                for hand_landmarks in results.multi_hand_landmarks:
                    # Draw landmarks
                    mp_drawing.draw_landmarks(
                        frame, hand_landmarks, mp_hands.HAND_CONNECTIONS)

                    # Extract features
                    try:
                        features = extract_features_from_mediapipe(hand_landmarks)
                        features = features.reshape(1, -1)

                        # Predict
                        gesture_idx = self.model.predict(features)[0]
                        predicted_gesture = gesture_idx

                        # Get confidence if available
                        if hasattr(self.model, 'predict_proba'):
                            probs = self.model.predict_proba(features)[0]
                            confidence = probs.max()
                        else:
                            confidence = 1.0

                    except Exception as e:
                        predicted_gesture = f"Error: {str(e)[:20]}"
                        confidence = 0.0

            # Calculate FPS
            curr_time = time.time()
            fps = 1 / (curr_time - prev_time) if (curr_time - prev_time) > 0 else 0
            prev_time = curr_time

            # Display prediction
            color = (0, 255, 0) if confidence >= confidence_threshold else (0, 165, 255)
            cv2.putText(frame, f"Gesture: {predicted_gesture}", (10, 50),
                       cv2.FONT_HERSHEY_SIMPLEX, 1.2, color, 3)
            cv2.putText(frame, f"Confidence: {confidence*100:.1f}%", (10, 100),
                       cv2.FONT_HERSHEY_SIMPLEX, 0.8, color, 2)
            cv2.putText(frame, f"FPS: {fps:.1f}", (10, h - 20),
                       cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 2)

            # Show frame
            cv2.imshow('Gesture Classifier Test', frame)

            # Exit on 'q'
            if cv2.waitKey(1) & 0xFF == ord('q'):
                break

        cap.release()
        cv2.destroyAllWindows()
        print("\n✓ Live test complete")


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(description='Test gesture classifier')
    parser.add_argument('--model', type=str, required=True,
                       help='Path to trained model (.pkl file)')
    parser.add_argument('--live', action='store_true',
                       help='Run live webcam test')
    parser.add_argument('--data', type=str, default='../data/processed/gestures_merged.csv',
                       help='Path to test dataset')
    parser.add_argument('--samples', type=int, default=10,
                       help='Number of samples to test (default: 10)')

    args = parser.parse_args()

    tester = GestureClassifierTester(model_path=args.model)

    if args.live:
        tester.test_live_webcam()
    else:
        tester.test_on_dataset(data_path=args.data, n_samples=args.samples)


if __name__ == "__main__":
    main()
