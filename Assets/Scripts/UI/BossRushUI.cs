using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime-built Boss Rush entry UI and progress display.
/// </summary>
public class BossRushUI : MonoBehaviour
{
    public static BossRushUI Instance { get; private set; }

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
        _canvas = new GameObject("BossRushCanvas").AddComponent<Canvas>();
        _canvas.transform.SetParent(transform);
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 890;
        _canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvas.gameObject.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("BossRushPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        var rt = _panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.08f, 0.25f); rt.anchorMax = new Vector2(0.92f, 0.75f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        _panel.AddComponent<Image>().color = new Color(0.1f, 0.06f, 0.06f, 0.97f);

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

        bool unlocked = BossRushManager.Instance?.IsUnlocked ?? false;
        bool active = BossRushManager.Instance?.IsActive ?? false;

        // Title
        var title = CreateLabel(_panel.transform, "Boss Rush", 28, TextAlignmentOptions.Center);
        title.color = new Color(1f, 0.4f, 0.3f);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.85f); titleRT.anchorMax = new Vector2(1, 0.97f);
        titleRT.offsetMin = Vector2.zero; titleRT.offsetMax = Vector2.zero;

        if (!unlocked)
        {
            var lockLabel = CreateLabel(_panel.transform,
                "Defeat 3 boss districts to unlock Boss Rush mode!",
                16, TextAlignmentOptions.Center);
            lockLabel.color = new Color(0.6f, 0.4f, 0.4f);
            var lockRT = lockLabel.GetComponent<RectTransform>();
            lockRT.anchorMin = new Vector2(0.05f, 0.4f); lockRT.anchorMax = new Vector2(0.95f, 0.6f);
            lockRT.offsetMin = Vector2.zero; lockRT.offsetMax = Vector2.zero;
        }
        else if (active && BossRushManager.Instance != null)
        {
            int cur = BossRushManager.Instance.CurrentRound;
            int total = BossRushManager.Instance.TotalRounds;
            int coins = BossRushManager.Instance.BonusCoinsEarned;

            var progressLabel = CreateLabel(_panel.transform,
                $"Round {cur + 1} / {total}\nBonus Coins: {coins}",
                20, TextAlignmentOptions.Center);
            var progRT = progressLabel.GetComponent<RectTransform>();
            progRT.anchorMin = new Vector2(0.1f, 0.4f); progRT.anchorMax = new Vector2(0.9f, 0.7f);
            progRT.offsetMin = Vector2.zero; progRT.offsetMax = Vector2.zero;

            // Progress bar
            float pct = total > 0 ? (float)cur / total : 0f;
            var barBG = new GameObject("BarBG");
            barBG.transform.SetParent(_panel.transform, false);
            var barBGRT = barBG.AddComponent<RectTransform>();
            barBGRT.anchorMin = new Vector2(0.1f, 0.3f); barBGRT.anchorMax = new Vector2(0.9f, 0.38f);
            barBGRT.offsetMin = Vector2.zero; barBGRT.offsetMax = Vector2.zero;
            barBG.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

            var barFill = new GameObject("BarFill");
            barFill.transform.SetParent(barBG.transform, false);
            var barFillRT = barFill.AddComponent<RectTransform>();
            barFillRT.anchorMin = Vector2.zero; barFillRT.anchorMax = new Vector2(pct, 1);
            barFillRT.offsetMin = Vector2.zero; barFillRT.offsetMax = Vector2.zero;
            barFill.AddComponent<Image>().color = new Color(0.8f, 0.3f, 0.2f);
        }
        else
        {
            // Ready to start
            int bestStreak = BossRushManager.Instance?.BestStreak ?? 0;
            int bossCount = TerritoryManager.Instance?.GetPlayerDistricts()
                .FindAll(d => d.IsBoss).Count ?? 0;

            var infoLabel = CreateLabel(_panel.transform,
                $"Fight all {bossCount} captured bosses back-to-back!\n\nBest streak: {bestStreak}\nBonus coins + legendary drops each round",
                16, TextAlignmentOptions.Center);
            var infoRT = infoLabel.GetComponent<RectTransform>();
            infoRT.anchorMin = new Vector2(0.05f, 0.4f); infoRT.anchorMax = new Vector2(0.95f, 0.75f);
            infoRT.offsetMin = Vector2.zero; infoRT.offsetMax = Vector2.zero;

            var startBtn = CreateButton(_panel.transform, "Start Boss Rush!", () =>
            {
                if (BossRushManager.Instance?.StartBossRush() == true)
                    Hide();
            });
            var startRT = startBtn.GetComponent<RectTransform>();
            startRT.anchorMin = new Vector2(0.2f, 0.2f); startRT.anchorMax = new Vector2(0.8f, 0.35f);
            startRT.offsetMin = Vector2.zero; startRT.offsetMax = Vector2.zero;
            startBtn.GetComponent<Image>().color = new Color(0.6f, 0.2f, 0.15f);
        }

        // Close button
        var closeBtn = CreateButton(_panel.transform, "Close", () => Hide());
        var closeRT = closeBtn.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(0.3f, 0.04f); closeRT.anchorMax = new Vector2(0.7f, 0.14f);
        closeRT.offsetMin = Vector2.zero; closeRT.offsetMax = Vector2.zero;
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
        tmp.text = label; tmp.fontSize = 16; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var tmpRT = tmp.GetComponent<RectTransform>();
        tmpRT.anchorMin = Vector2.zero; tmpRT.anchorMax = Vector2.one;
        tmpRT.offsetMin = Vector2.zero; tmpRT.offsetMax = Vector2.zero;
        return go;
    }
}
