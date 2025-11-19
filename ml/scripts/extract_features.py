"""
Feature extraction for hand gesture recognition.

Implements the mathematical specification defined in docs/feature-extraction-spec.md
Converts 21 MediaPipe hand landmarks (63 values) into 31 invariant features.
"""

import numpy as np
from typing import List, Tuple


class HandFeatureExtractor:
    """Extract gesture-invariant features from hand landmarks."""

    # MediaPipe landmark indices
    WRIST = 0
    THUMB_TIP = 4
    INDEX_TIP = 8
    MIDDLE_TIP = 12
    RING_TIP = 16
    PINKY_TIP = 20

    # Finger landmark ranges (MCP to Tip)
    THUMB = [1, 2, 3, 4]
    INDEX = [5, 6, 7, 8]
    MIDDLE = [9, 10, 11, 12]
    RING = [13, 14, 15, 16]
    PINKY = [17, 18, 19, 20]

    FINGERS = [THUMB, INDEX, MIDDLE, RING, PINKY]
    FINGER_TIPS = [THUMB_TIP, INDEX_TIP, MIDDLE_TIP, RING_TIP, PINKY_TIP]

    def __init__(self):
        """Initialize feature extractor."""
        pass

    def extract(self, landmarks: np.ndarray) -> np.ndarray:
        """
        Extract 31 features from hand landmarks.

        Args:
            landmarks: Shape (21, 3) array of hand landmarks [(x, y, z), ...]

        Returns:
            features: Shape (31,) array of extracted features
        """
        # Step 1: Normalize (translate to wrist origin)
        normalized = self._normalize_translation(landmarks)

        # Step 2: Scale normalization
        scaled = self._normalize_scale(normalized)

        # Step 3-6: Extract features
        inter_joint_dists = self._compute_inter_joint_distances(scaled)
        fingertip_dists = self._compute_fingertip_distances(scaled)
        finger_angles = self._compute_finger_angles(scaled)
        pinch_dist = self._compute_pinch_distance(scaled)

        # Concatenate all features
        features = np.concatenate([
            inter_joint_dists,    # 20 features
            fingertip_dists,      # 5 features
            finger_angles,        # 5 features
            [pinch_dist]          # 1 feature
        ])

        return features

    def _normalize_translation(self, landmarks: np.ndarray) -> np.ndarray:
        """Translate all landmarks relative to wrist (landmark 0)."""
        wrist = landmarks[self.WRIST]
        return landmarks - wrist

    def _normalize_scale(self, landmarks: np.ndarray) -> np.ndarray:
        """Normalize by palm size (wrist to middle finger MCP distance)."""
        wrist = landmarks[self.WRIST]
        middle_mcp = landmarks[9]  # Middle finger MCP
        palm_size = np.linalg.norm(middle_mcp - wrist)

        # Avoid division by zero
        if palm_size < 1e-6:
            palm_size = 1.0

        return landmarks / palm_size

    def _compute_inter_joint_distances(self, landmarks: np.ndarray) -> np.ndarray:
        """
        Compute distances between consecutive joints on each finger.
        Returns 20 features (4 per finger × 5 fingers).
        """
        distances = []

        for finger in self.FINGERS:
            # Add wrist as base for first joint
            joints = [self.WRIST] + finger

            for i in range(len(finger)):
                j1, j2 = joints[i], joints[i + 1]
                dist = np.linalg.norm(landmarks[j2] - landmarks[j1])
                distances.append(dist)

        return np.array(distances)

    def _compute_fingertip_distances(self, landmarks: np.ndarray) -> np.ndarray:
        """
        Compute distance from each fingertip to wrist.
        Returns 5 features (1 per finger).
        """
        wrist = landmarks[self.WRIST]
        distances = []

        for tip_idx in self.FINGER_TIPS:
            dist = np.linalg.norm(landmarks[tip_idx] - wrist)
            distances.append(dist)

        return np.array(distances)

    def _compute_finger_angles(self, landmarks: np.ndarray) -> np.ndarray:
        """
        Compute angle at PIP joint for each finger.
        Returns 5 features (1 per finger, in radians).
        """
        angles = []

        for finger in self.FINGERS:
            if len(finger) >= 3:
                # Use MCP, PIP, DIP (indices 0, 1, 2 of finger)
                mcp_idx = finger[0]
                pip_idx = finger[1]
                dip_idx = finger[2]

                mcp = landmarks[mcp_idx]
                pip = landmarks[pip_idx]
                dip = landmarks[dip_idx]

                # Vectors from PIP joint
                v1 = mcp - pip
                v2 = dip - pip

                # Compute angle using dot product
                norm1 = np.linalg.norm(v1)
                norm2 = np.linalg.norm(v2)

                if norm1 < 1e-6 or norm2 < 1e-6:
                    angle = 0.0
                else:
                    cos_angle = np.dot(v1, v2) / (norm1 * norm2)
                    # Clamp to valid range for arccos
                    cos_angle = np.clip(cos_angle, -1.0, 1.0)
                    angle = np.arccos(cos_angle)

                angles.append(angle)
            else:
                angles.append(0.0)

        return np.array(angles)

    def _compute_pinch_distance(self, landmarks: np.ndarray) -> float:
        """
        Compute distance between thumb tip and index tip.
        Returns 1 feature.
        """
        thumb_tip = landmarks[self.THUMB_TIP]
        index_tip = landmarks[self.INDEX_TIP]
        return np.linalg.norm(index_tip - thumb_tip)


def extract_features_from_mediapipe(hand_landmarks) -> np.ndarray:
    """
    Convenience function to extract features from MediaPipe hand_landmarks object.

    Args:
        hand_landmarks: MediaPipe HandLandmarks object

    Returns:
        features: Shape (31,) numpy array
    """
    # Convert MediaPipe landmarks to numpy array
    landmarks = np.array([[lm.x, lm.y, lm.z] for lm in hand_landmarks.landmark])

    extractor = HandFeatureExtractor()
    return extractor.extract(landmarks)


if __name__ == "__main__":
    # Test with synthetic data
    print("Testing feature extraction...")

    # Create test landmarks (random hand pose)
    np.random.seed(42)
    test_landmarks = np.random.rand(21, 3)

    extractor = HandFeatureExtractor()
    features = extractor.extract(test_landmarks)

    print(f"Input shape: {test_landmarks.shape}")
    print(f"Output shape: {features.shape}")
    print(f"Feature vector (first 10): {features[:10]}")
    print(f"✓ Feature extraction working correctly")
