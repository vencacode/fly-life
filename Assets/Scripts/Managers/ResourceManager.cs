using UnityEngine;

/// <summary>
/// Tracks player resources: PooCoins (main currency) and Gems (premium).
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    // Resources
    public int PooCoins { get; private set; } = Constants.STARTING_CLAN_COINS;
    public int Gems     { get; private set; } = Constants.STARTING_GEMS;

    [Header("Passive income")]
    public float IncomeIntervalSeconds = 60f;
    float _nextIncomeTime;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (Time.time >= _nextIncomeTime)
        {
            int districts = TerritoryManager.Instance?.GetPlayerDistricts().Count ?? 0;
            if (districts > 0)
            {
                AddPooCoins(districts * 5);
                Debug.Log($"[ResourceManager] Passive income: +{districts * 5} PooCoins from {districts} districts.");
            }
            _nextIncomeTime = Time.time + IncomeIntervalSeconds;
        }
    }

    // ---- PooCoins ----
    public void AddPooCoins(int amount)
    {
        float seasonalBonus = SeasonalEventManager.Instance?.CoinMultiplier ?? 1f;
        int bonus = Mathf.RoundToInt(amount * (BonusManager.Instance?.ResourceMultiplier ?? 1f) * seasonalBonus);
        PooCoins += bonus;
        UIEventBus.EmitResourceChanged("PooCoins", PooCoins);
    }
    public bool SpendPooCoins(int amount)
    {
        if (PooCoins < amount) return false;
        PooCoins -= amount;
        UIEventBus.EmitResourceChanged("PooCoins", PooCoins);
        AchievementManager.Instance?.TrackCoinsSpent(amount);
        DailyMissionManager.Instance?.TrackSpendCoins(amount);
        return true;
    }

    // Backward-compatible aliases
    public void AddCoins(int amount) => AddPooCoins(amount);
    public bool SpendCoins(int amount) => SpendPooCoins(amount);

    // ---- Gems (premium) ----
    public void AddGems(int amount)
    {
        Gems += amount;
        UIEventBus.EmitResourceChanged("Gems", Gems);
        AchievementManager.Instance?.TrackGemsEarned(amount);
    }
    public bool SpendGems(int amount)
    {
        if (Gems < amount) return false;
        Gems -= amount;
        UIEventBus.EmitResourceChanged("Gems", Gems);
        return true;
    }

    // ---- Save / Load ----
    [System.Serializable]
    public class SaveData
    {
        public int PooCoins, Gems;
    }

    public SaveData GetSaveData() => new()
    {
        PooCoins = PooCoins,
        Gems     = Gems
    };

    public void LoadSaveData(SaveData data)
    {
        if (data == null) return;
        PooCoins = data.PooCoins;
        Gems     = data.Gems;
    }
}
