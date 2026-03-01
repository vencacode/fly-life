using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Controls the 5v5 turn-based battle minigame.
/// Flow: Setup → RoundStart → Actions → ItemDropWindow → RoundEnd → repeat → BattleEnd
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("Config")]
    public EquipmentData[] PossibleDrops;   // items that can drop during battle

    // Battle state
    public BattleState State { get; private set; } = BattleState.Idle;
    public int Round         { get; private set; } = 0;
    public string PendingDistrictId { get; private set; }

    private List<FlyInstance> _playerSlots   = new(new FlyInstance[Constants.BATTLE_SLOTS]);
    private List<FlyInstance> _enemySlots    = new(new FlyInstance[Constants.BATTLE_SLOTS]);
    private List<FlyInstance> _pendingEnemies;
    private List<FlyInstance> _pendingPlayerFlies;
    private System.Action<bool, string> _onBattleComplete;
    private List<EquipmentData> _pendingDrops = new();
    private float _dropWindowEnd;

    public IReadOnlyList<FlyInstance> PlayerSlots => _playerSlots;
    public IReadOnlyList<FlyInstance> EnemySlots  => _enemySlots;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    /// <summary>Store pending battle data. Called from DistrictInfoPanel before scene load.</summary>
    public void PrepareBattle(string districtId, List<FlyInstance> enemies)
    {
        PendingDistrictId = districtId;
        _pendingEnemies = enemies;
        _pendingPlayerFlies = null;
        // Also set BattleContext for backward compatibility (BattleUI reads it)
        BattleContext.DistrictId = districtId;
        BattleContext.EnemyFlies = enemies;
    }

    /// <summary>Store pending battle data with player-selected flies.</summary>
    public void PrepareBattle(string districtId, List<FlyInstance> enemies, List<FlyInstance> playerFlies)
    {
        PrepareBattle(districtId, enemies);
        _pendingPlayerFlies = playerFlies;
    }

    /// <summary>Called by BattleSceneController to kick off the fight.</summary>
    public void ExecutePendingBattle(System.Action<bool, string> onComplete)
    {
        _onBattleComplete = onComplete;
        var enemies = _pendingEnemies ?? BattleContext.EnemyFlies;
        if (enemies == null || enemies.Count == 0)
        {
            Debug.LogError("[BattleManager] No pending enemies to fight!");
            onComplete?.Invoke(false, PendingDistrictId);
            return;
        }
        StartBattle(enemies);
    }

    // ---- Setup ----

    public void StartBattle(List<FlyInstance> enemyFlies)
    {
        int slots = Mathf.Max(Constants.BATTLE_SLOTS, enemyFlies.Count);
        // Use player-selected flies if available, otherwise fall back to auto-picking soldiers
        var playerFlies = _pendingPlayerFlies ?? ClanManager.Instance.GetSoldiers(slots);
        _pendingPlayerFlies = null;

        _playerSlots.Clear();
        _enemySlots.Clear();
        for (int i = 0; i < slots; i++)
        {
            _playerSlots.Add(i < playerFlies.Count ? playerFlies[i] : null);
            _enemySlots.Add(i < enemyFlies.Count   ? enemyFlies[i]  : null);
        }
        Round = 0;
        SetState(BattleState.RoundStart);
        StartCoroutine(BattleLoop());
    }

    // ---- Core battle loop ----

    IEnumerator BattleLoop()
    {
        while (State != BattleState.BattleEnd)
        {
            Round++;
            SetState(BattleState.RoundStart);
            UIEventBus.Emit(UIEvent.BattleRoundStart, Round);
            yield return new WaitForSeconds(0.5f);

            // Resolve attacks slot by slot, sorted by speed
            var actions = BuildActionOrder();
            foreach (var (attacker, target) in actions)
            {
                if (attacker == null || !attacker.IsAlive) continue;
                if (target  == null || !target.IsAlive)   continue;
                ResolveAttack(attacker, target);
                yield return new WaitForSeconds(0.3f);
            }

            // Item drop window
            TrySpawnItemDrop();
            if (_pendingDrops.Count > 0)
            {
                SetState(BattleState.ItemDrop);
                _dropWindowEnd = Time.time + Constants.ITEM_DROP_WINDOW_SEC;
                yield return new WaitForSeconds(Constants.ITEM_DROP_WINDOW_SEC);
                _pendingDrops.Clear();
            }

            SetState(BattleState.RoundEnd);

            // Check win/loss
            bool playerDead = _playerSlots.All(f => f == null || !f.IsAlive);
            bool enemyDead  = _enemySlots.All(f  => f == null || !f.IsAlive);

            if (playerDead || enemyDead)
            {
                EndBattle(!playerDead);
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
            SetState(BattleState.RoundStart);
        }
    }

    // ---- Attack resolution ----

    List<(FlyInstance attacker, FlyInstance target)> BuildActionOrder()
    {
        var pairs = new List<(FlyInstance, FlyInstance, int speed)>();
        int slots = Mathf.Max(_playerSlots.Count, _enemySlots.Count);
        for (int i = 0; i < slots; i++)
        {
            var p = i < _playerSlots.Count ? _playerSlots[i] : null;
            var e = i < _enemySlots.Count  ? _enemySlots[i]  : null;
            if (p != null && p.IsAlive) pairs.Add((p, e, p.Speed));
            if (e != null && e.IsAlive) pairs.Add((e, p, e.Speed));
        }
        return pairs.OrderByDescending(x => x.speed)
                    .Select(x => (x.Item1, x.Item2))
                    .ToList();
    }

    void ResolveAttack(FlyInstance attacker, FlyInstance target)
    {
        int damage = Mathf.Max(1, attacker.Attack - target.Defense);
        target.CurrentHealth = Mathf.Max(0, target.CurrentHealth - damage);
        Debug.Log($"[Battle] {attacker.Role} deals {damage} to {target.Role}. HP: {target.CurrentHealth}/{target.MaxHealth}");
        UIEventBus.Emit(UIEvent.BattleAttack, (attacker, target, damage));
    }

    // ---- Item drops ----

    void TrySpawnItemDrop()
    {
        if (PossibleDrops == null || PossibleDrops.Length == 0) return;
        if (Random.value > 0.4f) return; // 40% chance per round

        var drop = PossibleDrops[Random.Range(0, PossibleDrops.Length)];
        _pendingDrops.Add(drop);
        UIEventBus.Emit(UIEvent.BattleItemDropped, drop);
    }

    /// <summary>Player taps item drop in battle UI.</summary>
    public bool TryGrabDrop(EquipmentData item)
    {
        if (Time.time > _dropWindowEnd || !_pendingDrops.Contains(item)) return false;
        _pendingDrops.Remove(item);
        // Add to player inventory
        Debug.Log($"[Battle] Player grabbed {item.ItemName}!");
        UIEventBus.Emit(UIEvent.BattleItemGrabbed, item);
        return true;
    }

    // ---- Enemy generation ----

    public static List<FlyInstance> GenerateEnemyFlies(int count)
    {
        int level = ClanManager.Instance?.ClanLevel ?? 1;
        var enemies = new List<FlyInstance>();
        for (int i = 0; i < count; i++)
        {
            int baseAtk = 8 + level * 2 + Random.Range(-2, 3);
            int baseDef = 6 + level * 2 + Random.Range(-2, 3);
            int baseSpd = 8 + level     + Random.Range(-2, 3);
            int baseHp  = 40 + level * 5 + Random.Range(-5, 6);
            enemies.Add(new FlyInstance
            {
                InstanceId    = $"enemy_{i}_{System.Guid.NewGuid().ToString()[..8]}",
                Role          = FlyRole.Soldier,
                Level         = level,
                Attack        = Mathf.Max(1, baseAtk),
                Defense       = Mathf.Max(1, baseDef),
                Speed         = Mathf.Max(1, baseSpd),
                MaxHealth     = Mathf.Max(10, baseHp),
                CurrentHealth = Mathf.Max(10, baseHp)
            });
        }
        return enemies;
    }

    // ---- End ----

    void EndBattle(bool playerWon)
    {
        SetState(BattleState.BattleEnd);

        // Per-fly XP + clan XP (replaces flat AddXP)
        ClanManager.Instance?.AwardBattleXP(playerWon);
        // Full heal after every battle — no permadeath
        ClanManager.Instance?.HealAllFlies();

        if (playerWon)
        {
            // Check if this is a boss district
            var district = TerritoryManager.Instance?.GetDistrict(PendingDistrictId);
            bool isBoss = district?.IsBoss ?? false;

            ResourceManager.Instance?.AddCoins(isBoss ? Random.Range(40, 80) : Random.Range(10, 30));

            // Boss: guaranteed gear drop. Normal: 40% chance
            float dropChance = isBoss ? 1f : 0.4f;
            if (Random.value < dropChance)
            {
                var droppable = EquipmentRegistry.GetDroppableItems();
                if (droppable.Count > 0)
                {
                    string dropId = droppable[Random.Range(0, droppable.Count)];
                    InventoryManager.Instance?.AddItem(dropId);
                    UIEventBus.Emit(UIEvent.BattleItemDropped, dropId);
                    Debug.Log($"[Battle] {(isBoss ? "BOSS" : "Gear")} drop: {dropId}!");
                }
            }
        }

        UIEventBus.Emit(UIEvent.BattleEnded, playerWon);
        Debug.Log($"[Battle] Battle ended. Player {(playerWon ? "WON" : "LOST")}");

        // Notify BattleSceneController (handles district claiming + scene transition)
        _onBattleComplete?.Invoke(playerWon, PendingDistrictId);
        _pendingEnemies = null;
    }

    void SetState(BattleState s)
    {
        State = s;
        UIEventBus.Emit(UIEvent.BattleStateChanged, s);
    }
}

public enum BattleState { Idle, RoundStart, PlayerAction, EnemyAction, ItemDrop, RoundEnd, BattleEnd }
