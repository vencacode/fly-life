using UnityEngine;
using System.IO;

/// <summary>
/// JSON-based save/load. Stores to Application.persistentDataPath.
/// Call SaveSystem.Save() and SaveSystem.Load() from GameManager.
/// </summary>
public static class SaveSystem
{
    private const string SAVE_FILENAME = "flylife_save.json";
    private static string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILENAME);

    [System.Serializable]
    class SaveFile
    {
        public ClanManager.SaveData      Clan;
        public ResourceManager.SaveData  Resources;
        public System.Collections.Generic.Dictionary<string, string> TerritoryOwners;
        // PooManager poos are not saved — they respawn fresh each session
        public string Version = "1.0";
    }

    public static void Save()
    {
        var file = new SaveFile
        {
            Clan             = ClanManager.Instance?.GetSaveData(),
            Resources        = ResourceManager.Instance?.GetSaveData(),
            TerritoryOwners  = TerritoryManager.Instance?.GetSaveData()
        };
        string json = JsonUtility.ToJson(file, prettyPrint: true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[SaveSystem] Saved to {SavePath}");
    }

    public static void Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[SaveSystem] No save file found. Starting fresh.");
            return;
        }
        string json = File.ReadAllText(SavePath);
        var file    = JsonUtility.FromJson<SaveFile>(json);

        ClanManager.Instance?.LoadSaveData(file.Clan);
        ResourceManager.Instance?.LoadSaveData(file.Resources);
        TerritoryManager.Instance?.LoadSaveData(file.TerritoryOwners);
        Debug.Log("[SaveSystem] Loaded save.");
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
        Debug.Log("[SaveSystem] Save deleted.");
    }
}
