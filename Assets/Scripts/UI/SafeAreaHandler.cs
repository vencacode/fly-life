using UnityEngine;

/// <summary>
/// Adjusts a RectTransform to fit within the device safe area.
/// Attach to the root content panel (child of Canvas) in every scene.
/// Handles notches, punch-holes, and rounded corners.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaHandler : MonoBehaviour
{
    RectTransform _rect;
    Rect _lastSafeArea;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void Update()
    {
        // Recheck in case orientation changes
        if (_lastSafeArea != Screen.safeArea)
            ApplySafeArea();
    }

    void ApplySafeArea()
    {
        var safeArea = Screen.safeArea;
        _lastSafeArea = safeArea;

        if (Screen.width <= 0 || Screen.height <= 0) return;

        var min = safeArea.position;
        var max = safeArea.position + safeArea.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        _rect.anchorMin = min;
        _rect.anchorMax = max;
    }
}
