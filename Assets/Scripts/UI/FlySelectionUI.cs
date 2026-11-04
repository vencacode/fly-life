using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pre-battle fly selection overlay. Player picks up to BATTLE_SLOTS flies to send.
/// Shows fly cards with role, level, HP, equipped gear. Builds own UI at runtime.
/// </summary>
public class FlySelectionUI : MonoBehaviour
{
    public static FlySelectionUI Instance { get; private set; }

    // Runtime UI
    Canvas _canvas;
    GameObject _panel;
    Transform _cardContainer;
    TextMeshProUGUI _countText;
    Button _confirmButton;

    readonly List<FlyInstance> _selected = new();
    readonly List<GameObject>  _cards    = new();
    System.Action<List<FlyInstance>> _onConfirm;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    // ── Public API ──

    public void Show(System.Action<List<FlyInstance>> onConfirm)
    {
        _onConfirm = onConfirm;
        _selected.Clear();
        ClearCards();

        _panel.SetActive(true);
        PopulateCards();
        UpdateCountText();
    }

    public void Hide()
    {
        _panel.SetActive(false);
        _selected.Clear();
        ClearCards();
    }

    // ── UI Construction ──

    void BuildUI()
    {
        // Canvas
        var canvasGo = new GameObject("FlySelectionCanvas");
        canvasGo.transform.SetParent(transform);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 800;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Full-screen panel
        _panel = new GameObject("SelectionPanel");
        _panel.transform.SetParent(canvasGo.transform, false);
        var panelRect = _panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Dim background
        var dimBg = _panel.AddComponent<Image>();
        dimBg.color = new Color(0, 0, 0, 0.75f);
        dimBg.raycastTarget = true;

        // Title
        CreateLabel(_panel.transform, "Title", "SELECT YOUR FLIES",
            new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.95f), 30, Color.white, TextAlignmentOptions.Center);

        // Selected count
        _countText = CreateLabel(_panel.transform, "CountText", "0/5 selected",
            new Vector2(0.05f, 0.83f), new Vector2(0.95f, 0.88f), 20,
            new Color(0.8f, 0.8f, 0.6f), TextAlignmentOptions.Center);

        // Scroll view for fly cards
        BuildScrollView();

        // Bottom buttons
        _confirmButton = CreateButton(_panel.transform, "ConfirmBtn", "FIGHT!",
            new Vector2(0.55f, 0.03f), new Vector2(0.92f, 0.1f),
            new Color(0.15f, 0.45f, 0.15f, 0.95f), 24);
        _confirmButton.onClick.AddListener(OnConfirm);
        _confirmButton.interactable = false;

        var cancelBtn = CreateButton(_panel.transform, "CancelBtn", "Cancel",
            new Vector2(0.08f, 0.03f), new Vector2(0.45f, 0.1f),
            new Color(0.4f, 0.15f, 0.1f, 0.95f), 22);
        cancelBtn.onClick.AddListener(OnCancel);

        _panel.SetActive(false);
    }

    void BuildScrollView()
    {
        // Scroll view root
        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(_panel.transform, false);
        var scrollRect = scrollGo.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.05f, 0.12f);
        scrollRect.anchorMax = new Vector2(0.95f, 0.82f);
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;

        var scrollView = scrollGo.AddComponent<ScrollRect>();
        scrollView.horizontal = false;
        scrollView.vertical = true;
        scrollView.movementType = ScrollRect.MovementType.Clamped;

        // Mask
        var maskImg = scrollGo.AddComponent<Image>();
        maskImg.color = new Color(0.08f, 0.06f, 0.04f, 0.6f);
        scrollGo.AddComponent<Mask>().showMaskGraphic = true;

        // Content container with vertical layout
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(scrollGo.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.spacing = 6f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollView.content = contentRect;
        _cardContainer = contentGo.transform;
    }

    // ── Card Population ──

    void PopulateCards()
    {
        var flies = ClanManager.Instance?.GetAllFlies();
        if (flies == null) return;

        var sorted = flies.Where(f => f.IsAlive)
                         .OrderByDescending(f => f.Level)
                         .ThenBy(f => f.Role.ToString())
                         .ToList();

        foreach (var fly in sorted)
            _cards.Add(CreateFlyCard(fly));
    }

    GameObject CreateFlyCard(FlyInstance fly)
    {
        var go = new GameObject($"Card_{fly.Role}_{fly.Level}");
        go.transform.SetParent(_cardContainer, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 80);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 80;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.13f, 0.1f, 0.9f);

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 8, 8);
        hlg.spacing = 10f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Checkbox indicator
        var checkGo = new GameObject("Check");
        checkGo.transform.SetParent(go.transform, false);
        var checkImg = checkGo.AddComponent<Image>();
        checkImg.color = new Color(0.3f, 0.3f, 0.35f);
        var checkLE = checkGo.AddComponent<LayoutElement>();
        checkLE.preferredWidth = 40;
        checkLE.preferredHeight = 40;

        var checkLabel = new GameObject("CheckMark");
        checkLabel.transform.SetParent(checkGo.transform, false);
        var checkRect = checkLabel.AddComponent<RectTransform>();
        checkRect.anchorMin = Vector2.zero;
        checkRect.anchorMax = Vector2.one;
        checkRect.offsetMin = Vector2.zero;
        checkRect.offsetMax = Vector2.zero;
        var checkTMP = checkLabel.AddComponent<TextMeshProUGUI>();
        checkTMP.text = "";
        checkTMP.fontSize = 28;
        checkTMP.color = new Color(0.3f, 1f, 0.4f);
        checkTMP.alignment = TextAlignmentOptions.Center;

        // Fly info text
        var textGo = new GameObject("Info");
        textGo.transform.SetParent(go.transform, false);
        var textLE = textGo.AddComponent<LayoutElement>();
        textLE.flexibleWidth = 1;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();

        string gearText = GetGearSummary(fly.InstanceId);
        string roleColor = fly.Role switch
        {
            FlyRole.Scout   => "#8CE0FF",
            FlyRole.Soldier => "#FF9966",
            FlyRole.Worker  => "#99FF88",
            _               => "#FFFFFF"
        };
        tmp.text = $"<color={roleColor}>{fly.Role}</color> Lv{fly.Level}  |  " +
                   $"HP:{fly.MaxHealth}  ATK:{fly.Attack}  DEF:{fly.Defense}" +
                   (string.IsNullOrEmpty(gearText) ? "" : $"\n<size=16><color=#CCAA66>{gearText}</color></size>");
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.textWrappingMode = TextWrappingModes.Normal;

        // Click handler — make entire card tappable
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        var capturedFly = fly;
        var capturedBg = bg;
        var capturedCheck = checkTMP;
        btn.onClick.AddListener(() => ToggleFly(capturedFly, capturedBg, capturedCheck));

        return go;
    }

    void ToggleFly(FlyInstance fly, Image bg, TextMeshProUGUI checkMark)
    {
        if (_selected.Contains(fly))
        {
            _selected.Remove(fly);
            bg.color = new Color(0.15f, 0.13f, 0.1f, 0.9f);
            checkMark.text = "";
        }
        else if (_selected.Count < Constants.BATTLE_SLOTS)
        {
            _selected.Add(fly);
            bg.color = new Color(0.2f, 0.35f, 0.15f, 0.95f); // green tint = selected
            checkMark.text = "\u2713"; // checkmark
        }
        else
        {
            ToastManager.Instance?.ShowToast($"Max {Constants.BATTLE_SLOTS} flies!", ToastType.Warning);
        }
        UpdateCountText();
    }

    string GetGearSummary(string flyId)
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return "";

        var slots = new[] { EquipmentSlot.Weapon, EquipmentSlot.Head, EquipmentSlot.Body,
                            EquipmentSlot.Feet, EquipmentSlot.Wings, EquipmentSlot.Offhand };
        var equipped = new List<string>();
        foreach (var slot in slots)
        {
            var itemId = inv.GetEquippedItem(flyId, slot);
            if (!string.IsNullOrEmpty(itemId))
            {
                var data = EquipmentRegistry.Get(itemId);
                equipped.Add(data?.ItemName ?? itemId);
            }
        }
        return equipped.Count > 0 ? string.Join(", ", equipped) : "";
    }

    void UpdateCountText()
    {
        if (_countText) _countText.text = $"{_selected.Count}/{Constants.BATTLE_SLOTS} selected";
        if (_confirmButton) _confirmButton.interactable = _selected.Count > 0;
    }

    void ClearCards()
    {
        foreach (var card in _cards)
            if (card) Object.Destroy(card);
        _cards.Clear();
    }

    // ── Callbacks ──

    void OnConfirm()
    {
        _panel.SetActive(false);
        _onConfirm?.Invoke(new List<FlyInstance>(_selected));
    }

    void OnCancel()
    {
        _panel.SetActive(false);
        _selected.Clear();
    }

    // ── UI Helpers ──

    static TextMeshProUGUI CreateLabel(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        return tmp;
    }

    static Button CreateButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color bgColor, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8, 4);
        textRect.offsetMax = new Vector2(-8, -4);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }
}
