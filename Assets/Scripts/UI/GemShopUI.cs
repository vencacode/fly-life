using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime-built Gem shop overlay. Shows IAP packs with prices.
/// Accessed via a gem icon button in the HUD or Settings.
/// </summary>
public class GemShopUI : MonoBehaviour
{
    public static GemShopUI Instance { get; private set; }

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
        _canvas = new GameObject("GemShopCanvas").AddComponent<Canvas>();
        _canvas.transform.SetParent(transform);
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 860;
        _canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvas.gameObject.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("ShopPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        var rt = _panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(30, 100); rt.offsetMax = new Vector2(-30, -60);
        _panel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.97f);

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
        var title = CreateLabel(_panel.transform, "Gem Shop", 28, TextAlignmentOptions.Center);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1); titleRT.anchoredPosition = new Vector2(0, -10);
        titleRT.sizeDelta = new Vector2(0, 45);

        // Current balance
        int gems = ResourceManager.Instance?.Gems ?? 0;
        var balance = CreateLabel(_panel.transform, $"Balance: {gems} Gems", 18, TextAlignmentOptions.Center);
        balance.color = new Color(0.6f, 0.85f, 1f);
        var balRT = balance.GetComponent<RectTransform>();
        balRT.anchorMin = new Vector2(0, 1); balRT.anchorMax = new Vector2(1, 1);
        balRT.pivot = new Vector2(0.5f, 1); balRT.anchoredPosition = new Vector2(0, -55);
        balRT.sizeDelta = new Vector2(0, 30);

        // Product cards
        float yOffset = -100;
        string[] productIds = {
            Constants.IAP_GEMS_100, Constants.IAP_GEMS_500,
            Constants.IAP_GEMS_1200, Constants.IAP_GEMS_3000
        };
        string[] bonusLabels = { "", "", "+20% bonus!", "+50% bonus!" };

        for (int i = 0; i < productIds.Length; i++)
        {
            string pid = productIds[i];
            int gemAmt = IAPManager.Instance?.GetGemAmount(pid) ?? 0;
            string price = IAPManager.Instance?.GetPrice(pid) ?? "???";
            CreateProductCard(_panel.transform, pid, gemAmt, price, bonusLabels[i], yOffset);
            yOffset -= 80;
        }

        // Close button
        var closeBtn = CreateButton(_panel.transform, "Close", () => Hide());
        var closeBtnRT = closeBtn.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(0.3f, 0); closeBtnRT.anchorMax = new Vector2(0.7f, 0.08f);
        closeBtnRT.offsetMin = Vector2.zero; closeBtnRT.offsetMax = Vector2.zero;
    }

    void CreateProductCard(Transform parent, string productId, int gems, string price, string bonus, float yPos)
    {
        var card = new GameObject("Product_" + gems);
        card.transform.SetParent(parent, false);
        var cardRT = card.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.05f, 1); cardRT.anchorMax = new Vector2(0.95f, 1);
        cardRT.pivot = new Vector2(0.5f, 1); cardRT.anchoredPosition = new Vector2(0, yPos);
        cardRT.sizeDelta = new Vector2(0, 70);
        card.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f);

        // Gem amount
        string label = $"{gems} Gems";
        if (!string.IsNullOrEmpty(bonus)) label += $"  <color=#FFD700><size=14>{bonus}</size></color>";
        var gemLabel = CreateLabel(card.transform, label, 20, TextAlignmentOptions.Left);
        gemLabel.richText = true;
        var gemRT = gemLabel.GetComponent<RectTransform>();
        gemRT.anchorMin = new Vector2(0, 0); gemRT.anchorMax = new Vector2(0.65f, 1);
        gemRT.offsetMin = new Vector2(15, 0); gemRT.offsetMax = Vector2.zero;

        // Buy button
        string pid = productId;
        var btn = CreateButton(card.transform, price, () => IAPManager.Instance?.PurchaseProduct(pid));
        var btnRT = btn.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.68f, 0.15f); btnRT.anchorMax = new Vector2(0.95f, 0.85f);
        btnRT.offsetMin = Vector2.zero; btnRT.offsetMax = Vector2.zero;
        btn.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f);
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
        tmp.text = label; tmp.fontSize = 16; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var tmpRT = tmp.GetComponent<RectTransform>();
        tmpRT.anchorMin = Vector2.zero; tmpRT.anchorMax = Vector2.one;
        tmpRT.offsetMin = Vector2.zero; tmpRT.offsetMax = Vector2.zero;
        return go;
    }
}
