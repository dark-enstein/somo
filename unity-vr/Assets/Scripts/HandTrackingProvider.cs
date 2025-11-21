using UnityEngine;

/// <summary>
/// Abstract base class for hand tracking data providers.
///
/// Supports multiple tracking backends:
/// - MediaPipe (via external process or plugin)
/// - Oculus/Meta Hand Tracking
/// - Ultraleap
/// - Mock data (for testing)
///
/// Implement this interface for each tracking system.
/// </summary>
public abstract class HandTrackingProvider : MonoBehaviour
{
    /// <summary>
    /// Handedness of the tracked hand.
    /// </summary>
    public enum Handedness
    {
        Left,
        Right
    }

    /// <summary>
    /// Is hand currently being tracked?
    /// </summary>
    public abstract bool IsTracking { get; }

    /// <summary>
    /// Which hand is being tracked.
    /// </summary>
    public abstract Handedness TrackedHand { get; }

    /// <summary>
    /// Get current hand landmarks (21 Vector3 positions).
    /// Returns null if hand is not being tracked.
    /// </summary>
    public abstract Vector3[] GetLandmarks();

    /// <summary>
    /// Confidence score for tracking quality (0-1).
    /// </summary>
    public virtual float TrackingConfidence => IsTracking ? 1.0f : 0.0f;
}


/// <summary>
/// Mock hand tracking provider for testing without hardware.
/// Generates synthetic hand poses that can be controlled via inspector.
/// </summary>
public class MockHandTrackingProvider : HandTrackingProvider
{
    [Header("Mock Settings")]
    [SerializeField] private Handedness handedness = Handedness.Right;
    [SerializeField] private bool isTracking = true;

    [Header("Test Gestures")]
    [SerializeField] private GestureType testGesture = GestureType.OpenHand;

    public enum GestureType
    {
        OpenHand,
        Fist,
        Pinch,
        Point,
        ThumbsUp
    }

    public override bool IsTracking => isTracking;
    public override Handedness TrackedHand => handedness;

    /// <summary>
    /// Generate synthetic landmarks for the selected test gesture.
    /// </summary>
    public override Vector3[] GetLandmarks()
    {
        if (!isTracking)
            return null;

        // Generate landmarks based on selected gesture
        return GenerateMockLandmarks(testGesture);
    }

    /// <summary>
    /// Generate plausible hand landmarks for a given gesture.
    /// These are approximate positions for testing only.
    /// </summary>
    private Vector3[] GenerateMockLandmarks(GestureType gesture)
    {
        Vector3[] landmarks = new Vector3[21];

        // Wrist position (origin)
        landmarks[0] = Vector3.zero;

        switch (gesture)
        {
            case GestureType.OpenHand:
                GenerateOpenHandPose(landmarks);
                break;

            case GestureType.Fist:
                GenerateFistPose(landmarks);
                break;

            case GestureType.Pinch:
                GeneratePinchPose(landmarks);
                break;

            case GestureType.Point:
                GeneratePointPose(landmarks);
                break;

            case GestureType.ThumbsUp:
                GenerateThumbsUpPose(landmarks);
                break;
        }

        return landmarks;
    }

    private void GenerateOpenHandPose(Vector3[] landmarks)
    {
        // Thumb (extended)
        landmarks[1] = new Vector3(0.05f, 0.02f, 0.01f);
        landmarks[2] = new Vector3(0.08f, 0.04f, 0.02f);
        landmarks[3] = new Vector3(0.10f, 0.06f, 0.03f);
        landmarks[4] = new Vector3(0.12f, 0.08f, 0.04f);

        // Index (extended)
        landmarks[5] = new Vector3(0.02f, 0.08f, 0.0f);
        landmarks[6] = new Vector3(0.02f, 0.12f, 0.0f);
        landmarks[7] = new Vector3(0.02f, 0.16f, 0.0f);
        landmarks[8] = new Vector3(0.02f, 0.20f, 0.0f);

        // Middle (extended)
        landmarks[9] = new Vector3(0.0f, 0.08f, 0.0f);
        landmarks[10] = new Vector3(0.0f, 0.13f, 0.0f);
        landmarks[11] = new Vector3(0.0f, 0.17f, 0.0f);
        landmarks[12] = new Vector3(0.0f, 0.22f, 0.0f);

        // Ring (extended)
        landmarks[13] = new Vector3(-0.02f, 0.08f, 0.0f);
        landmarks[14] = new Vector3(-0.02f, 0.12f, 0.0f);
        landmarks[15] = new Vector3(-0.02f, 0.16f, 0.0f);
        landmarks[16] = new Vector3(-0.02f, 0.20f, 0.0f);

        // Pinky (extended)
        landmarks[17] = new Vector3(-0.04f, 0.07f, 0.0f);
        landmarks[18] = new Vector3(-0.04f, 0.10f, 0.0f);
        landmarks[19] = new Vector3(-0.04f, 0.13f, 0.0f);
        landmarks[20] = new Vector3(-0.04f, 0.16f, 0.0f);
    }

    private void GenerateFistPose(Vector3[] landmarks)
    {
        // Thumb (curled in)
        landmarks[1] = new Vector3(0.04f, 0.02f, 0.01f);
        landmarks[2] = new Vector3(0.05f, 0.03f, 0.02f);
        landmarks[3] = new Vector3(0.06f, 0.04f, 0.03f);
        landmarks[4] = new Vector3(0.07f, 0.05f, 0.04f);

        // All fingers curled
        for (int finger = 1; finger <= 4; finger++)
        {
            int baseIdx = 1 + finger * 4;
            float x = -0.02f + finger * 0.01f;
            landmarks[baseIdx + 0] = new Vector3(x, 0.04f, 0.0f);
            landmarks[baseIdx + 1] = new Vector3(x, 0.06f, -0.02f);
            landmarks[baseIdx + 2] = new Vector3(x, 0.06f, -0.04f);
            landmarks[baseIdx + 3] = new Vector3(x, 0.05f, -0.05f);
        }
    }

    private void GeneratePinchPose(Vector3[] landmarks)
    {
        // Thumb (extended)
        landmarks[1] = new Vector3(0.04f, 0.02f, 0.01f);
        landmarks[2] = new Vector3(0.06f, 0.04f, 0.02f);
        landmarks[3] = new Vector3(0.07f, 0.06f, 0.03f);
        landmarks[4] = new Vector3(0.08f, 0.08f, 0.04f);

        // Index (extended, tip close to thumb)
        landmarks[5] = new Vector3(0.02f, 0.06f, 0.0f);
        landmarks[6] = new Vector3(0.03f, 0.08f, 0.01f);
        landmarks[7] = new Vector3(0.05f, 0.09f, 0.02f);
        landmarks[8] = new Vector3(0.08f, 0.09f, 0.04f); // Close to thumb tip

        // Other fingers curled
        for (int finger = 2; finger <= 4; finger++)
        {
            int baseIdx = 1 + finger * 4;
            float x = -0.02f + (finger - 2) * 0.01f;
            landmarks[baseIdx + 0] = new Vector3(x, 0.04f, 0.0f);
            landmarks[baseIdx + 1] = new Vector3(x, 0.06f, -0.02f);
            landmarks[baseIdx + 2] = new Vector3(x, 0.06f, -0.04f);
            landmarks[baseIdx + 3] = new Vector3(x, 0.05f, -0.05f);
        }
    }

    private void GeneratePointPose(Vector3[] landmarks)
    {
        // Thumb (semi-extended)
        landmarks[1] = new Vector3(0.05f, 0.02f, 0.01f);
        landmarks[2] = new Vector3(0.07f, 0.03f, 0.02f);
        landmarks[3] = new Vector3(0.08f, 0.04f, 0.03f);
        landmarks[4] = new Vector3(0.09f, 0.05f, 0.04f);

        // Index (fully extended)
        landmarks[5] = new Vector3(0.02f, 0.08f, 0.0f);
        landmarks[6] = new Vector3(0.02f, 0.12f, 0.0f);
        landmarks[7] = new Vector3(0.02f, 0.16f, 0.0f);
        landmarks[8] = new Vector3(0.02f, 0.20f, 0.0f);

        // Other fingers curled
        for (int finger = 2; finger <= 4; finger++)
        {
            int baseIdx = 1 + finger * 4;
            float x = -0.02f + (finger - 2) * 0.01f;
            landmarks[baseIdx + 0] = new Vector3(x, 0.04f, 0.0f);
            landmarks[baseIdx + 1] = new Vector3(x, 0.06f, -0.02f);
            landmarks[baseIdx + 2] = new Vector3(x, 0.06f, -0.04f);
            landmarks[baseIdx + 3] = new Vector3(x, 0.05f, -0.05f);
        }
    }

    private void GenerateThumbsUpPose(Vector3[] landmarks)
    {
        // Thumb (extended upward)
        landmarks[1] = new Vector3(0.03f, 0.03f, 0.0f);
        landmarks[2] = new Vector3(0.04f, 0.06f, 0.0f);
        landmarks[3] = new Vector3(0.04f, 0.10f, 0.0f);
        landmarks[4] = new Vector3(0.04f, 0.14f, 0.0f);

        // All other fingers curled
        for (int finger = 1; finger <= 4; finger++)
        {
            int baseIdx = 1 + finger * 4;
            float x = -0.02f + finger * 0.01f;
            landmarks[baseIdx + 0] = new Vector3(x, 0.04f, 0.0f);
            landmarks[baseIdx + 1] = new Vector3(x, 0.06f, -0.02f);
            landmarks[baseIdx + 2] = new Vector3(x, 0.06f, -0.04f);
            landmarks[baseIdx + 3] = new Vector3(x, 0.05f, -0.05f);
        }
    }

    // Allow switching gestures at runtime for testing
    public void SetTestGesture(GestureType gesture)
    {
        testGesture = gesture;
    }

    public void SetTracking(bool enabled)
    {
        isTracking = enabled;
    }
}
