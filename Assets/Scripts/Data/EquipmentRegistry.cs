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
    };

    public static EquipmentDef? Get(string itemId)
    {
        return _items.TryGetValue(itemId, out var def) ? def : null;
    }

    public static IReadOnlyDictionary<string, EquipmentDef> All => _items;

    /// <summary>Returns all item IDs that can drop in battle.</summary>
    public static List<string> GetDroppableItems()
    {
        var list = new List<string>();
        foreach (var kv in _items)
            list.Add(kv.Key);
        return list;
    }
}
