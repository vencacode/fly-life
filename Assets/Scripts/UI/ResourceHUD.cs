using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Top HUD bar showing PooCoins, Gems, and a Clan button.
/// Subscribes to UIEventBus for live updates.
/// Builds its own UI if text fields aren't assigned in Inspector.
/// </summary>
public class ResourceHUD : MonoBehaviour
{
    public TextMeshProUGUI PooCoinsText;
    public TextMeshProUGUI GemsText;

    void Start()
    {
        EnsureUI();
        UIEventBus.OnResourceChanged += OnResourceChanged;
        RefreshAll();
    }

    void OnDestroy() => UIEventBus.OnResourceChanged -= OnResourceChanged;

    void Update() => RefreshAll();

    void EnsureUI()
    {
        if (PooCoinsText != null && GemsText != null) return;

        // Build a top bar inside the parent Canvas
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var bar = new GameObject("HUDBar");
        bar.transform.SetParent(canvas.transform, false);
        var barImg = bar.AddComponent<Image>();
        barImg.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);
        var barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0, 1);
        barRect.anchorMax = new Vector2(1, 1);
        barRect.pivot = new Vector2(0.5f, 1);
        barRect.anchoredPosition = Vector2.zero;
        barRect.sizeDelta = new Vector2(0, 45);

        // PooCoins label
        PooCoinsText = CreateLabel(bar.transform, "PooCoinsText",
            new Vector2(0, 0), new Vector2(0.3f, 1), "0");

        // Gems label
        GemsText = CreateLabel(bar.transform, "GemsText",
            new Vector2(0.3f, 0), new Vector2(0.55f, 1), "0");

        // Clan button
        var clanBtn = new GameObject("ClanButton");
        clanBtn.transform.SetParent(bar.transform, false);
        var clanImg = clanBtn.AddComponent<Image>();
        clanImg.color = new Color(0.3f, 0.45f, 0.6f);
        var clanRect = clanBtn.GetComponent<RectTransform>();
        clanRect.anchorMin = new Vector2(0.75f, 0.1f);
        clanRect.anchorMax = new Vector2(0.98f, 0.9f);
        clanRect.sizeDelta = Vector2.zero;
        var btn = clanBtn.AddComponent<Button>();
        btn.onClick.AddListener(() => GameManager.Instance?.LoadScene("ClanScreen"));

        var clanLabel = new GameObject("Label");
        clanLabel.transform.SetParent(clanBtn.transform, false);
        clanLabel.AddComponent<RectTransform>();
        var clanTmp = clanLabel.AddComponent<TextMeshProUGUI>();
        clanTmp.text = "Clan";
        clanTmp.fontSize = 16;
        clanTmp.color = Color.white;
        clanTmp.alignment = TextAlignmentOptions.Center;
        var clanLabelRect = clanLabel.GetComponent<RectTransform>();
        clanLabelRect.anchorMin = Vector2.zero;
        clanLabelRect.anchorMax = Vector2.one;
        clanLabelRect.sizeDelta = Vector2.zero;
    }

    TextMeshProUGUI CreateLabel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string text)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 16;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = Vector2.zero;
        return tmp;
    }

    void RefreshAll()
    {
        if (ResourceManager.Instance == null) return;
        UpdateText(PooCoinsText, "PooCoins", ResourceManager.Instance.PooCoins);
        UpdateText(GemsText,     "Gems",     ResourceManager.Instance.Gems);
    }

    void OnResourceChanged(string type, int newVal)
    {
        switch (type)
        {
            case "PooCoins": UpdateText(PooCoinsText, "PooCoins", newVal); break;
            case "Gems":     UpdateText(GemsText,     "Gems",     newVal); break;
        }
    }

    void UpdateText(TextMeshProUGUI label, string prefix, int val)
    {
        if (label) label.text = val >= 1000 ? $"{prefix}: {val/1000f:0.#}k" : $"{prefix}: {val}";
    }
}
