using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime-built "Welcome back!" popup showing offline earnings.
/// Displayed on return after being away.
/// </summary>
public class OfflineRewardPopup : MonoBehaviour
{
    public static OfflineRewardPopup Instance { get; private set; }

    Canvas _canvas;
    GameObject _panel;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start() => BuildUI();

    void BuildUI()
    {
        _canvas = new GameObject("OfflineRewardCanvas").AddComponent<Canvas>();
        _canvas.transform.SetParent(transform);
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 900;
        _canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvas.gameObject.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("OfflinePanel");
        _panel.transform.SetParent(_canvas.transform, false);
        var rt = _panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.3f); rt.anchorMax = new Vector2(0.9f, 0.7f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        _panel.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 0.97f);

        _panel.SetActive(false);
    }

    /// <summary>Show the offline reward popup with earned coins.</summary>
    public void Show(float hoursAway, int districts, int coinsEarned)
    {
        foreach (Transform child in _panel.transform)
            Destroy(child.gameObject);

        // Title
        var title = CreateLabel(_panel.transform, "Welcome Back!", 28, TextAlignmentOptions.Center);
        title.color = new Color(1f, 0.9f, 0.4f);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.78f); titleRT.anchorMax = new Vector2(1, 0.95f);
        titleRT.offsetMin = Vector2.zero; titleRT.offsetMax = Vector2.zero;

        // Time away
        int h = Mathf.FloorToInt(hoursAway);
        int m = Mathf.FloorToInt((hoursAway - h) * 60);
        var timeLabel = CreateLabel(_panel.transform,
            $"You were away for {h}h {m}m", 16, TextAlignmentOptions.Center);
        timeLabel.color = new Color(0.7f, 0.7f, 0.8f);
        var timeRT = timeLabel.GetComponent<RectTransform>();
        timeRT.anchorMin = new Vector2(0, 0.65f); timeRT.anchorMax = new Vector2(1, 0.78f);
        timeRT.offsetMin = Vector2.zero; timeRT.offsetMax = Vector2.zero;

        // Earnings breakdown
        var earnings = CreateLabel(_panel.transform,
            $"Your {districts} districts earned:\n<size=30><color=#FFD700>+{coinsEarned} PooCoins</color></size>",
            18, TextAlignmentOptions.Center);
        earnings.richText = true;
        var earningsRT = earnings.GetComponent<RectTransform>();
        earningsRT.anchorMin = new Vector2(0.05f, 0.35f); earningsRT.anchorMax = new Vector2(0.95f, 0.65f);
        earningsRT.offsetMin = Vector2.zero; earningsRT.offsetMax = Vector2.zero;

        // Collect button
        int coins = coinsEarned;
        var collectBtn = CreateButton(_panel.transform, $"Collect +{coinsEarned}", () =>
        {
            ResourceManager.Instance?.AddPooCoins(coins);
            _panel.SetActive(false);
        });
        var collectRT = collectBtn.GetComponent<RectTransform>();
        collectRT.anchorMin = new Vector2(0.15f, 0.08f); collectRT.anchorMax = new Vector2(0.55f, 0.25f);
        collectRT.offsetMin = Vector2.zero; collectRT.offsetMax = Vector2.zero;
        collectBtn.GetComponent<Image>().color = new Color(0.2f, 0.55f, 0.2f);

        // Watch ad for 2x button — only shown when ads are enabled
        if (FeatureFlags.ADS_ENABLED)
        {
            int doubled = coinsEarned * 2;
            var adBtn = CreateButton(_panel.transform, $"Watch Ad: +{doubled}", () =>
            {
                AdManager.Instance?.ShowRewardedAd(() =>
                {
                    ResourceManager.Instance?.AddPooCoins(doubled);
                    _panel.SetActive(false);
                });
            });
            var adRT = adBtn.GetComponent<RectTransform>();
            adRT.anchorMin = new Vector2(0.58f, 0.08f); adRT.anchorMax = new Vector2(0.85f, 0.25f);
            adRT.offsetMin = Vector2.zero; adRT.offsetMax = Vector2.zero;
            adBtn.GetComponent<Image>().color = new Color(0.5f, 0.4f, 0.1f);
        }
        else
        {
            // Widen collect button when no ad button
            collectRT.anchorMax = new Vector2(0.85f, 0.25f);
        }

        _panel.SetActive(true);
    }

    static TextMeshProUGUI CreateLabel(Transform parent, string text, int size, TextAlignmentOptions align)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.alignment = align;
        tmp.color = Color.white;
        return tmp;
    }

    static GameObject CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.45f, 0.7f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        var tmp = new GameObject("Label").AddComponent<TextMeshProUGUI>();
        tmp.transform.SetParent(go.transform, false);
        tmp.text = label; tmp.fontSize = 14; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var tmpRT = tmp.GetComponent<RectTransform>();
        tmpRT.anchorMin = Vector2.zero; tmpRT.anchorMax = Vector2.one;
        tmpRT.offsetMin = Vector2.zero; tmpRT.offsetMax = Vector2.zero;
        return go;
    }
}
