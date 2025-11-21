using UnityEngine;
using Unity.Barracuda;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Real-time gesture classifier using ONNX model via Unity Barracuda.
///
/// Loads trained ML model, processes hand landmarks, and predicts gestures.
/// Includes prediction smoothing to reduce jitter.
/// </summary>
public class GestureClassifier : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private NNModel modelAsset;
    [SerializeField] private WorkerFactory.Type workerType = WorkerFactory.Type.CSharpBurst;

    [Header("Hand Tracking")]
    [SerializeField] private HandTrackingProvider handTracker;

    [Header("Smoothing")]
    [Tooltip("Use majority vote over recent predictions")]
    [SerializeField] private bool useMajorityVote = true;

    [Tooltip("Number of recent predictions to consider (frames)")]
    [SerializeField] private int smoothingWindow = 5;

    [Tooltip("Minimum confidence threshold (0-1)")]
    [SerializeField] private float confidenceThreshold = 0.7f;

    [Tooltip("Dwell time before switching gestures (seconds)")]
    [SerializeField] private float dwellTime = 0.25f;

    // Gesture labels (must match training order)
    private static readonly string[] GESTURE_LABELS = {
        "open_hand",
        "fist",
        "pinch",
        "point",
        "thumbs_up"
    };

    // Runtime state
    private IWorker worker;
    private Model runtimeModel;
    private Queue<int> recentPredictions;
    private string currentGesture = "none";
    private float currentConfidence = 0.0f;
    private float lastGestureChangeTime = 0.0f;
    private string pendingGesture = null;

    // Events
    public delegate void GestureChangedHandler(string gesture, float confidence);
    public event GestureChangedHandler OnGestureChanged;

    // Public properties
    public string CurrentGesture => currentGesture;
    public float CurrentConfidence => currentConfidence;
    public bool IsModelLoaded => worker != null;

    private void Start()
    {
        InitializeModel();
        recentPredictions = new Queue<int>(smoothingWindow);

        if (handTracker == null)
        {
            Debug.LogWarning("GestureClassifier: No hand tracker assigned. Checking for component...");
            handTracker = GetComponent<HandTrackingProvider>();

            if (handTracker == null)
            {
                Debug.LogError("GestureClassifier: No HandTrackingProvider found!");
            }
        }
    }

    private void Update()
    {
        if (!IsModelLoaded || handTracker == null || !handTracker.IsTracking)
        {
            if (currentGesture != "none")
            {
                UpdateGesture("none", 0.0f);
            }
            return;
        }

        // Get hand landmarks
        Vector3[] landmarks = handTracker.GetLandmarks();
        if (landmarks == null || landmarks.Length != 21)
        {
            UpdateGesture("none", 0.0f);
            return;
        }

        // Extract features
        float[] features = HandFeatureExtractor.Extract(landmarks);

        // Predict gesture
        (int gestureIndex, float confidence) = Predict(features);

        // Apply smoothing
        if (useMajorityVote)
        {
            gestureIndex = ApplyMajorityVote(gestureIndex);
        }

        // Check confidence threshold
        if (confidence < confidenceThreshold)
        {
            UpdateGesture("none", confidence);
            return;
        }

        // Get gesture name
        string predictedGesture = GESTURE_LABELS[gestureIndex];

        // Apply dwell time (avoid rapid switching)
        float timeSinceLastChange = Time.time - lastGestureChangeTime;
        if (predictedGesture != currentGesture)
        {
            if (pendingGesture == predictedGesture)
            {
                // Same pending gesture, check if dwell time elapsed
                if (timeSinceLastChange >= dwellTime)
                {
                    UpdateGesture(predictedGesture, confidence);
                    pendingGesture = null;
                }
            }
            else
            {
                // New pending gesture, start dwell timer
                pendingGesture = predictedGesture;
                lastGestureChangeTime = Time.time;
            }
        }
        else
        {
            // Same gesture, update confidence
            currentConfidence = confidence;
            pendingGesture = null;
        }
    }

    /// <summary>
    /// Initialize Barracuda model from ONNX asset.
    /// </summary>
    private void InitializeModel()
    {
        if (modelAsset == null)
        {
            Debug.LogError("GestureClassifier: No model asset assigned!");
            return;
        }

        try
        {
            runtimeModel = ModelLoader.Load(modelAsset);
            worker = WorkerFactory.CreateWorker(workerType, runtimeModel);
            Debug.Log($"GestureClassifier: Model loaded successfully ({runtimeModel.inputs.Count} inputs, {runtimeModel.outputs.Count} outputs)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GestureClassifier: Failed to load model: {e.Message}");
        }
    }

    /// <summary>
    /// Predict gesture from feature vector.
    /// </summary>
    /// <param name="features">31-element feature array</param>
    /// <returns>Tuple of (gesture index, confidence)</returns>
    private (int, float) Predict(float[] features)
    {
        if (features.Length != 31)
        {
            Debug.LogError($"GestureClassifier: Expected 31 features, got {features.Length}");
            return (0, 0.0f);
        }

        // Create input tensor (batch_size=1, features=31)
        using (var inputTensor = new Tensor(1, 31, features))
        {
            // Run inference
            worker.Execute(inputTensor);

            // Get output
            // Note: Output format depends on model type (class index or probabilities)
            var output = worker.PeekOutput();

            // For RandomForest/kNN via sklearn-onnx:
            // Output is typically class index (int) or probabilities (float array)
            // We'll handle both cases

            if (output.length == 1)
            {
                // Single output: class index
                int predictedClass = (int)output[0];
                return (predictedClass, 1.0f); // No confidence available
            }
            else if (output.length == GESTURE_LABELS.Length)
            {
                // Multiple outputs: probabilities
                float[] probabilities = output.AsFloats();
                int maxIndex = 0;
                float maxProb = probabilities[0];

                for (int i = 1; i < probabilities.Length; i++)
                {
                    if (probabilities[i] > maxProb)
                    {
                        maxProb = probabilities[i];
                        maxIndex = i;
                    }
                }

                return (maxIndex, maxProb);
            }
            else
            {
                Debug.LogWarning($"GestureClassifier: Unexpected output length: {output.length}");
                return (0, 0.0f);
            }
        }
    }

    /// <summary>
    /// Apply majority vote smoothing over recent predictions.
    /// </summary>
    private int ApplyMajorityVote(int currentPrediction)
    {
        // Add current prediction to queue
        recentPredictions.Enqueue(currentPrediction);

        // Maintain window size
        if (recentPredictions.Count > smoothingWindow)
        {
            recentPredictions.Dequeue();
        }

        // Find most common prediction
        if (recentPredictions.Count == 0)
            return currentPrediction;

        var grouped = recentPredictions.GroupBy(x => x)
                                      .OrderByDescending(g => g.Count())
                                      .FirstOrDefault();

        return grouped != null ? grouped.Key : currentPrediction;
    }

    /// <summary>
    /// Update current gesture and fire event if changed.
    /// </summary>
    private void UpdateGesture(string gesture, float confidence)
    {
        if (gesture != currentGesture)
        {
            currentGesture = gesture;
            currentConfidence = confidence;
            lastGestureChangeTime = Time.time;

            Debug.Log($"Gesture changed: {gesture} (confidence: {confidence:F2})");
            OnGestureChanged?.Invoke(gesture, confidence);
        }
        else
        {
            currentConfidence = confidence;
        }
    }

    /// <summary>
    /// Get gesture index by name.
    /// </summary>
    public static int GetGestureIndex(string gestureName)
    {
        for (int i = 0; i < GESTURE_LABELS.Length; i++)
        {
            if (GESTURE_LABELS[i] == gestureName)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Get gesture name by index.
    /// </summary>
    public static string GetGestureName(int index)
    {
        if (index >= 0 && index < GESTURE_LABELS.Length)
            return GESTURE_LABELS[index];
        return "unknown";
    }

    private void OnDestroy()
    {
        // Clean up Barracuda resources
        worker?.Dispose();
    }
}
