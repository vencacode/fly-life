using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime-built achievement list overlay showing all achievements with progress.
/// </summary>
public class AchievementPopup : MonoBehaviour
{
    public static AchievementPopup Instance { get; private set; }

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
        _canvas = new GameObject("AchievementCanvas").AddComponent<Canvas>();
        _canvas.transform.SetParent(transform);
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 880;
        _canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvas.gameObject.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("AchievementPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        var rt = _panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(20, 50); rt.offsetMax = new Vector2(-20, -35);
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

        // Title
        var title = CreateLabel(_panel.transform, "Achievements", 26, TextAlignmentOptions.Center);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1); titleRT.anchoredPosition = new Vector2(0, -8);
        titleRT.sizeDelta = new Vector2(0, 36);

        // Completion summary
        int total = AchievementRegistry.All.Count;
        int unlocked = AchievementRegistry.All.Values.Count(a =>
            AchievementManager.Instance != null && AchievementManager.Instance.IsUnlocked(a.Id));
        var summary = CreateLabel(_panel.transform, $"{unlocked}/{total} completed", 14, TextAlignmentOptions.Center);
        summary.color = new Color(0.7f, 0.85f, 0.7f);
        var summaryRT = summary.GetComponent<RectTransform>();
        summaryRT.anchorMin = new Vector2(0, 1); summaryRT.anchorMax = new Vector2(1, 1);
        summaryRT.pivot = new Vector2(0.5f, 1); summaryRT.anchoredPosition = new Vector2(0, -42);
        summaryRT.sizeDelta = new Vector2(0, 22);

        // Scroll area
        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(_panel.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0.07f); scrollRT.anchorMax = new Vector2(1, 0.88f);
        scrollRT.offsetMin = new Vector2(6, 0); scrollRT.offsetMax = new Vector2(-6, 0);
        var scroll = scrollGO.AddComponent<ScrollRect>();
        scrollGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.12f);
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

        // Group by category
        var grouped = AchievementRegistry.All.Values
            .GroupBy(a => a.Category)
            .OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            // Category header
            var catLabel = CreateLabel(content.transform,
                $"--- {group.Key} ---", 14, TextAlignmentOptions.Center);
            catLabel.color = new Color(0.6f, 0.6f, 0.8f);
            var catLE = catLabel.gameObject.AddComponent<LayoutElement>();
            catLE.preferredHeight = 28;

            foreach (var achievement in group)
                CreateAchievementRow(content.transform, achievement);
        }

        // Close button
        var closeBtn = CreateButton(_panel.transform, "Close", () => Hide());
        var closeRT = closeBtn.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(0.3f, 0); closeRT.anchorMax = new Vector2(0.7f, 0.06f);
        closeRT.offsetMin = Vector2.zero; closeRT.offsetMax = Vector2.zero;
    }

    void CreateAchievementRow(Transform parent, AchievementRegistry.AchievementDef def)
    {
        bool isUnlocked = AchievementManager.Instance != null && AchievementManager.Instance.IsUnlocked(def.Id);
        float progress = AchievementManager.Instance?.GetProgress(def.Id) ?? 0f;
        int current = AchievementManager.Instance?.GetCurrentValue(def.Id) ?? 0;

        var row = new GameObject($"Achievement_{def.Id}");
        row.transform.SetParent(parent, false);
        row.AddComponent<LayoutElement>().preferredHeight = 60;
        var rowImg = row.AddComponent<Image>();
        rowImg.color = isUnlocked
            ? new Color(0.12f, 0.25f, 0.12f)
            : new Color(0.14f, 0.14f, 0.18f);

        // Check mark or progress
        string statusStr = isUnlocked ? "<color=#4F4>DONE</color>" : $"{current}/{def.Target}";
        var statusLabel = CreateLabel(row.transform, statusStr, 13, TextAlignmentOptions.Center);
        statusLabel.richText = true;
        var statusRT = statusLabel.GetComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0, 0); statusRT.anchorMax = new Vector2(0.14f, 1);
        statusRT.offsetMin = Vector2.zero; statusRT.offsetMax = Vector2.zero;

        // Name + description
        string nameColor = isUnlocked ? "#FFD700" : "#FFF";
        string nameStr = $"<color={nameColor}><b>{def.DisplayName}</b></color>\n<size=11>{def.Description}</size>";
        var nameLabel = CreateLabel(row.transform, nameStr, 14, TextAlignmentOptions.Left);
        nameLabel.richText = true;
        var nameRT = nameLabel.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0.15f, 0); nameRT.anchorMax = new Vector2(0.72f, 1);
        nameRT.offsetMin = new Vector2(4, 2); nameRT.offsetMax = new Vector2(0, -2);

        // Reward
        string rewardStr = $"+{def.GemReward} gems";
        if (!string.IsNullOrEmpty(def.SkinReward))
            rewardStr += $"\n<size=10>+{def.SkinReward} skin</size>";
        var rewardLabel = CreateLabel(row.transform, rewardStr, 12, TextAlignmentOptions.Center);
        rewardLabel.richText = true;
        rewardLabel.color = isUnlocked ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.3f, 0.9f, 1f);
        var rewardRT = rewardLabel.GetComponent<RectTransform>();
        rewardRT.anchorMin = new Vector2(0.73f, 0); rewardRT.anchorMax = new Vector2(1, 1);
        rewardRT.offsetMin = Vector2.zero; rewardRT.offsetMax = Vector2.zero;

        // Progress bar background
        if (!isUnlocked)
        {
            var barBG = new GameObject("BarBG");
            barBG.transform.SetParent(row.transform, false);
            var barBGRT = barBG.AddComponent<RectTransform>();
            barBGRT.anchorMin = new Vector2(0.15f, 0); barBGRT.anchorMax = new Vector2(0.72f, 0.12f);
            barBGRT.offsetMin = new Vector2(4, 2); barBGRT.offsetMax = new Vector2(-4, 0);
            barBG.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

            var barFill = new GameObject("BarFill");
            barFill.transform.SetParent(barBG.transform, false);
            var barFillRT = barFill.AddComponent<RectTransform>();
            barFillRT.anchorMin = Vector2.zero; barFillRT.anchorMax = new Vector2(progress, 1);
            barFillRT.offsetMin = Vector2.zero; barFillRT.offsetMax = Vector2.zero;
            barFill.AddComponent<Image>().color = new Color(0.2f, 0.7f, 0.3f);
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
        tmp.text = label; tmp.fontSize = 16; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var tmpRT = tmp.GetComponent<RectTransform>();
        tmpRT.anchorMin = Vector2.zero; tmpRT.anchorMax = Vector2.one;
        tmpRT.offsetMin = Vector2.zero; tmpRT.offsetMax = Vector2.zero;
        return go;
    }
}
