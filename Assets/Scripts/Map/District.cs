using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data model for a single city district.
/// Plain C# class (no MonoBehaviour) — serializable for save/load.
/// </summary>
[System.Serializable]
public class District
{
    public string Id;
    public string Name;
    public string OwnerId;          // empty = unclaimed
    public DistrictThemeType Theme;  // visual theme + gameplay modifiers
    public List<string> PooIds = new();

    // Grid position & world location (set by TerritoryManager)
    public int     GridX, GridY;
    public Vector2 Center;

    // Difficulty & rings
    public int  DifficultyLevel;   // 1-6
    public int  Ring;              // 0=center, 1=inner, 2=outer
    public bool IsBoss;            // corner districts with unique drops
    public bool IsLocked(int playerLevel) => playerLevel < DifficultyLevel;

    // Convenience properties
    public bool IsOwned       => !string.IsNullOrEmpty(OwnerId);
    public bool IsPlayerOwned => OwnerId == (ClanManager.Instance?.ClanId ?? "__none__");
}
