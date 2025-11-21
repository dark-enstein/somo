using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Debug UI overlay for gesture recognition system.
///
/// Displays:
/// - Current predicted gesture
/// - Confidence score
/// - Hand tracking status
/// - FPS counter
/// - Feature vector (optional)
/// </summary>
public class GestureDebugUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GestureClassifier gestureClassifier;
    [SerializeField] private HandTrackingProvider handTracker;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI gestureText;
    [SerializeField] private TextMeshProUGUI confidenceText;
    [SerializeField] private TextMeshProUGUI trackingStatusText;
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private TextMeshProUGUI featuresText;

    [Header("Display Options")]
    [SerializeField] private bool showFeatures = false;
    [SerializeField] private bool showFPS = true;

    [Header("Colors")]
    [SerializeField] private Color highConfidenceColor = Color.green;
    [SerializeField] private Color mediumConfidenceColor = Color.yellow;
    [SerializeField] private Color lowConfidenceColor = Color.red;
    [SerializeField] private Color noTrackingColor = Color.gray;

    // FPS calculation
    private float deltaTime = 0.0f;

    private void Start()
    {
        // Auto-find components if not assigned
        if (gestureClassifier == null)
            gestureClassifier = FindObjectOfType<GestureClassifier>();

        if (handTracker == null)
            handTracker = FindObjectOfType<HandTrackingProvider>();

        // Subscribe to gesture changes
        if (gestureClassifier != null)
        {
            gestureClassifier.OnGestureChanged += OnGestureChanged;
        }

        // Set initial UI state
        UpdateUI();
    }

    private void Update()
    {
        UpdateUI();
        UpdateFPS();
    }

    private void UpdateUI()
    {
        // Gesture and confidence
        if (gestureClassifier != null && gestureText != null)
        {
            string gesture = gestureClassifier.CurrentGesture;
            float confidence = gestureClassifier.CurrentConfidence;

            gestureText.text = $"Gesture: {FormatGestureName(gesture)}";

            if (confidenceText != null)
            {
                confidenceText.text = $"Confidence: {confidence:P0}";

                // Color code by confidence
                if (gesture == "none" || !handTracker.IsTracking)
                {
                    confidenceText.color = noTrackingColor;
                }
                else if (confidence >= 0.8f)
                {
                    confidenceText.color = highConfidenceColor;
                }
                else if (confidence >= 0.6f)
                {
                    confidenceText.color = mediumConfidenceColor;
                }
                else
                {
                    confidenceText.color = lowConfidenceColor;
                }
            }
        }

        // Tracking status
        if (handTracker != null && trackingStatusText != null)
        {
            bool isTracking = handTracker.IsTracking;
            string handedness = handTracker.TrackedHand.ToString();
            float trackingConf = handTracker.TrackingConfidence;

            trackingStatusText.text = $"Tracking: {(isTracking ? $"{handedness} hand" : "No hand")} ({trackingConf:P0})";
            trackingStatusText.color = isTracking ? highConfidenceColor : noTrackingColor;
        }

        // Features (optional, verbose)
        if (showFeatures && featuresText != null && handTracker != null && handTracker.IsTracking)
        {
            Vector3[] landmarks = handTracker.GetLandmarks();
            if (landmarks != null)
            {
                float[] features = HandFeatureExtractor.Extract(landmarks);
                featuresText.text = FormatFeatures(features);
            }
        }
        else if (featuresText != null)
        {
            featuresText.text = "";
        }
    }

    private void UpdateFPS()
    {
        if (!showFPS || fpsText == null)
            return;

        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        fpsText.text = $"FPS: {fps:F0}";

        // Color code by performance
        if (fps >= 60)
            fpsText.color = highConfidenceColor;
        else if (fps >= 30)
            fpsText.color = mediumConfidenceColor;
        else
            fpsText.color = lowConfidenceColor;
    }

    private void OnGestureChanged(string gesture, float confidence)
    {
        // Visual feedback on gesture change
        if (gestureText != null)
        {
            // Could add animation/flash here
            Debug.Log($"[GestureDebugUI] Gesture changed to: {gesture} ({confidence:P0})");
        }
    }

    private string FormatGestureName(string gesture)
    {
        if (gesture == "none")
            return "None";

        // Convert snake_case to Title Case
        return gesture.Replace("_", " ")
                     .Split(' ')
                     .Select(word => char.ToUpper(word[0]) + word.Substring(1))
                     .Aggregate((a, b) => a + " " + b);
    }

    private string FormatFeatures(float[] features)
    {
        if (features == null || features.Length == 0)
            return "No features";

        // Show first 10 features only (to avoid clutter)
        var firstTen = features.Take(10);
        return "Features: [" + string.Join(", ", firstTen.Select(f => f.ToString("F3"))) + "...]";
    }

    public void ToggleFeatureDisplay()
    {
        showFeatures = !showFeatures;
    }

    public void ToggleFPSDisplay()
    {
        showFPS = !showFPS;
        if (fpsText != null)
            fpsText.gameObject.SetActive(showFPS);
    }

    private void OnDestroy()
    {
        if (gestureClassifier != null)
        {
            gestureClassifier.OnGestureChanged -= OnGestureChanged;
        }
    }
}
