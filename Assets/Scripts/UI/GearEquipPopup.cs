using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Popup for equipping gear to a specific fly. Shows all owned unequipped items.
/// Builds own UI at runtime.
/// </summary>
public class GearEquipPopup : MonoBehaviour
{
    public static GearEquipPopup Instance { get; private set; }

    Canvas _canvas;
    GameObject _panel;
    TextMeshProUGUI _flyLabel;
    Transform _itemContainer;
    List<GameObject> _itemButtons = new();
    string _targetFlyId;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    // ── Public API ──

    public void Show(FlyInstance fly)
    {
        _targetFlyId = fly.InstanceId;
        _flyLabel.text = $"Equip gear for {fly.Role} Lv{fly.Level}";
        _panel.SetActive(true);
        PopulateItems(fly);
    }

    public void Hide() => _panel.SetActive(false);

    // ── UI Construction ──

    void BuildUI()
    {
        var canvasGo = new GameObject("GearEquipCanvas");
        canvasGo.transform.SetParent(transform);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 820;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Panel (full screen dim bg)
        _panel = new GameObject("GearPanel");
        _panel.transform.SetParent(canvasGo.transform, false);
        var panelRect = _panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var dimBg = _panel.AddComponent<Image>();
        dimBg.color = new Color(0, 0, 0, 0.75f);
        dimBg.raycastTarget = true;

        // Card
        var card = CreatePanel(_panel.transform, "Card",
            new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.85f),
            new Color(0.12f, 0.1f, 0.08f, 0.97f));

        // Title
        _flyLabel = CreateLabel(card.transform, "FlyLabel", "Equip gear for...",
            new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.98f), 24, Color.white, TextAlignmentOptions.Center);

        // Scroll view for item list
        BuildScrollView(card.transform);

        // Close button
        var closeBtn = CreateButton(card.transform, "CloseBtn", "Close",
            new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.09f),
            new Color(0.3f, 0.15f, 0.1f, 0.95f), 22);
        closeBtn.onClick.AddListener(Hide);

        _panel.SetActive(false);
    }

    void BuildScrollView(Transform parent)
    {
        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(parent, false);
        var scrollRect = scrollGo.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.03f, 0.1f);
        scrollRect.anchorMax = new Vector2(0.97f, 0.88f);
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;

        var sv = scrollGo.AddComponent<ScrollRect>();
        sv.horizontal = false;
        sv.vertical = true;
        sv.movementType = ScrollRect.MovementType.Clamped;

        var maskImg = scrollGo.AddComponent<Image>();
        maskImg.color = new Color(0.08f, 0.06f, 0.04f, 0.5f);
        scrollGo.AddComponent<Mask>().showMaskGraphic = true;

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

        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sv.content = contentRect;
        _itemContainer = contentGo.transform;
    }

    // ── Item Population ──

    void PopulateItems(FlyInstance fly)
    {
        ClearItems();

        var inv = InventoryManager.Instance;
        if (inv == null) return;

        // Show currently equipped items first (with "Unequip" option)
        var slots = new[] { EquipmentSlot.Weapon, EquipmentSlot.Head, EquipmentSlot.Body,
                            EquipmentSlot.Feet, EquipmentSlot.Wings, EquipmentSlot.Offhand };
        foreach (var slot in slots)
        {
            var equippedId = inv.GetEquippedItem(_targetFlyId, slot);
            if (!string.IsNullOrEmpty(equippedId))
            {
                var def = EquipmentRegistry.Get(equippedId);
                string name = def?.ItemName ?? equippedId;
                var row = CreateItemRow($"[{slot}] {name} (equipped)",
                    new Color(0.2f, 0.35f, 0.15f, 0.9f), "Unequip", () =>
                    {
                        inv.UnequipSlot(_targetFlyId, slot);
                        ClanScreenController.Instance?.RefreshFlyList();
                        PopulateItems(fly); // refresh popup
                    });
                _itemButtons.Add(row);
            }
        }

        // Show available unequipped items
        var ownedIds = inv.OwnedItemIds;
        var seen = new HashSet<string>();
        foreach (var itemId in ownedIds)
        {
            if (seen.Contains(itemId)) continue;
            seen.Add(itemId);

            // Check if this item is already equipped on ANY fly
            bool isEquipped = IsItemEquipped(itemId);
            if (isEquipped) continue;

            var def = EquipmentRegistry.Get(itemId);
            if (def == null) continue;

            string stats = BuildStatText(def.Value);
            string label = $"{def.Value.ItemName} [{def.Value.Slot}]\n<size=16>{stats}</size>";

            var capturedId = itemId;
            var row = CreateItemRow(label, new Color(0.18f, 0.16f, 0.12f, 0.9f), "Equip", () =>
            {
                inv.EquipItem(_targetFlyId, capturedId);
                ClanScreenController.Instance?.RefreshFlyList();
                PopulateItems(fly); // refresh popup
            });
            _itemButtons.Add(row);
        }

        if (_itemButtons.Count == 0)
        {
            var emptyLabel = new GameObject("EmptyLabel");
            emptyLabel.transform.SetParent(_itemContainer, false);
            emptyLabel.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 60);
            emptyLabel.AddComponent<LayoutElement>().preferredHeight = 60;
            var tmp = emptyLabel.AddComponent<TextMeshProUGUI>();
            tmp.text = "No gear available.\nWin battles to earn gear drops!";
            tmp.fontSize = 18;
            tmp.color = new Color(0.6f, 0.5f, 0.4f);
            tmp.alignment = TextAlignmentOptions.Center;
            _itemButtons.Add(emptyLabel);
        }
    }

    bool IsItemEquipped(string itemId)
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return false;
        var flies = ClanManager.Instance?.Flies;
        if (flies == null) return false;

        var slots = new[] { EquipmentSlot.Weapon, EquipmentSlot.Head, EquipmentSlot.Body,
                            EquipmentSlot.Feet, EquipmentSlot.Wings, EquipmentSlot.Offhand };
        foreach (var fly in flies)
            foreach (var slot in slots)
                if (inv.GetEquippedItem(fly.InstanceId, slot) == itemId) return true;
        return false;
    }

    string BuildStatText(EquipmentRegistry.EquipmentDef def)
    {
        var parts = new List<string>();
        if (def.AttackBonus  > 0) parts.Add($"ATK+{def.AttackBonus}");
        if (def.DefenseBonus > 0) parts.Add($"DEF+{def.DefenseBonus}");
        if (def.SpeedBonus   > 0) parts.Add($"SPD+{def.SpeedBonus}");
        if (def.HealthBonus  > 0) parts.Add($"HP+{def.HealthBonus}");
        return parts.Count > 0 ? string.Join("  ", parts) : "No stat bonuses";
    }

    GameObject CreateItemRow(string label, Color bgColor, string actionLabel, UnityEngine.Events.UnityAction onAction)
    {
        var go = new GameObject("ItemRow");
        go.transform.SetParent(_itemContainer, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 70);
        go.AddComponent<LayoutElement>().preferredHeight = 70;
        var bg = go.AddComponent<Image>();
        bg.color = bgColor;

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 6, 6);
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Item name/stats
        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        textGo.AddComponent<LayoutElement>().flexibleWidth = 1;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 17;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = true;
        tmp.richText = true;

        // Action button
        var btnGo = new GameObject("ActionBtn");
        btnGo.transform.SetParent(go.transform, false);
        btnGo.AddComponent<LayoutElement>().preferredWidth = 100;
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.25f, 0.45f, 0.25f, 0.95f);
        var btn = btnGo.AddComponent<Button>();
        btn.onClick.AddListener(onAction);

        var btnTextGo = new GameObject("BtnLabel");
        btnTextGo.transform.SetParent(btnGo.transform, false);
        var btnRect = btnTextGo.AddComponent<RectTransform>();
        btnRect.anchorMin = Vector2.zero;
        btnRect.anchorMax = Vector2.one;
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;
        var btnTmp = btnTextGo.AddComponent<TextMeshProUGUI>();
        btnTmp.text = actionLabel;
        btnTmp.fontSize = 16;
        btnTmp.color = Color.white;
        btnTmp.alignment = TextAlignmentOptions.Center;

        return go;
    }

    void ClearItems()
    {
        foreach (var item in _itemButtons)
            if (item) Destroy(item);
        _itemButtons.Clear();
    }

    // ── UI Helpers ──

    static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

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
