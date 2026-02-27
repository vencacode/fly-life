using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages city districts: ownership evaluation, bonuses, map layout.
/// </summary>
public class TerritoryManager : MonoBehaviour
{
    public static TerritoryManager Instance { get; private set; }

    [Header("Map Config")]
    public int   GridWidth  = 5;
    public int   GridHeight = 5;
    public float DistrictSize = 10f; // Unity units per district

    private Dictionary<string, District> _districts = new();

    public IReadOnlyDictionary<string, District> Districts => _districts;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        GenerateDistricts();
    }

    void GenerateDistricts()
    {
        _districts.Clear();
        for (int x = 0; x < GridWidth; x++)
        for (int y = 0; y < GridHeight; y++)
        {
            string id = $"district_{x}_{y}";
            _districts[id] = new District
            {
                Id       = id,
                Name     = GenerateDistrictName(x, y),
                GridX    = x,
                GridY    = y,
                Center   = new Vector2(x * DistrictSize, y * DistrictSize),
                OwnerId  = string.Empty
            };
        }
        Debug.Log($"[TerritoryManager] Generated {_districts.Count} districts.");
    }

    void Start()
    {
        SeedTestData();
    }

    /// <summary>Hardcoded test districts for UI testing. Remove before shipping.</summary>
    void SeedTestData()
    {
        string playerClan = ClanManager.Instance?.ClanId ?? "player_clan";

        // 2 player-owned
        ClaimDistrict("district_2_2", playerClan);
        ClaimDistrict("district_1_2", playerClan);

        // 2 enemy-owned
        ClaimDistrict("district_0_4", "enemy_clan");
        ClaimDistrict("district_4_0", "enemy_clan");

        // Add some fake poo IDs so UI shows counts
        if (_districts.TryGetValue("district_2_2", out var center))
            center.PooIds.AddRange(new[] { "poo_1", "poo_2", "poo_3" });
        if (_districts.TryGetValue("district_0_4", out var enemy))
            enemy.PooIds.AddRange(new[] { "poo_4", "poo_5" });

        // remaining 21 districts stay unclaimed
        Debug.Log("[TerritoryManager] Test data seeded: 2 player, 2 enemy, 21 neutral.");
    }

    /// <summary>Called by PooManager when a poo changes ownership.</summary>
    public void EvaluateDistrict(string districtId)
    {
        if (!_districts.TryGetValue(districtId, out var district)) return;

        var poos         = PooManager.Instance?.GetPoosByDistrict(districtId) ?? new();
        var playerClanId = ClanManager.Instance?.ClanId ?? "";

        // Count poos per clan
        var counts = poos.GroupBy(p => p.OwnerId)
                         .ToDictionary(g => g.Key, g => g.Count());
        int total  = poos.Count;
        if (total == 0) { district.OwnerId = string.Empty; return; }

        foreach (var (clanId, count) in counts)
        {
            float ratio = (float)count / total;
            bool  isPlayer = clanId == playerClanId;

            if (ratio >= Constants.DISTRICT_CLAIM_THRESHOLD && district.OwnerId != clanId)
            {
                string prev = district.OwnerId;
                district.OwnerId = clanId;
                Debug.Log($"[TerritoryManager] {district.Name} claimed by {clanId}");
                if (isPlayer)
                    UIEventBus.Emit(UIEvent.DistrictCaptured, district);
                else if (prev == playerClanId)
                    UIEventBus.Emit(UIEvent.DistrictLost, district);
            }
            else if (isPlayer && district.OwnerId == playerClanId
                     && ratio < Constants.DISTRICT_LOSS_THRESHOLD)
            {
                district.OwnerId = string.Empty;
                UIEventBus.Emit(UIEvent.DistrictLost, district);
            }
        }
    }

    public District GetDistrict(string id)
        => _districts.GetValueOrDefault(id);

    public District GetDistrictAt(Vector2 worldPos)
    {
        int gx = Mathf.FloorToInt(worldPos.x / DistrictSize);
        int gy = Mathf.FloorToInt(worldPos.y / DistrictSize);
        string id = $"district_{gx}_{gy}";
        return _districts.GetValueOrDefault(id);
    }

    public List<District> GetPlayerDistricts()
    {
        string id = ClanManager.Instance?.ClanId ?? "";
        return _districts.Values.Where(d => d.OwnerId == id).ToList();
    }

    /// <summary>Passive bonus: smell radius multiplier from owned districts.</summary>
    public float GetSmellRadiusBonus()
        => 1f + (GetPlayerDistricts().Count * 0.05f);

    string GenerateDistrictName(int x, int y)
    {
        string[] prefixes = { "Old", "East", "West", "North", "South", "New", "Upper", "Lower" };
        string[] suffixes = { "Alley", "Square", "Quarter", "Block", "Row", "Park", "Lane", "Yard" };
        return $"{prefixes[(x + y * 3) % prefixes.Length]} {suffixes[(x * 2 + y) % suffixes.Length]}";
    }

    // ---- Save / Load ----

    public Dictionary<string, string> GetSaveData()
        => _districts.ToDictionary(kv => kv.Key, kv => kv.Value.OwnerId);

    public void ClaimDistrict(string districtId, string ownerId)
    {
        if (_districts.TryGetValue(districtId, out var d))
        {
            d.OwnerId = ownerId;
            Debug.Log($"[TerritoryManager] {districtId} claimed by {ownerId}");
        }
    }

    public void LoadSaveData(Dictionary<string, string> saved)
    {
        if (saved == null) return;
        foreach (var (id, ownerId) in saved)
            if (_districts.ContainsKey(id))
                _districts[id].OwnerId = ownerId;
    }
}
