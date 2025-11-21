using UnityEngine;

/// <summary>
/// Extract 31 gesture-invariant features from hand landmarks.
///
/// This C# implementation matches the Python version in ml/scripts/extract_features.py
/// to ensure identical feature computation for ML model compatibility.
///
/// Features extracted:
/// - 20 inter-joint distances (4 per finger)
/// - 5 fingertip-to-wrist distances
/// - 5 finger angles (at PIP joints)
/// - 1 pinch distance (thumb-index proximity)
///
/// Total: 31 features
/// </summary>
public class HandFeatureExtractor
{
    // MediaPipe landmark indices
    private const int WRIST = 0;
    private const int THUMB_TIP = 4;
    private const int INDEX_TIP = 8;
    private const int MIDDLE_TIP = 12;
    private const int RING_TIP = 16;
    private const int PINKY_TIP = 20;

    // Finger landmark ranges (MCP to Tip)
    private static readonly int[] THUMB = { 1, 2, 3, 4 };
    private static readonly int[] INDEX = { 5, 6, 7, 8 };
    private static readonly int[] MIDDLE = { 9, 10, 11, 12 };
    private static readonly int[] RING = { 13, 14, 15, 16 };
    private static readonly int[] PINKY = { 17, 18, 19, 20 };

    private static readonly int[][] FINGERS = { THUMB, INDEX, MIDDLE, RING, PINKY };
    private static readonly int[] FINGER_TIPS = { THUMB_TIP, INDEX_TIP, MIDDLE_TIP, RING_TIP, PINKY_TIP };

    /// <summary>
    /// Extract 31 features from hand landmarks.
    /// </summary>
    /// <param name="landmarks">Array of 21 Vector3 landmarks (x, y, z positions)</param>
    /// <returns>Array of 31 features</returns>
    public static float[] Extract(Vector3[] landmarks)
    {
        if (landmarks == null || landmarks.Length != 21)
        {
            Debug.LogError("HandFeatureExtractor: Expected 21 landmarks, got " + (landmarks?.Length ?? 0));
            return new float[31]; // Return zeros on error
        }

        // Step 1: Normalize translation (wrist at origin)
        Vector3[] normalized = NormalizeTranslation(landmarks);

        // Step 2: Normalize scale (by palm size)
        Vector3[] scaled = NormalizeScale(normalized);

        // Step 3-6: Extract features
        float[] interJointDists = ComputeInterJointDistances(scaled);      // 20 features
        float[] fingertipDists = ComputeFingertipDistances(scaled);        // 5 features
        float[] fingerAngles = ComputeFingerAngles(scaled);                // 5 features
        float pinchDist = ComputePinchDistance(scaled);                    // 1 feature

        // Concatenate all features
        float[] features = new float[31];
        int index = 0;

        // Copy inter-joint distances (20)
        for (int i = 0; i < interJointDists.Length; i++)
            features[index++] = interJointDists[i];

        // Copy fingertip distances (5)
        for (int i = 0; i < fingertipDists.Length; i++)
            features[index++] = fingertipDists[i];

        // Copy finger angles (5)
        for (int i = 0; i < fingerAngles.Length; i++)
            features[index++] = fingerAngles[i];

        // Copy pinch distance (1)
        features[index++] = pinchDist;

        return features;
    }

    /// <summary>
    /// Translate all landmarks relative to wrist (landmark 0).
    /// </summary>
    private static Vector3[] NormalizeTranslation(Vector3[] landmarks)
    {
        Vector3 wrist = landmarks[WRIST];
        Vector3[] normalized = new Vector3[landmarks.Length];

        for (int i = 0; i < landmarks.Length; i++)
        {
            normalized[i] = landmarks[i] - wrist;
        }

        return normalized;
    }

    /// <summary>
    /// Normalize by palm size (wrist to middle finger MCP distance).
    /// </summary>
    private static Vector3[] NormalizeScale(Vector3[] landmarks)
    {
        Vector3 wrist = landmarks[WRIST];
        Vector3 middleMCP = landmarks[9]; // Middle finger MCP
        float palmSize = Vector3.Distance(middleMCP, wrist);

        // Avoid division by zero
        if (palmSize < 1e-6f)
            palmSize = 1.0f;

        Vector3[] scaled = new Vector3[landmarks.Length];
        for (int i = 0; i < landmarks.Length; i++)
        {
            scaled[i] = landmarks[i] / palmSize;
        }

        return scaled;
    }

    /// <summary>
    /// Compute distances between consecutive joints on each finger.
    /// Returns 20 features (4 per finger × 5 fingers).
    /// </summary>
    private static float[] ComputeInterJointDistances(Vector3[] landmarks)
    {
        float[] distances = new float[20];
        int index = 0;

        foreach (int[] finger in FINGERS)
        {
            // Add wrist as base for first joint
            int[] joints = new int[finger.Length + 1];
            joints[0] = WRIST;
            for (int i = 0; i < finger.Length; i++)
                joints[i + 1] = finger[i];

            // Compute distances between consecutive joints
            for (int i = 0; i < finger.Length; i++)
            {
                int j1 = joints[i];
                int j2 = joints[i + 1];
                distances[index++] = Vector3.Distance(landmarks[j2], landmarks[j1]);
            }
        }

        return distances;
    }

    /// <summary>
    /// Compute distance from each fingertip to wrist.
    /// Returns 5 features (1 per finger).
    /// </summary>
    private static float[] ComputeFingertipDistances(Vector3[] landmarks)
    {
        Vector3 wrist = landmarks[WRIST];
        float[] distances = new float[5];

        for (int i = 0; i < FINGER_TIPS.Length; i++)
        {
            distances[i] = Vector3.Distance(landmarks[FINGER_TIPS[i]], wrist);
        }

        return distances;
    }

    /// <summary>
    /// Compute angle at PIP joint for each finger.
    /// Returns 5 features (1 per finger, in radians).
    /// </summary>
    private static float[] ComputeFingerAngles(Vector3[] landmarks)
    {
        float[] angles = new float[5];
        int index = 0;

        foreach (int[] finger in FINGERS)
        {
            if (finger.Length >= 3)
            {
                // Use MCP, PIP, DIP (indices 0, 1, 2 of finger)
                int mcpIdx = finger[0];
                int pipIdx = finger[1];
                int dipIdx = finger[2];

                Vector3 mcp = landmarks[mcpIdx];
                Vector3 pip = landmarks[pipIdx];
                Vector3 dip = landmarks[dipIdx];

                // Vectors from PIP joint
                Vector3 v1 = mcp - pip;
                Vector3 v2 = dip - pip;

                // Compute angle using dot product
                float norm1 = v1.magnitude;
                float norm2 = v2.magnitude;

                if (norm1 < 1e-6f || norm2 < 1e-6f)
                {
                    angles[index++] = 0.0f;
                }
                else
                {
                    float cosAngle = Vector3.Dot(v1, v2) / (norm1 * norm2);
                    // Clamp to valid range for acos
                    cosAngle = Mathf.Clamp(cosAngle, -1.0f, 1.0f);
                    angles[index++] = Mathf.Acos(cosAngle); // Radians
                }
            }
            else
            {
                angles[index++] = 0.0f;
            }
        }

        return angles;
    }

    /// <summary>
    /// Compute distance between thumb tip and index tip (pinch detection).
    /// Returns 1 feature.
    /// </summary>
    private static float ComputePinchDistance(Vector3[] landmarks)
    {
        Vector3 thumbTip = landmarks[THUMB_TIP];
        Vector3 indexTip = landmarks[INDEX_TIP];
        return Vector3.Distance(indexTip, thumbTip);
    }
}
