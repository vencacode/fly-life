using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Bottom panel shown when player taps a district.
/// Shows district info and action buttons. Slides up/down.
/// </summary>
public class DistrictInfoPanel : MonoBehaviour
{
    [Header("Labels")]
    public TextMeshProUGUI DistrictNameText;
    public TextMeshProUGUI OwnerText;
    public TextMeshProUGUI PooCountText;
    public TextMeshProUGUI BonusText;
    public TextMeshProUGUI DifficultyText;

    [Header("Buttons")]
    public Button SendFliesButton;
    public Button AttackButton;
    public Button ClaimButton;
    public Button SpotPooButton;
    public Button WatchAdButton;

    [Header("Panels")]
    public FlyDeployPanel DeployPanel;

    [Header("Animation")]
    public float SlideDuration = 0.25f;

    public static District ActiveDistrict { get; private set; }
    District _current;
    RectTransform _rect;
    Coroutine _slideRoutine;
    float _panelHeight;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _panelHeight = _rect.sizeDelta.y;
        _rect.anchoredPosition = new Vector2(0, -_panelHeight);
        gameObject.SetActive(false);
        SendFliesButton?.onClick.AddListener(OnSendFlies);
        AttackButton?.onClick.AddListener(OnAttack);
        ClaimButton?.onClick.AddListener(OnClaim);
        SpotPooButton?.onClick.AddListener(OnSpotPoo);
        WatchAdButton?.onClick.AddListener(OnWatchAd);

        // Auto-create FlyDeployPanel if not assigned (e.g. scene-wired panel)
        if (DeployPanel == null)
        {
            var deployGo = new GameObject("FlyDeployPanel");
            DeployPanel = deployGo.AddComponent<FlyDeployPanel>();
        }
    }

    public void Show(District d)
    {
        _current = d;
        ActiveDistrict = d;
        gameObject.SetActive(true);
        SlideTo(0f);

        var theme = DistrictThemeData.Get(d.Theme);
        DistrictNameText.text = $"{theme.Icon} {d.Name}";

        bool isPlayer = d.OwnerId == (ClanManager.Instance?.ClanId ?? "player_clan");
        bool isEnemy  = !string.IsNullOrEmpty(d.OwnerId) && !isPlayer;
        bool isNeutral = string.IsNullOrEmpty(d.OwnerId);

        OwnerText.text    = isPlayer ? "[YOURS]" : isEnemy ? "[ENEMY]" : "[UNCLAIMED]";
        int pooCount = d.PooIds?.Count ?? 0;
        int deployed = ClanManager.Instance?.GetDeployedCount(d.Id) ?? 0;
        PooCountText.text = deployed > 0
            ? $"{pooCount} poos spotted  |  {deployed} flies stationed"
            : $"{pooCount} poos spotted";

        // Difficulty & lock
        int playerLevel = ClanManager.Instance?.ClanLevel ?? 1;
        bool locked = d.IsLocked(playerLevel);
        string bossTag = d.IsBoss ? " | BOSS" : "";
        if (DifficultyText) DifficultyText.text = $"Difficulty: {d.DifficultyLevel} | Ring {d.Ring}{bossTag}";

        if (locked)
            BonusText.text = $"Requires level {d.DifficultyLevel}";
        else
            BonusText.text = GetBonusText(d, theme);

        SendFliesButton?.gameObject.SetActive(isPlayer && !locked);
        AttackButton?.gameObject.SetActive(isEnemy && !locked);
        ClaimButton?.gameObject.SetActive(isNeutral && !locked);
        SpotPooButton?.gameObject.SetActive(!locked && pooCount < Constants.MAX_POOS_PER_DISTRICT);
        WatchAdButton?.gameObject.SetActive(FeatureFlags.ADS_ENABLED && (isEnemy || isNeutral) && !locked);
    }

    public void Hide()
    {
        ActiveDistrict = null;
        SlideTo(-_panelHeight, () => gameObject.SetActive(false));
    }

    void SlideTo(float targetY, System.Action onComplete = null)
    {
        if (_slideRoutine != null) StopCoroutine(_slideRoutine);
        _slideRoutine = StartCoroutine(SlideCoroutine(targetY, onComplete));
    }

    IEnumerator SlideCoroutine(float targetY, System.Action onComplete)
    {
        float startY = _rect.anchoredPosition.x == 0 ? _rect.anchoredPosition.y : -_panelHeight;
        startY = _rect.anchoredPosition.y;
        float elapsed = 0f;
        while (elapsed < SlideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / SlideDuration);
            _rect.anchoredPosition = new Vector2(0, Mathf.Lerp(startY, targetY, t));
            yield return null;
        }
        _rect.anchoredPosition = new Vector2(0, targetY);
        _slideRoutine = null;
        onComplete?.Invoke();
    }

    string GetBonusText(District d, DistrictThemeData theme)
    {
        string themeBonus = theme.Description;

        // Theme-specific modifiers
        if (theme.PooSpawnRateMultiplier != 1f)
            themeBonus += $"  |  Poo: x{theme.PooSpawnRateMultiplier:F1}";
        if (theme.PooQualityMultiplier != 1f)
            themeBonus += $"  |  Quality: x{theme.PooQualityMultiplier:F1}";
        if (theme.ResourceBonusMultiplier != 1f)
            themeBonus += $"  |  Resources: x{theme.ResourceBonusMultiplier:F2}";
        if (theme.XPBonusMultiplier != 1f)
            themeBonus += $"  |  XP: x{theme.XPBonusMultiplier:F1}";

        if (!string.IsNullOrEmpty(d.OwnerId))
        {
            bool isPlayer = d.OwnerId == (ClanManager.Instance?.ClanId ?? "player_clan");
            themeBonus += isPlayer ? "\n+5 PooCoins/min income" : "\nEnemy gets +10% resources";
        }

        return themeBonus;
    }

    void OnSendFlies()
    {
        if (_current == null) return;
        if (DeployPanel != null)
            DeployPanel.Show(_current, () => Show(_current));
        else
            Debug.LogWarning("[InfoPanel] DeployPanel not assigned.");
    }

    void OnWatchAd()
    {
        AdManager.Instance?.ShowRewardedAd(() =>
        {
            AdManager.Instance?.GrantWarDrumsBooster();
            Debug.Log("[InfoPanel] Ad watched — WarDrums booster active!");
        });
    }

    void OnAttack()
    {
        if (_current == null) return;
        FlySelectionUI.Instance?.Show(selectedFlies =>
        {
            var enemies = GenerateEnemyFlies(_current.DifficultyLevel);
            BattleManager.Instance?.PrepareBattle(_current.Id, enemies, selectedFlies);
            Debug.Log($"[InfoPanel] Attacking {_current.Name} (difficulty {_current.DifficultyLevel})!");
            GameManager.Instance?.LoadScene(Constants.SCENE_BATTLE);
        });
    }

    void OnSpotPoo()
    {
        if (_current == null) return;
        if ((_current.PooIds?.Count ?? 0) >= Constants.MAX_POOS_PER_DISTRICT)
        {
            Debug.LogWarning("[InfoPanel] District full — max poos reached.");
            return;
        }

        // Spawn poo at a random spot within the district
        var tm = TerritoryManager.Instance;
        float half = (tm?.DistrictSize ?? 10f) * 0.4f;
        Vector2 pos = _current.Center + new Vector2(Random.Range(-half, half), Random.Range(-half, half));

        var poo = PooManager.Instance?.SpawnRandomPoo(pos);
        if (poo != null)
        {
            string clanId = ClanManager.Instance?.ClanId ?? "player_clan";
            PooManager.Instance?.ClaimPoo(poo, clanId);
            UIEventBus.Emit(UIEvent.PooDiscovered, poo);
            Debug.Log($"[InfoPanel] Spotted poo in {_current.Name}!");
        }

        MapManager.Instance?.RefreshAllTiles();
        Show(_current);
    }

    void OnClaim()
    {
        if (_current == null) return;
        FlySelectionUI.Instance?.Show(selectedFlies =>
        {
            // Neutral districts also require a battle
            var enemies = GenerateEnemyFlies(_current.DifficultyLevel);
            BattleManager.Instance?.PrepareBattle(_current.Id, enemies, selectedFlies);
            Debug.Log($"[InfoPanel] Claiming {_current.Name} (difficulty {_current.DifficultyLevel})!");
            GameManager.Instance?.LoadScene(Constants.SCENE_BATTLE);
        });
    }

    System.Collections.Generic.List<FlyInstance> GenerateEnemyFlies(int difficulty)
    {
        var roles = ClanManager.Instance?.AllRoles;
        var enemies = new System.Collections.Generic.List<FlyInstance>();
        int playerFlies = ClanManager.Instance?.TotalFlies ?? 1;
        // Scale enemy count: match player count for easy districts, +1 for harder ones
        int baseSlots = Mathf.Clamp(playerFlies + difficulty - 1, 1, Constants.BATTLE_SLOTS);
        int slots = (_current != null && _current.IsBoss) ? baseSlots + 2 : baseSlots;
        for (int i = 0; i < slots; i++)
        {
            var role = roles != null && roles.Length > 0
                ? roles[i % roles.Length]
                : null;
            int hp = (role?.BaseHealth ?? 50) * difficulty;
            enemies.Add(new FlyInstance
            {
                InstanceId    = $"enemy_{i}",
                Role          = role?.Role ?? FlyRole.Soldier,
                Level         = difficulty,
                Attack        = (role?.BaseAttack  ?? 10) * difficulty,
                Defense       = (role?.BaseDefense ?? 5) * difficulty,
                Speed         = role?.BaseSpeed ?? 8,
                MaxHealth     = hp,
                CurrentHealth = hp
            });
        }
        return enemies;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (DistrictNameText == null) Debug.LogWarning("[DistrictInfoPanel] DistrictNameText not assigned!", this);
        if (OwnerText == null)        Debug.LogWarning("[DistrictInfoPanel] OwnerText not assigned!", this);
        if (PooCountText == null)     Debug.LogWarning("[DistrictInfoPanel] PooCountText not assigned!", this);
        if (BonusText == null)        Debug.LogWarning("[DistrictInfoPanel] BonusText not assigned!", this);
        if (SendFliesButton == null)  Debug.LogWarning("[DistrictInfoPanel] SendFliesButton not assigned!", this);
        if (AttackButton == null)     Debug.LogWarning("[DistrictInfoPanel] AttackButton not assigned!", this);
        if (ClaimButton == null)      Debug.LogWarning("[DistrictInfoPanel] ClaimButton not assigned!", this);
    }
#endif
}
