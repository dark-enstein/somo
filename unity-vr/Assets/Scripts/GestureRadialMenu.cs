using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Radial menu that appears/disappears via open hand gesture.
///
/// Features:
/// - Toggle visibility with open hand gesture
/// - Point gesture to hover/select items
/// - Thumbs up to confirm selection
/// - Circular layout of menu items
/// </summary>
public class GestureRadialMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GestureClassifier gestureClassifier;
    [SerializeField] private Transform handTransform; // For positioning menu
    [SerializeField] private Canvas menuCanvas;

    [Header("Menu Settings")]
    [SerializeField] private float menuRadius = 0.3f;
    [SerializeField] private float distanceFromHand = 0.5f;
    [SerializeField] private bool faceCamera = true;

    [Header("Menu Items")]
    [SerializeField] private List<RadialMenuItem> menuItems = new List<RadialMenuItem>();

    [Header("Visual")]
    [SerializeField] private Color normalItemColor = Color.white;
    [SerializeField] private Color hoverItemColor = Color.yellow;
    [SerializeField] private Color selectedItemColor = Color.green;

    // State
    private bool isMenuVisible = false;
    private int hoveredItemIndex = -1;
    private int selectedItemIndex = -1;
    private bool waitingForConfirmation = false;

    // Menu item data
    [System.Serializable]
    public class RadialMenuItem
    {
        public string label;
        public Sprite icon;
        public UnityEngine.Events.UnityEvent onSelected;

        [HideInInspector] public GameObject itemObject;
        [HideInInspector] public Image background;
        [HideInInspector] public Image iconImage;
        [HideInInspector] public TextMeshProUGUI labelText;
    }

    private void Start()
    {
        if (gestureClassifier == null)
        {
            gestureClassifier = FindObjectOfType<GestureClassifier>();
        }

        if (gestureClassifier != null)
        {
            gestureClassifier.OnGestureChanged += OnGestureChanged;
        }

        // Initialize menu
        if (menuCanvas == null)
        {
            Debug.LogWarning("GestureRadialMenu: No canvas assigned!");
        }
        else
        {
            SetupMenuItems();
            HideMenu();
        }
    }

    private void Update()
    {
        if (isMenuVisible)
        {
            UpdateMenuPosition();
            UpdateHoverSelection();
        }
    }

    private void OnGestureChanged(string gesture, float confidence)
    {
        switch (gesture)
        {
            case "open_hand":
                ToggleMenu();
                break;

            case "point":
                if (isMenuVisible)
                {
                    // Hover handled in UpdateHoverSelection()
                }
                break;

            case "thumbs_up":
                if (isMenuVisible && hoveredItemIndex >= 0)
                {
                    ConfirmSelection();
                }
                break;

            case "fist":
                if (isMenuVisible)
                {
                    HideMenu();
                }
                break;
        }
    }

    /// <summary>
    /// Toggle menu visibility.
    /// </summary>
    private void ToggleMenu()
    {
        if (isMenuVisible)
        {
            HideMenu();
        }
        else
        {
            ShowMenu();
        }
    }

    /// <summary>
    /// Show the radial menu.
    /// </summary>
    private void ShowMenu()
    {
        if (menuCanvas == null)
            return;

        isMenuVisible = true;
        menuCanvas.gameObject.SetActive(true);
        Debug.Log("Radial menu shown");
    }

    /// <summary>
    /// Hide the radial menu.
    /// </summary>
    private void HideMenu()
    {
        if (menuCanvas == null)
            return;

        isMenuVisible = false;
        menuCanvas.gameObject.SetActive(false);
        hoveredItemIndex = -1;
        waitingForConfirmation = false;
        Debug.Log("Radial menu hidden");
    }

    /// <summary>
    /// Update menu position to follow hand.
    /// </summary>
    private void UpdateMenuPosition()
    {
        if (menuCanvas == null || handTransform == null)
            return;

        // Position menu in front of hand
        Vector3 targetPosition = handTransform.position + handTransform.forward * distanceFromHand;
        menuCanvas.transform.position = targetPosition;

        // Face camera or hand
        if (faceCamera && Camera.main != null)
        {
            menuCanvas.transform.LookAt(Camera.main.transform);
            menuCanvas.transform.Rotate(0, 180, 0); // Flip to face correctly
        }
        else
        {
            menuCanvas.transform.rotation = handTransform.rotation;
        }
    }

    /// <summary>
    /// Update which menu item is being hovered based on hand orientation.
    /// </summary>
    private void UpdateHoverSelection()
    {
        if (menuItems.Count == 0 || handTransform == null)
            return;

        // Simple approach: use hand forward direction to select item
        // In a real implementation, you'd raycast from index finger
        Vector3 handDirection = handTransform.forward;
        Vector3 menuCenter = menuCanvas.transform.position;

        // Project hand direction onto menu plane
        Vector3 menuForward = menuCanvas.transform.forward;
        Vector3 projectedDirection = Vector3.ProjectOnPlane(handDirection, menuForward);

        // Calculate angle
        float angle = Mathf.Atan2(projectedDirection.y, projectedDirection.x) * Mathf.Rad2Deg;
        angle = (angle + 360) % 360; // Normalize to 0-360

        // Determine which segment the angle falls into
        float segmentAngle = 360f / menuItems.Count;
        int newHoveredIndex = Mathf.FloorToInt(angle / segmentAngle);

        if (newHoveredIndex != hoveredItemIndex)
        {
            // Update hover state
            if (hoveredItemIndex >= 0 && hoveredItemIndex < menuItems.Count)
            {
                SetItemColor(hoveredItemIndex, normalItemColor);
            }

            hoveredItemIndex = newHoveredIndex;

            if (hoveredItemIndex >= 0 && hoveredItemIndex < menuItems.Count)
            {
                SetItemColor(hoveredItemIndex, hoverItemColor);
                Debug.Log($"Hovering: {menuItems[hoveredItemIndex].label}");
            }
        }
    }

    /// <summary>
    /// Confirm selection of currently hovered item.
    /// </summary>
    private void ConfirmSelection()
    {
        if (hoveredItemIndex < 0 || hoveredItemIndex >= menuItems.Count)
            return;

        selectedItemIndex = hoveredItemIndex;
        RadialMenuItem item = menuItems[selectedItemIndex];

        // Visual feedback
        SetItemColor(selectedItemIndex, selectedItemColor);

        // Invoke action
        item.onSelected?.Invoke();

        Debug.Log($"Selected: {item.label}");

        // Hide menu after selection
        Invoke(nameof(HideMenu), 0.5f);
    }

    /// <summary>
    /// Setup menu item UI elements in circular layout.
    /// </summary>
    private void SetupMenuItems()
    {
        if (menuCanvas == null || menuItems.Count == 0)
            return;

        float segmentAngle = 360f / menuItems.Count;

        for (int i = 0; i < menuItems.Count; i++)
        {
            // Calculate position on circle
            float angle = i * segmentAngle * Mathf.Deg2Rad;
            Vector2 position = new Vector2(
                Mathf.Cos(angle) * menuRadius,
                Mathf.Sin(angle) * menuRadius
            );

            // Create menu item GameObject
            GameObject itemObject = new GameObject($"MenuItem_{i}_{menuItems[i].label}");
            itemObject.transform.SetParent(menuCanvas.transform, false);

            RectTransform rectTransform = itemObject.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = position * 100f; // UI units
            rectTransform.sizeDelta = new Vector2(80, 80);

            // Background image
            Image background = itemObject.AddComponent<Image>();
            background.color = normalItemColor;
            menuItems[i].background = background;

            // Icon (if provided)
            if (menuItems[i].icon != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(itemObject.transform, false);
                RectTransform iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchoredPosition = Vector2.zero;
                iconRect.sizeDelta = new Vector2(60, 60);

                Image iconImage = iconObj.AddComponent<Image>();
                iconImage.sprite = menuItems[i].icon;
                menuItems[i].iconImage = iconImage;
            }

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(itemObject.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(0, -50);
            labelRect.sizeDelta = new Vector2(100, 30);

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = menuItems[i].label;
            labelText.fontSize = 12;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.black;
            menuItems[i].labelText = labelText;

            menuItems[i].itemObject = itemObject;
        }
    }

    /// <summary>
    /// Set color of a menu item.
    /// </summary>
    private void SetItemColor(int index, Color color)
    {
        if (index < 0 || index >= menuItems.Count)
            return;

        if (menuItems[index].background != null)
        {
            menuItems[index].background.color = color;
        }
    }

    /// <summary>
    /// Add a menu item programmatically.
    /// </summary>
    public void AddMenuItem(string label, Sprite icon, UnityEngine.Events.UnityAction callback)
    {
        RadialMenuItem item = new RadialMenuItem
        {
            label = label,
            icon = icon,
            onSelected = new UnityEngine.Events.UnityEvent()
        };

        if (callback != null)
        {
            item.onSelected.AddListener(callback);
        }

        menuItems.Add(item);

        // Rebuild menu if already initialized
        if (Application.isPlaying && menuCanvas != null)
        {
            SetupMenuItems();
        }
    }

    private void OnDestroy()
    {
        if (gestureClassifier != null)
        {
            gestureClassifier.OnGestureChanged -= OnGestureChanged;
        }
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (menuCanvas != null && isMenuVisible)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(menuCanvas.transform.position, menuRadius);
        }
    }
}
