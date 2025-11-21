using UnityEngine;

/// <summary>
/// Visualize hand landmarks in 3D space.
///
/// Draws spheres for each landmark and lines connecting joints.
/// Useful for debugging hand tracking and feature extraction.
/// </summary>
public class HandVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandTrackingProvider handTracker;

    [Header("Visualization")]
    [SerializeField] private bool showLandmarks = true;
    [SerializeField] private bool showConnections = true;
    [SerializeField] private float landmarkSize = 0.01f;
    [SerializeField] private float visualizationScale = 1.0f;

    [Header("Colors")]
    [SerializeField] private Color wristColor = Color.yellow;
    [SerializeField] private Color thumbColor = Color.red;
    [SerializeField] private Color indexColor = Color.green;
    [SerializeField] private Color middleColor = Color.blue;
    [SerializeField] private Color ringColor = Color.cyan;
    [SerializeField] private Color pinkyColor = Color.magenta;
    [SerializeField] private Color connectionColor = new Color(1f, 1f, 1f, 0.5f);

    // MediaPipe hand connections
    private static readonly int[][] CONNECTIONS = {
        // Wrist to palm
        new int[] { 0, 1 }, new int[] { 0, 5 }, new int[] { 0, 9 }, new int[] { 0, 13 }, new int[] { 0, 17 },
        // Thumb
        new int[] { 1, 2 }, new int[] { 2, 3 }, new int[] { 3, 4 },
        // Index
        new int[] { 5, 6 }, new int[] { 6, 7 }, new int[] { 7, 8 },
        // Middle
        new int[] { 9, 10 }, new int[] { 10, 11 }, new int[] { 11, 12 },
        // Ring
        new int[] { 13, 14 }, new int[] { 14, 15 }, new int[] { 15, 16 },
        // Pinky
        new int[] { 17, 18 }, new int[] { 18, 19 }, new int[] { 19, 20 },
        // Palm connections
        new int[] { 5, 9 }, new int[] { 9, 13 }, new int[] { 13, 17 }
    };

    private GameObject[] landmarkObjects;
    private LineRenderer[] connectionLines;

    private void Start()
    {
        if (handTracker == null)
        {
            handTracker = GetComponent<HandTrackingProvider>();
            if (handTracker == null)
            {
                Debug.LogError("HandVisualizer: No HandTrackingProvider found!");
                enabled = false;
                return;
            }
        }

        InitializeLandmarkObjects();
        InitializeConnectionLines();
    }

    private void Update()
    {
        if (handTracker == null || !handTracker.IsTracking)
        {
            HideVisualization();
            return;
        }

        Vector3[] landmarks = handTracker.GetLandmarks();
        if (landmarks == null || landmarks.Length != 21)
        {
            HideVisualization();
            return;
        }

        UpdateVisualization(landmarks);
    }

    private void InitializeLandmarkObjects()
    {
        landmarkObjects = new GameObject[21];

        for (int i = 0; i < 21; i++)
        {
            GameObject landmark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            landmark.name = $"Landmark_{i}";
            landmark.transform.parent = transform;
            landmark.transform.localScale = Vector3.one * landmarkSize;

            // Remove collider (visualization only)
            Destroy(landmark.GetComponent<Collider>());

            // Set color based on finger
            Renderer renderer = landmark.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = GetLandmarkColor(i);

            landmarkObjects[i] = landmark;
        }
    }

    private void InitializeConnectionLines()
    {
        connectionLines = new LineRenderer[CONNECTIONS.Length];

        for (int i = 0; i < CONNECTIONS.Length; i++)
        {
            GameObject lineObj = new GameObject($"Connection_{i}");
            lineObj.transform.parent = transform;

            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.startWidth = landmarkSize * 0.5f;
            line.endWidth = landmarkSize * 0.5f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = connectionColor;
            line.endColor = connectionColor;
            line.positionCount = 2;

            connectionLines[i] = line;
        }
    }

    private void UpdateVisualization(Vector3[] landmarks)
    {
        // Update landmark positions
        if (showLandmarks && landmarkObjects != null)
        {
            for (int i = 0; i < 21; i++)
            {
                landmarkObjects[i].SetActive(true);
                landmarkObjects[i].transform.localPosition = landmarks[i] * visualizationScale;
            }
        }
        else if (landmarkObjects != null)
        {
            foreach (var obj in landmarkObjects)
                obj.SetActive(false);
        }

        // Update connection lines
        if (showConnections && connectionLines != null)
        {
            for (int i = 0; i < CONNECTIONS.Length; i++)
            {
                int startIdx = CONNECTIONS[i][0];
                int endIdx = CONNECTIONS[i][1];

                connectionLines[i].enabled = true;
                connectionLines[i].SetPosition(0, transform.TransformPoint(landmarks[startIdx] * visualizationScale));
                connectionLines[i].SetPosition(1, transform.TransformPoint(landmarks[endIdx] * visualizationScale));
            }
        }
        else if (connectionLines != null)
        {
            foreach (var line in connectionLines)
                line.enabled = false;
        }
    }

    private void HideVisualization()
    {
        if (landmarkObjects != null)
        {
            foreach (var obj in landmarkObjects)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        if (connectionLines != null)
        {
            foreach (var line in connectionLines)
            {
                if (line != null)
                    line.enabled = false;
            }
        }
    }

    private Color GetLandmarkColor(int index)
    {
        if (index == 0)
            return wristColor;
        else if (index >= 1 && index <= 4)
            return thumbColor;
        else if (index >= 5 && index <= 8)
            return indexColor;
        else if (index >= 9 && index <= 12)
            return middleColor;
        else if (index >= 13 && index <= 16)
            return ringColor;
        else if (index >= 17 && index <= 20)
            return pinkyColor;
        else
            return Color.white;
    }

    public void ToggleLandmarks()
    {
        showLandmarks = !showLandmarks;
    }

    public void ToggleConnections()
    {
        showConnections = !showConnections;
    }

    public void SetVisualizationScale(float scale)
    {
        visualizationScale = scale;
    }

    private void OnDestroy()
    {
        // Clean up created objects
        if (landmarkObjects != null)
        {
            foreach (var obj in landmarkObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
        }

        if (connectionLines != null)
        {
            foreach (var line in connectionLines)
            {
                if (line != null && line.gameObject != null)
                    Destroy(line.gameObject);
            }
        }
    }
}
