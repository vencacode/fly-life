using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Checks for active seasonal events on startup.
/// Displays a banner and applies global multipliers during events.
/// Awards the event skin on first login during an event.
/// </summary>
public class SeasonalEventManager : MonoBehaviour
{
    public static SeasonalEventManager Instance { get; private set; }

    Canvas _canvas;
    GameObject _banner;
    TextMeshProUGUI _bannerText;

    public SeasonalEventRegistry.EventDef? ActiveEvent { get; private set; }
    public bool HasActiveEvent => ActiveEvent.HasValue;

    // Multipliers other systems can query
    public float PooRateMultiplier  => ActiveEvent?.PooRateMultiplier  ?? 1f;
    public float XPMultiplier       => ActiveEvent?.XPMultiplier       ?? 1f;
    public float CoinMultiplier     => ActiveEvent?.CoinMultiplier     ?? 1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        BuildBanner();
        CheckForEvent();
    }

    void CheckForEvent()
    {
        ActiveEvent = SeasonalEventRegistry.GetActiveEvent();
        if (ActiveEvent.HasValue)
        {
            var e = ActiveEvent.Value;
            Debug.Log($"[SeasonalEventManager] Active event: {e.DisplayName}");
            ShowBanner(e);
            UIEventBus.Emit(UIEvent.SeasonalEventStarted, e);

            // Award skin if not already unlocked
            if (SkinSelectPopup.Instance != null && !SkinSelectPopup.Instance.IsSkinUnlocked(e.RewardSkinId))
            {
                SkinSelectPopup.Instance.UnlockSkin(e.RewardSkinId);
                ToastManager.Instance?.ShowToast(
                    $"Event reward: {SkinRegistry.Get(e.RewardSkinId)?.DisplayName} skin unlocked!",
                    ToastType.Reward);
            }
        }
        else
        {
            _banner.SetActive(false);
        }
    }

    void BuildBanner()
    {
        _canvas = new GameObject("EventBannerCanvas").AddComponent<Canvas>();
        _canvas.transform.SetParent(transform);
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 750;
        _canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvas.gameObject.AddComponent<GraphicRaycaster>();

        _banner = new GameObject("Banner");
        _banner.transform.SetParent(_canvas.transform, false);
        var rt = _banner.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1); rt.anchoredPosition = new Vector2(0, -45);
        rt.sizeDelta = new Vector2(0, 35);
        _banner.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.1f, 0.9f);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(_banner.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(10, 0); textRT.offsetMax = new Vector2(-10, 0);
        _bannerText = textGO.AddComponent<TextMeshProUGUI>();
        _bannerText.fontSize = 16; _bannerText.alignment = TextAlignmentOptions.Center;
        _bannerText.color = Color.white;

        _banner.SetActive(false);
    }

    void ShowBanner(SeasonalEventRegistry.EventDef e)
    {
        if (ColorUtility.TryParseHtmlString(e.BannerColorHex, out var color))
            _banner.GetComponent<Image>().color = new Color(color.r, color.g, color.b, 0.9f);
        _bannerText.text = $"{e.DisplayName} - {e.Description}";
        _banner.SetActive(true);
    }
}
