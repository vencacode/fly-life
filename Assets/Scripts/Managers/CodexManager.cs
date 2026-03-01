using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Tracks lifetime collections: gear, skins, districts, fly roles.
/// Shows overall completion percentage in the Codex UI.
/// </summary>
public class CodexManager : MonoBehaviour
{
    public static CodexManager Instance { get; private set; }

    HashSet<string> _discoveredGear = new();
    HashSet<string> _discoveredSkins = new();
    HashSet<string> _capturedDistricts = new();
    HashSet<string> _discoveredRoles = new();

    public IReadOnlyCollection<string> DiscoveredGear => _discoveredGear;
    public IReadOnlyCollection<string> DiscoveredSkins => _discoveredSkins;
    public IReadOnlyCollection<string> CapturedDistricts => _capturedDistricts;
    public IReadOnlyCollection<string> DiscoveredRoles => _discoveredRoles;

    // Cached delegates
    Action<object> _onDistrictCaptured;
    Action<object> _onClanUpdated;
    Action<object> _onBattleItemDropped;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        _onDistrictCaptured  = data => OnDistrictCaptured(data);
        _onClanUpdated       = _ => ScanFliesAndSkins();
        _onBattleItemDropped = data => OnItemDropped(data);

        UIEventBus.On(UIEvent.DistrictCaptured, _onDistrictCaptured);
        UIEventBus.On(UIEvent.ClanUpdated, _onClanUpdated);
        UIEventBus.On(UIEvent.BattleItemDropped, _onBattleItemDropped);

        // Initial scan
        ScanAll();
    }

    void OnDisable()
    {
        if (_onDistrictCaptured != null)   UIEventBus.Off(UIEvent.DistrictCaptured, _onDistrictCaptured);
        if (_onClanUpdated != null)        UIEventBus.Off(UIEvent.ClanUpdated, _onClanUpdated);
        if (_onBattleItemDropped != null)  UIEventBus.Off(UIEvent.BattleItemDropped, _onBattleItemDropped);
    }

    void ScanAll()
    {
        ScanGear();
        ScanFliesAndSkins();
        ScanDistricts();
    }

    void ScanGear()
    {
        if (InventoryManager.Instance == null) return;
        bool changed = false;
        foreach (var id in InventoryManager.Instance.OwnedItemIds)
        {
            if (_discoveredGear.Add(id)) changed = true;
        }
        if (changed) UIEventBus.Emit(UIEvent.CodexUpdated);
    }

    void ScanFliesAndSkins()
    {
        bool changed = false;
        if (ClanManager.Instance != null)
        {
            foreach (var fly in ClanManager.Instance.Flies)
            {
                if (_discoveredRoles.Add(fly.Role.ToString())) changed = true;
                if (!string.IsNullOrEmpty(fly.SkinId) && _discoveredSkins.Add(fly.SkinId)) changed = true;
            }
        }
        // Also check SkinSelectPopup unlocked skins
        if (SkinSelectPopup.Instance != null)
        {
            foreach (var kv in SkinRegistry.All)
            {
                if (SkinSelectPopup.Instance.IsSkinUnlocked(kv.Key))
                    if (_discoveredSkins.Add(kv.Key)) changed = true;
            }
        }
        if (changed) UIEventBus.Emit(UIEvent.CodexUpdated);
    }

    void ScanDistricts()
    {
        if (TerritoryManager.Instance == null) return;
        bool changed = false;
        foreach (var d in TerritoryManager.Instance.GetPlayerDistricts())
        {
            if (_capturedDistricts.Add(d.Id)) changed = true;
        }
        if (changed) UIEventBus.Emit(UIEvent.CodexUpdated);
    }

    void OnDistrictCaptured(object data)
    {
        if (data is District d)
            _capturedDistricts.Add(d.Id);
        else
            ScanDistricts();
        UIEventBus.Emit(UIEvent.CodexUpdated);
    }

    void OnItemDropped(object data)
    {
        if (data is string itemId)
            _discoveredGear.Add(itemId);
        ScanGear();
    }

    /// <summary>Register a gear discovery directly.</summary>
    public void DiscoverGear(string itemId)
    {
        if (_discoveredGear.Add(itemId))
            UIEventBus.Emit(UIEvent.CodexUpdated);
    }

    public float GetCompletionPercent()
    {
        int totalGear = EquipmentRegistry.All.Count;
        int totalSkins = SkinRegistry.All.Count;
        int totalDistricts = (TerritoryManager.Instance?.GridWidth ?? 7) * (TerritoryManager.Instance?.GridHeight ?? 7);
        int totalRoles = 3; // Scout, Soldier, Worker

        int total = totalGear + totalSkins + totalDistricts + totalRoles;
        if (total == 0) return 0f;

        int discovered = _discoveredGear.Count + _discoveredSkins.Count + _capturedDistricts.Count + _discoveredRoles.Count;
        return Mathf.Clamp01((float)discovered / total);
    }

    // ---- Save / Load ----

    [Serializable]
    public class SaveData
    {
        public List<string> Gear = new();
        public List<string> Skins = new();
        public List<string> Districts = new();
        public List<string> Roles = new();
    }

    public SaveData GetSaveData() => new()
    {
        Gear      = new List<string>(_discoveredGear),
        Skins     = new List<string>(_discoveredSkins),
        Districts = new List<string>(_capturedDistricts),
        Roles     = new List<string>(_discoveredRoles)
    };

    public void LoadSaveData(SaveData data)
    {
        if (data == null) return;
        _discoveredGear     = new HashSet<string>(data.Gear ?? new());
        _discoveredSkins    = new HashSet<string>(data.Skins ?? new());
        _capturedDistricts  = new HashSet<string>(data.Districts ?? new());
        _discoveredRoles    = new HashSet<string>(data.Roles ?? new());
    }
}
