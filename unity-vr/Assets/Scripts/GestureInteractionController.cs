using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Main controller for all gesture-based interactions.
///
/// Orchestrates:
/// - Pinch grab/release of interactable objects
/// - Wrist roll rotation while grabbed
/// - Two-hand pinch distance scaling
/// - Radial menu control
/// - Thumbs-up confirmation
/// </summary>
public class GestureInteractionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GestureClassifier leftHandClassifier;
    [SerializeField] private GestureClassifier rightHandClassifier;
    [SerializeField] private HandTrackingProvider leftHandTracker;
    [SerializeField] private HandTrackingProvider rightHandTracker;
    [SerializeField] private GestureRadialMenu radialMenu;

    [Header("Grab Settings")]
    [SerializeField] private float grabRange = 0.5f;
    [SerializeField] private LayerMask grabbableLayer = -1;
    [SerializeField] private Transform leftHandTransform;
    [SerializeField] private Transform rightHandTransform;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSensitivity = 1.0f;

    [Header("Scaling Settings")]
    [SerializeField] private bool enableTwoHandScaling = true;
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 3.0f;

    // State
    private GestureInteractable leftHandGrabbedObject;
    private GestureInteractable rightHandGrabbedObject;
    private string leftHandGesture = "none";
    private string rightHandGesture = "none";
    private float initialTwoHandDistance = 0f;
    private Vector3 initialScale = Vector3.one;

    private void Start()
    {
        // Auto-find components if not assigned
        if (leftHandClassifier == null || rightHandClassifier == null)
        {
            GestureClassifier[] classifiers = FindObjectsOfType<GestureClassifier>();
            if (classifiers.Length > 0) leftHandClassifier = classifiers[0];
            if (classifiers.Length > 1) rightHandClassifier = classifiers[1];
        }

        // Subscribe to gesture events
        if (leftHandClassifier != null)
        {
            leftHandClassifier.OnGestureChanged += (g, c) => OnLeftHandGestureChanged(g, c);
        }

        if (rightHandClassifier != null)
        {
            rightHandClassifier.OnGestureChanged += (g, c) => OnRightHandGestureChanged(g, c);
        }

        // Auto-find hand trackers
        if (leftHandTracker == null || rightHandTracker == null)
        {
            HandTrackingProvider[] trackers = FindObjectsOfType<HandTrackingProvider>();
            foreach (var tracker in trackers)
            {
                if (tracker.TrackedHand == HandTrackingProvider.Handedness.Left)
                    leftHandTracker = tracker;
                else if (tracker.TrackedHand == HandTrackingProvider.Handedness.Right)
                    rightHandTracker = tracker;
            }
        }
    }

    private void Update()
    {
        // Update grabbed objects
        UpdateGrabbedObjects();

        // Update two-hand scaling
        if (enableTwoHandScaling)
        {
            UpdateTwoHandScaling();
        }
    }

    private void OnLeftHandGestureChanged(string gesture, float confidence)
    {
        leftHandGesture = gesture;
        HandleGestureChange(gesture, leftHandTransform, ref leftHandGrabbedObject, true);
    }

    private void OnRightHandGestureChanged(string gesture, float confidence)
    {
        rightHandGesture = gesture;
        HandleGestureChange(gesture, rightHandTransform, ref rightHandGrabbedObject, false);
    }

    private void HandleGestureChange(string gesture, Transform handTransform,
                                     ref GestureInteractable grabbedObject, bool isLeftHand)
    {
        switch (gesture)
        {
            case "pinch":
                if (grabbedObject == null)
                {
                    TryGrabNearestObject(handTransform, ref grabbedObject);
                }
                break;

            case "open_hand":
            case "fist":
            case "point":
            case "thumbs_up":
                if (grabbedObject != null)
                {
                    ReleaseObject(ref grabbedObject);
                }
                break;
        }
    }

    /// <summary>
    /// Try to grab the nearest interactable object.
    /// </summary>
    private void TryGrabNearestObject(Transform handTransform, ref GestureInteractable grabbedObject)
    {
        if (handTransform == null)
            return;

        // Find all interactables in range
        Collider[] colliders = Physics.OverlapSphere(handTransform.position, grabRange, grabbableLayer);

        GestureInteractable nearestInteractable = null;
        float nearestDistance = float.MaxValue;

        foreach (var collider in colliders)
        {
            GestureInteractable interactable = collider.GetComponent<GestureInteractable>();
            if (interactable != null && interactable.IsGrabbable())
            {
                float distance = Vector3.Distance(handTransform.position, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestInteractable = interactable;
                }
            }
        }

        // Grab nearest object
        if (nearestInteractable != null)
        {
            Vector3 grabPoint = handTransform.position;
            if (nearestInteractable.TryGrab(handTransform, grabPoint))
            {
                grabbedObject = nearestInteractable;
                Debug.Log($"Grabbed: {nearestInteractable.gameObject.name}");
            }
        }
        else
        {
            Debug.Log("No grabbable objects in range");
        }
    }

    /// <summary>
    /// Release grabbed object.
    /// </summary>
    private void ReleaseObject(ref GestureInteractable grabbedObject)
    {
        if (grabbedObject == null)
            return;

        grabbedObject.Release();
        grabbedObject = null;
    }

    /// <summary>
    /// Update grabbed objects (rotation, position, etc.).
    /// </summary>
    private void UpdateGrabbedObjects()
    {
        // Left hand
        if (leftHandGrabbedObject != null && leftHandTransform != null)
        {
            // Apply wrist roll rotation if not scaling with two hands
            if (!(enableTwoHandScaling && IsTwoHandScaling()))
            {
                leftHandGrabbedObject.ApplyWristRollRotation(leftHandTransform, rotationSensitivity);
            }
        }

        // Right hand
        if (rightHandGrabbedObject != null && rightHandTransform != null)
        {
            if (!(enableTwoHandScaling && IsTwoHandScaling()))
            {
                rightHandGrabbedObject.ApplyWristRollRotation(rightHandTransform, rotationSensitivity);
            }
        }
    }

    /// <summary>
    /// Update two-hand pinch distance scaling.
    /// </summary>
    private void UpdateTwoHandScaling()
    {
        if (!IsTwoHandScaling())
        {
            initialTwoHandDistance = 0f;
            return;
        }

        float currentDistance = Vector3.Distance(leftHandTransform.position, rightHandTransform.position);

        // Initialize on first frame
        if (initialTwoHandDistance == 0f)
        {
            initialTwoHandDistance = currentDistance;

            // Use left hand grabbed object for scaling (or right if left is null)
            GestureInteractable targetObject = leftHandGrabbedObject ?? rightHandGrabbedObject;
            if (targetObject != null)
            {
                initialScale = targetObject.transform.localScale;
            }
        }

        // Apply scaling to whichever hand's object
        GestureInteractable objectToScale = leftHandGrabbedObject ?? rightHandGrabbedObject;
        if (objectToScale != null)
        {
            objectToScale.ApplyTwoHandScale(currentDistance, initialTwoHandDistance, minScale, maxScale);
        }
    }

    /// <summary>
    /// Check if both hands are pinching (two-hand scaling mode).
    /// </summary>
    private bool IsTwoHandScaling()
    {
        return leftHandGesture == "pinch" &&
               rightHandGesture == "pinch" &&
               (leftHandGrabbedObject != null || rightHandGrabbedObject != null);
    }

    /// <summary>
    /// Get currently grabbed object from either hand.
    /// </summary>
    public GestureInteractable GetGrabbedObject()
    {
        return leftHandGrabbedObject ?? rightHandGrabbedObject;
    }

    /// <summary>
    /// Force release all grabbed objects.
    /// </summary>
    public void ReleaseAll()
    {
        ReleaseObject(ref leftHandGrabbedObject);
        ReleaseObject(ref rightHandGrabbedObject);
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        // Draw grab range spheres
        if (leftHandTransform != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(leftHandTransform.position, grabRange);
        }

        if (rightHandTransform != null)
        {
            Gizmos.color = new Color(0, 0, 1, 0.2f);
            Gizmos.DrawWireSphere(rightHandTransform.position, grabRange);
        }

        // Draw line between hands for two-hand scaling
        if (IsTwoHandScaling() && leftHandTransform != null && rightHandTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(leftHandTransform.position, rightHandTransform.position);
        }
    }
}
