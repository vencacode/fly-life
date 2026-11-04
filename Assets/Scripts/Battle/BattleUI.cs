using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime-built battle scene UI. Listens to UIEventBus for battle state
/// and renders fly slots, round info, item drops, and win/loss screen.
/// Attach to any GameObject in the Battle scene or let it self-bootstrap.
/// </summary>
public class BattleUI : MonoBehaviour
{
    // Slot UI references
    TextMeshProUGUI[] _playerNameLabels = new TextMeshProUGUI[Constants.BATTLE_SLOTS];
    TextMeshProUGUI[] _playerHpLabels   = new TextMeshProUGUI[Constants.BATTLE_SLOTS];
    Image[]           _playerHpBars     = new Image[Constants.BATTLE_SLOTS];
    Image[]           _playerSlotBgs    = new Image[Constants.BATTLE_SLOTS];

    TextMeshProUGUI[] _enemyNameLabels  = new TextMeshProUGUI[Constants.BATTLE_SLOTS];
    TextMeshProUGUI[] _enemyHpLabels    = new TextMeshProUGUI[Constants.BATTLE_SLOTS];
    Image[]           _enemyHpBars      = new Image[Constants.BATTLE_SLOTS];
    Image[]           _enemySlotBgs     = new Image[Constants.BATTLE_SLOTS];

    TextMeshProUGUI _roundText;
    TextMeshProUGUI _logText;
    GameObject      _endPanel;
    TextMeshProUGUI _endTitle;
    TextMeshProUGUI _endDetails;
    Button          _returnButton;
    GameObject      _itemDropButton;
    TextMeshProUGUI _itemDropLabel;

    Canvas _canvas;

    void Awake()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name != Constants.SCENE_BATTLE) return;
        // Destroy old canvas from previous battle
        if (_canvas != null) Object.Destroy(_canvas.gameObject);
        BuildUI();
    }

    void OnEnable()
    {
        UIEventBus.On(UIEvent.BattleRoundStart,   OnRoundStart);
        UIEventBus.On(UIEvent.BattleAttack,        OnAttack);
        UIEventBus.On(UIEvent.BattleItemDropped,   OnItemDropped);
        UIEventBus.On(UIEvent.BattleStateChanged,  OnStateChanged);
        UIEventBus.On(UIEvent.BattleEnded,         OnBattleEnded);
    }

    void OnDisable()
    {
        UIEventBus.Off(UIEvent.BattleRoundStart,   OnRoundStart);
        UIEventBus.Off(UIEvent.BattleAttack,        OnAttack);
        UIEventBus.Off(UIEvent.BattleItemDropped,   OnItemDropped);
        UIEventBus.Off(UIEvent.BattleStateChanged,  OnStateChanged);
        UIEventBus.Off(UIEvent.BattleEnded,         OnBattleEnded);
    }

    // ---- Event handlers ----

    void OnRoundStart(object data)
    {
        int round = (int)data;
        _roundText.text = $"Round {round}";
        _logText.text = "";
        RefreshSlots();
    }

    void OnAttack(object data)
    {
        var (attacker, target, damage) = ((FlyInstance, FlyInstance, int))data;
        _logText.text = $"{attacker.Role} hits {target.Role} for {damage} dmg";
        RefreshSlots();
        FlashTargetSlot(target);
    }

    void OnItemDropped(object data)
    {
        var dropId = data as string;
        if (string.IsNullOrEmpty(dropId)) return;
        var itemDef = EquipmentRegistry.Get(dropId);
        string itemName = itemDef?.ItemName ?? dropId;

        _itemDropButton.SetActive(true);
        _itemDropLabel.text = $"GRAB: {itemName}";

        // Wire tap
        var btn = _itemDropButton.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            InventoryManager.Instance?.AddItem(dropId);
            _itemDropButton.SetActive(false);
        });
    }

    void OnStateChanged(object data)
    {
        var state = (BattleState)data;
        if (state != BattleState.ItemDrop)
            _itemDropButton.SetActive(false);
    }

    void OnBattleEnded(object data)
    {
        bool playerWon = (bool)data;
        _endPanel.SetActive(true);
        _endTitle.text = playerWon ? "VICTORY!" : "DEFEATED";
        _endTitle.color = playerWon ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.9f, 0.3f, 0.3f);

        string districtId = BattleManager.Instance?.PendingDistrictId ?? BattleContext.DistrictId;
        string districtName = "";
        if (!string.IsNullOrEmpty(districtId))
        {
            var d = TerritoryManager.Instance?.GetDistrict(districtId);
            districtName = d?.Name ?? districtId;
        }

        _endDetails.text = playerWon
            ? $"{districtName} is now yours!\n+{Constants.XP_PER_BATTLE_WIN} XP"
            : $"Failed to capture {districtName}\n+{Constants.XP_PER_BATTLE_LOSS} XP";
    }

    void ReturnToMap()
    {
        BattleContext.Clear();
        GameManager.Instance?.LoadScene(Constants.SCENE_CITY);
    }

    // ---- Hit flash ----

    void FlashTargetSlot(FlyInstance target)
    {
        if (BattleManager.Instance == null) return;
        // Find which slot the target is in
        for (int i = 0; i < Constants.BATTLE_SLOTS; i++)
        {
            if (i < BattleManager.Instance.PlayerSlots.Count && BattleManager.Instance.PlayerSlots[i] == target)
            {
                if (i < _playerSlotBgs.Length && _playerSlotBgs[i] != null)
                    StartCoroutine(FlashImage(_playerSlotBgs[i]));
                return;
            }
            if (i < BattleManager.Instance.EnemySlots.Count && BattleManager.Instance.EnemySlots[i] == target)
            {
                if (i < _enemySlotBgs.Length && _enemySlotBgs[i] != null)
                    StartCoroutine(FlashImage(_enemySlotBgs[i]));
                return;
            }
        }
    }

    IEnumerator FlashImage(Image img)
    {
        Color original = img.color;
        Color flash = new Color(0.9f, 0.15f, 0.15f, 0.7f);
        img.color = flash;
        yield return new WaitForSeconds(0.2f);
        img.color = original;
    }

    // ---- Slot refresh ----

    void RefreshSlots()
    {
        var bm = BattleManager.Instance;
        if (bm == null || _playerNameLabels == null || _enemyNameLabels == null) return;

        // Slot lists are empty before StartBattle(); UI arrays hold BATTLE_SLOTS entries
        for (int i = 0; i < Constants.BATTLE_SLOTS; i++)
        {
            var p = i < bm.PlayerSlots.Count ? bm.PlayerSlots[i] : null;
            var e = i < bm.EnemySlots.Count  ? bm.EnemySlots[i]  : null;
            if (i < _playerNameLabels.Length)
                UpdateSlot(p, _playerNameLabels[i], _playerHpLabels[i], _playerHpBars[i]);
            if (i < _enemyNameLabels.Length)
                UpdateSlot(e, _enemyNameLabels[i], _enemyHpLabels[i], _enemyHpBars[i]);
        }
    }

    void UpdateSlot(FlyInstance fly, TextMeshProUGUI nameLabel, TextMeshProUGUI hpLabel, Image hpBar)
    {
        if (nameLabel == null || hpLabel == null || hpBar == null) return; // UI not built yet
        if (fly == null)
        {
            nameLabel.text = "-";
            hpLabel.text = "";
            hpBar.fillAmount = 0;
            return;
        }

        nameLabel.text = $"{fly.Role} Lv{fly.Level}";
        hpLabel.text = $"HP:{fly.CurrentHealth}/{fly.MaxHealth}";
        hpBar.fillAmount = fly.MaxHealth > 0 ? (float)fly.CurrentHealth / fly.MaxHealth : 0f;
        hpBar.color = fly.IsAlive
            ? Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.2f, 0.9f, 0.3f), hpBar.fillAmount)
            : new Color(0.3f, 0.3f, 0.3f);
    }

    // ---- UI construction ----

    void BuildUI()
    {
        var canvasGo = new GameObject("BattleCanvas");
        _canvas = canvasGo.AddComponent<Canvas>();
        var canvas = _canvas;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGo.AddComponent<GraphicRaycaster>();

        var root = canvasGo.transform;

        // Background
        var bg = CreatePanel(root, "Background", Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Color(0.08f, 0.08f, 0.1f));

        // Round counter — top center
        _roundText = CreateTMP(root, "RoundText", 36, FontStyles.Bold,
            new Vector2(0.3f, 0.92f), new Vector2(0.7f, 0.97f));
        _roundText.text = "Preparing...";
        _roundText.alignment = TextAlignmentOptions.Center;

        // Enemy slots — top area
        BuildSlotRow(root, _enemyNameLabels, _enemyHpLabels, _enemyHpBars,
            yMin: 0.72f, yMax: 0.90f, color: new Color(0.9f, 0.2f, 0.2f, 0.8f), label: "ENEMY",
            slotBgs: _enemySlotBgs);

        // Battle log — center
        _logText = CreateTMP(root, "BattleLog", 24, FontStyles.Normal,
            new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.55f));
        _logText.alignment = TextAlignmentOptions.Center;
        _logText.color = new Color(1f, 0.9f, 0.4f);

        // Item drop button — center
        _itemDropButton = CreateButtonGO(root, "ItemDrop", "GRAB ITEM",
            new Vector2(0.25f, 0.56f), new Vector2(0.75f, 0.62f),
            new Color(1f, 0.8f, 0.1f), out _itemDropLabel);
        _itemDropButton.SetActive(false);

        // Player slots — bottom area
        BuildSlotRow(root, _playerNameLabels, _playerHpLabels, _playerHpBars,
            yMin: 0.22f, yMax: 0.40f, color: new Color(0.2f, 0.6f, 0.9f, 0.8f), label: "YOUR FLIES",
            slotBgs: _playerSlotBgs);

        // Team labels
        var enemyLabel = CreateTMP(root, "EnemyLabel", 20, FontStyles.Bold,
            new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.92f));
        enemyLabel.text = "ENEMY";
        enemyLabel.color = new Color(0.9f, 0.4f, 0.4f);
        enemyLabel.alignment = TextAlignmentOptions.Left;

        var playerLabel = CreateTMP(root, "PlayerLabel", 20, FontStyles.Bold,
            new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.42f));
        playerLabel.text = "YOUR FLIES";
        playerLabel.color = new Color(0.4f, 0.7f, 1f);
        playerLabel.alignment = TextAlignmentOptions.Left;

        // End panel — hidden until battle ends
        BuildEndPanel(root);

        // Keep HUD rows clear of notches / gesture bars
        SafeAreaCanvas.Apply(canvasGo);
    }

    void BuildSlotRow(Transform root, TextMeshProUGUI[] names, TextMeshProUGUI[] hps,
        Image[] bars, float yMin, float yMax, Color color, string label,
        Image[] slotBgs = null)
    {
        float slotWidth = 1f / Constants.BATTLE_SLOTS;
        float rowHeight = yMax - yMin;

        for (int i = 0; i < Constants.BATTLE_SLOTS; i++)
        {
            float xMin = i * slotWidth + 0.01f;
            float xMax = (i + 1) * slotWidth - 0.01f;

            // Slot background
            var slotBgRect = CreatePanel(root, $"Slot_{label}_{i}",
                new Vector2(xMin, yMin), new Vector2(xMax, yMax),
                new Vector2(0.5f, 0.5f), Vector2.zero,
                new Color(color.r, color.g, color.b, 0.15f));
            if (slotBgs != null && i < slotBgs.Length)
                slotBgs[i] = slotBgRect.GetComponent<Image>();

            // Name
            names[i] = CreateTMP(root, $"Name_{label}_{i}", 18, FontStyles.Bold,
                new Vector2(xMin, yMin + rowHeight * 0.6f), new Vector2(xMax, yMax - 0.005f));
            names[i].alignment = TextAlignmentOptions.Center;
            names[i].text = "-";

            // HP text
            hps[i] = CreateTMP(root, $"HP_{label}_{i}", 14, FontStyles.Normal,
                new Vector2(xMin, yMin + rowHeight * 0.35f), new Vector2(xMax, yMin + rowHeight * 0.55f));
            hps[i].alignment = TextAlignmentOptions.Center;

            // HP bar background
            var barBg = CreatePanel(root, $"BarBg_{label}_{i}",
                new Vector2(xMin + 0.005f, yMin + 0.01f),
                new Vector2(xMax - 0.005f, yMin + rowHeight * 0.3f),
                new Vector2(0.5f, 0.5f), Vector2.zero,
                new Color(0.2f, 0.2f, 0.2f));

            // HP bar fill
            var barGo = new GameObject($"Bar_{label}_{i}");
            barGo.transform.SetParent(root, false);
            var barRect = barGo.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(xMin + 0.005f, yMin + 0.01f);
            barRect.anchorMax = new Vector2(xMax - 0.005f, yMin + rowHeight * 0.3f);
            barRect.sizeDelta = Vector2.zero;
            barRect.offsetMin = Vector2.zero;
            barRect.offsetMax = Vector2.zero;
            bars[i] = barGo.AddComponent<Image>();
            bars[i].type = Image.Type.Filled;
            bars[i].fillMethod = Image.FillMethod.Horizontal;
            bars[i].fillAmount = 1f;
            bars[i].color = color;
        }
    }

    void BuildEndPanel(Transform root)
    {
        _endPanel = new GameObject("EndPanel");
        _endPanel.transform.SetParent(root, false);
        var rect = _endPanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        var img = _endPanel.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.85f);

        _endTitle = CreateTMP(_endPanel.transform, "EndTitle", 48, FontStyles.Bold,
            new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.7f));
        _endTitle.alignment = TextAlignmentOptions.Center;

        _endDetails = CreateTMP(_endPanel.transform, "EndDetails", 24, FontStyles.Normal,
            new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.55f));
        _endDetails.alignment = TextAlignmentOptions.Center;

        var btnGo = CreateButtonGO(_endPanel.transform, "ReturnBtn", "Return to Map",
            new Vector2(0.25f, 0.28f), new Vector2(0.75f, 0.36f),
            new Color(0.3f, 0.6f, 0.9f), out _);
        btnGo.GetComponent<Button>().onClick.AddListener(ReturnToMap);

        _endPanel.SetActive(false);
    }

    // ---- UI helpers ----

    RectTransform CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.pivot = pivot;
        r.sizeDelta = sizeDelta;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        return r;
    }

    TextMeshProUGUI CreateTMP(Transform parent, string name, int fontSize, FontStyles style,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.sizeDelta = Vector2.zero;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        return tmp;
    }

    GameObject CreateButtonGO(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color bgColor, out TextMeshProUGUI textRef)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.sizeDelta = Vector2.zero;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        go.AddComponent<Button>();

        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        var tr = txtGo.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.sizeDelta = Vector2.zero;
        textRef = txtGo.AddComponent<TextMeshProUGUI>();
        textRef.text = label;
        textRef.fontSize = 22;
        textRef.alignment = TextAlignmentOptions.Center;
        textRef.color = Color.white;
        return go;
    }

}
