using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a district theme with visual style and gameplay modifiers.
/// Themes are registered in a static lookup — no ScriptableObjects needed.
/// </summary>
[System.Serializable]
public class DistrictThemeData
{
    public DistrictThemeType Type;
    public string ThemeName;
    public string Icon;          // short text icon for tile display
    public Color ThemeColor;
    public float PooSpawnRateMultiplier = 1f;
    public float PooQualityMultiplier   = 1f;
    public float ResourceBonusMultiplier = 1f;
    public float XPBonusMultiplier       = 1f;
    public string Description;

    // ---- Static registry ----

    static Dictionary<DistrictThemeType, DistrictThemeData> _registry;

    public static DistrictThemeData Get(DistrictThemeType type)
    {
        if (_registry == null) BuildRegistry();
        return _registry.TryGetValue(type, out var data) ? data : _registry[DistrictThemeType.Residential];
    }

    static void BuildRegistry()
    {
        _registry = new Dictionary<DistrictThemeType, DistrictThemeData>
        {
            [DistrictThemeType.DogPark] = new()
            {
                Type = DistrictThemeType.DogPark,
                ThemeName = "Dog Park",
                Icon = "[D]",
                ThemeColor = new Color(0.3f, 0.75f, 0.2f),
                PooSpawnRateMultiplier = 2.0f,
                PooQualityMultiplier = 1.0f,
                ResourceBonusMultiplier = 1.0f,
                XPBonusMultiplier = 1.0f,
                Description = "Dogs everywhere. Poo paradise."
            },
            [DistrictThemeType.BackAlley] = new()
            {
                Type = DistrictThemeType.BackAlley,
                ThemeName = "Back Alley",
                Icon = "[A]",
                ThemeColor = new Color(0.35f, 0.35f, 0.4f),
                PooSpawnRateMultiplier = 1.2f,
                PooQualityMultiplier = 0.7f,
                ResourceBonusMultiplier = 1.0f,
                XPBonusMultiplier = 1.0f,
                Description = "Shady and smelly. +20% trash."
            },
            [DistrictThemeType.Sewer] = new()
            {
                Type = DistrictThemeType.Sewer,
                ThemeName = "Sewer",
                Icon = "[S]",
                ThemeColor = new Color(0.15f, 0.45f, 0.4f),
                PooSpawnRateMultiplier = 0.5f,
                PooQualityMultiplier = 2.0f,
                ResourceBonusMultiplier = 1.0f,
                XPBonusMultiplier = 1.0f,
                Description = "Rare finds, but premium quality."
            },
            [DistrictThemeType.Playground] = new()
            {
                Type = DistrictThemeType.Playground,
                ThemeName = "Playground",
                Icon = "[P]",
                ThemeColor = new Color(0.6f, 0.8f, 0.2f),
                PooSpawnRateMultiplier = 1.5f,
                PooQualityMultiplier = 1.0f,
                ResourceBonusMultiplier = 1.0f,
                XPBonusMultiplier = 1.1f,
                Description = "Kids and pets. +10% XP."
            },
            [DistrictThemeType.TownSquare] = new()
            {
                Type = DistrictThemeType.TownSquare,
                ThemeName = "Town Square",
                Icon = "[T]",
                ThemeColor = new Color(0.75f, 0.65f, 0.45f),
                PooSpawnRateMultiplier = 0.8f,
                PooQualityMultiplier = 1.0f,
                ResourceBonusMultiplier = 1.15f,
                XPBonusMultiplier = 1.0f,
                Description = "Busy hub. +15% coin income."
            },
            [DistrictThemeType.Residential] = new()
            {
                Type = DistrictThemeType.Residential,
                ThemeName = "Residential",
                Icon = "[R]",
                ThemeColor = new Color(0.45f, 0.6f, 0.8f),
                PooSpawnRateMultiplier = 1.0f,
                PooQualityMultiplier = 1.0f,
                ResourceBonusMultiplier = 1.0f,
                XPBonusMultiplier = 1.0f,
                Description = "Quiet neighborhood. No bonuses."
            },
            [DistrictThemeType.Market] = new()
            {
                Type = DistrictThemeType.Market,
                ThemeName = "Market",
                Icon = "[M]",
                ThemeColor = new Color(0.85f, 0.55f, 0.15f),
                PooSpawnRateMultiplier = 0.6f,
                PooQualityMultiplier = 1.0f,
                ResourceBonusMultiplier = 1.25f,
                XPBonusMultiplier = 1.0f,
                Description = "Trade hub. +25% resource income."
            },
            [DistrictThemeType.Construction] = new()
            {
                Type = DistrictThemeType.Construction,
                ThemeName = "Construction",
                Icon = "[C]",
                ThemeColor = new Color(0.55f, 0.4f, 0.2f),
                PooSpawnRateMultiplier = 1.3f,
                PooQualityMultiplier = 1.0f,
                ResourceBonusMultiplier = 1.0f,
                XPBonusMultiplier = 1.0f,
                Description = "Messy site. +20% poo leftovers."
            },
            [DistrictThemeType.University] = new()
            {
                Type = DistrictThemeType.University,
                ThemeName = "University",
                Icon = "[U]",
                ThemeColor = new Color(0.55f, 0.35f, 0.7f),
                PooSpawnRateMultiplier = 0.7f,
                PooQualityMultiplier = 1.0f,
                ResourceBonusMultiplier = 1.0f,
                XPBonusMultiplier = 1.2f,
                Description = "Knowledge hub. +20% XP."
            },
            [DistrictThemeType.Waterfront] = new()
            {
                Type = DistrictThemeType.Waterfront,
                ThemeName = "Waterfront",
                Icon = "[W]",
                ThemeColor = new Color(0.2f, 0.7f, 0.75f),
                PooSpawnRateMultiplier = 0.4f,
                PooQualityMultiplier = 1.5f,
                ResourceBonusMultiplier = 1.0f,
                XPBonusMultiplier = 1.0f,
                Description = "Seaside. Rare but high-value poo."
            },
            [DistrictThemeType.Hospital] = new()
            {
                Type = DistrictThemeType.Hospital,
                ThemeName = "Hospital",
                Icon = "[H]",
                ThemeColor = new Color(0.85f, 0.85f, 0.9f),
                PooSpawnRateMultiplier = 0.6f,
                PooQualityMultiplier = 1.3f,
                ResourceBonusMultiplier = 1.1f,
                XPBonusMultiplier = 1.0f,
                Description = "Clean but busy. +30% poo quality."
            },
            [DistrictThemeType.Airport] = new()
            {
                Type = DistrictThemeType.Airport,
                ThemeName = "Airport",
                Icon = "[+]",
                ThemeColor = new Color(0.5f, 0.6f, 0.8f),
                PooSpawnRateMultiplier = 1.4f,
                PooQualityMultiplier = 0.8f,
                ResourceBonusMultiplier = 1.2f,
                XPBonusMultiplier = 1.0f,
                Description = "International poo. +40% rate, +20% income."
            }
        };
    }
}

public enum DistrictThemeType
{
    Residential,
    DogPark,
    BackAlley,
    Sewer,
    Playground,
    TownSquare,
    Market,
    Construction,
    University,
    Waterfront,
    Hospital,
    Airport
}
