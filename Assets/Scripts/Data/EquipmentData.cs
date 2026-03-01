using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipment", menuName = "FlyLife/EquipmentData")]
public class EquipmentData : ScriptableObject
{
    [Header("Identity")]
    public string ItemName;
    public EquipmentSlot Slot;
    public Sprite Icon;
    [TextArea(1,3)] public string FunnyDescription;
    public EquipmentRarity Rarity = EquipmentRarity.Common;

    [Header("Stats")]
    public int AttackBonus   = 0;
    public int DefenseBonus  = 0;
    public int SpeedBonus    = 0;
    public int HealthBonus   = 0;

    [Header("Special Effect")]
    public SpecialEffect Effect = SpecialEffect.None;
    public float EffectValue   = 0f;
    [TextArea(1,2)] public string EffectDescription;

    [Header("Acquire")]
    public int GemCost      = 0;
    public int CoinCost     = 0;
    public bool DropInBattle = true;
}

public enum EquipmentSlot   { Weapon, Head, Body, Feet, Wings, Offhand, Accessory }
public enum EquipmentRarity { Common, Uncommon, Rare, Epic, Legendary }
public enum SpecialEffect
{
    None,
    AOESplash,          // Poo Thrower: hits all enemies
    ReflectDamage,      // Nail Helmet: reflect % back
    BlockFirstHit,      // Bottle Cap Shield
    ActFirst,           // Sugar Cube Boots: speed priority
    ReduceAccuracy,     // Rotten Cheese Armor: enemy misses
    StunEnemy,          // Gum Wings: enemy loses turn
    FastItemGrab,       // Antenna Radar: grab items faster
    // Legendary effects:
    LifeSteal,          // heal % of damage dealt
    DoubleStrike,       // chance to attack twice
    Invincible,         // survive one lethal hit at 1 HP
    RallyAllies         // boost all allies' ATK by %
}
