using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Modal overlay for deploying flies to a player-owned district.
/// Shows available flies per role with +/- selectors, deploy and recall buttons.
/// Built at runtime (same pattern as MapSceneBootstrap).
/// </summary>
public class FlyDeployPanel : MonoBehaviour
{
    District _district;

    // Per-role UI
    TextMeshProUGUI[] _availLabels = new TextMeshProUGUI[3];
    TextMeshProUGUI[] _countLabels = new TextMeshProUGUI[3];
    TextMeshProUGUI   _deployedLabel;
    TextMeshProUGUI   _titleLabel;
    int[]             _selected = new int[3]; // amounts to deploy

    GameObject _root;
    Action _onClose;

    static readonly FlyRole[] Roles = { FlyRole.Scout, FlyRole.Soldier, FlyRole.Worker };

    void Awake()
    {
        _root = BuildUI();
        _root.SetActive(false);
    }

    public void Show(District district, Action onClose = null)
    {
        _district = district;
        _onClose = onClose;
        _selected = new int[3];
        _root.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        _root.SetActive(false);
        _onClose?.Invoke();
    }

    public bool IsVisible => _root != null && _root.activeSelf;

    void Refresh()
    {
        if (_district == null || ClanManager.Instance == null) return;

        _titleLabel.text = $"Deploy to {_district.Name}";
        int deployed = ClanManager.Instance.GetDeployedCount(_district.Id);
        _deployedLabel.text = $"{deployed} flies stationed here";

        for (int i = 0; i < 3; i++)
        {
            int avail = ClanManager.Instance.GetAvailableCount(Roles[i]);
            _availLabels[i].text = $"{Roles[i]}: {avail} available";
            _selected[i] = Mathf.Clamp(_selected[i], 0, avail);
            _countLabels[i].text = _selected[i].ToString();
        }
    }

    void OnDeploy()
    {
        if (ClanManager.Instance == null) return;
        for (int i = 0; i < 3; i++)
        {
            if (_selected[i] > 0)
                ClanManager.Instance.DeployFlies(_district.Id, Roles[i], _selected[i]);
        }
        _selected = new int[3];
        Refresh();
        MapManager.Instance?.RefreshAllTiles();
    }

    void OnRecall()
    {
        ClanManager.Instance?.RecallFlies(_district.Id);
        Refresh();
        MapManager.Instance?.RefreshAllTiles();
    }

    // ---- UI construction ----

    GameObject BuildUI()
    {
        var canvasGo = new GameObject("FlyDeployCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20; // above other UI
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Dark overlay
        var overlay = MakeRect(canvasGo.transform, "Overlay", Vector2.zero, Vector2.one);
        overlay.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);

        // Panel
        var panel = MakeRect(canvasGo.transform, "Panel",
            new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.75f));
        panel.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 0.98f);

        // Title
        _titleLabel = MakeTMP(panel, "Title", 30, FontStyles.Bold,
            new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.97f));
        _titleLabel.alignment = TextAlignmentOptions.Center;

        // Deployed count
        _deployedLabel = MakeTMP(panel, "Deployed", 20, FontStyles.Italic,
            new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.85f));
        _deployedLabel.alignment = TextAlignmentOptions.Center;
        _deployedLabel.color = new Color(0.7f, 0.8f, 1f);

        // Role rows
        float rowTop = 0.72f;
        for (int i = 0; i < 3; i++)
        {
            float yMax = rowTop - i * 0.18f;
            float yMin = yMax - 0.14f;
            BuildRoleRow(panel, i, yMin, yMax);
        }

        // Bottom buttons
        float btnY = 0.04f;
        float btnH = 0.12f;

        var deployBtn = MakeButton(panel, "DeployBtn", "Deploy",
            new Vector2(0.05f, btnY), new Vector2(0.38f, btnY + btnH),
            new Color(0.2f, 0.7f, 0.3f));
        deployBtn.GetComponent<Button>().onClick.AddListener(OnDeploy);

        var recallBtn = MakeButton(panel, "RecallBtn", "Recall All",
            new Vector2(0.40f, btnY), new Vector2(0.68f, btnY + btnH),
            new Color(0.8f, 0.6f, 0.1f));
        recallBtn.GetComponent<Button>().onClick.AddListener(OnRecall);

        var closeBtn = MakeButton(panel, "CloseBtn", "Close",
            new Vector2(0.70f, btnY), new Vector2(0.95f, btnY + btnH),
            new Color(0.5f, 0.5f, 0.5f));
        closeBtn.GetComponent<Button>().onClick.AddListener(Hide);

        return canvasGo;
    }

    void BuildRoleRow(RectTransform parent, int index, float yMin, float yMax)
    {
        int idx = index; // capture for closures

        // Role label + available count
        _availLabels[idx] = MakeTMP(parent, $"Avail_{Roles[idx]}", 22, FontStyles.Normal,
            new Vector2(0.05f, yMin), new Vector2(0.50f, yMax));
        _availLabels[idx].alignment = TextAlignmentOptions.MidlineLeft;

        // Minus button
        var minus = MakeButton(parent, $"Minus_{Roles[idx]}", "-",
            new Vector2(0.52f, yMin + 0.02f), new Vector2(0.62f, yMax - 0.02f),
            new Color(0.7f, 0.3f, 0.3f));
        minus.GetComponent<Button>().onClick.AddListener(() =>
        {
            _selected[idx] = Mathf.Max(0, _selected[idx] - 1);
            Refresh();
        });

        // Count display
        _countLabels[idx] = MakeTMP(parent, $"Count_{Roles[idx]}", 26, FontStyles.Bold,
            new Vector2(0.63f, yMin), new Vector2(0.77f, yMax));
        _countLabels[idx].alignment = TextAlignmentOptions.Center;

        // Plus button
        var plus = MakeButton(parent, $"Plus_{Roles[idx]}", "+",
            new Vector2(0.78f, yMin + 0.02f), new Vector2(0.88f, yMax - 0.02f),
            new Color(0.3f, 0.7f, 0.3f));
        plus.GetComponent<Button>().onClick.AddListener(() =>
        {
            int avail = ClanManager.Instance?.GetAvailableCount(Roles[idx]) ?? 0;
            _selected[idx] = Mathf.Min(avail, _selected[idx] + 1);
            Refresh();
        });
    }

    // ---- Helpers ----

    RectTransform MakeRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        return r;
    }

    TextMeshProUGUI MakeTMP(RectTransform parent, string name, int fontSize, FontStyles style,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var r = MakeRect(parent, name, anchorMin, anchorMax);
        var tmp = r.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        return tmp;
    }

    GameObject MakeButton(RectTransform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
    {
        var r = MakeRect(parent, name, anchorMin, anchorMax);
        var img = r.gameObject.AddComponent<Image>();
        img.color = bgColor;
        r.gameObject.AddComponent<Button>();

        var txt = MakeRect(r, "Label", Vector2.zero, Vector2.one);
        var tmp = txt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return r.gameObject;
    }
}
