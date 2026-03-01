using System.Collections.Generic;

/// <summary>
/// Static registry of all cosmetic fly skins.
/// Skins are purely visual — no stat changes.
/// </summary>
public static class SkinRegistry
{
    public struct SkinDef
    {
        public string SkinId;
        public string DisplayName;
        public string Description;
        public string ColorHex;        // Tint color for fly sprite
        public SkinSource Source;
        public int GemCost;            // 0 if not purchasable
        public string UnlockCondition; // human-readable
    }

    public enum SkinSource { Default, GemShop, BattleReward, LevelReward, Seasonal }

    static readonly Dictionary<string, SkinDef> _skins = new()
    {
        ["default"] = new SkinDef
        {
            SkinId = "default", DisplayName = "Classic Fly", Description = "The OG. Nothing fancy.",
            ColorHex = "#888888", Source = SkinSource.Default, GemCost = 0,
            UnlockCondition = "Already unlocked"
        },
        ["golden"] = new SkinDef
        {
            SkinId = "golden", DisplayName = "Golden Fly", Description = "Dripping in PooGold.",
            ColorHex = "#FFD700", Source = SkinSource.GemShop, GemCost = 100,
            UnlockCondition = "Purchase for 100 Gems"
        },
        ["neon"] = new SkinDef
        {
            SkinId = "neon", DisplayName = "Neon Buzz", Description = "Glow in the dark. Rave fly.",
            ColorHex = "#39FF14", Source = SkinSource.GemShop, GemCost = 75,
            UnlockCondition = "Purchase for 75 Gems"
        },
        ["royal"] = new SkinDef
        {
            SkinId = "royal", DisplayName = "Royal Purple", Description = "Bow before the king fly.",
            ColorHex = "#7B2FBE", Source = SkinSource.GemShop, GemCost = 150,
            UnlockCondition = "Purchase for 150 Gems"
        },
        ["crimson"] = new SkinDef
        {
            SkinId = "crimson", DisplayName = "Crimson Warrior", Description = "Battle-hardened and fierce.",
            ColorHex = "#DC143C", Source = SkinSource.BattleReward, GemCost = 0,
            UnlockCondition = "Win 50 battles"
        },
        ["ice"] = new SkinDef
        {
            SkinId = "ice", DisplayName = "Frost Wing", Description = "Cool as a frozen turd.",
            ColorHex = "#87CEEB", Source = SkinSource.LevelReward, GemCost = 0,
            UnlockCondition = "Reach Clan Level 10"
        },
        ["shadow"] = new SkinDef
        {
            SkinId = "shadow", DisplayName = "Shadow Fly", Description = "Moves unseen. Spooky.",
            ColorHex = "#1A1A2E", Source = SkinSource.GemShop, GemCost = 200,
            UnlockCondition = "Purchase for 200 Gems"
        },
        ["candy"] = new SkinDef
        {
            SkinId = "candy", DisplayName = "Candy Coat", Description = "Sweet and sticky.",
            ColorHex = "#FF69B4", Source = SkinSource.Seasonal, GemCost = 0,
            UnlockCondition = "Seasonal event reward"
        },
    };

    public static SkinDef? Get(string skinId)
        => _skins.TryGetValue(skinId ?? "default", out var def) ? def : null;

    public static IReadOnlyDictionary<string, SkinDef> All => _skins;

    public static List<SkinDef> GetShopSkins()
    {
        var list = new List<SkinDef>();
        foreach (var kv in _skins)
            if (kv.Value.Source == SkinSource.GemShop)
                list.Add(kv.Value);
        return list;
    }
}
