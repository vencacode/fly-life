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
        _flies.Add(CreateFly(FlyRole.Scout));
        _flies.Add(CreateFly(FlyRole.Soldier));
        _flies.Add(CreateFly(FlyRole.Worker));
        Debug.Log("[ClanManager] Starting flies spawned (Scout, Soldier, Worker). Welcome to Fly Life.");
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
        float seasonalXP = SeasonalEventManager.Instance?.XPMultiplier ?? 1f;
        int bonus = Mathf.RoundToInt(amount * (BonusManager.Instance?.XPMultiplier ?? 1f) * seasonalXP);
        ClanXP += bonus;
        int newLevel = Mathf.FloorToInt(ClanXP / (float)Constants.CLAN_XP_PER_LEVEL) + 1;
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
        int xpPerFly = won ? Constants.XP_PER_BATTLE_WIN : Constants.XP_PER_BATTLE_LOSS;
        foreach (var fly in _flies.Where(f => f.IsAlive))
        {
            fly.XP += xpPerFly;
            int newLevel = Mathf.FloorToInt(fly.XP / (float)Constants.XP_PER_LEVEL) + 1;
            if (newLevel > fly.Level)
            {
                fly.Level = newLevel;
                fly.Attack    = Mathf.RoundToInt(fly.Attack  * Constants.STAT_ATTACK_GROWTH);
                fly.Defense   = Mathf.RoundToInt(fly.Defense * Constants.STAT_DEFENSE_GROWTH);
                fly.MaxHealth = Mathf.RoundToInt(fly.MaxHealth * Constants.STAT_HEALTH_GROWTH);
                fly.CurrentHealth = fly.MaxHealth;
                Debug.Log($"[ClanManager] {fly.Role} levelled up to {fly.Level}!");
                UIEventBus.Emit(UIEvent.LevelUp, fly);
            }
        }
        // Also award clan-level XP
        AddXP(won ? Constants.XP_PER_BATTLE_WIN : Constants.XP_PER_BATTLE_LOSS);
    }

    // ---- Training ----

    public bool TrainFly(string flyId)
    {
        var fly = _flies.FirstOrDefault(f => f.InstanceId == flyId);
        if (fly == null) return false;
        if (ResourceManager.Instance == null || !ResourceManager.Instance.SpendPooCoins(Constants.TRAINING_COST))
            return false;

        fly.XP += Constants.TRAINING_XP;
        int newLevel = Mathf.FloorToInt(fly.XP / (float)Constants.XP_PER_LEVEL) + 1;
        if (newLevel > fly.Level)
        {
            fly.Level = newLevel;
            fly.Attack    = Mathf.RoundToInt(fly.Attack  * Constants.STAT_ATTACK_GROWTH);
            fly.Defense   = Mathf.RoundToInt(fly.Defense * Constants.STAT_DEFENSE_GROWTH);
            fly.MaxHealth = Mathf.RoundToInt(fly.MaxHealth * Constants.STAT_HEALTH_GROWTH);
            fly.CurrentHealth = fly.MaxHealth;
            UIEventBus.Emit(UIEvent.LevelUp, fly);
        }
        Debug.Log($"[ClanManager] Trained {fly.Role}: +{Constants.TRAINING_XP} XP (now {fly.XP})");
        DailyMissionManager.Instance?.TrackTrainFly();
        return true;
    }

    // ---- Promotion ----

    public bool PromoteFly(string flyId)
    {
        var fly = _flies.FirstOrDefault(f => f.InstanceId == flyId);
        if (fly == null || fly.IsElite || fly.Level < Constants.PROMOTION_LEVEL_REQ)
            return false;
        if (ResourceManager.Instance == null ||
            !ResourceManager.Instance.SpendPooCoins(Constants.PROMOTION_COST))
            return false;

        fly.IsElite = true;
        fly.Attack    = Mathf.RoundToInt(fly.Attack  * Constants.ELITE_STAT_MULTIPLIER);
        fly.Defense   = Mathf.RoundToInt(fly.Defense * Constants.ELITE_STAT_MULTIPLIER);
        fly.Speed     = Mathf.RoundToInt(fly.Speed   * Constants.ELITE_STAT_MULTIPLIER);
        fly.MaxHealth = Mathf.RoundToInt(fly.MaxHealth * Constants.ELITE_STAT_MULTIPLIER);
        fly.CurrentHealth = fly.MaxHealth;

        Debug.Log($"[ClanManager] Fly {flyId} promoted to ELITE! Stats boosted by {(Constants.ELITE_STAT_MULTIPLIER - 1f) * 100}%");
        ToastManager.Instance?.ShowToast($"{fly.Role} promoted to Elite!", ToastType.Success);
        AchievementManager.Instance?.TrackElitePromoted();
        DailyMissionManager.Instance?.TrackPromoteFly();
        UIEventBus.Emit(UIEvent.ClanUpdated);
        return true;
    }

    // ---- Breeding (Fusion) ----

    public FlyInstance BreedFlies(string flyId1, string flyId2)
    {
        var fly1 = _flies.FirstOrDefault(f => f.InstanceId == flyId1);
        var fly2 = _flies.FirstOrDefault(f => f.InstanceId == flyId2);
        if (fly1 == null || fly2 == null) return null;
        if (fly1.Role != fly2.Role) return null;
        if (fly1.Level < Constants.BREEDING_MIN_LEVEL || fly2.Level < Constants.BREEDING_MIN_LEVEL) return null;
        if (ResourceManager.Instance == null || !ResourceManager.Instance.SpendPooCoins(Constants.BREEDING_COST))
            return null;

        // Offspring: best of each stat + bonus
        var offspring = CreateFly(fly1.Role);
        offspring.Level = Mathf.Max(fly1.Level, fly2.Level);
        offspring.XP = offspring.Level * 100;
        offspring.Attack    = Mathf.RoundToInt(Mathf.Max(fly1.Attack, fly2.Attack) * (1f + Constants.BREEDING_STAT_BONUS));
        offspring.Defense   = Mathf.RoundToInt(Mathf.Max(fly1.Defense, fly2.Defense) * (1f + Constants.BREEDING_STAT_BONUS));
        offspring.Speed     = Mathf.RoundToInt(Mathf.Max(fly1.Speed, fly2.Speed) * (1f + Constants.BREEDING_STAT_BONUS));
        offspring.MaxHealth = Mathf.RoundToInt(Mathf.Max(fly1.MaxHealth, fly2.MaxHealth) * (1f + Constants.BREEDING_STAT_BONUS));
        offspring.CurrentHealth = offspring.MaxHealth;

        // Transfer elite status if either parent was elite
        offspring.IsElite = fly1.IsElite || fly2.IsElite;

        // Remove parents, add offspring
        _flies.Remove(fly1);
        _flies.Remove(fly2);
        _flies.Add(offspring);

        UIEventBus.Emit(UIEvent.FlyBred, offspring);
        UIEventBus.Emit(UIEvent.ClanUpdated);
        ToastManager.Instance?.ShowToast(
            $"New {offspring.Role} bred! ATK:{offspring.Attack} DEF:{offspring.Defense}",
            ToastType.Success);
        Debug.Log($"[ClanManager] Bred {fly1.InstanceId} + {fly2.InstanceId} → {offspring.InstanceId}");
        return offspring;
    }

    // ---- Stat Respec ----

    public bool RespecFly(string flyId)
    {
        var fly = _flies.FirstOrDefault(f => f.InstanceId == flyId);
        if (fly == null) return false;
        if (ResourceManager.Instance == null || !ResourceManager.Instance.SpendPooCoins(Constants.RESPEC_COST))
            return false;

        var data = GetRoleData(fly.Role);
        float levelMult = 1f + (fly.Level - 1) * 0.1f;
        float eliteMult = fly.IsElite ? Constants.ELITE_STAT_MULTIPLIER : 1f;
        float variance = 0.2f; // ±20%

        fly.Attack    = Mathf.RoundToInt(data.BaseAttack  * levelMult * eliteMult * Random.Range(1f - variance, 1f + variance));
        fly.Defense   = Mathf.RoundToInt(data.BaseDefense * levelMult * eliteMult * Random.Range(1f - variance, 1f + variance));
        fly.Speed     = Mathf.RoundToInt(data.BaseSpeed   * levelMult * eliteMult * Random.Range(1f - variance, 1f + variance));
        fly.MaxHealth = Mathf.RoundToInt(data.BaseHealth  * levelMult * eliteMult * Random.Range(1f - variance, 1f + variance));
        fly.CurrentHealth = fly.MaxHealth;

        UIEventBus.Emit(UIEvent.ClanUpdated);
        ToastManager.Instance?.ShowToast(
            $"Stats rerolled! ATK:{fly.Attack} DEF:{fly.Defense} SPD:{fly.Speed}",
            ToastType.Info);
        Debug.Log($"[ClanManager] Respec fly {flyId}: ATK={fly.Attack} DEF={fly.Defense} SPD={fly.Speed} HP={fly.MaxHealth}");
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

    void PruneDeadFlies()
    {
        // Never prune during active battle — EndBattle calls HealAllFlies to restore them
        if (BattleManager.Instance != null && BattleManager.Instance.State != BattleState.Idle)
            return;
        _flies.RemoveAll(f => !f.IsAlive);
        // Always keep at least one fly
        if (_flies.Count == 0)
            SpawnStartingFly();
    }

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
        ClanId    = string.IsNullOrEmpty(data.ClanId) ? "player_clan" : data.ClanId;
        ClanName  = string.IsNullOrEmpty(data.ClanName) ? "The Fly Lifers" : data.ClanName;
        ClanXP    = data.ClanXP;
        ClanLevel = Mathf.Max(1, data.ClanLevel);
        HasCustomClan = data.HasCustomClan;
        _flies    = data.Flies    ?? new();
        _eggBatches = data.Eggs   ?? new();
        _boosters = data.Boosters ?? new();

        // Ensure at least one fly exists
        if (_flies.Count == 0)
            SpawnStartingFly();

        // Heal all flies on load (no permadeath between sessions)
        HealAllFlies();
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
