using UnityEngine;

/// <summary>
/// Makes an object interactable via hand gestures.
///
/// Supports:
/// - Pinch grab/release
/// - Wrist roll rotation while grabbed
/// - Two-hand pinch distance scaling
/// - Visual feedback for interaction states
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GestureInteractable : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private bool canGrab = true;
    [SerializeField] private bool canRotate = true;
    [SerializeField] private bool canScale = true;

    [Header("Physics")]
    [SerializeField] private bool usePhysics = true;
    [SerializeField] private float throwMultiplier = 1.5f;

    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color grabbedColor = Color.green;
    [SerializeField] private bool showOutline = true;

    // State
    private bool isGrabbed = false;
    private Transform grabbingHand;
    private Vector3 grabOffset;
    private Quaternion grabRotationOffset;
    private float initialWristRoll;
    private Vector3 lastPosition;

    // Components
    private Rigidbody rb;
    private Renderer objectRenderer;
    private Material objectMaterial;
    private Color originalColor;

    // Properties
    public bool IsGrabbed => isGrabbed;
    public Transform GrabbingHand => grabbingHand;

    // Events
    public delegate void InteractionHandler(GestureInteractable interactable);
    public event InteractionHandler OnGrabbed;
    public event InteractionHandler OnReleased;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        objectRenderer = GetComponent<Renderer>();

        if (objectRenderer != null)
        {
            objectMaterial = objectRenderer.material;
            originalColor = objectMaterial.color;
        }

        lastPosition = transform.position;
    }

    private void Update()
    {
        if (isGrabbed && grabbingHand != null)
        {
            UpdateGrabbedPosition();
        }

        // Track velocity for throwing
        if (!isGrabbed)
        {
            lastPosition = transform.position;
        }
    }

    /// <summary>
    /// Attempt to grab the object with the given hand.
    /// </summary>
    public bool TryGrab(Transform handTransform, Vector3 grabPoint)
    {
        if (!canGrab || isGrabbed)
            return false;

        isGrabbed = true;
        grabbingHand = handTransform;

        // Calculate offsets for smooth grabbing
        grabOffset = transform.InverseTransformPoint(grabPoint);
        grabRotationOffset = Quaternion.Inverse(handTransform.rotation) * transform.rotation;

        // Store initial wrist orientation for rotation
        initialWristRoll = GetWristRoll(handTransform);

        // Disable physics while grabbed
        if (usePhysics && rb != null)
        {
            rb.isKinematic = true;
        }

        // Visual feedback
        SetColor(grabbedColor);

        OnGrabbed?.Invoke(this);
        Debug.Log($"Grabbed: {gameObject.name}");

        return true;
    }

    /// <summary>
    /// Release the object.
    /// </summary>
    public void Release()
    {
        if (!isGrabbed)
            return;

        isGrabbed = false;

        // Re-enable physics and apply throw velocity
        if (usePhysics && rb != null)
        {
            rb.isKinematic = false;

            Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
            rb.velocity = velocity * throwMultiplier;
        }

        // Visual feedback
        SetColor(normalColor);

        OnReleased?.Invoke(this);
        Debug.Log($"Released: {gameObject.name}");

        grabbingHand = null;
    }

    /// <summary>
    /// Update object position to follow hand while grabbed.
    /// </summary>
    private void UpdateGrabbedPosition()
    {
        if (grabbingHand == null)
            return;

        // Position: follow hand with grab offset
        Vector3 targetPosition = grabbingHand.TransformPoint(grabOffset);
        transform.position = targetPosition;

        // Rotation: follow hand with offset (if rotation enabled)
        if (canRotate)
        {
            Quaternion targetRotation = grabbingHand.rotation * grabRotationOffset;
            transform.rotation = targetRotation;
        }

        lastPosition = transform.position;
    }

    /// <summary>
    /// Rotate object based on wrist roll while grabbed.
    /// </summary>
    public void ApplyWristRollRotation(Transform handTransform, float sensitivity = 1.0f)
    {
        if (!isGrabbed || !canRotate || grabbingHand != handTransform)
            return;

        float currentWristRoll = GetWristRoll(handTransform);
        float rollDelta = currentWristRoll - initialWristRoll;

        // Apply rotation around hand's forward axis
        transform.Rotate(handTransform.forward, rollDelta * sensitivity, Space.World);

        initialWristRoll = currentWristRoll;
    }

    /// <summary>
    /// Scale object based on two-hand pinch distance.
    /// </summary>
    public void ApplyTwoHandScale(float distance, float initialDistance, float minScale = 0.5f, float maxScale = 3.0f)
    {
        if (!isGrabbed || !canScale)
            return;

        float scaleRatio = distance / initialDistance;
        scaleRatio = Mathf.Clamp(scaleRatio, minScale, maxScale);

        transform.localScale = Vector3.one * scaleRatio;
    }

    /// <summary>
    /// Get wrist roll angle from hand transform.
    /// Assumes hand forward is Z-axis and roll is around Z.
    /// </summary>
    private float GetWristRoll(Transform handTransform)
    {
        // Project hand's up vector onto plane perpendicular to forward
        Vector3 forward = handTransform.forward;
        Vector3 up = handTransform.up;
        Vector3 right = handTransform.right;

        // Calculate roll angle
        float roll = Mathf.Atan2(right.y, up.y) * Mathf.Rad2Deg;
        return roll;
    }

    /// <summary>
    /// Set visual highlight state.
    /// </summary>
    public void SetHighlight(bool highlighted)
    {
        if (isGrabbed)
            return; // Don't change color while grabbed

        SetColor(highlighted ? hoverColor : normalColor);
    }

    private void SetColor(Color color)
    {
        if (objectMaterial != null)
        {
            objectMaterial.color = color;
        }
    }

    /// <summary>
    /// Check if object is grabbable (for raycasts/proximity checks).
    /// </summary>
    public bool IsGrabbable()
    {
        return canGrab && !isGrabbed;
    }

    private void OnDestroy()
    {
        // Clean up material instance
        if (objectMaterial != null)
        {
            Destroy(objectMaterial);
        }
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (isGrabbed && grabbingHand != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, grabbingHand.position);
            Gizmos.DrawWireSphere(grabbingHand.position, 0.02f);
        }
    }
}
