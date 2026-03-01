using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Settings overlay — accessible from a gear button in the top-right corner.
/// Controls: SFX slider, Music slider, Reset Progress (with confirmation), Credits, Back.
/// Builds its own UI at runtime.
/// </summary>
public class SettingsScreenController : MonoBehaviour
{
    public static SettingsScreenController Instance { get; private set; }

    // Runtime UI
    Canvas _canvas;
    GameObject _panel;
    GameObject _confirmPanel;
    Slider _sfxSlider;
    Slider _musicSlider;
    Button _gearButton; // persistent gear icon in top-right
    bool _visible;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    void BuildUI()
    {
        // ── Canvas ──
        var canvasGo = new GameObject("SettingsCanvas");
        canvasGo.transform.SetParent(transform);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 850; // above HUD (100), below tutorial (999)
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Gear button (always visible, top-right) ──
        _gearButton = CreateButton(canvasGo.transform, "GearButton", "\u2699",
            new Vector2(0.9f, 0.94f), new Vector2(0.98f, 0.98f),
            new Color(0.2f, 0.2f, 0.25f, 0.85f), 36);
        _gearButton.onClick.AddListener(Toggle);

        // ── Main settings panel (hidden by default) ──
        _panel = new GameObject("SettingsPanel");
        _panel.transform.SetParent(canvasGo.transform, false);
        var panelRect = _panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Dim background
        var dimBg = _panel.AddComponent<Image>();
        dimBg.color = new Color(0, 0, 0, 0.7f);
        dimBg.raycastTarget = true;

        // Center card
        var card = CreatePanel(_panel.transform, "Card",
            new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.8f),
            new Color(0.12f, 0.1f, 0.08f, 0.97f));

        // ── Title ──
        CreateLabel(card.transform, "Title", "SETTINGS",
            new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f), 32, Color.white, TextAlignmentOptions.Center);

        // ── Music Volume ──
        CreateLabel(card.transform, "MusicLabel", "Music Volume",
            new Vector2(0.08f, 0.74f), new Vector2(0.92f, 0.82f), 22, Color.white, TextAlignmentOptions.MidlineLeft);
        _musicSlider = CreateSlider(card.transform, "MusicSlider",
            new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.74f));
        _musicSlider.value = AudioManager.Instance?.MusicVolume ?? 0.6f;
        _musicSlider.onValueChanged.AddListener(v =>
        {
            AudioManager.Instance?.SetMusicVolume(v);
            PlayerPrefs.SetFloat("MusicVolume", v);
        });

        // ── SFX Volume ──
        CreateLabel(card.transform, "SFXLabel", "SFX Volume",
            new Vector2(0.08f, 0.54f), new Vector2(0.92f, 0.62f), 22, Color.white, TextAlignmentOptions.MidlineLeft);
        _sfxSlider = CreateSlider(card.transform, "SFXSlider",
            new Vector2(0.08f, 0.46f), new Vector2(0.92f, 0.54f));
        _sfxSlider.value = AudioManager.Instance?.SfxVolume ?? 0.8f;
        _sfxSlider.onValueChanged.AddListener(v =>
        {
            AudioManager.Instance?.SetSfxVolume(v);
            PlayerPrefs.SetFloat("SFXVolume", v);
        });

        // ── Reset Progress ──
        var resetBtn = CreateButton(card.transform, "ResetButton", "Reset Progress",
            new Vector2(0.15f, 0.3f), new Vector2(0.85f, 0.4f),
            new Color(0.5f, 0.12f, 0.1f, 0.95f), 22);
        resetBtn.onClick.AddListener(ShowConfirm);

        // ── Credits ──
        CreateLabel(card.transform, "Credits", "Fly Life v1.0\nMade with Unity",
            new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.25f), 16,
            new Color(0.6f, 0.6f, 0.6f), TextAlignmentOptions.Center);

        // ── Back button ──
        var backBtn = CreateButton(card.transform, "BackButton", "Back",
            new Vector2(0.3f, 0.03f), new Vector2(0.7f, 0.11f),
            new Color(0.2f, 0.25f, 0.35f, 0.95f), 24);
        backBtn.onClick.AddListener(Hide);

        // ── Confirm Reset Panel (hidden by default) ──
        BuildConfirmPanel(card.transform);

        _panel.SetActive(false);
    }

    void BuildConfirmPanel(Transform parent)
    {
        _confirmPanel = new GameObject("ConfirmPanel");
        _confirmPanel.transform.SetParent(parent, false);
        var rect = _confirmPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.05f, 0.3f);
        rect.anchorMax = new Vector2(0.95f, 0.7f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = _confirmPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.06f, 0.04f, 0.98f);
        bg.raycastTarget = true;

        CreateLabel(_confirmPanel.transform, "ConfirmText",
            "Delete ALL progress?\nThis cannot be undone!",
            new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.9f), 22,
            new Color(1f, 0.6f, 0.4f), TextAlignmentOptions.Center);

        var yesBtn = CreateButton(_confirmPanel.transform, "YesButton", "Yes, Delete",
            new Vector2(0.08f, 0.1f), new Vector2(0.45f, 0.4f),
            new Color(0.6f, 0.1f, 0.05f, 0.95f), 20);
        yesBtn.onClick.AddListener(OnResetConfirmed);

        var noBtn = CreateButton(_confirmPanel.transform, "NoButton", "Cancel",
            new Vector2(0.55f, 0.1f), new Vector2(0.92f, 0.4f),
            new Color(0.2f, 0.25f, 0.35f, 0.95f), 20);
        noBtn.onClick.AddListener(() => _confirmPanel.SetActive(false));

        _confirmPanel.SetActive(false);
    }

    // ── Public API ──

    public void Toggle()
    {
        if (_visible) Hide();
        else Show();
    }

    public void Show()
    {
        _visible = true;
        _panel.SetActive(true);

        // Refresh slider values in case volumes changed externally
        if (AudioManager.Instance != null)
        {
            _musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
            _sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
        }
    }

    public void Hide()
    {
        _visible = false;
        _panel.SetActive(false);
        _confirmPanel.SetActive(false);
        PlayerPrefs.Save();
    }

    void ShowConfirm() => _confirmPanel.SetActive(true);

    void OnResetConfirmed()
    {
        SaveSystem.DeleteSave();
        PlayerPrefs.DeleteAll();
        Debug.Log("[Settings] Progress reset. Quitting app...");
        ToastManager.Instance?.ShowToast("Progress reset! Restart the app.", ToastType.Warning);

        // Quit — next launch starts from scratch
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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

        // Button label
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

    static Slider CreateSlider(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Background track
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(go.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.3f);
        bgRect.anchorMax = new Vector2(1, 0.7f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.25f, 0.25f, 0.3f);

        // Fill area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.3f);
        fillAreaRect.anchorMax = new Vector2(1, 0.7f);
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.4f, 0.7f, 1f);

        // Handle slide area
        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(go.transform, false);
        var handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, 0);
        handleAreaRect.offsetMax = new Vector2(-10, 0);

        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(30, 30);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        // Wire up Slider component
        var slider = go.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        return slider;
    }
}
