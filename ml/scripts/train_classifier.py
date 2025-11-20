"""
Train gesture classifier and export to ONNX.

Trains k-NN and Random Forest models on gesture dataset,
evaluates performance, and exports best model to ONNX for Unity.

Usage:
    python train_classifier.py
    python train_classifier.py --model rf --output ../models/gesture_rf.onnx
    python train_classifier.py --model knn --k 5 --test-size 0.2
"""

import argparse
import pickle
from pathlib import Path
from datetime import datetime

import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
from sklearn.model_selection import train_test_split, cross_val_score
from sklearn.neighbors import KNeighborsClassifier
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import (
    classification_report,
    confusion_matrix,
    accuracy_score,
    ConfusionMatrixDisplay
)
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType


class GestureClassifierTrainer:
    """Train and evaluate gesture classifiers."""

    GESTURE_LABELS = ['open_hand', 'fist', 'pinch', 'point', 'thumbs_up']

    def __init__(self, data_path: str = '../data/processed/gestures_merged.csv'):
        """
        Initialize trainer.

        Args:
            data_path: Path to merged dataset CSV
        """
        self.data_path = Path(data_path)
        self.X_train = None
        self.X_test = None
        self.y_train = None
        self.y_test = None
        self.model = None
        self.model_name = None

    def load_data(self, test_size: float = 0.2, random_state: int = 42):
        """
        Load and split dataset.

        Args:
            test_size: Fraction of data for testing
            random_state: Random seed for reproducibility
        """
        print(f"\nLoading data from {self.data_path}...")

        if not self.data_path.exists():
            raise FileNotFoundError(f"Dataset not found: {self.data_path}")

        df = pd.read_csv(self.data_path)

        # Separate features and labels
        feature_cols = [f'feature_{i}' for i in range(31)]
        X = df[feature_cols].values
        y = df['gesture'].values

        # Train-test split
        self.X_train, self.X_test, self.y_train, self.y_test = train_test_split(
            X, y, test_size=test_size, random_state=random_state, stratify=y
        )

        print(f"✓ Loaded {len(df)} samples")
        print(f"  Train: {len(self.X_train)} samples")
        print(f"  Test:  {len(self.X_test)} samples")
        print(f"  Features: {X.shape[1]}")
        print(f"  Classes: {len(np.unique(y))}")

        # Class distribution
        print("\nClass distribution (train):")
        unique, counts = np.unique(self.y_train, return_counts=True)
        for label, count in zip(unique, counts):
            print(f"  {label:15s}: {count:4d} samples")

    def train_knn(self, n_neighbors: int = 5):
        """
        Train k-Nearest Neighbors classifier.

        Args:
            n_neighbors: Number of neighbors for k-NN
        """
        print(f"\nTraining k-NN (k={n_neighbors})...")
        self.model = KNeighborsClassifier(n_neighbors=n_neighbors)
        self.model.fit(self.X_train, self.y_train)
        self.model_name = f'knn_k{n_neighbors}'
        print("✓ Training complete")

    def train_random_forest(self, n_estimators: int = 100, max_depth: int = 10):
        """
        Train Random Forest classifier.

        Args:
            n_estimators: Number of trees
            max_depth: Maximum tree depth
        """
        print(f"\nTraining Random Forest (trees={n_estimators}, depth={max_depth})...")
        self.model = RandomForestClassifier(
            n_estimators=n_estimators,
            max_depth=max_depth,
            random_state=42
        )
        self.model.fit(self.X_train, self.y_train)
        self.model_name = f'rf_t{n_estimators}_d{max_depth}'
        print("✓ Training complete")

    def evaluate(self, output_dir: str = '../models'):
        """
        Evaluate model and save confusion matrix.

        Args:
            output_dir: Directory to save evaluation plots
        """
        if self.model is None:
            raise ValueError("No model trained. Call train_knn() or train_random_forest() first.")

        print("\n" + "="*60)
        print("Model Evaluation")
        print("="*60)

        # Predictions
        y_pred = self.model.predict(self.X_test)

        # Accuracy
        accuracy = accuracy_score(self.y_test, y_pred)
        print(f"\nTest Accuracy: {accuracy*100:.2f}%")

        # Cross-validation score
        cv_scores = cross_val_score(self.model, self.X_train, self.y_train, cv=5)
        print(f"Cross-val Accuracy: {cv_scores.mean()*100:.2f}% ± {cv_scores.std()*100:.2f}%")

        # Classification report
        print("\nClassification Report:")
        print(classification_report(self.y_test, y_pred, target_names=self.GESTURE_LABELS))

        # Confusion matrix
        cm = confusion_matrix(self.y_test, y_pred, labels=self.GESTURE_LABELS)
        self._plot_confusion_matrix(cm, output_dir)

        return accuracy

    def _plot_confusion_matrix(self, cm: np.ndarray, output_dir: str):
        """Plot and save confusion matrix."""
        output_path = Path(output_dir)
        output_path.mkdir(parents=True, exist_ok=True)

        fig, ax = plt.subplots(figsize=(10, 8))
        disp = ConfusionMatrixDisplay(
            confusion_matrix=cm,
            display_labels=self.GESTURE_LABELS
        )
        disp.plot(ax=ax, cmap='Blues', values_format='d')
        plt.title(f'Confusion Matrix - {self.model_name}', fontsize=14, pad=20)
        plt.tight_layout()

        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
        filename = f'confusion_matrix_{self.model_name}_{timestamp}.png'
        filepath = output_path / filename

        plt.savefig(filepath, dpi=150)
        print(f"\n✓ Confusion matrix saved: {filepath}")
        plt.close()

    def export_onnx(self, output_path: str = '../models/gesture_classifier.onnx'):
        """
        Export model to ONNX format for Unity Barracuda.

        Args:
            output_path: Path to save ONNX model
        """
        if self.model is None:
            raise ValueError("No model trained.")

        output_file = Path(output_path)
        output_file.parent.mkdir(parents=True, exist_ok=True)

        print(f"\nExporting model to ONNX...")
        print(f"  Output: {output_file}")

        # Define input type (31 features, float32)
        initial_type = [('float_input', FloatTensorType([None, 31]))]

        # Convert to ONNX
        onnx_model = convert_sklearn(
            self.model,
            initial_types=initial_type,
            target_opset=12
        )

        # Save
        with open(output_file, 'wb') as f:
            f.write(onnx_model.SerializeToString())

        print(f"✓ ONNX export complete")
        print(f"  File size: {output_file.stat().st_size / 1024:.2f} KB")

    def save_pickle(self, output_path: str = '../models/gesture_classifier.pkl'):
        """
        Save model as pickle (for Python inference).

        Args:
            output_path: Path to save pickle file
        """
        if self.model is None:
            raise ValueError("No model trained.")

        output_file = Path(output_path)
        output_file.parent.mkdir(parents=True, exist_ok=True)

        with open(output_file, 'wb') as f:
            pickle.dump(self.model, f)

        print(f"✓ Pickle saved: {output_file}")


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(description='Train gesture classifier')
    parser.add_argument('--data', type=str, default='../data/processed/gestures_merged.csv',
                       help='Path to merged dataset CSV')
    parser.add_argument('--model', type=str, choices=['knn', 'rf'], default='rf',
                       help='Model type: knn or rf (default: rf)')
    parser.add_argument('--k', type=int, default=5,
                       help='k for k-NN (default: 5)')
    parser.add_argument('--trees', type=int, default=100,
                       help='Number of trees for Random Forest (default: 100)')
    parser.add_argument('--depth', type=int, default=10,
                       help='Max depth for Random Forest (default: 10)')
    parser.add_argument('--test-size', type=float, default=0.2,
                       help='Test set fraction (default: 0.2)')
    parser.add_argument('--output', type=str, default='../models/gesture_classifier.onnx',
                       help='Output ONNX file path')

    args = parser.parse_args()

    print("\n" + "="*60)
    print("Gesture Classifier Training")
    print("="*60)

    # Initialize trainer
    trainer = GestureClassifierTrainer(data_path=args.data)

    # Load data
    trainer.load_data(test_size=args.test_size)

    # Train model
    if args.model == 'knn':
        trainer.train_knn(n_neighbors=args.k)
    else:
        trainer.train_random_forest(n_estimators=args.trees, max_depth=args.depth)

    # Evaluate
    accuracy = trainer.evaluate(output_dir='../models')

    # Export
    trainer.export_onnx(output_path=args.output)
    trainer.save_pickle(output_path=args.output.replace('.onnx', '.pkl'))

    print("\n" + "="*60)
    print(f"Training Complete - Accuracy: {accuracy*100:.2f}%")
    print("="*60)
    print(f"\nModel saved to: {args.output}")
    print("\nNext steps:")
    print("  1. Integrate ONNX model into Unity (Day 4)")
    print("  2. Implement C# feature extraction")
    print("  3. Test real-time gesture recognition")


if __name__ == "__main__":
    main()
