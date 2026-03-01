using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// JSON-based save/load. Stores to Application.persistentDataPath.
/// Call SaveSystem.Save() and SaveSystem.Load() from GameManager.
/// </summary>
public static class SaveSystem
{
    private const string SAVE_FILENAME = "flylife_save.json";
    private static string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILENAME);

    /// <summary>Last login date (yyyy-MM-dd). Set after Load, updated before Save.</summary>
    public static string LastLoginDate { get; set; }

    /// <summary>Whether the tutorial has been completed.</summary>
    public static bool TutorialComplete { get; set; }

    /// <summary>Unix timestamp of last save (for offline progress calculation).</summary>
    public static long LastSaveTimestamp { get; set; }

    // JsonUtility can't serialize Dictionary — use a list of key-value pairs instead
    [System.Serializable]
    class StringPair
    {
        public string Key;
        public string Value;
    }

    [System.Serializable]
    class SaveFile
    {
        public ClanManager.SaveData         Clan;
        public ResourceManager.SaveData     Resources;
        public InventoryManager.SaveData    Inventory;
        public SkinSelectPopup.SaveData     Skins;
        public AchievementManager.SaveData  Achievements;
        public DailyMissionManager.SaveData DailyMissions;
        public CodexManager.SaveData        Codex;
        public BossRushManager.SaveData     BossRush;
        public RateAppManager.SaveData      RateApp;
        public List<StringPair>             TerritoryOwners;
        public string                       LastLoginDate;
        public bool                         TutorialComplete;
        public long                         LastSaveTimestamp;
        // PooManager poos are not saved — they respawn fresh each session
        public string Version = "1.0";
    }

    public static void Save()
    {
        // Convert territory dictionary to serializable list
        List<StringPair> territoryList = null;
        var territoryDict = TerritoryManager.Instance?.GetSaveData();
        if (territoryDict != null)
        {
            territoryList = new List<StringPair>();
            foreach (var kv in territoryDict)
                territoryList.Add(new StringPair { Key = kv.Key, Value = kv.Value });
        }

        var file = new SaveFile
        {
            Clan             = ClanManager.Instance?.GetSaveData(),
            Resources        = ResourceManager.Instance?.GetSaveData(),
            Inventory        = InventoryManager.Instance?.GetSaveData(),
            Skins            = SkinSelectPopup.Instance?.GetSaveData(),
            Achievements     = AchievementManager.Instance?.GetSaveData(),
            DailyMissions    = DailyMissionManager.Instance?.GetSaveData(),
            Codex            = CodexManager.Instance?.GetSaveData(),
            BossRush         = BossRushManager.Instance?.GetSaveData(),
            RateApp          = RateAppManager.Instance?.GetSaveData(),
            TerritoryOwners  = territoryList,
            LastLoginDate    = LastLoginDate,
            TutorialComplete = TutorialComplete,
            LastSaveTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        string json = JsonUtility.ToJson(file, prettyPrint: true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[SaveSystem] Saved to {SavePath}");

        // Cloud sync: push after local save succeeds
        CloudSyncManager.Instance?.PushToCloud();
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
        InventoryManager.Instance?.LoadSaveData(file.Inventory);
        SkinSelectPopup.Instance?.LoadSaveData(file.Skins);
        AchievementManager.Instance?.LoadSaveData(file.Achievements);
        DailyMissionManager.Instance?.LoadSaveData(file.DailyMissions);
        CodexManager.Instance?.LoadSaveData(file.Codex);
        BossRushManager.Instance?.LoadSaveData(file.BossRush);
        RateAppManager.Instance?.LoadSaveData(file.RateApp);
        LastLoginDate      = file.LastLoginDate;
        TutorialComplete   = file.TutorialComplete;
        LastSaveTimestamp   = file.LastSaveTimestamp;

        // Convert serializable list back to dictionary for TerritoryManager
        if (file.TerritoryOwners != null)
        {
            var dict = new Dictionary<string, string>();
            foreach (var pair in file.TerritoryOwners)
                dict[pair.Key] = pair.Value;
            TerritoryManager.Instance?.LoadSaveData(dict);
        }

        // Refresh map tiles to reflect loaded ownership
        MapManager.Instance?.RefreshAllTiles();

        Debug.Log("[SaveSystem] Loaded save.");
    }

    public static void MarkTutorialComplete()
    {
        TutorialComplete = true;
        Save();
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
        Debug.Log("[SaveSystem] Save deleted.");
    }
}
