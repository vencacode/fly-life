using System.Collections.Generic;

/// <summary>
/// Pool of possible daily mission definitions.
/// DailyMissionManager picks 3 random missions each day.
/// </summary>
public static class MissionRegistry
{
    public enum MissionType
    {
        WinBattles,
        CaptureDistricts,
        TrainFlies,
        SpendCoins,
        CollectPoo,
        HatchEggs,
        UpgradeGear,
        PromoteFly
    }

    public struct MissionDef
    {
        public string Id;
        public string DisplayTemplate; // e.g. "Win {0} battles"
        public MissionType Type;
        public int MinTarget, MaxTarget;
        public int CoinReward;
        public int GemReward;
    }

    static readonly List<MissionDef> _pool = new()
    {
        new()
        {
            Id = "win_battles", DisplayTemplate = "Win {0} battles",
            Type = MissionType.WinBattles,
            MinTarget = 1, MaxTarget = 5, CoinReward = 100, GemReward = 5
        },
        new()
        {
            Id = "capture_districts", DisplayTemplate = "Capture {0} districts",
            Type = MissionType.CaptureDistricts,
            MinTarget = 1, MaxTarget = 3, CoinReward = 150, GemReward = 10
        },
        new()
        {
            Id = "train_flies", DisplayTemplate = "Train {0} flies",
            Type = MissionType.TrainFlies,
            MinTarget = 2, MaxTarget = 5, CoinReward = 80, GemReward = 3
        },
        new()
        {
            Id = "spend_coins", DisplayTemplate = "Spend {0} PooCoins",
            Type = MissionType.SpendCoins,
            MinTarget = 200, MaxTarget = 500, CoinReward = 120, GemReward = 5
        },
        new()
        {
            Id = "collect_poo", DisplayTemplate = "Discover {0} poos",
            Type = MissionType.CollectPoo,
            MinTarget = 3, MaxTarget = 8, CoinReward = 60, GemReward = 3
        },
        new()
        {
            Id = "hatch_eggs", DisplayTemplate = "Hatch {0} egg batches",
            Type = MissionType.HatchEggs,
            MinTarget = 1, MaxTarget = 3, CoinReward = 90, GemReward = 5
        },
        new()
        {
            Id = "upgrade_gear", DisplayTemplate = "Upgrade gear {0} times",
            Type = MissionType.UpgradeGear,
            MinTarget = 1, MaxTarget = 3, CoinReward = 100, GemReward = 5
        },
        new()
        {
            Id = "promote_fly", DisplayTemplate = "Promote {0} fly to Elite",
            Type = MissionType.PromoteFly,
            MinTarget = 1, MaxTarget = 1, CoinReward = 200, GemReward = 15
        },
    };

    public static IReadOnlyList<MissionDef> Pool => _pool;
}
