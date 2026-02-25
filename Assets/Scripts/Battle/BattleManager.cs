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

    private List<FlyInstance> _playerSlots   = new(new FlyInstance[Constants.BATTLE_SLOTS]);
    private List<FlyInstance> _enemySlots    = new(new FlyInstance[Constants.BATTLE_SLOTS]);
    private List<EquipmentData> _pendingDrops = new();
    private float _dropWindowEnd;

    public IReadOnlyList<FlyInstance> PlayerSlots => _playerSlots;
    public IReadOnlyList<FlyInstance> EnemySlots  => _enemySlots;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ---- Setup ----

    public void StartBattle(List<FlyInstance> enemyFlies)
    {
        var playerSoldiers = ClanManager.Instance.GetSoldiers(Constants.BATTLE_SLOTS);
        for (int i = 0; i < Constants.BATTLE_SLOTS; i++)
        {
            _playerSlots[i] = i < playerSoldiers.Count ? playerSoldiers[i] : null;
            _enemySlots[i]  = i < enemyFlies.Count     ? enemyFlies[i]     : null;
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
        for (int i = 0; i < Constants.BATTLE_SLOTS; i++)
        {
            var p = _playerSlots[i];
            var e = _enemySlots[i];
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

    // ---- End ----

    void EndBattle(bool playerWon)
    {
        SetState(BattleState.BattleEnd);
        int xp = playerWon ? Constants.XP_PER_BATTLE_WIN : Constants.XP_PER_BATTLE_LOSS;
        ClanManager.Instance?.AddXP(xp);

        if (playerWon)
            ResourceManager.Instance?.AddCoins(Random.Range(10, 30));

        UIEventBus.Emit(UIEvent.BattleEnded, playerWon);
        Debug.Log($"[Battle] Battle ended. Player {(playerWon ? "WON 🎉" : "LOST 😢")}");
    }

    void SetState(BattleState s)
    {
        State = s;
        UIEventBus.Emit(UIEvent.BattleStateChanged, s);
    }
}

public enum BattleState { Idle, RoundStart, PlayerAction, EnemyAction, ItemDrop, RoundEnd, BattleEnd }
