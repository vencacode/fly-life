using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Spawns, tracks, and updates all poo instances on the city map.
/// </summary>
public class PooManager : MonoBehaviour
{
    public static PooManager Instance { get; private set; }

    [Header("Config")]
    public PooData[] AllPooTypes;           // assign all PooData SOs in Inspector
    public float SpawnIntervalSeconds = 60f;
    public int   MaxPoosOnMap         = 50;

    private List<PooInstance> _activePoos = new();
    private float _nextSpawnTime;

    public IReadOnlyList<PooInstance> ActivePoos => _activePoos;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        // Remove dried poos
        _activePoos.RemoveAll(p => p.IsDry);

        // Spawn new poos
        if (Time.time >= _nextSpawnTime && _activePoos.Count < MaxPoosOnMap)
        {
            SpawnRandomPoo();
            _nextSpawnTime = Time.time + SpawnIntervalSeconds;
        }
    }

    public PooInstance SpawnRandomPoo(Vector2? position = null)
    {
        if (AllPooTypes == null || AllPooTypes.Length == 0) return null;

        var data = PickWeightedPooType();
        var poo  = CreateInstance(data, position ?? RandomCityPosition());
        _activePoos.Add(poo);

        Debug.Log($"[PooManager] Spawned {data.PooName} at {poo.WorldPosition}");
        return poo;
    }

    public bool ClaimPoo(PooInstance poo, string clanId)
    {
        if (poo == null || poo.IsDry) return false;
        poo.OwnerId = clanId;
        GameManager.Instance?.TerritoryManager?.EvaluateDistrict(poo.DistrictId);
        return true;
    }

    public void ReleasePoo(PooInstance poo)
    {
        if (poo == null) return;
        poo.OwnerId = string.Empty;
        GameManager.Instance?.TerritoryManager?.EvaluateDistrict(poo.DistrictId);
    }

    public List<PooInstance> GetPoosByDistrict(string districtId)
        => _activePoos.Where(p => p.DistrictId == districtId).ToList();

    public List<PooInstance> GetPoosByClan(string clanId)
        => _activePoos.Where(p => p.OwnerId == clanId).ToList();

    /// <summary>Returns closest unowned poo to a world position, within maxRange.</summary>
    public PooInstance FindNearestUnowned(Vector2 from, float maxRange = 999f)
    {
        return _activePoos
            .Where(p => !p.IsOwned && !p.IsDry)
            .OrderBy(p => Vector2.Distance(from, p.WorldPosition))
            .FirstOrDefault(p => Vector2.Distance(from, p.WorldPosition) <= maxRange);
    }

    // ---- Private helpers ----

    PooData PickWeightedPooType()
    {
        float total = AllPooTypes.Sum(p => p.SpawnWeight);
        float roll  = Random.Range(0f, total);
        float cumulative = 0f;
        foreach (var poo in AllPooTypes)
        {
            cumulative += poo.SpawnWeight;
            if (roll <= cumulative) return poo;
        }
        return AllPooTypes[0];
    }

    PooInstance CreateInstance(PooData data, Vector2 pos)
    {
        return new PooInstance
        {
            PooDataId    = data.name,
            Fertility    = Random.Range(data.FertilityMin,   data.FertilityMax + 1),
            Consistency  = Random.Range(data.ConsistencyMin, data.ConsistencyMax + 1),
            Smell        = Random.Range(data.SmellMin,       data.SmellMax + 1),
            Leftovers    = Random.Range(data.LeftoversMin,   data.LeftoversMax + 1),
            SpawnTime    = Time.time,
            Duration     = data.BaseDurationSeconds,
            WorldPosition= pos,
            DistrictId   = GetDistrictAtPosition(pos)
        };
    }

    Vector2 RandomCityPosition()
    {
        // TODO: replace with actual city map bounds from TerritoryManager
        return new Vector2(Random.Range(-20f, 20f), Random.Range(-20f, 20f));
    }

    string GetDistrictAtPosition(Vector2 pos)
    {
        // TODO: query TerritoryManager for district at pos
        return "district_default";
    }

    // ---- Save / Load ----

    public List<PooInstance> GetSaveData() => _activePoos;

    public void LoadSaveData(List<PooInstance> saved)
    {
        _activePoos = saved ?? new List<PooInstance>();
        _activePoos.RemoveAll(p => p.IsDry); // prune stale poos
    }
}
