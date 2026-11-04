using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// In-game toast popups for events: district lost, gear drop, level up, daily bonus, etc.
/// Slides in from top, auto-dismiss after 3s. Builds its own UI at runtime.
/// </summary>
public class ToastManager : MonoBehaviour
{
    public static ToastManager Instance { get; private set; }

    [Header("Config")]
    public int MaxVisible = 3;
    public float DisplayDuration = 3f;
    public float FadeDuration = 0.3f;
    public float SlideSpeed = 600f;

    // Runtime UI
    Canvas _canvas;
    RectTransform _container;
    readonly Queue<GameObject> _active = new();

    // Cached delegates for clean unsubscribe
    Action<object> _onDistrictLost;
    Action<object> _onItemDropped;
    Action<object> _onLevelUp;
    Action<object> _onDailyLogin;
    Action<object> _onDistrictCaptured;
    Action<object> _onBoosterActivated;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    void OnEnable()
    {
        _onDistrictLost = d =>
        {
            var district = d as District;
            string name = district?.Name ?? "unknown";
            ShowToast($"District lost: {name}!", ToastType.Warning);
        };

        _onItemDropped = d =>
        {
            var item = d as EquipmentData;
            string name = item?.ItemName ?? "an item";
            ShowToast($"Gear drop: {name}!", ToastType.Reward);
        };

        _onLevelUp = d =>
        {
            var fly = d as FlyInstance;
            if (fly != null)
                ShowToast($"{fly.Role} levelled up to {fly.Level}!", ToastType.Success);
        };

        _onDailyLogin = _ => ShowToast("+50 PooCoins, +5 Gems! Daily bonus!", ToastType.Success);

        _onDistrictCaptured = d =>
        {
            var district = d as District;
            string name = district?.Name ?? "a district";
            ShowToast($"District captured: {name}!", ToastType.Success);
        };

        _onBoosterActivated = _ => ShowToast("Booster activated!", ToastType.Info);

        UIEventBus.On(UIEvent.DistrictLost,       _onDistrictLost);
        UIEventBus.On(UIEvent.BattleItemDropped,   _onItemDropped);
        UIEventBus.On(UIEvent.LevelUp,             _onLevelUp);
        UIEventBus.On(UIEvent.DailyLoginBonus,     _onDailyLogin);
        UIEventBus.On(UIEvent.DistrictCaptured,    _onDistrictCaptured);
        UIEventBus.On(UIEvent.BoosterActivated,    _onBoosterActivated);
    }

    void OnDisable()
    {
        UIEventBus.Off(UIEvent.DistrictLost,       _onDistrictLost);
        UIEventBus.Off(UIEvent.BattleItemDropped,   _onItemDropped);
        UIEventBus.Off(UIEvent.LevelUp,             _onLevelUp);
        UIEventBus.Off(UIEvent.DailyLoginBonus,     _onDailyLogin);
        UIEventBus.Off(UIEvent.DistrictCaptured,    _onDistrictCaptured);
        UIEventBus.Off(UIEvent.BoosterActivated,    _onBoosterActivated);
    }

    void BuildUI()
    {
        // Canvas (overlay, high sort order but below tutorial)
        var canvasGo = new GameObject("ToastCanvas");
        canvasGo.transform.SetParent(transform);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 900;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Container — top-anchored vertical layout, toasts stack downward
        var containerGo = new GameObject("ToastContainer");
        containerGo.transform.SetParent(canvasGo.transform, false);
        _container = containerGo.AddComponent<RectTransform>();
        _container.anchorMin = new Vector2(0.05f, 0.82f);
        _container.anchorMax = new Vector2(0.95f, 0.98f);
        _container.offsetMin = Vector2.zero;
        _container.offsetMax = Vector2.zero;

        var vlg = containerGo.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 8f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // ContentSizeFitter so container grows with children
        var csf = containerGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    /// <summary>Show a toast message with optional type for color coding.</summary>
    public void ShowToast(string message, ToastType type = ToastType.Info)
    {
        if (_container == null) return;

        var go = CreateToastItem(message, type);
        _active.Enqueue(go);

        // Remove excess
        while (_active.Count > MaxVisible)
        {
            var old = _active.Dequeue();
            if (old) Destroy(old);
        }

        StartCoroutine(SlideInAndDismiss(go));
    }

    GameObject CreateToastItem(string message, ToastType type)
    {
        // Root toast object
        var go = new GameObject("Toast");
        go.transform.SetParent(_container, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 70);

        var cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f; // starts invisible, slides in

        var layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = 70;

        // Background image
        var bg = go.AddComponent<Image>();
        bg.color = GetBgColor(type);
        bg.raycastTarget = false;

        // Horizontal layout for icon + text
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(16, 16, 8, 8);
        hlg.spacing = 10f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Icon/prefix color bar
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(go.transform, false);
        var iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(6, 0);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.color = GetAccentColor(type);
        iconImg.raycastTarget = false;
        var iconLE = iconGo.AddComponent<LayoutElement>();
        iconLE.preferredWidth = 6;

        // Text label
        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = message;
        tmp.fontSize = 20;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        var textLE = textGo.AddComponent<LayoutElement>();
        textLE.flexibleWidth = 1;

        return go;
    }

    IEnumerator SlideInAndDismiss(GameObject go)
    {
        if (go == null) yield break;
        var cg = go.GetComponent<CanvasGroup>();

        // Fade in
        float fadeIn = 0.15f;
        float elapsed = 0f;
        while (elapsed < fadeIn && go != null)
        {
            elapsed += Time.unscaledDeltaTime;
            if (cg) cg.alpha = Mathf.Clamp01(elapsed / fadeIn);
            yield return null;
        }
        if (cg) cg.alpha = 1f;

        // Hold
        yield return new WaitForSecondsRealtime(DisplayDuration);

        // Fade out
        if (go == null) yield break;
        elapsed = FadeDuration;
        while (elapsed > 0 && go != null)
        {
            elapsed -= Time.unscaledDeltaTime;
            if (cg) cg.alpha = Mathf.Clamp01(elapsed / FadeDuration);
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    Color GetBgColor(ToastType type) => type switch
    {
        ToastType.Warning => new Color(0.45f, 0.15f, 0.05f, 0.92f),
        ToastType.Success => new Color(0.08f, 0.35f, 0.12f, 0.92f),
        ToastType.Reward  => new Color(0.40f, 0.30f, 0.05f, 0.92f),
        _                 => new Color(0.12f, 0.12f, 0.18f, 0.92f),
    };

    Color GetAccentColor(ToastType type) => type switch
    {
        ToastType.Warning => new Color(1f, 0.4f, 0.2f),
        ToastType.Success => new Color(0.3f, 1f, 0.4f),
        ToastType.Reward  => new Color(1f, 0.85f, 0.2f),
        _                 => new Color(0.5f, 0.7f, 1f),
    };
}

public enum ToastType
{
    Info,
    Warning,
    Success,
    Reward
}
