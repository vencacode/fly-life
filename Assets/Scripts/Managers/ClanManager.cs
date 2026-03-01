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
    public string ClanName { get; private set; } = "The Fly Lifers";
    public bool   HasCustomClan { get; private set; } = false;
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
        DontDestroyOnLoad(gameObject);
        EnsureRoleData();
    }

    /// <summary>All role data assets (Scout, Soldier, Worker).</summary>
    public FlyRoleData[] AllRoles => new[] { ScoutRoleData, SoldierRoleData, WorkerRoleData };

    void EnsureRoleData()
    {
        ScoutRoleData   ??= CreateRoleData("Scout",   FlyRole.Scout,   8,  5, 15, 40, scoutFind: 2.0f);
        SoldierRoleData ??= CreateRoleData("Soldier", FlyRole.Soldier, 15, 12, 8, 80, soldierAtk: 1.5f);
        WorkerRoleData  ??= CreateRoleData("Worker",  FlyRole.Worker,  5,  8,  6, 60, workerHatch: 2.0f);
    }

    static FlyRoleData CreateRoleData(string name, FlyRole role, int atk, int def, int spd, int hp,
        float scoutFind = 1f, float soldierAtk = 1f, float workerHatch = 1f)
    {
        var data = ScriptableObject.CreateInstance<FlyRoleData>();
        data.name               = name;
        data.RoleName           = name;
        data.Role               = role;
        data.BaseAttack         = atk;
        data.BaseDefense        = def;
        data.BaseSpeed          = spd;
        data.BaseHealth         = hp;
        data.ScoutFindMultiplier      = scoutFind;
        data.SoldierAttackMultiplier  = soldierAtk;
        data.WorkerHatchMultiplier    = workerHatch;
        return data;
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

        float hatchMultiplier = GetBoosterMultiplier(BoosterType.HatchSpeed)
                              * (BonusManager.Instance?.HatchSpeedMultiplier ?? 1f);
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

    // ---- Clan identity ----

    public void CreateClan(string name)
    {
        ClanName = name;
        ClanId = "clan_" + name.ToLower().Replace(" ", "_") + "_" + Random.Range(1000, 9999);
        HasCustomClan = true;
        UIEventBus.Emit(UIEvent.ClanUpdated);
        Debug.Log($"[ClanManager] Clan created: {ClanName} ({ClanId})");
    }

    public void JoinClan(string clanId, string clanName)
    {
        ClanId = clanId;
        ClanName = clanName;
        HasCustomClan = true;
        UIEventBus.Emit(UIEvent.ClanUpdated);
        Debug.Log($"[ClanManager] Joined clan: {ClanName} ({ClanId})");
    }

    // ---- XP & Levelling ----

    public void AddXP(int amount)
    {
        int bonus = Mathf.RoundToInt(amount * (BonusManager.Instance?.XPMultiplier ?? 1f));
        ClanXP += bonus;
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

    // ---- Post-battle ----

    public void HealAllFlies()
    {
        foreach (var fly in _flies)
            fly.CurrentHealth = fly.MaxHealth;
    }

    public void AwardBattleXP(bool won)
    {
        int xpPerFly = won ? 50 : 10;
        foreach (var fly in _flies.Where(f => f.IsAlive))
        {
            fly.XP += xpPerFly;
            int newLevel = Mathf.FloorToInt(fly.XP / 100f) + 1;
            if (newLevel > fly.Level)
            {
                fly.Level = newLevel;
                fly.Attack    = Mathf.RoundToInt(fly.Attack  * 1.1f);
                fly.Defense   = Mathf.RoundToInt(fly.Defense * 1.1f);
                fly.MaxHealth = Mathf.RoundToInt(fly.MaxHealth * 1.15f);
                fly.CurrentHealth = fly.MaxHealth;
                Debug.Log($"[ClanManager] {fly.Role} levelled up to {fly.Level}!");
                UIEventBus.Emit(UIEvent.LevelUp, fly);
            }
        }
        // Also award clan-level XP
        AddXP(won ? 50 : 10);
    }

    // ---- Training ----

    public bool TrainFly(string flyId)
    {
        var fly = _flies.FirstOrDefault(f => f.InstanceId == flyId);
        if (fly == null) return false;
        if (ResourceManager.Instance == null || !ResourceManager.Instance.SpendPooCoins(Constants.TRAINING_COST))
            return false;

        fly.XP += Constants.TRAINING_XP;
        int newLevel = Mathf.FloorToInt(fly.XP / 100f) + 1;
        if (newLevel > fly.Level)
        {
            fly.Level = newLevel;
            fly.Attack    = Mathf.RoundToInt(fly.Attack  * 1.1f);
            fly.Defense   = Mathf.RoundToInt(fly.Defense * 1.1f);
            fly.MaxHealth = Mathf.RoundToInt(fly.MaxHealth * 1.15f);
            fly.CurrentHealth = fly.MaxHealth;
            UIEventBus.Emit(UIEvent.LevelUp, fly);
        }
        Debug.Log($"[ClanManager] Trained {fly.Role}: +{Constants.TRAINING_XP} XP (now {fly.XP})");
        return true;
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

    public List<FlyInstance> GetAllFlies() => new(_flies);

    public List<FlyInstance> GetSoldiers(int maxCount = Constants.BATTLE_SLOTS)
        => _flies.Where(f => f.Role == FlyRole.Soldier && f.IsAlive)
                 .Take(maxCount).ToList();

    // ---- Deployment ----

    public List<FlyInstance> GetAvailableFlies(FlyRole role)
        => _flies.Where(f => f.Role == role && f.IsAlive && !f.IsDeployed).ToList();

    public int GetAvailableCount(FlyRole role)
        => _flies.Count(f => f.Role == role && f.IsAlive && !f.IsDeployed);

    public List<FlyInstance> GetDeployedFlies(string districtId)
        => _flies.Where(f => f.IsAlive && f.DeployedDistrictId == districtId).ToList();

    public int GetDeployedCount(string districtId)
        => _flies.Count(f => f.IsAlive && f.DeployedDistrictId == districtId);

    public int DeployFlies(string districtId, FlyRole role, int count)
    {
        var available = GetAvailableFlies(role);
        int toDeploy = Mathf.Min(count, available.Count);
        for (int i = 0; i < toDeploy; i++)
            available[i].DeployedDistrictId = districtId;
        if (toDeploy > 0)
            Debug.Log($"[ClanManager] Deployed {toDeploy} {role}s to {districtId}");
        return toDeploy;
    }

    public void RecallFlies(string districtId)
    {
        int count = 0;
        foreach (var f in _flies)
            if (f.DeployedDistrictId == districtId)
            {
                f.DeployedDistrictId = null;
                count++;
            }
        if (count > 0)
            Debug.Log($"[ClanManager] Recalled {count} flies from {districtId}");
    }

    // ---- Save / Load ----

    [System.Serializable]
    public class SaveData
    {
        public string ClanId, ClanName;
        public int    ClanXP, ClanLevel;
        public bool   HasCustomClan;
        public List<FlyInstance>   Flies;
        public List<EggBatch>      Eggs;
        public List<ActiveBooster> Boosters;
    }

    public SaveData GetSaveData() => new()
    {
        ClanId    = ClanId,   ClanName  = ClanName,
        ClanXP    = ClanXP,   ClanLevel = ClanLevel,
        HasCustomClan = HasCustomClan,
        Flies     = _flies,   Eggs      = _eggBatches,
        Boosters  = _boosters
    };

    public void LoadSaveData(SaveData data)
    {
        if (data == null) return;
        ClanId    = data.ClanId;    ClanName  = data.ClanName;
        ClanXP    = data.ClanXP;    ClanLevel = data.ClanLevel;
        HasCustomClan = data.HasCustomClan;
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
