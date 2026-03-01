using System.Collections.Generic;

/// <summary>
/// Static registry of all achievements with rewards.
/// </summary>
public static class AchievementRegistry
{
    public enum AchievementCategory { Combat, Territory, Clan, Economy, Collection }

    public struct AchievementDef
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public AchievementCategory Category;
        public int Target;           // e.g. win 10 battles
        public int GemReward;
        public string SkinReward;    // null if none
    }

    static readonly Dictionary<string, AchievementDef> _achievements = new()
    {
        ["first_blood"] = new()
        {
            Id = "first_blood", DisplayName = "First Blood",
            Description = "Win your first battle.",
            Category = AchievementCategory.Combat, Target = 1, GemReward = 10
        },
        ["battle_veteran"] = new()
        {
            Id = "battle_veteran", DisplayName = "Battle Veteran",
            Description = "Win 25 battles.",
            Category = AchievementCategory.Combat, Target = 25, GemReward = 50
        },
        ["warlord"] = new()
        {
            Id = "warlord", DisplayName = "Warlord",
            Description = "Win 100 battles.",
            Category = AchievementCategory.Combat, Target = 100, GemReward = 100, SkinReward = "crimson"
        },
        ["boss_slayer"] = new()
        {
            Id = "boss_slayer", DisplayName = "Boss Slayer",
            Description = "Defeat 5 boss districts.",
            Category = AchievementCategory.Combat, Target = 5, GemReward = 60
        },
        ["landlord"] = new()
        {
            Id = "landlord", DisplayName = "Landlord",
            Description = "Own 5 districts.",
            Category = AchievementCategory.Territory, Target = 5, GemReward = 25
        },
        ["empire_builder"] = new()
        {
            Id = "empire_builder", DisplayName = "Empire Builder",
            Description = "Own 15 districts.",
            Category = AchievementCategory.Territory, Target = 15, GemReward = 75
        },
        ["world_domination"] = new()
        {
            Id = "world_domination", DisplayName = "World Domination",
            Description = "Own all 49 districts.",
            Category = AchievementCategory.Territory, Target = 49, GemReward = 200, SkinReward = "royal"
        },
        ["swarm_leader"] = new()
        {
            Id = "swarm_leader", DisplayName = "Swarm Leader",
            Description = "Have 20 flies in your clan.",
            Category = AchievementCategory.Clan, Target = 20, GemReward = 30
        },
        ["elite_force"] = new()
        {
            Id = "elite_force", DisplayName = "Elite Force",
            Description = "Promote 5 flies to Elite.",
            Category = AchievementCategory.Clan, Target = 5, GemReward = 60
        },
        ["clan_level_10"] = new()
        {
            Id = "clan_level_10", DisplayName = "Rising Power",
            Description = "Reach clan level 10.",
            Category = AchievementCategory.Clan, Target = 10, GemReward = 50
        },
        ["big_spender"] = new()
        {
            Id = "big_spender", DisplayName = "Big Spender",
            Description = "Spend 5000 PooCoins total.",
            Category = AchievementCategory.Economy, Target = 5000, GemReward = 40
        },
        ["gem_collector"] = new()
        {
            Id = "gem_collector", DisplayName = "Gem Collector",
            Description = "Accumulate 500 gems (lifetime).",
            Category = AchievementCategory.Economy, Target = 500, GemReward = 50
        },
        ["fashionista"] = new()
        {
            Id = "fashionista", DisplayName = "Fashionista",
            Description = "Unlock 5 fly skins.",
            Category = AchievementCategory.Collection, Target = 5, GemReward = 35
        },
        ["legendary_hunter"] = new()
        {
            Id = "legendary_hunter", DisplayName = "Legendary Hunter",
            Description = "Own 3 legendary items.",
            Category = AchievementCategory.Collection, Target = 3, GemReward = 80, SkinReward = "golden"
        },
    };

    public static AchievementDef? Get(string id)
        => _achievements.TryGetValue(id, out var def) ? def : null;

    public static IReadOnlyDictionary<string, AchievementDef> All => _achievements;
}
