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
    public List<string> PooIds = new();

    // Grid position & world location (set by TerritoryManager)
    public int     GridX, GridY;
    public Vector2 Center;

    // Convenience properties
    public bool IsOwned       => !string.IsNullOrEmpty(OwnerId);
    public bool IsPlayerOwned => OwnerId == (ClanManager.Instance?.ClanId ?? "__none__");
}
