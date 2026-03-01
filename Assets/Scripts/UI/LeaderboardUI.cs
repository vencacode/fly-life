using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime-built leaderboard overlay showing rankings.
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI Instance { get; private set; }

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
        _canvas = new GameObject("LeaderboardCanvas").AddComponent<Canvas>();
        _canvas.transform.SetParent(transform);
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 870;
        _canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvas.gameObject.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("LeaderboardPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        var rt = _panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(25, 60); rt.offsetMax = new Vector2(-25, -40);
        _panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.97f);

        _panel.SetActive(false);
    }

    public void Show()
    {
        LeaderboardManager.Instance?.RefreshScore();
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
        var title = CreateLabel(_panel.transform, "Leaderboard", 28, TextAlignmentOptions.Center);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1); titleRT.anchoredPosition = new Vector2(0, -10);
        titleRT.sizeDelta = new Vector2(0, 40);

        // Scroll area
        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(_panel.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0.08f); scrollRT.anchorMax = new Vector2(1, 0.9f);
        scrollRT.offsetMin = new Vector2(8, 0); scrollRT.offsetMax = new Vector2(-8, 0);
        var scroll = scrollGO.AddComponent<ScrollRect>();
        scrollGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.15f);
        scrollGO.AddComponent<Mask>().showMaskGraphic = true;

        var content = new GameObject("Content");
        content.transform.SetParent(scrollGO.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4; vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRT;
        scroll.vertical = true; scroll.horizontal = false;

        // Entries
        var entries = LeaderboardManager.Instance?.Entries;
        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
                CreateEntryRow(content.transform, i + 1, entries[i]);
        }

        // Close button
        var closeBtn = CreateButton(_panel.transform, "Close", () => Hide());
        var closeRT = closeBtn.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(0.3f, 0); closeRT.anchorMax = new Vector2(0.7f, 0.07f);
        closeRT.offsetMin = Vector2.zero; closeRT.offsetMax = Vector2.zero;
    }

    void CreateEntryRow(Transform parent, int rank, LeaderboardManager.LeaderboardEntry entry)
    {
        var row = new GameObject($"Rank_{rank}");
        row.transform.SetParent(parent, false);
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 50;
        var rowImg = row.AddComponent<Image>();
        rowImg.color = entry.IsPlayer
            ? new Color(0.15f, 0.3f, 0.15f)
            : (rank <= 3 ? new Color(0.25f, 0.22f, 0.1f) : new Color(0.15f, 0.15f, 0.18f));

        // Rank
        string medal = rank == 1 ? "#1" : rank == 2 ? "#2" : rank == 3 ? "#3" : $"#{rank}";
        var rankLabel = CreateLabel(row.transform, medal, 18, TextAlignmentOptions.Center);
        if (rank <= 3) rankLabel.color = new Color(1f, 0.84f, 0f);
        var rankRT = rankLabel.GetComponent<RectTransform>();
        rankRT.anchorMin = new Vector2(0, 0); rankRT.anchorMax = new Vector2(0.12f, 1);
        rankRT.offsetMin = Vector2.zero; rankRT.offsetMax = Vector2.zero;

        // Name + clan
        string nameStr = entry.IsPlayer ? $"<b>{entry.PlayerName}</b>" : entry.PlayerName;
        var nameLabel = CreateLabel(row.transform, $"{nameStr}\n<size=11><color=#AAA>{entry.ClanName}</color></size>", 15, TextAlignmentOptions.Left);
        nameLabel.richText = true;
        var nameRT = nameLabel.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0.13f, 0); nameRT.anchorMax = new Vector2(0.6f, 1);
        nameRT.offsetMin = new Vector2(5, 0); nameRT.offsetMax = Vector2.zero;

        // Score
        var scoreLabel = CreateLabel(row.transform, $"{entry.Score}\n<size=11>pts</size>", 18, TextAlignmentOptions.Center);
        scoreLabel.richText = true;
        if (rank <= 3) scoreLabel.color = new Color(1f, 0.84f, 0f);
        var scoreRT = scoreLabel.GetComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(0.62f, 0); scoreRT.anchorMax = new Vector2(0.82f, 1);
        scoreRT.offsetMin = Vector2.zero; scoreRT.offsetMax = Vector2.zero;

        // Stats mini
        var statsLabel = CreateLabel(row.transform, $"D:{entry.Districts} F:{entry.TotalFlies}", 11, TextAlignmentOptions.Center);
        statsLabel.color = new Color(0.7f, 0.7f, 0.7f);
        var statsRT = statsLabel.GetComponent<RectTransform>();
        statsRT.anchorMin = new Vector2(0.82f, 0); statsRT.anchorMax = new Vector2(1, 1);
        statsRT.offsetMin = Vector2.zero; statsRT.offsetMax = Vector2.zero;
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
        tmp.text = label; tmp.fontSize = 16; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var tmpRT = tmp.GetComponent<RectTransform>();
        tmpRT.anchorMin = Vector2.zero; tmpRT.anchorMax = Vector2.one;
        tmpRT.offsetMin = Vector2.zero; tmpRT.offsetMax = Vector2.zero;
        return go;
    }
}
