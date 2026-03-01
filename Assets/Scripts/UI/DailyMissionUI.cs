using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime-built daily missions panel showing 3 active missions with progress.
/// </summary>
public class DailyMissionUI : MonoBehaviour
{
    public static DailyMissionUI Instance { get; private set; }

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
        _canvas = new GameObject("MissionCanvas").AddComponent<Canvas>();
        _canvas.transform.SetParent(transform);
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 875;
        _canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvas.gameObject.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("MissionPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        var rt = _panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 0.25f); rt.anchorMax = new Vector2(0.95f, 0.75f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        _panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.13f, 0.97f);

        _panel.SetActive(false);
    }

    public void Show()
    {
        _panel.SetActive(true);
        RebuildContent();
    }

    public void Hide() => _panel.SetActive(false);
    public void Toggle() { if (_panel.activeSelf) Hide(); else Show(); }

    void RebuildContent()
    {
        foreach (Transform child in _panel.transform)
            Destroy(child.gameObject);

        // Title
        var title = CreateLabel(_panel.transform, "Daily Missions", 24, TextAlignmentOptions.Center);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1); titleRT.anchoredPosition = new Vector2(0, -8);
        titleRT.sizeDelta = new Vector2(0, 34);

        // Timer
        float hours = DailyMissionManager.Instance?.HoursUntilRefresh() ?? 0f;
        int h = Mathf.FloorToInt(hours);
        int m = Mathf.FloorToInt((hours - h) * 60);
        var timer = CreateLabel(_panel.transform, $"Resets in {h}h {m}m", 13, TextAlignmentOptions.Center);
        timer.color = new Color(0.6f, 0.6f, 0.8f);
        var timerRT = timer.GetComponent<RectTransform>();
        timerRT.anchorMin = new Vector2(0, 1); timerRT.anchorMax = new Vector2(1, 1);
        timerRT.pivot = new Vector2(0.5f, 1); timerRT.anchoredPosition = new Vector2(0, -40);
        timerRT.sizeDelta = new Vector2(0, 20);

        // Mission cards
        var missions = DailyMissionManager.Instance?.Missions;
        if (missions != null)
        {
            for (int i = 0; i < missions.Count; i++)
                CreateMissionCard(i, missions[i]);
        }

        // Close button
        var closeBtn = CreateButton(_panel.transform, "Close", () => Hide());
        var closeRT = closeBtn.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(0.3f, 0.02f); closeRT.anchorMax = new Vector2(0.7f, 0.1f);
        closeRT.offsetMin = Vector2.zero; closeRT.offsetMax = Vector2.zero;
    }

    void CreateMissionCard(int index, DailyMissionManager.ActiveMission mission)
    {
        float cardHeight = 0.22f;
        float startY = 0.88f - 0.04f; // below title/timer
        float top = startY - index * (cardHeight + 0.02f);
        float bottom = top - cardHeight;

        var card = new GameObject($"Mission_{index}");
        card.transform.SetParent(_panel.transform, false);
        var cardRT = card.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.04f, bottom);
        cardRT.anchorMax = new Vector2(0.96f, top);
        cardRT.offsetMin = Vector2.zero; cardRT.offsetMax = Vector2.zero;

        Color cardColor = mission.Claimed
            ? new Color(0.15f, 0.25f, 0.15f)
            : (mission.Progress >= mission.Target
                ? new Color(0.2f, 0.35f, 0.15f)
                : new Color(0.13f, 0.13f, 0.18f));
        card.AddComponent<Image>().color = cardColor;

        // Description
        var desc = CreateLabel(card.transform, mission.DisplayText, 16, TextAlignmentOptions.Left);
        var descRT = desc.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0.04f, 0.55f); descRT.anchorMax = new Vector2(0.96f, 0.95f);
        descRT.offsetMin = Vector2.zero; descRT.offsetMax = Vector2.zero;

        // Progress text
        string progStr = mission.Claimed ? "<color=#4F4>CLAIMED</color>"
            : $"{mission.Progress}/{mission.Target}";
        var prog = CreateLabel(card.transform, progStr, 14, TextAlignmentOptions.Left);
        prog.richText = true;
        var progRT = prog.GetComponent<RectTransform>();
        progRT.anchorMin = new Vector2(0.04f, 0.25f); progRT.anchorMax = new Vector2(0.4f, 0.55f);
        progRT.offsetMin = Vector2.zero; progRT.offsetMax = Vector2.zero;

        // Progress bar
        var barBG = new GameObject("BarBG");
        barBG.transform.SetParent(card.transform, false);
        var barBGRT = barBG.AddComponent<RectTransform>();
        barBGRT.anchorMin = new Vector2(0.04f, 0.08f); barBGRT.anchorMax = new Vector2(0.6f, 0.22f);
        barBGRT.offsetMin = Vector2.zero; barBGRT.offsetMax = Vector2.zero;
        barBG.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);

        float fillPct = mission.Target > 0 ? Mathf.Clamp01((float)mission.Progress / mission.Target) : 0f;
        var barFill = new GameObject("BarFill");
        barFill.transform.SetParent(barBG.transform, false);
        var barFillRT = barFill.AddComponent<RectTransform>();
        barFillRT.anchorMin = Vector2.zero; barFillRT.anchorMax = new Vector2(fillPct, 1);
        barFillRT.offsetMin = Vector2.zero; barFillRT.offsetMax = Vector2.zero;
        barFill.AddComponent<Image>().color = mission.Claimed
            ? new Color(0.3f, 0.6f, 0.3f)
            : new Color(0.3f, 0.7f, 1f);

        // Reward text
        var reward = CreateLabel(card.transform,
            $"+{mission.CoinReward}c +{mission.GemReward}g", 13, TextAlignmentOptions.Center);
        reward.color = new Color(1f, 0.9f, 0.5f);
        var rewardRT = reward.GetComponent<RectTransform>();
        rewardRT.anchorMin = new Vector2(0.62f, 0.55f); rewardRT.anchorMax = new Vector2(0.96f, 0.95f);
        rewardRT.offsetMin = Vector2.zero; rewardRT.offsetMax = Vector2.zero;

        // Claim button
        if (!mission.Claimed && mission.Progress >= mission.Target)
        {
            int idx = index; // capture for closure
            var claimBtn = CreateButton(card.transform, "Claim!", () =>
            {
                DailyMissionManager.Instance?.ClaimReward(idx);
                RebuildContent();
            });
            var claimRT = claimBtn.GetComponent<RectTransform>();
            claimRT.anchorMin = new Vector2(0.62f, 0.08f); claimRT.anchorMax = new Vector2(0.96f, 0.5f);
            claimRT.offsetMin = Vector2.zero; claimRT.offsetMax = Vector2.zero;
            claimBtn.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f);
        }
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
