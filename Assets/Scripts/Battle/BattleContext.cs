using System.Collections.Generic;

/// <summary>
/// Static data holder for passing battle parameters across scene loads.
/// Set before loading Battle scene, read on arrival, then clear.
/// </summary>
public static class BattleContext
{
    public static string DistrictId;
    public static string DefenderClanId;
    public static List<FlyInstance> EnemyFlies;

    public static bool HasPendingBattle => EnemyFlies != null && EnemyFlies.Count > 0;

    public static void Clear()
    {
        DistrictId = null;
        DefenderClanId = null;
        EnemyFlies = null;
    }
}
