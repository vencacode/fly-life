using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows a "Rate this app" prompt after key milestones.
/// Uses Unity's Application.RequestStoreReview() via reflection.
/// Only shows once per milestone, max 3 prompts total.
/// </summary>
public class RateAppManager : MonoBehaviour
{
    public static RateAppManager Instance { get; private set; }

    HashSet<string> _shownPrompts = new();
    bool _hasRated;

    Canvas _canvas;
    GameObject _popup;

    // Cached delegates
    Action<object> _onBattleEnded;
    Action<object> _onLevelUp;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        BuildPopupUI();

        _onBattleEnded = data => OnBattleEnded(data);
        _onLevelUp = _ => OnLevelUp();

        UIEventBus.On(UIEvent.BattleEnded, _onBattleEnded);
        UIEventBus.On(UIEvent.LevelUp, _onLevelUp);
    }

    void OnDisable()
    {
        if (_onBattleEnded != null) UIEventBus.Off(UIEvent.BattleEnded, _onBattleEnded);
        if (_onLevelUp != null)     UIEventBus.Off(UIEvent.LevelUp, _onLevelUp);
    }

    void BuildPopupUI()
    {
        _canvas = new GameObject("RateAppCanvas").AddComponent<Canvas>();
        _canvas.transform.SetParent(transform);
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 950;
        _canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvas.gameObject.AddComponent<GraphicRaycaster>();

        _popup = new GameObject("RatePopup");
        _popup.transform.SetParent(_canvas.transform, false);
        var rt = _popup.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.12f, 0.35f); rt.anchorMax = new Vector2(0.88f, 0.65f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        _popup.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.98f);

        // Title
        var title = CreateLabel(_popup.transform, "Enjoying Fly Life?", 22, TextAlignmentOptions.Center);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.7f); titleRT.anchorMax = new Vector2(1, 0.95f);
        titleRT.offsetMin = Vector2.zero; titleRT.offsetMax = Vector2.zero;

        // Subtitle
        var sub = CreateLabel(_popup.transform, "Rate us on the store!", 15, TextAlignmentOptions.Center);
        sub.color = new Color(0.7f, 0.7f, 0.8f);
        var subRT = sub.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0, 0.52f); subRT.anchorMax = new Vector2(1, 0.7f);
        subRT.offsetMin = Vector2.zero; subRT.offsetMax = Vector2.zero;

        // Yes button
        var yesBtn = CreateButton(_popup.transform, "Rate Now!", () =>
        {
            _hasRated = true;
            RequestStoreReview();
            _popup.SetActive(false);
        });
        var yesRT = yesBtn.GetComponent<RectTransform>();
        yesRT.anchorMin = new Vector2(0.05f, 0.1f); yesRT.anchorMax = new Vector2(0.35f, 0.4f);
        yesRT.offsetMin = Vector2.zero; yesRT.offsetMax = Vector2.zero;
        yesBtn.GetComponent<Image>().color = new Color(0.2f, 0.55f, 0.2f);

        // Later button
        var laterBtn = CreateButton(_popup.transform, "Later", () =>
        {
            _popup.SetActive(false);
        });
        var laterRT = laterBtn.GetComponent<RectTransform>();
        laterRT.anchorMin = new Vector2(0.37f, 0.1f); laterRT.anchorMax = new Vector2(0.63f, 0.4f);
        laterRT.offsetMin = Vector2.zero; laterRT.offsetMax = Vector2.zero;

        // No thanks button
        var noBtn = CreateButton(_popup.transform, "No Thanks", () =>
        {
            _hasRated = true;
            _popup.SetActive(false);
        });
        var noRT = noBtn.GetComponent<RectTransform>();
        noRT.anchorMin = new Vector2(0.65f, 0.1f); noRT.anchorMax = new Vector2(0.95f, 0.4f);
        noRT.offsetMin = Vector2.zero; noRT.offsetMax = Vector2.zero;
        noBtn.GetComponent<Image>().color = new Color(0.4f, 0.2f, 0.15f);

        _popup.SetActive(false);
    }

    void OnBattleEnded(object data)
    {
        if (_hasRated) return;
        bool won = data is bool b && b;
        if (!won) return;

        int wins = AchievementManager.Instance?.GetCounter("battles_won") ?? 0;
        if (wins >= 5) TryShow("5_wins");

        // Check if boss kill
        var district = TerritoryManager.Instance?.GetDistrict(BattleManager.Instance?.PendingDistrictId);
        if (district != null && district.IsBoss)
            TryShow("first_boss");
    }

    void OnLevelUp()
    {
        if (_hasRated) return;
        int level = ClanManager.Instance?.ClanLevel ?? 0;
        if (level >= 5) TryShow("clan_level_5");
    }

    void TryShow(string milestoneId)
    {
        if (_hasRated || _shownPrompts.Contains(milestoneId)) return;
        _shownPrompts.Add(milestoneId);
        _popup.SetActive(true);
        Debug.Log($"[RateApp] Showing prompt for milestone: {milestoneId}");
    }

    void RequestStoreReview()
    {
        Debug.Log("[RateApp] Requesting store review...");
#if UNITY_IOS
        try
        {
            var deviceType = Type.GetType("UnityEngine.iOS.Device, UnityEngine.CoreModule");
            deviceType?.GetMethod("RequestStoreReview")?.Invoke(null, null);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RateApp] iOS review failed: {e.Message}");
            Application.OpenURL("https://apps.apple.com");
        }
#elif UNITY_ANDROID
        try
        {
            var reviewType = Type.GetType("Google.Play.Review.ReviewManager, Google.Play.Review");
            if (reviewType != null)
            {
                Debug.Log("[RateApp] Google Play In-App Review API detected.");
            }
            else
            {
                Application.OpenURL("market://details?id=" + Application.identifier);
            }
        }
        catch
        {
            Application.OpenURL("market://details?id=" + Application.identifier);
        }
#else
        Debug.Log("[RateApp] Store review not available on this platform.");
#endif
    }

    // ---- Save / Load ----

    [Serializable]
    public class SaveData
    {
        public List<string> ShownPrompts = new();
        public bool HasRated;
    }

    public SaveData GetSaveData() => new()
    {
        ShownPrompts = new List<string>(_shownPrompts),
        HasRated = _hasRated
    };

    public void LoadSaveData(SaveData data)
    {
        if (data == null) return;
        _shownPrompts = new HashSet<string>(data.ShownPrompts ?? new());
        _hasRated = data.HasRated;
    }

    // ---- UI Helpers ----

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
        tmp.text = label; tmp.fontSize = 14; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var tmpRT = tmp.GetComponent<RectTransform>();
        tmpRT.anchorMin = Vector2.zero; tmpRT.anchorMax = Vector2.one;
        tmpRT.offsetMin = Vector2.zero; tmpRT.offsetMax = Vector2.zero;
        return go;
    }
}
