using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime-built popup for selecting / purchasing skins for a fly.
/// Launched from ClanScreenController.
/// </summary>
public class SkinSelectPopup : MonoBehaviour
{
    public static SkinSelectPopup Instance { get; private set; }

    Canvas _canvas;
    GameObject _panel;
    string _flyId;
    HashSet<string> _unlockedSkins = new() { "default" };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start() => BuildUI();

    void BuildUI()
    {
        _canvas = new GameObject("SkinSelectCanvas").AddComponent<Canvas>();
        _canvas.transform.SetParent(transform);
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 830;
        _canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvas.gameObject.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("SkinPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        var panelRT = _panel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero; panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = new Vector2(40, 80); panelRT.offsetMax = new Vector2(-40, -80);
        var panelImg = _panel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.15f, 0.97f);

        _panel.SetActive(false);
    }

    public void Show(string flyId)
    {
        _flyId = flyId;
        _panel.SetActive(true);
        RebuildContent();
    }

    public void Hide() => _panel.SetActive(false);

    void RebuildContent()
    {
        // Clear previous children
        foreach (Transform child in _panel.transform)
            Destroy(child.gameObject);

        // Title
        var title = CreateLabel(_panel.transform, "Choose Skin", 28, TextAlignmentOptions.Center);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1); titleRT.anchoredPosition = new Vector2(0, -10);
        titleRT.sizeDelta = new Vector2(0, 40);

        // Scroll area
        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(_panel.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0.1f); scrollRT.anchorMax = new Vector2(1, 0.88f);
        scrollRT.offsetMin = new Vector2(10, 0); scrollRT.offsetMax = new Vector2(-10, 0);
        var scroll = scrollGO.AddComponent<ScrollRect>();
        scrollGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.2f);
        scrollGO.AddComponent<Mask>().showMaskGraphic = true;

        var content = new GameObject("Content");
        content.transform.SetParent(scrollGO.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1); contentRT.sizeDelta = new Vector2(0, 0);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6; vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRT;
        scroll.vertical = true; scroll.horizontal = false;

        // Current fly skin
        string currentSkin = "default";
        if (ClanManager.Instance != null)
        {
            foreach (var f in ClanManager.Instance.Flies)
            {
                if (f.InstanceId == _flyId)
                {
                    currentSkin = f.SkinId ?? "default";
                    break;
                }
            }
        }

        // Skin cards
        foreach (var kv in SkinRegistry.All)
        {
            var skin = kv.Value;
            bool unlocked = _unlockedSkins.Contains(skin.SkinId);
            bool equipped = skin.SkinId == currentSkin;
            CreateSkinCard(content.transform, skin, unlocked, equipped);
        }

        // Close button
        var closeBtn = CreateButton(_panel.transform, "Close", () => Hide());
        var closeBtnRT = closeBtn.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(0.3f, 0); closeBtnRT.anchorMax = new Vector2(0.7f, 0.1f);
        closeBtnRT.offsetMin = Vector2.zero; closeBtnRT.offsetMax = Vector2.zero;
    }

    void CreateSkinCard(Transform parent, SkinRegistry.SkinDef skin, bool unlocked, bool equipped)
    {
        var card = new GameObject(skin.SkinId);
        card.transform.SetParent(parent, false);
        var le = card.AddComponent<LayoutElement>();
        le.preferredHeight = 70;
        var cardImg = card.AddComponent<Image>();

        if (equipped)
            cardImg.color = new Color(0.15f, 0.35f, 0.15f);
        else if (unlocked)
            cardImg.color = new Color(0.2f, 0.2f, 0.25f);
        else
            cardImg.color = new Color(0.15f, 0.15f, 0.18f);

        // Color swatch
        var swatch = new GameObject("Swatch");
        swatch.transform.SetParent(card.transform, false);
        var swatchRT = swatch.AddComponent<RectTransform>();
        swatchRT.anchorMin = new Vector2(0, 0.15f); swatchRT.anchorMax = new Vector2(0, 0.85f);
        swatchRT.pivot = new Vector2(0, 0.5f); swatchRT.anchoredPosition = new Vector2(10, 0);
        swatchRT.sizeDelta = new Vector2(40, 0);
        var swatchImg = swatch.AddComponent<Image>();
        ColorUtility.TryParseHtmlString(skin.ColorHex, out var skinColor);
        swatchImg.color = skinColor;

        // Name + description
        var nameLabel = CreateLabel(card.transform, skin.DisplayName + (equipped ? " ✓" : ""), 18, TextAlignmentOptions.Left);
        var nameRT = nameLabel.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0.5f); nameRT.anchorMax = new Vector2(0.65f, 1);
        nameRT.offsetMin = new Vector2(60, 0); nameRT.offsetMax = Vector2.zero;

        var descLabel = CreateLabel(card.transform, skin.Description, 13, TextAlignmentOptions.Left);
        descLabel.color = new Color(0.7f, 0.7f, 0.7f);
        var descRT = descLabel.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0, 0); descRT.anchorMax = new Vector2(0.65f, 0.5f);
        descRT.offsetMin = new Vector2(60, 0); descRT.offsetMax = Vector2.zero;

        // Action button
        if (equipped)
        {
            var eqLabel = CreateLabel(card.transform, "Equipped", 14, TextAlignmentOptions.Center);
            eqLabel.color = new Color(0.4f, 0.9f, 0.4f);
            var eqRT = eqLabel.GetComponent<RectTransform>();
            eqRT.anchorMin = new Vector2(0.7f, 0.2f); eqRT.anchorMax = new Vector2(0.98f, 0.8f);
            eqRT.offsetMin = Vector2.zero; eqRT.offsetMax = Vector2.zero;
        }
        else if (unlocked)
        {
            string sid = skin.SkinId;
            var btn = CreateButton(card.transform, "Apply", () => ApplySkin(sid));
            var btnRT = btn.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.7f, 0.2f); btnRT.anchorMax = new Vector2(0.98f, 0.8f);
            btnRT.offsetMin = Vector2.zero; btnRT.offsetMax = Vector2.zero;
        }
        else if (skin.Source == SkinRegistry.SkinSource.GemShop && skin.GemCost > 0)
        {
            string sid = skin.SkinId;
            int cost = skin.GemCost;
            var btn = CreateButton(card.transform, $"{cost} Gems", () => BuySkin(sid, cost));
            var btnRT = btn.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.7f, 0.2f); btnRT.anchorMax = new Vector2(0.98f, 0.8f);
            btnRT.offsetMin = Vector2.zero; btnRT.offsetMax = Vector2.zero;
        }
        else
        {
            var lockLabel = CreateLabel(card.transform, "Locked\n" + skin.UnlockCondition, 11, TextAlignmentOptions.Center);
            lockLabel.color = new Color(0.6f, 0.6f, 0.6f);
            var lockRT = lockLabel.GetComponent<RectTransform>();
            lockRT.anchorMin = new Vector2(0.65f, 0.1f); lockRT.anchorMax = new Vector2(0.98f, 0.9f);
            lockRT.offsetMin = Vector2.zero; lockRT.offsetMax = Vector2.zero;
        }
    }

    void ApplySkin(string skinId)
    {
        if (ClanManager.Instance == null) return;
        foreach (var f in ClanManager.Instance.Flies)
        {
            if (f.InstanceId == _flyId)
            {
                f.SkinId = skinId;
                Debug.Log($"[SkinSelectPopup] Applied skin '{skinId}' to fly {_flyId}");
                ToastManager.Instance?.ShowToast($"Skin applied: {SkinRegistry.Get(skinId)?.DisplayName}", ToastType.Success);
                break;
            }
        }
        RebuildContent();
        ClanScreenController.Instance?.RefreshFlyList();
    }

    void BuySkin(string skinId, int gemCost)
    {
        if (ResourceManager.Instance == null || !ResourceManager.Instance.SpendGems(gemCost))
        {
            ToastManager.Instance?.ShowToast("Not enough Gems!", ToastType.Warning);
            return;
        }
        _unlockedSkins.Add(skinId);
        Debug.Log($"[SkinSelectPopup] Purchased skin '{skinId}' for {gemCost} Gems");
        ToastManager.Instance?.ShowToast($"Skin unlocked: {SkinRegistry.Get(skinId)?.DisplayName}", ToastType.Reward);
        RebuildContent();
    }

    public void UnlockSkin(string skinId)
    {
        if (!_unlockedSkins.Contains(skinId))
        {
            _unlockedSkins.Add(skinId);
            Debug.Log($"[SkinSelectPopup] Unlocked skin '{skinId}'");
        }
    }

    public bool IsSkinUnlocked(string skinId) => _unlockedSkins.Contains(skinId);

    // ---- Save / Load ----

    [System.Serializable]
    public class SaveData
    {
        public List<string> UnlockedSkins;
    }

    public SaveData GetSaveData() => new() { UnlockedSkins = new List<string>(_unlockedSkins) };

    public void LoadSaveData(SaveData data)
    {
        if (data?.UnlockedSkins == null) return;
        _unlockedSkins = new HashSet<string>(data.UnlockedSkins);
        if (!_unlockedSkins.Contains("default")) _unlockedSkins.Add("default");
    }

    // ---- UI Helpers ----

    static TextMeshProUGUI CreateLabel(Transform parent, string text, int fontSize, TextAlignmentOptions align)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.alignment = align;
        tmp.color = Color.white; tmp.enableAutoSizing = false;
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
