using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime-built collection codex showing all discovered gear, skins, districts, and flies.
/// </summary>
public class CodexUI : MonoBehaviour
{
    public static CodexUI Instance { get; private set; }

    Canvas _canvas;
    GameObject _panel;
    string _activeTab = "Gear";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start() => BuildUI();

    void BuildUI()
    {
        _canvas = new GameObject("CodexCanvas").AddComponent<Canvas>();
        _canvas.transform.SetParent(transform);
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 885;
        _canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvas.gameObject.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("CodexPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        var rt = _panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(15, 45); rt.offsetMax = new Vector2(-15, -30);
        _panel.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.12f, 0.97f);

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

        // Title + completion
        float pct = CodexManager.Instance?.GetCompletionPercent() ?? 0f;
        var title = CreateLabel(_panel.transform,
            $"Collection Codex  ({Mathf.RoundToInt(pct * 100)}%)", 22, TextAlignmentOptions.Center);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1); titleRT.anchoredPosition = new Vector2(0, -6);
        titleRT.sizeDelta = new Vector2(0, 30);

        // Tab bar
        string[] tabs = { "Gear", "Skins", "Districts", "Flies" };
        float tabWidth = 1f / tabs.Length;
        for (int i = 0; i < tabs.Length; i++)
        {
            string tab = tabs[i];
            float x = i * tabWidth;
            var tabBtn = CreateTabButton(tab, x, tabWidth, tab == _activeTab);
        }

        // Content area
        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(_panel.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0.07f); scrollRT.anchorMax = new Vector2(1, 0.88f);
        scrollRT.offsetMin = new Vector2(6, 0); scrollRT.offsetMax = new Vector2(-6, 0);
        var scroll = scrollGO.AddComponent<ScrollRect>();
        scrollGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.1f);
        scrollGO.AddComponent<Mask>().showMaskGraphic = true;

        var content = new GameObject("Content");
        content.transform.SetParent(scrollGO.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 3; vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRT;
        scroll.vertical = true; scroll.horizontal = false;

        // Populate based on active tab
        switch (_activeTab)
        {
            case "Gear":    PopulateGear(content.transform); break;
            case "Skins":   PopulateSkins(content.transform); break;
            case "Districts": PopulateDistricts(content.transform); break;
            case "Flies":   PopulateFlies(content.transform); break;
        }

        // Close button
        var closeBtn = CreateButton(_panel.transform, "Close", () => Hide());
        var closeRT = closeBtn.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(0.3f, 0.01f); closeRT.anchorMax = new Vector2(0.7f, 0.06f);
        closeRT.offsetMin = Vector2.zero; closeRT.offsetMax = Vector2.zero;
    }

    GameObject CreateTabButton(string label, float x, float width, bool active)
    {
        var btn = new GameObject($"Tab_{label}");
        btn.transform.SetParent(_panel.transform, false);
        var rt = btn.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(x + 0.005f, 0.89f);
        rt.anchorMax = new Vector2(x + width - 0.005f, 0.95f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        btn.AddComponent<Image>().color = active
            ? new Color(0.2f, 0.35f, 0.5f)
            : new Color(0.15f, 0.15f, 0.2f);

        string tab = label;
        var button = btn.AddComponent<Button>();
        button.onClick.AddListener(() => { _activeTab = tab; RebuildContent(); });

        var tmp = new GameObject("Label").AddComponent<TextMeshProUGUI>();
        tmp.transform.SetParent(btn.transform, false);
        tmp.text = label; tmp.fontSize = 14; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = active ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        var tmpRT = tmp.GetComponent<RectTransform>();
        tmpRT.anchorMin = Vector2.zero; tmpRT.anchorMax = Vector2.one;
        tmpRT.offsetMin = Vector2.zero; tmpRT.offsetMax = Vector2.zero;
        return btn;
    }

    void PopulateGear(Transform parent)
    {
        var discovered = CodexManager.Instance?.DiscoveredGear;
        foreach (var kv in EquipmentRegistry.All)
        {
            bool found = discovered != null && discovered.Contains(kv.Key);
            CreateEntryRow(parent, found ? kv.Value.ItemName : "???",
                found ? $"{kv.Value.Rarity} | {kv.Value.Slot} | ATK+{kv.Value.AttackBonus} DEF+{kv.Value.DefenseBonus}" : "Not discovered yet",
                found);
        }
    }

    void PopulateSkins(Transform parent)
    {
        var discovered = CodexManager.Instance?.DiscoveredSkins;
        foreach (var kv in SkinRegistry.All)
        {
            bool found = discovered != null && discovered.Contains(kv.Key);
            CreateEntryRow(parent, found ? kv.Value.DisplayName : "???",
                found ? kv.Value.Description : "Not discovered yet",
                found);
        }
    }

    void PopulateDistricts(Transform parent)
    {
        var captured = CodexManager.Instance?.CapturedDistricts;
        int w = TerritoryManager.Instance?.GridWidth ?? 7;
        int h = TerritoryManager.Instance?.GridHeight ?? 7;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                string id = $"district_{x}_{y}";
                var district = TerritoryManager.Instance?.GetDistrict(id);
                bool found = captured != null && captured.Contains(id);
                string name = found && district != null ? district.Name : "???";
                string desc = found && district != null ? $"{district.Theme} | Difficulty {district.DifficultyLevel}" : "Not captured yet";
                CreateEntryRow(parent, name, desc, found);
            }
        }
    }

    void PopulateFlies(Transform parent)
    {
        var discovered = CodexManager.Instance?.DiscoveredRoles;
        foreach (FlyRole role in new[] { FlyRole.Scout, FlyRole.Soldier, FlyRole.Worker })
        {
            bool found = discovered != null && discovered.Contains(role.ToString());
            CreateEntryRow(parent, found ? role.ToString() : "???",
                found ? $"Base role: {role}" : "Not discovered yet",
                found);
        }
    }

    void CreateEntryRow(Transform parent, string name, string desc, bool discovered)
    {
        var row = new GameObject("Entry");
        row.transform.SetParent(parent, false);
        row.AddComponent<LayoutElement>().preferredHeight = 44;
        row.AddComponent<Image>().color = discovered
            ? new Color(0.12f, 0.18f, 0.12f)
            : new Color(0.12f, 0.12f, 0.15f);

        var label = CreateLabel(row.transform,
            $"<b>{name}</b>\n<size=11>{desc}</size>", 13, TextAlignmentOptions.Left);
        label.richText = true;
        label.color = discovered ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        var labelRT = label.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0.03f, 0); labelRT.anchorMax = new Vector2(0.97f, 1);
        labelRT.offsetMin = new Vector2(4, 2); labelRT.offsetMax = new Vector2(-4, -2);
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
