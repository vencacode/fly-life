using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// Put on a Canvas root to fit its content inside the device safe area.
/// At the end of the first frame (after other scripts' Start() built their UI)
/// it creates a "SafeArea" child driven by SafeAreaHandler and moves the canvas'
/// children into it. Full-screen backdrops (names in KeepFullScreen) stay outside
/// so they still cover the notch area.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class SafeAreaCanvas : MonoBehaviour
{
    public static readonly string[] DefaultKeepFullScreen = { "Background", "Overlay", "Dim" };

    public string[] KeepFullScreen = DefaultKeepFullScreen;

    bool _wrapped;

    IEnumerator Start()
    {
        yield return null;
        Wrap();
    }

    /// <summary>Wrap immediately (for canvases built entirely in code).</summary>
    public void Wrap()
    {
        if (_wrapped) return;
        _wrapped = true;

        var children = transform.Cast<Transform>()
            .Where(c => !KeepFullScreen.Contains(c.name) && c.name != "SafeArea")
            .ToList();

        var safe = new GameObject("SafeArea", typeof(RectTransform));
        var rt = safe.GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        safe.AddComponent<SafeAreaHandler>();

        // Keep backdrops rendering behind the content
        int firstContentIndex = children.Count > 0 ? children.Min(c => c.GetSiblingIndex()) : transform.childCount - 1;
        foreach (var c in children) c.SetParent(rt, false);
        rt.SetSiblingIndex(Mathf.Clamp(firstContentIndex, 0, transform.childCount - 1));
    }

    /// <summary>Add to a code-built canvas and wrap its current children right away.</summary>
    public static void Apply(GameObject canvasGo)
    {
        if (canvasGo == null) return;
        var sac = canvasGo.GetComponent<SafeAreaCanvas>();
        if (sac == null) sac = canvasGo.AddComponent<SafeAreaCanvas>();
        sac.Wrap();
    }
}
