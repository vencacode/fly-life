using System;
using System.Collections.Generic;
using System.Linq;
using FlyLife.Cloud;
using UnityEngine;

/// <summary>
/// Converts between local manager SaveData and cloud DTOs.
/// Handles Time.time ↔ UTC ticks conversion for eggs/boosters.
/// No Firebase dependency — always compiles.
/// </summary>
public static class CloudDataConverter
{
    // ---- Player data: Local → Cloud ----

    public static CloudPlayerData ToCloud(ClanManager.SaveData clan, ResourceManager.SaveData resources)
    {
        return new CloudPlayerData
        {
            Clan = clan != null ? new CloudClanData
            {
                ClanId = clan.ClanId,
                ClanName = clan.ClanName,
                ClanXP = clan.ClanXP,
                ClanLevel = clan.ClanLevel,
                HasCustomClan = clan.HasCustomClan
            } : null,
            Resources = resources != null ? new CloudResourceData
            {
                PooCoins = resources.PooCoins,
                Gems = resources.Gems
            } : null,
            Flies = clan?.Flies?.Select(ToCloudFly).ToList() ?? new List<CloudFlyData>(),
            Eggs = clan?.Eggs?.Select(ToCloudEgg).ToList() ?? new List<CloudEggData>(),
            Boosters = clan?.Boosters?.Select(ToCloudBooster).ToList() ?? new List<CloudBoosterData>(),
            LastSaveUtcTicks = DateTime.UtcNow.Ticks,
            Version = "1.0"
        };
    }

    // ---- Player data: Cloud → Local ----

    public static ClanManager.SaveData ToClanSaveData(CloudPlayerData cloud)
    {
        if (cloud == null) return null;
        return new ClanManager.SaveData
        {
            ClanId = cloud.Clan?.ClanId ?? "player_clan",
            ClanName = cloud.Clan?.ClanName ?? "The Fly Lifers",
            ClanXP = cloud.Clan?.ClanXP ?? 0,
            ClanLevel = cloud.Clan?.ClanLevel ?? 1,
            HasCustomClan = cloud.Clan?.HasCustomClan ?? false,
            Flies = cloud.Flies?.Select(ToLocalFly).ToList() ?? new List<FlyInstance>(),
            Eggs = cloud.Eggs?.Select(ToLocalEgg).ToList() ?? new List<EggBatch>(),
            Boosters = cloud.Boosters?.Select(ToLocalBooster).ToList() ?? new List<ActiveBooster>()
        };
    }

    public static ResourceManager.SaveData ToResourceSaveData(CloudPlayerData cloud)
    {
        if (cloud?.Resources == null) return null;
        return new ResourceManager.SaveData
        {
            PooCoins = cloud.Resources.PooCoins,
            Gems = cloud.Resources.Gems
        };
    }

    // ---- Territory ----

    public static CloudTerritoryData ToCloudTerritory(Dictionary<string, string> owners)
    {
        return new CloudTerritoryData
        {
            Districts = owners != null ? new Dictionary<string, string>(owners) : new(),
            LastUpdateUtcTicks = DateTime.UtcNow.Ticks
        };
    }

    public static Dictionary<string, string> ToLocalTerritory(CloudTerritoryData cloud)
    {
        return cloud?.Districts != null
            ? new Dictionary<string, string>(cloud.Districts)
            : new Dictionary<string, string>();
    }

    // ---- Fly conversion ----

    static CloudFlyData ToCloudFly(FlyInstance f) => new()
    {
        InstanceId = f.InstanceId,
        Role = f.Role.ToString(),
        Level = f.Level,
        XP = f.XP,
        Attack = f.Attack,
        Defense = f.Defense,
        Speed = f.Speed,
        CurrentHealth = f.CurrentHealth,
        MaxHealth = f.MaxHealth,
        DeployedDistrictId = f.DeployedDistrictId,
        EquippedWeaponId = f.EquippedWeaponId,
        EquippedHeadId = f.EquippedHeadId,
        EquippedBodyId = f.EquippedBodyId,
        EquippedFeetId = f.EquippedFeetId,
        EquippedWingsId = f.EquippedWingsId,
        EquippedOffhandId = f.EquippedOffhandId
    };

    static FlyInstance ToLocalFly(CloudFlyData c) => new()
    {
        InstanceId = c.InstanceId,
        Role = Enum.TryParse<FlyRole>(c.Role, out var role) ? role : FlyRole.Scout,
        Level = c.Level,
        XP = c.XP,
        Attack = c.Attack,
        Defense = c.Defense,
        Speed = c.Speed,
        CurrentHealth = c.CurrentHealth,
        MaxHealth = c.MaxHealth,
        DeployedDistrictId = c.DeployedDistrictId,
        EquippedWeaponId = c.EquippedWeaponId,
        EquippedHeadId = c.EquippedHeadId,
        EquippedBodyId = c.EquippedBodyId,
        EquippedFeetId = c.EquippedFeetId,
        EquippedWingsId = c.EquippedWingsId,
        EquippedOffhandId = c.EquippedOffhandId
    };

    // ---- Egg conversion (Time.time ↔ UTC ticks) ----

    static CloudEggData ToCloudEgg(EggBatch e) => new()
    {
        PooInstanceId = e.PooInstanceId,
        EggCount = e.EggCount,
        LayTimeUtcTicks = LocalTimeToUtcTicks(e.LayTime),
        HatchDuration = e.HatchDuration,
        Role = e.Role.ToString()
    };

    static EggBatch ToLocalEgg(CloudEggData c) => new()
    {
        PooInstanceId = c.PooInstanceId,
        EggCount = c.EggCount,
        LayTime = UtcTicksToLocalTime(c.LayTimeUtcTicks),
        HatchDuration = c.HatchDuration,
        Role = Enum.TryParse<FlyRole>(c.Role, out var role) ? role : FlyRole.Scout
    };

    // ---- Booster conversion ----

    static CloudBoosterData ToCloudBooster(ActiveBooster b) => new()
    {
        BoosterDataId = b.BoosterDataId,
        ActivatedAtUtcTicks = LocalTimeToUtcTicks(b.ActivatedAt),
        Duration = b.Duration
    };

    static ActiveBooster ToLocalBooster(CloudBoosterData c) => new()
    {
        BoosterDataId = c.BoosterDataId,
        ActivatedAt = UtcTicksToLocalTime(c.ActivatedAtUtcTicks),
        Duration = c.Duration
    };

    // ---- Time helpers ----
    // Time.time is seconds since app start. We convert to/from UTC ticks:
    //   Save:  utcTicks = now - (Time.time - localTime) in ticks
    //   Load:  localTime = Time.time - (now - utcTicks) in seconds

    static long LocalTimeToUtcTicks(float localTime)
    {
        float secondsAgo = Time.time - localTime;
        return DateTime.UtcNow.Ticks - (long)(secondsAgo * TimeSpan.TicksPerSecond);
    }

    static float UtcTicksToLocalTime(long utcTicks)
    {
        float secondsAgo = (float)(DateTime.UtcNow.Ticks - utcTicks) / TimeSpan.TicksPerSecond;
        return Time.time - secondsAgo;
    }
}
