using System.Collections.Generic;

/// <summary>
/// Static registry of all equipment items.
/// Used by InventoryManager to look up stats by item ID (string name).
/// No ScriptableObject assets needed — all defined in code.
/// </summary>
public static class EquipmentRegistry
{
    public struct EquipmentDef
    {
        public string ItemName;
        public EquipmentSlot Slot;
        public EquipmentRarity Rarity;
        public int AttackBonus, DefenseBonus, SpeedBonus, HealthBonus;
        public SpecialEffect Effect;
        public float EffectValue;
        public string Description;
    }

    static readonly Dictionary<string, EquipmentDef> _items = new()
    {
        ["PooThrower"] = new EquipmentDef
        {
            ItemName = "Poo Thrower", Slot = EquipmentSlot.Weapon, Rarity = EquipmentRarity.Uncommon,
            AttackBonus = 8, Description = "Flings poo at all enemies. Disgusting but effective.",
            Effect = SpecialEffect.AOESplash, EffectValue = 0.5f
        },
        ["NailHelmet"] = new EquipmentDef
        {
            ItemName = "Nail Helmet", Slot = EquipmentSlot.Head, Rarity = EquipmentRarity.Uncommon,
            DefenseBonus = 6, Description = "Pointy! Reflects 20% damage back at attackers.",
            Effect = SpecialEffect.ReflectDamage, EffectValue = 0.2f
        },
        ["BottleCapShield"] = new EquipmentDef
        {
            ItemName = "Bottle Cap Shield", Slot = EquipmentSlot.Offhand, Rarity = EquipmentRarity.Rare,
            DefenseBonus = 10, Description = "Blocks the first hit completely. Classic fly engineering.",
            Effect = SpecialEffect.BlockFirstHit, EffectValue = 1f
        },
        ["SugarCubeBoots"] = new EquipmentDef
        {
            ItemName = "Sugar Cube Boots", Slot = EquipmentSlot.Feet, Rarity = EquipmentRarity.Uncommon,
            SpeedBonus = 8, Description = "Sweet kicks! Always act first in battle.",
            Effect = SpecialEffect.ActFirst, EffectValue = 1f
        },
        // ---- Legendary tier ----
        ["GoldenDungScepter"] = new EquipmentDef
        {
            ItemName = "Golden Dung Scepter", Slot = EquipmentSlot.Weapon,
            Rarity = EquipmentRarity.Legendary,
            AttackBonus = 20, SpeedBonus = 5,
            Description = "The holy grail of poo weapons. Heals 25% of damage dealt.",
            Effect = SpecialEffect.LifeSteal, EffectValue = 0.25f
        },
        ["DiamondWings"] = new EquipmentDef
        {
            ItemName = "Diamond Wings", Slot = EquipmentSlot.Wings,
            Rarity = EquipmentRarity.Legendary,
            SpeedBonus = 15, DefenseBonus = 8,
            Description = "Impossibly fast. 30% chance to strike twice.",
            Effect = SpecialEffect.DoubleStrike, EffectValue = 0.3f
        },
        ["CrownOfTheSwarm"] = new EquipmentDef
        {
            ItemName = "Crown of the Swarm", Slot = EquipmentSlot.Head,
            Rarity = EquipmentRarity.Legendary,
            DefenseBonus = 12, HealthBonus = 30,
            Description = "The swarm obeys. All allies gain +20% ATK.",
            Effect = SpecialEffect.RallyAllies, EffectValue = 0.2f
        },
        ["PhoenixCarapace"] = new EquipmentDef
        {
            ItemName = "Phoenix Carapace", Slot = EquipmentSlot.Body,
            Rarity = EquipmentRarity.Legendary,
            DefenseBonus = 18, HealthBonus = 20,
            Description = "Survive one lethal blow. Refuse to die.",
            Effect = SpecialEffect.Invincible, EffectValue = 1f
        },
        // ---- Accessory slot (elite only) ----
        ["LuckyCharm"] = new EquipmentDef
        {
            ItemName = "Lucky Charm", Slot = EquipmentSlot.Accessory,
            Rarity = EquipmentRarity.Rare,
            AttackBonus = 3, DefenseBonus = 3, SpeedBonus = 3, HealthBonus = 10,
            Description = "A little bit of everything. Elite flies only.",
            Effect = SpecialEffect.None, EffectValue = 0f
        },
        ["VeteranBadge"] = new EquipmentDef
        {
            ItemName = "Veteran Badge", Slot = EquipmentSlot.Accessory,
            Rarity = EquipmentRarity.Epic,
            AttackBonus = 8, HealthBonus = 20,
            Description = "Battle-tested. Elite flies only.",
            Effect = SpecialEffect.None, EffectValue = 0f
        },
    };

    public static EquipmentDef? Get(string itemId)
    {
        return _items.TryGetValue(itemId, out var def) ? def : null;
    }

    public static IReadOnlyDictionary<string, EquipmentDef> All => _items;

    /// <summary>Returns all non-legendary item IDs that can drop in normal battle.</summary>
    public static List<string> GetDroppableItems()
    {
        var list = new List<string>();
        foreach (var kv in _items)
            if (kv.Value.Rarity != EquipmentRarity.Legendary)
                list.Add(kv.Key);
        return list;
    }

    /// <summary>Returns only Legendary item IDs.</summary>
    public static List<string> GetLegendaryItems()
    {
        var list = new List<string>();
        foreach (var kv in _items)
            if (kv.Value.Rarity == EquipmentRarity.Legendary)
                list.Add(kv.Key);
        return list;
    }

    /// <summary>Returns items droppable in Boss districts (legendary only).</summary>
    public static List<string> GetBossDrops() => GetLegendaryItems();
}
