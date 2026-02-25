using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the player's fly clan: roles, XP, evolution, egg hatching.
/// </summary>
public class ClanManager : MonoBehaviour
{
    public static ClanManager Instance { get; private set; }

    [Header("Starting config")]
    public FlyRoleData ScoutRoleData;
    public FlyRoleData SoldierRoleData;
    public FlyRoleData WorkerRoleData;

    // Clan state
    public string ClanId   { get; private set; } = "player_clan";
    public string ClanName { get; set; }         = "The Fly Lifers";
    public int    ClanXP   { get; private set; } = 0;
    public int    ClanLevel{ get; private set; } = 1;

    private List<FlyInstance> _flies     = new();
    private List<EggBatch>    _eggBatches = new();
    private List<ActiveBooster> _boosters = new();

    public IReadOnlyList<FlyInstance>   Flies    => _flies;
    public IReadOnlyList<EggBatch>      Eggs     => _eggBatches;
    public IReadOnlyList<ActiveBooster> Boosters => _boosters;

    public int ScoutCount   => _flies.Count(f => f.Role == FlyRole.Scout   && f.IsAlive);
    public int SoldierCount => _flies.Count(f => f.Role == FlyRole.Soldier && f.IsAlive);
    public int WorkerCount  => _flies.Count(f => f.Role == FlyRole.Worker  && f.IsAlive);
    public int TotalFlies   => _flies.Count(f => f.IsAlive);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (_flies.Count == 0)
            SpawnStartingFly();
    }

    void Update()
    {
        CheckEggHatching();
        PruneDeadFlies();
    }

    // ---- Fly spawning ----

    void SpawnStartingFly()
    {
        var fly = CreateFly(FlyRole.Scout);
        _flies.Add(fly);
        Debug.Log("[ClanManager] Starting fly spawned. Welcome to Fly Life.");
    }

    FlyInstance CreateFly(FlyRole role)
    {
        var data = GetRoleData(role);
        return new FlyInstance
        {
            InstanceId    = System.Guid.NewGuid().ToString(),
            Role          = role,
            Level         = 1,
            XP            = 0,
            Attack        = data.BaseAttack,
            Defense       = data.BaseDefense,
            Speed         = data.BaseSpeed,
            MaxHealth     = data.BaseHealth,
            CurrentHealth = data.BaseHealth
        };
    }

    // ---- Egg system ----

    public void LayEggs(PooInstance poo)
    {
        if (poo == null || poo.IsDry) return;

        float hatchMultiplier = GetBoosterMultiplier(BoosterType.HatchSpeed);
        float fertilityMultiplier = GetBoosterMultiplier(BoosterType.FertilitySpike);

        int eggCount = Mathf.RoundToInt(
            Constants.EGGS_PER_LAY * (poo.Fertility / 50f) * fertilityMultiplier
        );
        float hatchTime = Constants.BASE_EGG_HATCH_TIME_SEC / hatchMultiplier;

        var batch = new EggBatch
        {
            PooInstanceId = poo.PooDataId,
            EggCount      = eggCount,
            LayTime       = Time.time,
            HatchDuration = hatchTime,
            Role          = PickAutoRole()
        };
        _eggBatches.Add(batch);
        Debug.Log($"[ClanManager] Laid {eggCount} eggs on {poo.PooDataId}. Hatches in {hatchTime}s.");
    }

    void CheckEggHatching()
    {
        var hatched = _eggBatches.Where(e => e.IsReady).ToList();
        foreach (var batch in hatched)
        {
            int canAdd = Constants.MAX_CLAN_SIZE - TotalFlies;
            int toSpawn = Mathf.Min(batch.EggCount, canAdd);
            for (int i = 0; i < toSpawn; i++)
                _flies.Add(CreateFly(batch.Role));

            if (toSpawn > 0)
                Debug.Log($"[ClanManager] {toSpawn} new {batch.Role}s hatched! 🪰");
        }
        _eggBatches.RemoveAll(e => e.IsReady);
    }

    // ---- XP & Levelling ----

    public void AddXP(int amount)
    {
        ClanXP += amount;
        int newLevel = Mathf.FloorToInt(ClanXP / 100f) + 1;
        if (newLevel > ClanLevel)
        {
            ClanLevel = newLevel;
            OnLevelUp(ClanLevel);
        }
    }

    void OnLevelUp(int level)
    {
        Debug.Log($"[ClanManager] LEVEL UP! Clan is now level {level}. 🎉");
        // TODO: trigger UI event, unlock upgrades every 5 levels
    }

    // ---- Role assignment ----

    public bool ReassignFly(string flyId, FlyRole newRole)
    {
        var fly = _flies.FirstOrDefault(f => f.InstanceId == flyId);
        if (fly == null) return false;

        // Enforce ratio limits
        int newCount = _flies.Count(f => f.Role == newRole && f.IsAlive) + 1;
        float ratio  = (float)newCount / TotalFlies;
        float limit  = newRole == FlyRole.Scout   ? Constants.MAX_SCOUT_RATIO :
                       newRole == FlyRole.Soldier ? Constants.MAX_SOLDIER_RATIO :
                                                    Constants.MAX_WORKER_RATIO;
        if (ratio > limit) return false;

        fly.Role = newRole;
        return true;
    }

    FlyRole PickAutoRole()
    {
        // Auto-balance: assign to most needed role
        if (ScoutCount <= SoldierCount && ScoutCount <= WorkerCount) return FlyRole.Scout;
        if (SoldierCount <= WorkerCount) return FlyRole.Soldier;
        return FlyRole.Worker;
    }

    // ---- Boosters ----

    public void ActivateBooster(ActiveBooster booster)
    {
        _boosters.RemoveAll(b => b.BoosterDataId == booster.BoosterDataId);
        _boosters.Add(booster);
        Debug.Log($"[ClanManager] Booster activated: {booster.BoosterDataId}");
    }

    public float GetBoosterMultiplier(BoosterType type)
    {
        var active = _boosters.FirstOrDefault(b =>
            b.IsActive && b.BoosterDataId.Contains(type.ToString()));
        return active != null ? 2.0f : 1.0f; // TODO: read from BoosterData
    }

    // ---- Helpers ----

    void PruneDeadFlies() => _flies.RemoveAll(f => !f.IsAlive);

    FlyRoleData GetRoleData(FlyRole role) => role switch
    {
        FlyRole.Scout   => ScoutRoleData,
        FlyRole.Soldier => SoldierRoleData,
        FlyRole.Worker  => WorkerRoleData,
        _               => ScoutRoleData
    };

    public List<FlyInstance> GetSoldiers(int maxCount = Constants.BATTLE_SLOTS)
        => _flies.Where(f => f.Role == FlyRole.Soldier && f.IsAlive)
                 .Take(maxCount).ToList();

    // ---- Save / Load ----

    [System.Serializable]
    public class SaveData
    {
        public string ClanId, ClanName;
        public int    ClanXP, ClanLevel;
        public List<FlyInstance>   Flies;
        public List<EggBatch>      Eggs;
        public List<ActiveBooster> Boosters;
    }

    public SaveData GetSaveData() => new()
    {
        ClanId    = ClanId,   ClanName  = ClanName,
        ClanXP    = ClanXP,   ClanLevel = ClanLevel,
        Flies     = _flies,   Eggs      = _eggBatches,
        Boosters  = _boosters
    };

    public void LoadSaveData(SaveData data)
    {
        if (data == null) return;
        ClanId    = data.ClanId;    ClanName  = data.ClanName;
        ClanXP    = data.ClanXP;    ClanLevel = data.ClanLevel;
        _flies    = data.Flies    ?? new();
        _eggBatches = data.Eggs   ?? new();
        _boosters = data.Boosters ?? new();
    }
}

[System.Serializable]
public class EggBatch
{
    public string  PooInstanceId;
    public int     EggCount;
    public float   LayTime;
    public float   HatchDuration;
    public FlyRole Role;
    public bool    IsReady => UnityEngine.Time.time - LayTime >= HatchDuration;
}
