using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Boss Rush mode: fight all captured boss districts back-to-back for bonus rewards.
/// Unlocked after beating 3+ boss districts.
/// </summary>
public class BossRushManager : MonoBehaviour
{
    public static BossRushManager Instance { get; private set; }

    public bool IsUnlocked => (AchievementManager.Instance?.GetCounter("bosses_defeated") ?? 0) >= 3;
    public bool IsActive { get; private set; }
    public int CurrentRound { get; private set; }
    public int TotalRounds { get; private set; }
    public int BonusCoinsEarned { get; private set; }
    public int BestStreak { get; private set; }

    List<string> _bossQueue = new();
    Action<object> _onBattleEnded;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        _onBattleEnded = data => OnBattleEnded(data);
        UIEventBus.On(UIEvent.BattleEnded, _onBattleEnded);
    }

    void OnDisable()
    {
        if (_onBattleEnded != null) UIEventBus.Off(UIEvent.BattleEnded, _onBattleEnded);
    }

    public bool StartBossRush()
    {
        if (!IsUnlocked || IsActive) return false;

        // Get all captured boss districts
        var bosses = TerritoryManager.Instance?.GetPlayerDistricts()
            .Where(d => d.IsBoss)
            .Select(d => d.Id)
            .ToList();

        if (bosses == null || bosses.Count == 0)
        {
            ToastManager.Instance?.ShowToast("No boss districts captured yet!", ToastType.Warning);
            return false;
        }

        _bossQueue = new List<string>(bosses);
        TotalRounds = _bossQueue.Count;
        CurrentRound = 0;
        BonusCoinsEarned = 0;
        IsActive = true;

        UIEventBus.Emit(UIEvent.BossRushStarted);
        Debug.Log($"[BossRush] Started! {TotalRounds} bosses to fight.");

        StartNextBattle();
        return true;
    }

    void StartNextBattle()
    {
        if (CurrentRound >= _bossQueue.Count)
        {
            EndBossRush(true);
            return;
        }

        string districtId = _bossQueue[CurrentRound];
        var district = TerritoryManager.Instance?.GetDistrict(districtId);
        if (district == null)
        {
            EndBossRush(true);
            return;
        }

        // Generate boss enemies (scaled difficulty)
        int enemyCount = 5;
        var enemies = BattleManager.GenerateEnemyFlies(enemyCount);
        // Scale up for boss rush (1.5x base stats)
        foreach (var e in enemies)
        {
            e.Attack    = Mathf.RoundToInt(e.Attack * 1.5f);
            e.Defense   = Mathf.RoundToInt(e.Defense * 1.5f);
            e.MaxHealth = Mathf.RoundToInt(e.MaxHealth * 1.5f);
            e.CurrentHealth = e.MaxHealth;
        }

        BattleManager.Instance?.PrepareBattle(districtId, enemies);
        GameManager.Instance?.LoadScene(Constants.SCENE_BATTLE);

        Debug.Log($"[BossRush] Round {CurrentRound + 1}/{TotalRounds}: {district.Name}");
    }

    void OnBattleEnded(object data)
    {
        if (!IsActive) return;

        bool won = data is bool b && b;
        if (won)
        {
            CurrentRound++;
            int roundBonus = Constants.BOSS_RUSH_ROUND_COINS_BASE + CurrentRound * Constants.BOSS_RUSH_ROUND_COINS_STEP;
            BonusCoinsEarned += roundBonus;
            ResourceManager.Instance?.AddPooCoins(roundBonus);

            // Chance for legendary drop each round
            if (UnityEngine.Random.value < Constants.BOSS_RUSH_LEGENDARY_CHANCE)
            {
                var legendaries = EquipmentRegistry.GetLegendaryItems();
                if (legendaries.Count > 0)
                {
                    string dropId = legendaries[UnityEngine.Random.Range(0, legendaries.Count)];
                    InventoryManager.Instance?.AddItem(dropId);
                    ToastManager.Instance?.ShowToast($"Boss Rush legendary drop: {dropId}!", ToastType.Reward);
                }
            }

            if (CurrentRound < TotalRounds)
            {
                // Small delay then start next (will happen after scene transition)
                // The BattleSceneController handles going back to MainMap,
                // so we queue the next battle
                StartNextBattle();
            }
            else
            {
                EndBossRush(true);
            }
        }
        else
        {
            EndBossRush(false);
        }
    }

    void EndBossRush(bool completed)
    {
        IsActive = false;

        if (CurrentRound > BestStreak)
            BestStreak = CurrentRound;

        string result = completed ? "COMPLETED" : $"Ended at round {CurrentRound}/{TotalRounds}";
        Debug.Log($"[BossRush] {result}! Earned {BonusCoinsEarned} bonus coins.");

        ToastManager.Instance?.ShowToast(
            $"Boss Rush {result}! +{BonusCoinsEarned} bonus coins",
            completed ? ToastType.Reward : ToastType.Info);

        UIEventBus.Emit(UIEvent.BossRushEnded, completed);
    }

    // ---- Save / Load ----

    [Serializable]
    public class SaveData
    {
        public int BestStreak;
    }

    public SaveData GetSaveData() => new() { BestStreak = BestStreak };

    public void LoadSaveData(SaveData data)
    {
        if (data == null) return;
        BestStreak = data.BestStreak;
    }
}
