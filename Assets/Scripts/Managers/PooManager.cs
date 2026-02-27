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
    public float SpawnIntervalSeconds = 30f;
    public int   MaxPoosOnMap         = 50;

    [Header("Visuals")]
    public GameObject PooMarkerPrefab;      // assign in Inspector
    public Transform  MarkersParent;

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
        // Remove dried poos and destroy their markers
        foreach (var p in _activePoos)
            if (p.IsDry && p.MarkerObject != null)
                GameObject.Destroy(p.MarkerObject);
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
        string districtId = GetDistrictAtPosition(pos);
        var poo = new PooInstance
        {
            PooDataId    = data.name,
            Fertility    = Random.Range(data.FertilityMin,   data.FertilityMax + 1),
            Consistency  = Random.Range(data.ConsistencyMin, data.ConsistencyMax + 1),
            Smell        = Random.Range(data.SmellMin,       data.SmellMax + 1),
            Leftovers    = Random.Range(data.LeftoversMin,   data.LeftoversMax + 1),
            SpawnTime    = Time.time,
            Duration     = data.BaseDurationSeconds,
            WorldPosition= pos,
            DistrictId   = districtId
        };

        // Register poo in district
        if (!string.IsNullOrEmpty(districtId) && TerritoryManager.Instance != null)
            if (TerritoryManager.Instance.Districts.TryGetValue(districtId, out var d))
                d.PooIds.Add(poo.PooDataId + "_" + poo.SpawnTime);

        // Spawn visual marker
        SpawnMarker(poo);

        return poo;
    }

    void SpawnMarker(PooInstance poo)
    {
        if (PooMarkerPrefab == null) return;
        var parent = MarkersParent ?? transform;
        var go = GameObject.Instantiate(PooMarkerPrefab, (Vector3)poo.WorldPosition, Quaternion.identity, parent);
        go.name = $"Poo_{poo.DistrictId}";
        poo.MarkerObject = go;
    }

    Vector2 RandomCityPosition()
    {
        var tm = TerritoryManager.Instance;
        if (tm == null) return Vector2.zero;

        // Pick a random district then offset randomly within it
        var districts = tm.Districts.Values.ToList();
        var d = districts[Random.Range(0, districts.Count)];
        float half = tm.DistrictSize * 0.4f;
        return d.Center + new Vector2(Random.Range(-half, half), Random.Range(-half, half));
    }

    string GetDistrictAtPosition(Vector2 pos)
    {
        var tm = TerritoryManager.Instance;
        if (tm == null) return string.Empty;

        float half = tm.DistrictSize * 0.5f;
        foreach (var d in tm.Districts.Values)
        {
            if (Mathf.Abs(pos.x - d.Center.x) <= half &&
                Mathf.Abs(pos.y - d.Center.y) <= half)
                return d.Id;
        }
        return string.Empty;
    }

    // ---- Save / Load ----

    public List<PooInstance> GetSaveData() => _activePoos;

    public void LoadSaveData(List<PooInstance> saved)
    {
        _activePoos = saved ?? new List<PooInstance>();
        _activePoos.RemoveAll(p => p.IsDry); // prune stale poos
    }
}
