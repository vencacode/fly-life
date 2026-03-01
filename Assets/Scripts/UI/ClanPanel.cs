using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Modal panel for creating or joining a clan.
/// Shows current clan info, or create/join form if no custom clan.
/// Built at runtime (same pattern as FlyDeployPanel).
/// </summary>
public class ClanPanel : MonoBehaviour
{
    GameObject _root;
    TMP_InputField _nameInput;
    TMP_InputField _joinIdInput;
    TextMeshProUGUI _statusLabel;
    TextMeshProUGUI _infoLabel;
    GameObject _createSection;
    GameObject _infoSection;
    System.Action _onClose;

    void Awake()
    {
        _root = BuildUI();
        _root.SetActive(false);
    }

    public void Show(System.Action onClose = null)
    {
        _onClose = onClose;
        _root.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        _root.SetActive(false);
        _onClose?.Invoke();
    }

    public bool IsVisible => _root != null && _root.activeSelf;

    void Refresh()
    {
        var cm = ClanManager.Instance;
        bool hasClan = cm?.HasCustomClan ?? false;

        _createSection.SetActive(!hasClan);
        _infoSection.SetActive(hasClan);

        if (hasClan)
        {
            _infoLabel.text = $"Clan: {cm.ClanName}\n" +
                              $"ID: {cm.ClanId}\n" +
                              $"Level: {cm.ClanLevel}  |  XP: {cm.ClanXP}\n" +
                              $"Flies: {cm.TotalFlies}  ({cm.ScoutCount}S / {cm.SoldierCount}F / {cm.WorkerCount}W)\n" +
                              $"Districts: {TerritoryManager.Instance?.GetPlayerDistricts()?.Count ?? 0}";
        }
        _statusLabel.text = "";
    }

    void OnCreate()
    {
        string name = _nameInput?.text?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length < 2)
        {
            _statusLabel.text = "Name must be at least 2 characters";
            _statusLabel.color = new Color(1f, 0.4f, 0.3f);
            return;
        }
        ClanManager.Instance?.CreateClan(name);
        Refresh();
    }

    void OnJoin()
    {
        string id = _joinIdInput?.text?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            _statusLabel.text = "Enter a clan ID to join";
            _statusLabel.color = new Color(1f, 0.4f, 0.3f);
            return;
        }
        ClanManager.Instance?.JoinClan(id, "Joined Clan");
        Refresh();
    }

    // ---- UI construction ----

    GameObject BuildUI()
    {
        var canvasGo = new GameObject("ClanPanelCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Dark overlay
        var overlay = MakeRect(canvasGo.transform, "Overlay", Vector2.zero, Vector2.one);
        overlay.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);

        // Panel
        var panel = MakeRect(canvasGo.transform, "Panel",
            new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.8f));
        panel.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 0.98f);

        // Title
        var title = MakeTMP(panel, "Title", 32, FontStyles.Bold,
            new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f));
        title.text = "Clan Management";
        title.alignment = TextAlignmentOptions.Center;

        // --- Create/Join section (shown when no custom clan) ---
        _createSection = new GameObject("CreateSection");
        _createSection.transform.SetParent(panel, false);
        var csRect = _createSection.AddComponent<RectTransform>();
        csRect.anchorMin = new Vector2(0, 0.1f);
        csRect.anchorMax = new Vector2(1, 0.87f);
        csRect.offsetMin = csRect.offsetMax = Vector2.zero;

        // Create clan label
        var createLabel = MakeTMP(csRect, "CreateLabel", 22, FontStyles.Bold,
            new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.92f));
        createLabel.text = "Create a New Clan";
        createLabel.alignment = TextAlignmentOptions.Left;

        // Name input
        _nameInput = MakeInputField(csRect, "NameInput", "Enter clan name...",
            new Vector2(0.05f, 0.62f), new Vector2(0.65f, 0.76f));

        // Create button
        var createBtn = MakeButton(csRect, "CreateBtn", "Create",
            new Vector2(0.68f, 0.62f), new Vector2(0.95f, 0.76f),
            new Color(0.2f, 0.7f, 0.3f));
        createBtn.GetComponent<Button>().onClick.AddListener(OnCreate);

        // Divider label
        var orLabel = MakeTMP(csRect, "OrLabel", 18, FontStyles.Italic,
            new Vector2(0.3f, 0.48f), new Vector2(0.7f, 0.58f));
        orLabel.text = "— or join existing —";
        orLabel.alignment = TextAlignmentOptions.Center;
        orLabel.color = new Color(0.6f, 0.6f, 0.6f);

        // Join input
        _joinIdInput = MakeInputField(csRect, "JoinInput", "Enter clan ID...",
            new Vector2(0.05f, 0.32f), new Vector2(0.65f, 0.46f));

        // Join button
        var joinBtn = MakeButton(csRect, "JoinBtn", "Join",
            new Vector2(0.68f, 0.32f), new Vector2(0.95f, 0.46f),
            new Color(0.2f, 0.5f, 0.9f));
        joinBtn.GetComponent<Button>().onClick.AddListener(OnJoin);

        // Status label
        _statusLabel = MakeTMP(csRect, "Status", 18, FontStyles.Normal,
            new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.30f));
        _statusLabel.alignment = TextAlignmentOptions.Center;

        // --- Info section (shown when clan exists) ---
        _infoSection = new GameObject("InfoSection");
        _infoSection.transform.SetParent(panel, false);
        var isRect = _infoSection.AddComponent<RectTransform>();
        isRect.anchorMin = new Vector2(0, 0.1f);
        isRect.anchorMax = new Vector2(1, 0.87f);
        isRect.offsetMin = isRect.offsetMax = Vector2.zero;

        _infoLabel = MakeTMP(isRect, "InfoLabel", 22, FontStyles.Normal,
            new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.95f));
        _infoLabel.alignment = TextAlignmentOptions.Left;

        // Close button
        var closeBtn = MakeButton(panel, "CloseBtn", "Close",
            new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.09f),
            new Color(0.5f, 0.5f, 0.5f));
        closeBtn.GetComponent<Button>().onClick.AddListener(Hide);

        return canvasGo;
    }

    // ---- Helpers ----

    RectTransform MakeRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        return r;
    }

    TextMeshProUGUI MakeTMP(RectTransform parent, string name, int fontSize, FontStyles style,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var r = MakeRect(parent, name, anchorMin, anchorMax);
        var tmp = r.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        return tmp;
    }

    TMP_InputField MakeInputField(RectTransform parent, string name, string placeholder,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var r = MakeRect(parent, name, anchorMin, anchorMax);
        var bg = r.gameObject.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.25f);

        // Text area
        var textArea = MakeRect(r, "TextArea", new Vector2(0.05f, 0), new Vector2(0.95f, 1));

        // Placeholder
        var phRect = MakeRect(textArea, "Placeholder", Vector2.zero, Vector2.one);
        var phTmp = phRect.gameObject.AddComponent<TextMeshProUGUI>();
        phTmp.text = placeholder;
        phTmp.fontSize = 20;
        phTmp.fontStyle = FontStyles.Italic;
        phTmp.color = new Color(0.5f, 0.5f, 0.5f);

        // Input text
        var txtRect = MakeRect(textArea, "Text", Vector2.zero, Vector2.one);
        var txtTmp = txtRect.gameObject.AddComponent<TextMeshProUGUI>();
        txtTmp.fontSize = 20;
        txtTmp.color = Color.white;

        var input = r.gameObject.AddComponent<TMP_InputField>();
        input.textViewport = textArea;
        input.textComponent = txtTmp;
        input.placeholder = phTmp;
        input.characterLimit = 30;

        return input;
    }

    GameObject MakeButton(RectTransform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
    {
        var r = MakeRect(parent, name, anchorMin, anchorMax);
        r.gameObject.AddComponent<Image>().color = bgColor;
        r.gameObject.AddComponent<Button>();

        var txt = MakeRect(r, "Label", Vector2.zero, Vector2.one);
        var tmp = txt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return r.gameObject;
    }
}
