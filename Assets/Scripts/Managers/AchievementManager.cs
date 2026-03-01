using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Tracks player milestones and awards gems/skins when achievements are unlocked.
/// Subscribes to UIEventBus for automatic counter updates.
/// </summary>
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    // Lifetime counters
    int _battlesWon;
    int _bossesDefeated;
    int _coinsSpent;
    int _gemsEarned;
    int _elitesPromoted;

    HashSet<string> _unlocked = new();

    // Cached delegates for clean unsubscribe
    Action<object> _onBattleEnded;
    Action<object> _onDistrictCaptured;
    Action<object> _onClanUpdated;
    Action<object> _onLevelUp;

    public int GetCounter(string name) => name switch
    {
        "battles_won"      => _battlesWon,
        "bosses_defeated"  => _bossesDefeated,
        "coins_spent"      => _coinsSpent,
        "gems_earned"      => _gemsEarned,
        "elites_promoted"  => _elitesPromoted,
        "districts_owned"  => TerritoryManager.Instance?.GetPlayerDistricts().Count ?? 0,
        "total_flies"      => ClanManager.Instance?.TotalFlies ?? 0,
        "clan_level"       => ClanManager.Instance?.ClanLevel ?? 0,
        "skins_unlocked"   => SkinSelectPopup.Instance != null
                                ? SkinRegistry.All.Values.Count(s => SkinSelectPopup.Instance.IsSkinUnlocked(s.SkinId))
                                : 0,
        "legendary_owned"  => CountLegendaryOwned(),
        _ => 0
    };

    static int CountLegendaryOwned()
    {
        if (InventoryManager.Instance == null) return 0;
        int count = 0;
        foreach (var id in EquipmentRegistry.GetLegendaryItems())
        {
            if (InventoryManager.Instance.HasItem(id))
                count++;
        }
        return count;
    }

    public bool IsUnlocked(string achievementId) => _unlocked.Contains(achievementId);

    public float GetProgress(string achievementId)
    {
        var def = AchievementRegistry.Get(achievementId);
        if (!def.HasValue) return 0f;
        int current = GetCounterForAchievement(def.Value);
        return Mathf.Clamp01((float)current / def.Value.Target);
    }

    public int GetCurrentValue(string achievementId)
    {
        var def = AchievementRegistry.Get(achievementId);
        if (!def.HasValue) return 0;
        return GetCounterForAchievement(def.Value);
    }

    int GetCounterForAchievement(AchievementRegistry.AchievementDef def) => def.Id switch
    {
        "first_blood"       => _battlesWon,
        "battle_veteran"    => _battlesWon,
        "warlord"           => _battlesWon,
        "boss_slayer"       => _bossesDefeated,
        "landlord"          => GetCounter("districts_owned"),
        "empire_builder"    => GetCounter("districts_owned"),
        "world_domination"  => GetCounter("districts_owned"),
        "swarm_leader"      => GetCounter("total_flies"),
        "elite_force"       => _elitesPromoted,
        "clan_level_10"     => GetCounter("clan_level"),
        "big_spender"       => _coinsSpent,
        "gem_collector"     => _gemsEarned,
        "fashionista"       => GetCounter("skins_unlocked"),
        "legendary_hunter"  => GetCounter("legendary_owned"),
        _ => 0
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        _onBattleEnded      = data => OnBattleEnded(data);
        _onDistrictCaptured = data => OnDistrictCaptured(data);
        _onClanUpdated      = _ => CheckAll();
        _onLevelUp          = _ => CheckAll();

        UIEventBus.On(UIEvent.BattleEnded, _onBattleEnded);
        UIEventBus.On(UIEvent.DistrictCaptured, _onDistrictCaptured);
        UIEventBus.On(UIEvent.ClanUpdated, _onClanUpdated);
        UIEventBus.On(UIEvent.LevelUp, _onLevelUp);
    }

    void OnDisable()
    {
        if (_onBattleEnded != null)      UIEventBus.Off(UIEvent.BattleEnded, _onBattleEnded);
        if (_onDistrictCaptured != null)  UIEventBus.Off(UIEvent.DistrictCaptured, _onDistrictCaptured);
        if (_onClanUpdated != null)       UIEventBus.Off(UIEvent.ClanUpdated, _onClanUpdated);
        if (_onLevelUp != null)           UIEventBus.Off(UIEvent.LevelUp, _onLevelUp);
    }

    void OnBattleEnded(object data)
    {
        bool won = data is bool b && b;
        if (won)
        {
            _battlesWon++;

            // Check if boss
            var district = TerritoryManager.Instance?.GetDistrict(BattleManager.Instance?.PendingDistrictId);
            if (district != null && district.IsBoss)
                _bossesDefeated++;
        }
        CheckAll();
    }

    void OnDistrictCaptured(object data)
    {
        CheckAll();
    }

    /// <summary>Call from ResourceManager when coins are spent.</summary>
    public void TrackCoinsSpent(int amount)
    {
        _coinsSpent += amount;
        CheckAll();
    }

    /// <summary>Call from ResourceManager when gems are earned.</summary>
    public void TrackGemsEarned(int amount)
    {
        _gemsEarned += amount;
        CheckAll();
    }

    /// <summary>Call from ClanManager when a fly is promoted.</summary>
    public void TrackElitePromoted()
    {
        _elitesPromoted++;
        CheckAll();
    }

    void CheckAll()
    {
        foreach (var kv in AchievementRegistry.All)
        {
            if (_unlocked.Contains(kv.Key)) continue;

            int current = GetCounterForAchievement(kv.Value);
            if (current >= kv.Value.Target)
            {
                _unlocked.Add(kv.Key);
                AwardAchievement(kv.Value);
            }
        }
    }

    void AwardAchievement(AchievementRegistry.AchievementDef def)
    {
        Debug.Log($"[Achievement] Unlocked: {def.DisplayName}! +{def.GemReward} gems");

        if (def.GemReward > 0)
            ResourceManager.Instance?.AddGems(def.GemReward);

        if (!string.IsNullOrEmpty(def.SkinReward))
            SkinSelectPopup.Instance?.UnlockSkin(def.SkinReward);

        ToastManager.Instance?.ShowToast(
            $"Achievement: {def.DisplayName}! +{def.GemReward} gems",
            ToastType.Reward);

        UIEventBus.Emit(UIEvent.AchievementUnlocked, def);
    }

    // ---- Save / Load ----

    [System.Serializable]
    public class SaveData
    {
        public int BattlesWon;
        public int BossesDefeated;
        public int CoinsSpent;
        public int GemsEarned;
        public int ElitesPromoted;
        public List<string> Unlocked = new();
    }

    public SaveData GetSaveData() => new()
    {
        BattlesWon      = _battlesWon,
        BossesDefeated  = _bossesDefeated,
        CoinsSpent      = _coinsSpent,
        GemsEarned      = _gemsEarned,
        ElitesPromoted  = _elitesPromoted,
        Unlocked        = new List<string>(_unlocked)
    };

    public void LoadSaveData(SaveData data)
    {
        if (data == null) return;
        _battlesWon      = data.BattlesWon;
        _bossesDefeated  = data.BossesDefeated;
        _coinsSpent      = data.CoinsSpent;
        _gemsEarned      = data.GemsEarned;
        _elitesPromoted  = data.ElitesPromoted;
        _unlocked        = new HashSet<string>(data.Unlocked);
    }
}
