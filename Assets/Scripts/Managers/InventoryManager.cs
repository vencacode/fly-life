using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the player's gear inventory and fly equipment.
/// Items are stored by ScriptableObject asset name (string ID).
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private List<string> _ownedItemIds = new();

    /// <summary>Map of flyId → slot → itemId for equipped gear.</summary>
    private Dictionary<string, Dictionary<EquipmentSlot, string>> _equipped = new();

    /// <summary>Gear upgrade levels: itemId → upgrade count.</summary>
    private Dictionary<string, int> _upgradeLevels = new();

    public IReadOnlyList<string> OwnedItemIds => _ownedItemIds;

    public bool HasItem(string itemId) => _ownedItemIds.Contains(itemId);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    // ---- Add / Remove ----

    public void AddItem(string itemId)
    {
        _ownedItemIds.Add(itemId);
        Debug.Log($"[Inventory] Added {itemId}. Total items: {_ownedItemIds.Count}");
    }

    public bool RemoveItem(string itemId)
    {
        bool removed = _ownedItemIds.Remove(itemId);
        if (removed) Debug.Log($"[Inventory] Removed {itemId}");
        return removed;
    }

    // ---- Equip / Unequip ----

    public bool EquipItem(string flyId, string itemId)
    {
        if (!_ownedItemIds.Contains(itemId)) return false;

        var maybeData = EquipmentRegistry.Get(itemId);
        if (maybeData == null) return false;
        var data = maybeData.Value;

        var fly = ClanManager.Instance?.Flies.FirstOrDefault(f => f.InstanceId == flyId);
        if (fly == null) return false;

        // Accessory slot requires elite status
        if (data.Slot == EquipmentSlot.Accessory && !fly.IsElite)
        {
            Debug.LogWarning("[Inventory] Only Elite flies can equip accessories.");
            return false;
        }

        // Unequip existing item in that slot first
        UnequipSlot(flyId, data.Slot);

        // Equip new item
        if (!_equipped.ContainsKey(flyId))
            _equipped[flyId] = new Dictionary<EquipmentSlot, string>();
        _equipped[flyId][data.Slot] = itemId;

        // Apply stat bonuses
        fly.Attack    += data.AttackBonus;
        fly.Defense   += data.DefenseBonus;
        fly.Speed     += data.SpeedBonus;
        fly.MaxHealth += data.HealthBonus;
        fly.CurrentHealth = Mathf.Min(fly.CurrentHealth + data.HealthBonus, fly.MaxHealth);

        // Update FlyInstance slot references
        SetFlySlot(fly, data.Slot, itemId);

        Debug.Log($"[Inventory] Equipped {itemId} on fly {flyId} ({data.Slot})");
        return true;
    }

    public void UnequipSlot(string flyId, EquipmentSlot slot)
    {
        if (!_equipped.TryGetValue(flyId, out var slots)) return;
        if (!slots.TryGetValue(slot, out var oldItemId)) return;

        var maybeData = EquipmentRegistry.Get(oldItemId);
        var fly = ClanManager.Instance?.Flies.FirstOrDefault(f => f.InstanceId == flyId);
        if (maybeData != null && fly != null)
        {
            var d = maybeData.Value;
            fly.Attack    -= d.AttackBonus;
            fly.Defense   -= d.DefenseBonus;
            fly.Speed     -= d.SpeedBonus;
            fly.MaxHealth -= d.HealthBonus;
            fly.CurrentHealth = Mathf.Min(fly.CurrentHealth, fly.MaxHealth);
            SetFlySlot(fly, slot, null);
        }
        slots.Remove(slot);
    }

    public string GetEquippedItem(string flyId, EquipmentSlot slot)
    {
        if (_equipped.TryGetValue(flyId, out var slots) && slots.TryGetValue(slot, out var id))
            return id;
        return null;
    }

    public SpecialEffect GetFlyEffect(string flyId)
    {
        if (!_equipped.TryGetValue(flyId, out var slots)) return SpecialEffect.None;
        foreach (var itemId in slots.Values)
        {
            var maybeData = EquipmentRegistry.Get(itemId);
            if (maybeData != null && maybeData.Value.Effect != SpecialEffect.None)
                return maybeData.Value.Effect;
        }
        return SpecialEffect.None;
    }

    static void SetFlySlot(FlyInstance fly, EquipmentSlot slot, string itemId)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon:  fly.EquippedWeaponId  = itemId; break;
            case EquipmentSlot.Head:    fly.EquippedHeadId    = itemId; break;
            case EquipmentSlot.Body:    fly.EquippedBodyId    = itemId; break;
            case EquipmentSlot.Feet:    fly.EquippedFeetId    = itemId; break;
            case EquipmentSlot.Wings:   fly.EquippedWingsId   = itemId; break;
            case EquipmentSlot.Offhand:   fly.EquippedOffhandId   = itemId; break;
            case EquipmentSlot.Accessory: fly.EquippedAccessoryId = itemId; break;
        }
    }

    // ---- Gear Upgrades ----

    public int GetUpgradeLevel(string itemId) =>
        _upgradeLevels.TryGetValue(itemId, out var lvl) ? lvl : 0;

    public bool UpgradeItem(string itemId)
    {
        if (!_ownedItemIds.Contains(itemId)) return false;
        if (ResourceManager.Instance == null || !ResourceManager.Instance.SpendPooCoins(Constants.GEAR_UPGRADE_COST))
            return false;

        _upgradeLevels.TryGetValue(itemId, out int current);
        _upgradeLevels[itemId] = current + 1;
        Debug.Log($"[Inventory] Upgraded {itemId} to +{current + 1}");
        DailyMissionManager.Instance?.TrackUpgradeGear();
        return true;
    }

    // ---- Save / Load ----

    [System.Serializable]
    public class SaveData
    {
        public List<string> OwnedItemIds;
        public List<EquipEntry> Equipped;
        public List<string> UpgradeKeys;
        public List<int>    UpgradeValues;
    }

    [System.Serializable]
    public class EquipEntry
    {
        public string FlyId;
        public int Slot; // cast of EquipmentSlot
        public string ItemId;
    }

    public SaveData GetSaveData()
    {
        var equips = new List<EquipEntry>();
        foreach (var (flyId, slots) in _equipped)
            foreach (var (slot, itemId) in slots)
                equips.Add(new EquipEntry { FlyId = flyId, Slot = (int)slot, ItemId = itemId });

        var upgradeKeys   = new List<string>(_upgradeLevels.Keys);
        var upgradeValues = new List<int>(_upgradeLevels.Values);

        return new SaveData
        {
            OwnedItemIds  = new List<string>(_ownedItemIds),
            Equipped      = equips,
            UpgradeKeys   = upgradeKeys,
            UpgradeValues = upgradeValues
        };
    }

    public void LoadSaveData(SaveData data)
    {
        if (data == null) return;
        _ownedItemIds = data.OwnedItemIds ?? new List<string>();
        _equipped.Clear();
        if (data.Equipped != null)
        {
            foreach (var e in data.Equipped)
            {
                if (!_equipped.ContainsKey(e.FlyId))
                    _equipped[e.FlyId] = new Dictionary<EquipmentSlot, string>();
                _equipped[e.FlyId][(EquipmentSlot)e.Slot] = e.ItemId;
            }
        }

        _upgradeLevels.Clear();
        if (data.UpgradeKeys != null && data.UpgradeValues != null)
        {
            for (int i = 0; i < data.UpgradeKeys.Count && i < data.UpgradeValues.Count; i++)
                _upgradeLevels[data.UpgradeKeys[i]] = data.UpgradeValues[i];
        }
    }
}
