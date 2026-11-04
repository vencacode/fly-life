using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Dedicated screen showing all flies, their levels, gear, and role assignment.
/// Includes Equip and Train buttons per fly card.
/// Builds UI at runtime — no prefab dependencies.
/// </summary>
public class ClanScreenController : MonoBehaviour
{
    public static ClanScreenController Instance { get; private set; }

    Canvas _canvas;
    Transform _scrollContent;
    List<GameObject> _flyCards = new();

    void Awake()
    {
        Instance = this;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name != "ClanScreen") return;
        // Canvas is destroyed on scene change — rebuild it
        _canvas = null;
        _scrollContent = null;
        foreach (var card in _flyCards) if (card != null) Destroy(card);
        _flyCards.Clear();
        BuildUI();
        RefreshFlyList();
    }

    void BuildUI()
    {
        // Canvas
        var canvasGo = new GameObject("ClanCanvas");
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Background (must be first child so it renders behind everything)
        var bg = CreatePanel(canvasGo.transform, "Background", new Color(0.1f, 0.1f, 0.15f));
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bg.GetComponent<Image>().raycastTarget = false;

        // Safe area wrapper
        var safeArea = new GameObject("SafeArea");
        safeArea.transform.SetParent(canvasGo.transform, false);
        var safeRT = safeArea.AddComponent<RectTransform>();
        safeRT.anchorMin = Vector2.zero; safeRT.anchorMax = Vector2.one;
        safeRT.offsetMin = Vector2.zero; safeRT.offsetMax = Vector2.zero;
        safeArea.AddComponent<SafeAreaHandler>();

        // Title
        var title = CreateText(safeArea.transform, "Title", "YOUR CLAN", 28, TextAlignmentOptions.Center);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleRect.sizeDelta = new Vector2(0, 50);

        // Clan info
        string clanName = ClanManager.Instance?.ClanName ?? "The Fly Lifers";
        int clanLevel = ClanManager.Instance?.ClanLevel ?? 1;
        int totalFlies = ClanManager.Instance?.TotalFlies ?? 0;
        var info = CreateText(safeArea.transform, "ClanInfo",
            $"{clanName}  |  Level {clanLevel}  |  {totalFlies} flies", 18, TextAlignmentOptions.Center);
        var infoRect = info.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0, 1);
        infoRect.anchorMax = new Vector2(1, 1);
        infoRect.pivot = new Vector2(0.5f, 1);
        infoRect.anchoredPosition = new Vector2(0, -60);
        infoRect.sizeDelta = new Vector2(0, 30);

        // Scroll view
        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(safeArea.transform, false);
        var scrollRect = scrollGo.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.05f, 0.05f);
        scrollRect.anchorMax = new Vector2(0.95f, 0.85f);
        scrollRect.sizeDelta = Vector2.zero;
        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.vertical = true;
        scroll.horizontal = false;

        var viewport = CreatePanel(scrollGo.transform, "Viewport", Color.clear);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        var vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.sizeDelta = Vector2.zero;
        scroll.viewport = vpRect;

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        _scrollContent = content.transform;
        var contentRect = content.GetComponent<RectTransform>() ?? content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(5, 5, 5, 5);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRect;

        // Back button
        var backBtn = CreateButton(safeArea.transform, "BackButton", "< Back to Map", () =>
        {
            GameManager.Instance?.LoadScene("MainMap");
        });
        var backRect = backBtn.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0, 0);
        backRect.anchorMax = new Vector2(0.3f, 0.05f);
        backRect.sizeDelta = Vector2.zero;

        // Breed button
        var breedBtn = CreateButton(safeArea.transform, "BreedButton", "Breed Flies", () =>
        {
            BreedingUI.Instance?.Toggle();
        });
        var breedRect = breedBtn.GetComponent<RectTransform>();
        breedRect.anchorMin = new Vector2(0.35f, 0);
        breedRect.anchorMax = new Vector2(0.65f, 0.05f);
        breedRect.sizeDelta = Vector2.zero;
        breedBtn.GetComponent<Image>().color = new Color(0.45f, 0.25f, 0.5f);
    }

    public void RefreshFlyList()
    {
        foreach (var card in _flyCards) Destroy(card);
        _flyCards.Clear();

        var flies = ClanManager.Instance?.Flies;
        if (flies == null || flies.Count == 0)
        {
            Debug.Log($"[ClanScreen] No flies to display (ClanManager null: {ClanManager.Instance == null})");
            return;
        }

        Debug.Log($"[ClanScreen] Displaying {flies.Count} flies");
        foreach (var fly in flies)
        {
            try
            {
                var card = CreateFlyCard(fly);
                _flyCards.Add(card);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ClanScreen] Failed to create card for fly {fly?.InstanceId}: {e.Message}");
            }
        }
    }

    GameObject CreateFlyCard(FlyInstance fly)
    {
        var card = CreatePanel(_scrollContent, $"Fly_{fly.InstanceId}", new Color(0.2f, 0.2f, 0.25f));
        var cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(0, 100);
        var le = card.AddComponent<LayoutElement>();
        le.preferredHeight = 100;
        le.minHeight = 100;

        // ── Row 1: Name + Stats + Gear ──

        // Name + Level + Elite badge
        string nameStr = fly.IsElite ? $"★ {fly.Role} Lv{fly.Level} ELITE" : $"{fly.Role} Lv{fly.Level}";
        var nameLabel = CreateText(card.transform, "Name", nameStr, 18, TextAlignmentOptions.Left);
        if (fly.IsElite) nameLabel.color = new Color(1f, 0.84f, 0f); // gold for elite
        var nameRect = nameLabel.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.02f, 0.55f);
        nameRect.anchorMax = new Vector2(0.3f, 1f);
        nameRect.sizeDelta = Vector2.zero;

        // HP + XP
        var hpLabel = CreateText(card.transform, "HP",
            $"HP:{fly.CurrentHealth}/{fly.MaxHealth}  XP:{fly.XP}", 12, TextAlignmentOptions.Left);
        var hpRect = hpLabel.GetComponent<RectTransform>();
        hpRect.anchorMin = new Vector2(0.02f, 0.3f);
        hpRect.anchorMax = new Vector2(0.3f, 0.55f);
        hpRect.sizeDelta = Vector2.zero;

        // Stats
        var statsLabel = CreateText(card.transform, "Stats",
            $"ATK:{fly.Attack} DEF:{fly.Defense} SPD:{fly.Speed}", 13, TextAlignmentOptions.Center);
        var statsRect = statsLabel.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.31f, 0.55f);
        statsRect.anchorMax = new Vector2(0.6f, 1f);
        statsRect.sizeDelta = Vector2.zero;

        // Gear summary + Skin
        string gear = GetGearText(fly);
        string skinName = SkinRegistry.Get(fly.SkinId)?.DisplayName ?? "Classic Fly";
        var gearLabel = CreateText(card.transform, "Gear", $"{gear} | {skinName}", 12, TextAlignmentOptions.Center);
        gearLabel.color = gear == "No gear" ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.9f, 0.8f, 0.4f);
        var gearRect = gearLabel.GetComponent<RectTransform>();
        gearRect.anchorMin = new Vector2(0.31f, 0.3f);
        gearRect.anchorMax = new Vector2(0.6f, 0.55f);
        gearRect.sizeDelta = Vector2.zero;

        // ── Row 2: Action buttons ──

        // Equip button
        var flyRef = fly;
        var equipBtn = CreateButton(card.transform, "EquipBtn", "Equip", () =>
        {
            GearEquipPopup.Instance?.Show(flyRef);
        });
        var equipRect = equipBtn.GetComponent<RectTransform>();
        equipRect.anchorMin = new Vector2(0.02f, 0.02f);
        equipRect.anchorMax = new Vector2(0.18f, 0.28f);
        equipRect.sizeDelta = Vector2.zero;
        equipBtn.GetComponent<Image>().color = new Color(0.3f, 0.4f, 0.5f);

        // Train button
        var trainBtn = CreateButton(card.transform, "TrainBtn", $"Train ({Constants.TRAINING_COST})", () =>
        {
            if (ClanManager.Instance?.TrainFly(flyRef.InstanceId) == true)
                RefreshFlyList();
            else
                ToastManager.Instance?.ShowToast("Not enough PooCoins!", ToastType.Warning);
        });
        var trainRect = trainBtn.GetComponent<RectTransform>();
        trainRect.anchorMin = new Vector2(0.19f, 0.02f);
        trainRect.anchorMax = new Vector2(0.36f, 0.28f);
        trainRect.sizeDelta = Vector2.zero;
        trainBtn.GetComponent<Image>().color = new Color(0.35f, 0.45f, 0.2f);

        // Skin button
        var skinBtn = CreateButton(card.transform, "SkinBtn", "Skin", () =>
        {
            SkinSelectPopup.Instance?.Show(flyRef.InstanceId);
        });
        var skinRect = skinBtn.GetComponent<RectTransform>();
        skinRect.anchorMin = new Vector2(0.37f, 0.02f);
        skinRect.anchorMax = new Vector2(0.52f, 0.28f);
        skinRect.sizeDelta = Vector2.zero;
        skinBtn.GetComponent<Image>().color = new Color(0.45f, 0.3f, 0.5f);

        // Promote button (only for level 10+ non-elite flies)
        if (!fly.IsElite && fly.Level >= Constants.PROMOTION_LEVEL_REQ)
        {
            var promoteBtn = CreateButton(card.transform, "PromoteBtn", $"Promote ({Constants.PROMOTION_COST})", () =>
            {
                if (ClanManager.Instance?.PromoteFly(flyRef.InstanceId) == true)
                    RefreshFlyList();
                else
                    ToastManager.Instance?.ShowToast("Not enough PooCoins!", ToastType.Warning);
            });
            var promoteRect = promoteBtn.GetComponent<RectTransform>();
            promoteRect.anchorMin = new Vector2(0.53f, 0.02f);
            promoteRect.anchorMax = new Vector2(0.75f, 0.28f);
            promoteRect.sizeDelta = Vector2.zero;
            promoteBtn.GetComponent<Image>().color = new Color(0.6f, 0.5f, 0.1f);
        }

        // Respec button
        var respecBtn = CreateButton(card.transform, "RespecBtn",
            $"Respec ({Constants.RESPEC_COST})", () =>
        {
            if (ClanManager.Instance?.RespecFly(flyRef.InstanceId) == true)
                RefreshFlyList();
        });
        var respecRect = respecBtn.GetComponent<RectTransform>();
        respecRect.anchorMin = new Vector2(0.77f, 0.02f);
        respecRect.anchorMax = new Vector2(0.98f, 0.28f);
        respecRect.sizeDelta = Vector2.zero;
        respecBtn.GetComponent<Image>().color = new Color(0.35f, 0.3f, 0.5f);

        // Reassign role buttons
        float btnX = 0.62f;
        foreach (FlyRole role in new[] { FlyRole.Scout, FlyRole.Soldier, FlyRole.Worker })
        {
            if (role == fly.Role) continue;
            string label = role.ToString()[0].ToString();
            var roleRef = role;
            var btn = CreateButton(card.transform, $"Btn_{role}", label, () =>
            {
                ClanManager.Instance?.ReassignFly(flyRef.InstanceId, roleRef);
                RefreshFlyList();
            });
            var btnRect = btn.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(btnX, 0.55f);
            btnRect.anchorMax = new Vector2(btnX + 0.12f, 0.95f);
            btnRect.sizeDelta = Vector2.zero;
            btnX += 0.13f;
        }

        return card;
    }

    string GetGearText(FlyInstance fly)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(fly.EquippedWeaponId))  parts.Add("W");
        if (!string.IsNullOrEmpty(fly.EquippedHeadId))    parts.Add("H");
        if (!string.IsNullOrEmpty(fly.EquippedBodyId))    parts.Add("B");
        if (!string.IsNullOrEmpty(fly.EquippedFeetId))    parts.Add("F");
        if (!string.IsNullOrEmpty(fly.EquippedWingsId))   parts.Add("Wi");
        if (!string.IsNullOrEmpty(fly.EquippedOffhandId))   parts.Add("O");
        if (!string.IsNullOrEmpty(fly.EquippedAccessoryId)) parts.Add("Acc");
        return parts.Count > 0 ? string.Join(" ", parts) : "No gear";
    }

    // ---- UI Helpers ----

    static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string text, int size, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = Color.white;
        tmp.alignment = align;
        return tmp;
    }

    static GameObject CreateButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.5f, 0.3f);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
        var txt = CreateText(go.transform, "Label", label, 14, TextAlignmentOptions.Center);
        var txtRect = txt.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
        return go;
    }
}
