using UnityEngine;

/// <summary>
/// Tracks all player resources: Poo Resources, Trash Resources, Clan Coins, Gems.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    // Resources
    public int PooResources   { get; private set; } = 0;
    public int TrashResources { get; private set; } = 0;
    public int ClanCoins      { get; private set; } = Constants.STARTING_CLAN_COINS;
    public int Gems           { get; private set; } = Constants.STARTING_GEMS;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ---- Poo Resources ----
    public void AddPooResources(int amount)
    {
        PooResources += amount;
        UIEventBus.EmitResourceChanged("PooResources", PooResources);
    }
    public bool SpendPooResources(int amount)
    {
        if (PooResources < amount) return false;
        PooResources -= amount;
        UIEventBus.EmitResourceChanged("PooResources", PooResources);
        return true;
    }

    // ---- Trash Resources ----
    public void AddTrashResources(int amount)
    {
        TrashResources += amount;
        UIEventBus.EmitResourceChanged("TrashResources", TrashResources);
    }
    public bool SpendTrashResources(int amount)
    {
        if (TrashResources < amount) return false;
        TrashResources -= amount;
        UIEventBus.EmitResourceChanged("TrashResources", TrashResources);
        return true;
    }

    // ---- Coins ----
    public void AddCoins(int amount)
    {
        ClanCoins += amount;
        UIEventBus.EmitResourceChanged("ClanCoins", ClanCoins);
    }
    public bool SpendCoins(int amount)
    {
        if (ClanCoins < amount) return false;
        ClanCoins -= amount;
        UIEventBus.EmitResourceChanged("ClanCoins", ClanCoins);
        return true;
    }

    // ---- Gems (premium) ----
    public void AddGems(int amount)
    {
        Gems += amount;
        UIEventBus.EmitResourceChanged("Gems", Gems);
    }
    public bool SpendGems(int amount)
    {
        if (Gems < amount) return false;
        Gems -= amount;
        UIEventBus.EmitResourceChanged("Gems", Gems);
        return true;
    }

    // ---- Save / Load ----
    [System.Serializable]
    public class SaveData
    {
        public int PooResources, TrashResources, ClanCoins, Gems;
    }

    public SaveData GetSaveData() => new()
    {
        PooResources   = PooResources,
        TrashResources = TrashResources,
        ClanCoins      = ClanCoins,
        Gems           = Gems
    };

    public void LoadSaveData(SaveData data)
    {
        if (data == null) return;
        PooResources   = data.PooResources;
        TrashResources = data.TrashResources;
        ClanCoins      = data.ClanCoins;
        Gems           = data.Gems;
    }
}
