using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Picks 3 random daily missions and tracks progress.
/// Missions refresh at midnight (based on local date change).
/// </summary>
public class DailyMissionManager : MonoBehaviour
{
    public static DailyMissionManager Instance { get; private set; }

    [Serializable]
    public class ActiveMission
    {
        public string PoolId;
        public string DisplayText;
        public int Target;
        public int Progress;
        public bool Claimed;
        public int CoinReward;
        public int GemReward;
        public MissionRegistry.MissionType Type;
    }

    List<ActiveMission> _missions = new();
    string _lastRefreshDate;

    public IReadOnlyList<ActiveMission> Missions => _missions;

    // Cached delegates
    Action<object> _onBattleEnded;
    Action<object> _onDistrictCaptured;
    Action<object> _onClanUpdated;
    Action<object> _onPooDiscovered;
    Action<object> _onEggHatched;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        CheckRefresh();

        _onBattleEnded      = data => OnBattleEnded(data);
        _onDistrictCaptured = _ => IncrementType(MissionRegistry.MissionType.CaptureDistricts);
        _onClanUpdated      = _ => { }; // placeholder, training tracked separately
        _onPooDiscovered    = _ => IncrementType(MissionRegistry.MissionType.CollectPoo);
        _onEggHatched       = _ => IncrementType(MissionRegistry.MissionType.HatchEggs);

        UIEventBus.On(UIEvent.BattleEnded, _onBattleEnded);
        UIEventBus.On(UIEvent.DistrictCaptured, _onDistrictCaptured);
        UIEventBus.On(UIEvent.PooDiscovered, _onPooDiscovered);
        UIEventBus.On(UIEvent.EggHatched, _onEggHatched);
    }

    void OnDisable()
    {
        if (_onBattleEnded != null)      UIEventBus.Off(UIEvent.BattleEnded, _onBattleEnded);
        if (_onDistrictCaptured != null)  UIEventBus.Off(UIEvent.DistrictCaptured, _onDistrictCaptured);
        if (_onPooDiscovered != null)     UIEventBus.Off(UIEvent.PooDiscovered, _onPooDiscovered);
        if (_onEggHatched != null)        UIEventBus.Off(UIEvent.EggHatched, _onEggHatched);
    }

    void CheckRefresh()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        if (_lastRefreshDate == today && _missions.Count > 0) return;

        _lastRefreshDate = today;
        GenerateMissions();
        Debug.Log("[DailyMissions] New missions generated for " + today);
    }

    void GenerateMissions()
    {
        _missions.Clear();
        var pool = MissionRegistry.Pool.ToList();
        var rng = new System.Random(DateTime.Now.DayOfYear * 1000 + DateTime.Now.Year);

        // Shuffle and pick 3
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        for (int i = 0; i < Mathf.Min(3, pool.Count); i++)
        {
            var def = pool[i];
            int target = rng.Next(def.MinTarget, def.MaxTarget + 1);
            _missions.Add(new ActiveMission
            {
                PoolId      = def.Id,
                DisplayText = string.Format(def.DisplayTemplate, target),
                Target      = target,
                Progress    = 0,
                Claimed     = false,
                CoinReward  = def.CoinReward,
                GemReward   = def.GemReward,
                Type        = def.Type
            });
        }

        UIEventBus.Emit(UIEvent.MissionUpdated);
    }

    // ---- Progress tracking ----

    void OnBattleEnded(object data)
    {
        bool won = data is bool b && b;
        if (won)
            IncrementType(MissionRegistry.MissionType.WinBattles);
    }

    void IncrementType(MissionRegistry.MissionType type, int amount = 1)
    {
        bool changed = false;
        foreach (var m in _missions)
        {
            if (m.Type == type && !m.Claimed && m.Progress < m.Target)
            {
                m.Progress = Mathf.Min(m.Progress + amount, m.Target);
                changed = true;
                if (m.Progress >= m.Target)
                    UIEventBus.Emit(UIEvent.MissionCompleted, m);
            }
        }
        if (changed) UIEventBus.Emit(UIEvent.MissionUpdated);
    }

    /// <summary>Call from ClanManager.TrainFly when training succeeds.</summary>
    public void TrackTrainFly() => IncrementType(MissionRegistry.MissionType.TrainFlies);

    /// <summary>Call from ResourceManager.SpendPooCoins.</summary>
    public void TrackSpendCoins(int amount) => IncrementType(MissionRegistry.MissionType.SpendCoins, amount);

    /// <summary>Call from InventoryManager.UpgradeItem.</summary>
    public void TrackUpgradeGear() => IncrementType(MissionRegistry.MissionType.UpgradeGear);

    /// <summary>Call from ClanManager.PromoteFly.</summary>
    public void TrackPromoteFly() => IncrementType(MissionRegistry.MissionType.PromoteFly);

    // ---- Claim rewards ----

    public bool ClaimReward(int index)
    {
        if (index < 0 || index >= _missions.Count) return false;
        var m = _missions[index];
        if (m.Claimed || m.Progress < m.Target) return false;

        m.Claimed = true;
        ResourceManager.Instance?.AddPooCoins(m.CoinReward);
        ResourceManager.Instance?.AddGems(m.GemReward);
        ToastManager.Instance?.ShowToast(
            $"Mission complete! +{m.CoinReward} coins, +{m.GemReward} gems",
            ToastType.Reward);

        UIEventBus.Emit(UIEvent.MissionUpdated);
        Debug.Log($"[DailyMissions] Claimed: {m.DisplayText}");
        return true;
    }

    /// <summary>Hours until next mission refresh (midnight).</summary>
    public float HoursUntilRefresh()
    {
        var now = DateTime.Now;
        var midnight = now.Date.AddDays(1);
        return (float)(midnight - now).TotalHours;
    }

    // ---- Save / Load ----

    [Serializable]
    public class SaveData
    {
        public string LastRefreshDate;
        public List<ActiveMission> Missions;
    }

    public SaveData GetSaveData() => new()
    {
        LastRefreshDate = _lastRefreshDate,
        Missions        = new List<ActiveMission>(_missions)
    };

    public void LoadSaveData(SaveData data)
    {
        if (data == null) return;
        _lastRefreshDate = data.LastRefreshDate;
        _missions = data.Missions ?? new List<ActiveMission>();
        // If date changed, will be refreshed in Start→CheckRefresh
    }
}
