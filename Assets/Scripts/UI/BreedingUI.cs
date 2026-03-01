using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime-built fly breeding (fusion) popup.
/// Select two same-role, level 5+ flies to breed into one stronger fly.
/// </summary>
public class BreedingUI : MonoBehaviour
{
    public static BreedingUI Instance { get; private set; }

    Canvas _canvas;
    GameObject _panel;

    string _selectedFlyId1;
    string _selectedFlyId2;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start() => BuildUI();

    void BuildUI()
    {
        _canvas = new GameObject("BreedingCanvas").AddComponent<Canvas>();
        _canvas.transform.SetParent(transform);
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 835;
        _canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvas.gameObject.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("BreedingPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        var rt = _panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.03f, 0.15f); rt.anchorMax = new Vector2(0.97f, 0.85f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        _panel.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.12f, 0.97f);

        _panel.SetActive(false);
    }

    public void Show()
    {
        _selectedFlyId1 = null;
        _selectedFlyId2 = null;
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
        var title = CreateLabel(_panel.transform, "Fly Breeding", 24, TextAlignmentOptions.Center);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1); titleRT.anchoredPosition = new Vector2(0, -6);
        titleRT.sizeDelta = new Vector2(0, 30);

        // Cost info
        var info = CreateLabel(_panel.transform,
            $"Combine 2 same-role flies (Lv{Constants.BREEDING_MIN_LEVEL}+) into 1 stronger fly.\nCost: {Constants.BREEDING_COST} PooCoins | +{Constants.BREEDING_STAT_BONUS * 100}% stat bonus",
            12, TextAlignmentOptions.Center);
        info.color = new Color(0.7f, 0.7f, 0.8f);
        var infoRT = info.GetComponent<RectTransform>();
        infoRT.anchorMin = new Vector2(0, 1); infoRT.anchorMax = new Vector2(1, 1);
        infoRT.pivot = new Vector2(0.5f, 1); infoRT.anchoredPosition = new Vector2(0, -36);
        infoRT.sizeDelta = new Vector2(-20, 32);

        // Selected slots
        BuildSelectedSlots();

        // Eligible fly list
        BuildFlyList();

        // Breed button
        bool canBreed = _selectedFlyId1 != null && _selectedFlyId2 != null;
        var breedBtn = CreateButton(_panel.transform, canBreed ? "BREED!" : "Select 2 Flies", () =>
        {
            if (_selectedFlyId1 == null || _selectedFlyId2 == null) return;
            var offspring = ClanManager.Instance?.BreedFlies(_selectedFlyId1, _selectedFlyId2);
            if (offspring != null)
            {
                _selectedFlyId1 = null;
                _selectedFlyId2 = null;
                RebuildContent();
            }
        });
        var breedRT = breedBtn.GetComponent<RectTransform>();
        breedRT.anchorMin = new Vector2(0.2f, 0.01f); breedRT.anchorMax = new Vector2(0.55f, 0.07f);
        breedRT.offsetMin = Vector2.zero; breedRT.offsetMax = Vector2.zero;
        breedBtn.GetComponent<Image>().color = canBreed
            ? new Color(0.2f, 0.55f, 0.2f)
            : new Color(0.3f, 0.3f, 0.35f);

        // Close button
        var closeBtn = CreateButton(_panel.transform, "Close", () => Hide());
        var closeRT = closeBtn.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(0.58f, 0.01f); closeRT.anchorMax = new Vector2(0.8f, 0.07f);
        closeRT.offsetMin = Vector2.zero; closeRT.offsetMax = Vector2.zero;
    }

    void BuildSelectedSlots()
    {
        // Slot 1
        var slot1 = CreateSlotDisplay(0.05f, 0.82f, 0.48f, 0.92f, _selectedFlyId1, "Parent 1", () =>
        {
            _selectedFlyId1 = null;
            RebuildContent();
        });

        // Plus sign
        var plus = CreateLabel(_panel.transform, "+", 28, TextAlignmentOptions.Center);
        var plusRT = plus.GetComponent<RectTransform>();
        plusRT.anchorMin = new Vector2(0.46f, 0.82f); plusRT.anchorMax = new Vector2(0.54f, 0.92f);
        plusRT.offsetMin = Vector2.zero; plusRT.offsetMax = Vector2.zero;

        // Slot 2
        var slot2 = CreateSlotDisplay(0.52f, 0.82f, 0.95f, 0.92f, _selectedFlyId2, "Parent 2", () =>
        {
            _selectedFlyId2 = null;
            RebuildContent();
        });

        // Preview offspring stats
        if (_selectedFlyId1 != null && _selectedFlyId2 != null)
        {
            var fly1 = ClanManager.Instance?.Flies.FirstOrDefault(f => f.InstanceId == _selectedFlyId1);
            var fly2 = ClanManager.Instance?.Flies.FirstOrDefault(f => f.InstanceId == _selectedFlyId2);
            if (fly1 != null && fly2 != null)
            {
                int atk = Mathf.RoundToInt(Mathf.Max(fly1.Attack, fly2.Attack) * (1f + Constants.BREEDING_STAT_BONUS));
                int def = Mathf.RoundToInt(Mathf.Max(fly1.Defense, fly2.Defense) * (1f + Constants.BREEDING_STAT_BONUS));
                int spd = Mathf.RoundToInt(Mathf.Max(fly1.Speed, fly2.Speed) * (1f + Constants.BREEDING_STAT_BONUS));
                int hp = Mathf.RoundToInt(Mathf.Max(fly1.MaxHealth, fly2.MaxHealth) * (1f + Constants.BREEDING_STAT_BONUS));

                var preview = CreateLabel(_panel.transform,
                    $"<color=#4F4>Offspring:</color> ATK:{atk} DEF:{def} SPD:{spd} HP:{hp}",
                    13, TextAlignmentOptions.Center);
                preview.richText = true;
                var prevRT = preview.GetComponent<RectTransform>();
                prevRT.anchorMin = new Vector2(0.05f, 0.76f); prevRT.anchorMax = new Vector2(0.95f, 0.82f);
                prevRT.offsetMin = Vector2.zero; prevRT.offsetMax = Vector2.zero;
            }
        }
    }

    GameObject CreateSlotDisplay(float xMin, float yMin, float xMax, float yMax,
        string flyId, string placeholder, UnityEngine.Events.UnityAction onClear)
    {
        var slot = new GameObject("Slot");
        slot.transform.SetParent(_panel.transform, false);
        var slotRT = slot.AddComponent<RectTransform>();
        slotRT.anchorMin = new Vector2(xMin, yMin);
        slotRT.anchorMax = new Vector2(xMax, yMax);
        slotRT.offsetMin = Vector2.zero; slotRT.offsetMax = Vector2.zero;
        slot.AddComponent<Image>().color = flyId != null
            ? new Color(0.15f, 0.25f, 0.15f)
            : new Color(0.15f, 0.15f, 0.2f);

        if (flyId != null)
        {
            var fly = ClanManager.Instance?.Flies.FirstOrDefault(f => f.InstanceId == flyId);
            if (fly != null)
            {
                var label = CreateLabel(slot.transform,
                    $"{fly.Role} Lv{fly.Level}\nATK:{fly.Attack} DEF:{fly.Defense}",
                    12, TextAlignmentOptions.Center);
                var labelRT = label.GetComponent<RectTransform>();
                labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = new Vector2(0.8f, 1);
                labelRT.offsetMin = new Vector2(4, 0); labelRT.offsetMax = Vector2.zero;

                // Clear button
                var clearBtn = CreateButton(slot.transform, "X", onClear);
                var clearRT = clearBtn.GetComponent<RectTransform>();
                clearRT.anchorMin = new Vector2(0.82f, 0.2f); clearRT.anchorMax = new Vector2(0.98f, 0.8f);
                clearRT.offsetMin = Vector2.zero; clearRT.offsetMax = Vector2.zero;
                clearBtn.GetComponent<Image>().color = new Color(0.5f, 0.15f, 0.1f);
            }
        }
        else
        {
            var label = CreateLabel(slot.transform, placeholder, 14, TextAlignmentOptions.Center);
            label.color = new Color(0.5f, 0.5f, 0.6f);
            var labelRT = label.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero; labelRT.offsetMax = Vector2.zero;
        }

        return slot;
    }

    void BuildFlyList()
    {
        // Scroll area
        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(_panel.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.03f, 0.08f); scrollRT.anchorMax = new Vector2(0.97f, 0.75f);
        scrollRT.offsetMin = Vector2.zero; scrollRT.offsetMax = Vector2.zero;
        var scroll = scrollGO.AddComponent<ScrollRect>();
        scrollGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.1f);
        scrollGO.AddComponent<Mask>().showMaskGraphic = true;

        var content = new GameObject("Content");
        content.transform.SetParent(scrollGO.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 3; vlg.padding = new RectOffset(3, 3, 3, 3);
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRT;
        scroll.vertical = true; scroll.horizontal = false;

        // Get eligible flies (level 5+, alive, not already selected)
        var flies = ClanManager.Instance?.Flies;
        if (flies == null) return;

        // If parent 1 is selected, filter to same role
        FlyRole? requiredRole = null;
        if (_selectedFlyId1 != null)
        {
            var parent1 = flies.FirstOrDefault(f => f.InstanceId == _selectedFlyId1);
            requiredRole = parent1?.Role;
        }

        foreach (var fly in flies.Where(f => f.IsAlive && f.Level >= Constants.BREEDING_MIN_LEVEL))
        {
            if (fly.InstanceId == _selectedFlyId1 || fly.InstanceId == _selectedFlyId2) continue;
            if (requiredRole.HasValue && fly.Role != requiredRole.Value) continue;

            var row = new GameObject($"Fly_{fly.InstanceId[..8]}");
            row.transform.SetParent(content.transform, false);
            row.AddComponent<LayoutElement>().preferredHeight = 44;
            row.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f);

            var label = CreateLabel(row.transform,
                $"{fly.Role} Lv{fly.Level} | ATK:{fly.Attack} DEF:{fly.Defense} SPD:{fly.Speed} HP:{fly.MaxHealth}"
                + (fly.IsElite ? " <color=#FFD700>[E]</color>" : ""),
                12, TextAlignmentOptions.Left);
            label.richText = true;
            var labelRT = label.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0.02f, 0); labelRT.anchorMax = new Vector2(0.75f, 1);
            labelRT.offsetMin = Vector2.zero; labelRT.offsetMax = Vector2.zero;

            string capturedId = fly.InstanceId;
            var selectBtn = CreateButton(row.transform, "Select", () =>
            {
                if (_selectedFlyId1 == null)
                    _selectedFlyId1 = capturedId;
                else if (_selectedFlyId2 == null)
                    _selectedFlyId2 = capturedId;
                RebuildContent();
            });
            var selectRT = selectBtn.GetComponent<RectTransform>();
            selectRT.anchorMin = new Vector2(0.77f, 0.1f); selectRT.anchorMax = new Vector2(0.98f, 0.9f);
            selectRT.offsetMin = Vector2.zero; selectRT.offsetMax = Vector2.zero;
            selectBtn.GetComponent<Image>().color = new Color(0.2f, 0.4f, 0.6f);
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
        tmp.text = label; tmp.fontSize = 13; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var tmpRT = tmp.GetComponent<RectTransform>();
        tmpRT.anchorMin = Vector2.zero; tmpRT.anchorMax = Vector2.one;
        tmpRT.offsetMin = Vector2.zero; tmpRT.offsetMax = Vector2.zero;
        return go;
    }
}
