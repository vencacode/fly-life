using System;
using System.Collections.Generic;

/// <summary>
/// Plain C# DTOs for Firestore serialization.
/// No Unity or Firebase dependencies — always compiles.
/// Maps to Firestore documents: players/{playerId} and world/territory.
/// </summary>
namespace FlyLife.Cloud
{
    [Serializable]
    public class CloudPlayerData
    {
        public CloudClanData Clan;
        public CloudResourceData Resources;
        public List<CloudFlyData> Flies;
        public List<CloudEggData> Eggs;
        public List<CloudBoosterData> Boosters;
        public long LastSaveUtcTicks;
        public string Version = "1.0";
    }

    [Serializable]
    public class CloudClanData
    {
        public string ClanId;
        public string ClanName;
        public int ClanXP;
        public int ClanLevel;
        public bool HasCustomClan;
    }

    [Serializable]
    public class CloudResourceData
    {
        public int PooCoins;
        public int Gems;
    }

    [Serializable]
    public class CloudFlyData
    {
        public string InstanceId;
        public string Role;
        public int Level;
        public int XP;
        public int Attack;
        public int Defense;
        public int Speed;
        public int CurrentHealth;
        public int MaxHealth;
        public string DeployedDistrictId;
        public string EquippedWeaponId;
        public string EquippedHeadId;
        public string EquippedBodyId;
        public string EquippedFeetId;
        public string EquippedWingsId;
        public string EquippedOffhandId;
    }

    [Serializable]
    public class CloudEggData
    {
        public string PooInstanceId;
        public int EggCount;
        public long LayTimeUtcTicks;
        public float HatchDuration;
        public string Role;
    }

    [Serializable]
    public class CloudBoosterData
    {
        public string BoosterDataId;
        public long ActivatedAtUtcTicks;
        public float Duration;
        public float Multiplier;
    }

    [Serializable]
    public class CloudTerritoryData
    {
        public Dictionary<string, string> Districts;
        public long LastUpdateUtcTicks;
    }
}
