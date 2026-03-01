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
    public int   GridWidth  = 7;
    public int   GridHeight = 7;
    public float DistrictSize = 10f; // Unity units per district

    private Dictionary<string, District> _districts = new();

    public IReadOnlyDictionary<string, District> Districts => _districts;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        GenerateDistricts();
    }

    // City layout: themed 7x7 grid (y=0 is bottom row)
    static readonly DistrictThemeType[,] CityLayout =
    {
        // x=0                          x=1                            x=2                            x=3                            x=4                            x=5                            x=6
        { DistrictThemeType.Airport,    DistrictThemeType.Market,      DistrictThemeType.Residential,  DistrictThemeType.TownSquare,   DistrictThemeType.University,   DistrictThemeType.Waterfront,  DistrictThemeType.Airport      }, // y=0
        { DistrictThemeType.Hospital,   DistrictThemeType.Market,      DistrictThemeType.Residential,  DistrictThemeType.TownSquare,   DistrictThemeType.University,   DistrictThemeType.Waterfront,  DistrictThemeType.Hospital     }, // y=1
        { DistrictThemeType.Residential,DistrictThemeType.Residential, DistrictThemeType.BackAlley,    DistrictThemeType.Playground,   DistrictThemeType.Construction, DistrictThemeType.Residential, DistrictThemeType.Construction }, // y=2
        { DistrictThemeType.Waterfront, DistrictThemeType.Construction,DistrictThemeType.DogPark,      DistrictThemeType.TownSquare,   DistrictThemeType.DogPark,      DistrictThemeType.BackAlley,   DistrictThemeType.Sewer        }, // y=3
        { DistrictThemeType.Construction,DistrictThemeType.Residential,DistrictThemeType.Playground,   DistrictThemeType.DogPark,      DistrictThemeType.Sewer,        DistrictThemeType.Residential, DistrictThemeType.Residential  }, // y=4
        { DistrictThemeType.Hospital,   DistrictThemeType.Waterfront,  DistrictThemeType.University,   DistrictThemeType.TownSquare,   DistrictThemeType.Residential,  DistrictThemeType.Market,      DistrictThemeType.Hospital     }, // y=5
        { DistrictThemeType.Airport,    DistrictThemeType.Residential, DistrictThemeType.Market,       DistrictThemeType.Playground,   DistrictThemeType.University,   DistrictThemeType.Residential, DistrictThemeType.Airport      }, // y=6
    };

    void GenerateDistricts()
    {
        _districts.Clear();
        for (int x = 0; x < GridWidth; x++)
        for (int y = 0; y < GridHeight; y++)
        {
            var theme = (x < CityLayout.GetLength(1) && y < CityLayout.GetLength(0))
                ? CityLayout[y, x]
                : DistrictThemeType.Residential;

            int ring = Mathf.Max(Mathf.Abs(x - 3), Mathf.Abs(y - 3)); // Chebyshev distance from center (3,3)
            bool isBoss = (x == 0 || x == GridWidth - 1) && (y == 0 || y == GridHeight - 1);
            int difficulty = isBoss ? 8 : (ring == 0 ? 1 : ring == 1 ? 3 : ring == 2 ? 5 : 7);

            string id = $"district_{x}_{y}";
            _districts[id] = new District
            {
                Id              = id,
                Name            = GenerateDistrictName(x, y, theme),
                Theme           = theme,
                GridX           = x,
                GridY           = y,
                Center          = new Vector2(x * DistrictSize, y * DistrictSize),
                Ring            = ring,
                DifficultyLevel = difficulty,
                IsBoss          = isBoss,
                OwnerId         = ring > 0 ? $"enemy_clan_{ring}" : string.Empty
            };
        }
        Debug.Log($"[TerritoryManager] Generated {_districts.Count} themed districts.");
    }

    void Start()
    {
        // Center district (3,3) starts player-owned as tutorial start
        string playerClan = ClanManager.Instance?.ClanId ?? "player_clan";
        if (_districts.TryGetValue("district_3_3", out var center) && string.IsNullOrEmpty(center.OwnerId))
            center.OwnerId = playerClan;
        // Backward compat: old saves may have district_2_2 as player-owned
        if (_districts.TryGetValue("district_2_2", out var oldCenter) && oldCenter.OwnerId == playerClan)
        {
            oldCenter.OwnerId = string.Empty;
            if (_districts.TryGetValue("district_3_3", out var newCenter))
                newCenter.OwnerId = playerClan;
        }
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

    string GenerateDistrictName(int x, int y, DistrictThemeType theme)
    {
        string[] prefixes = { "Old", "East", "West", "North", "South", "New", "Upper", "Lower" };
        string themeName = DistrictThemeData.Get(theme).ThemeName;
        string prefix = prefixes[(x + y * 3) % prefixes.Length];
        return $"{prefix} {themeName}";
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
            CloudSyncManager.Instance?.PushTerritoryToCloud();
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
