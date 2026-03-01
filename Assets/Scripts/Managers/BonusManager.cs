using UnityEngine;

/// <summary>
/// Calculates passive bonuses from owned districts.
/// +10% hatch speed and +10% resource gain per player-owned district.
/// </summary>
public class BonusManager : MonoBehaviour
{
    public static BonusManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    int PlayerDistrictCount
        => TerritoryManager.Instance?.GetPlayerDistricts()?.Count ?? 0;

    /// <summary>Egg hatch speed multiplier: 1.0 + 0.1 per owned district.</summary>
    public float HatchSpeedMultiplier => 1f + PlayerDistrictCount * 0.1f;

    /// <summary>Resource gain multiplier: base 1.0 + 0.1 per owned district + theme bonuses.</summary>
    public float ResourceMultiplier
    {
        get
        {
            float mult = 1f + PlayerDistrictCount * 0.1f;
            // Add theme resource bonuses from owned districts
            var playerDistricts = TerritoryManager.Instance?.GetPlayerDistricts();
            if (playerDistricts != null)
                foreach (var d in playerDistricts)
                    mult += DistrictThemeData.Get(d.Theme).ResourceBonusMultiplier - 1f;
            return mult;
        }
    }

    /// <summary>XP multiplier from owned district themes.</summary>
    public float XPMultiplier
    {
        get
        {
            float mult = 1f;
            var playerDistricts = TerritoryManager.Instance?.GetPlayerDistricts();
            if (playerDistricts != null)
                foreach (var d in playerDistricts)
                    mult += DistrictThemeData.Get(d.Theme).XPBonusMultiplier - 1f;
            return mult;
        }
    }

    /// <summary>Deployed fly defense bonus: +5% per deployed fly in district.</summary>
    public float GetDefenseBonus(string districtId)
    {
        int deployed = ClanManager.Instance?.GetDeployedCount(districtId) ?? 0;
        return 1f + deployed * 0.05f;
    }
}
