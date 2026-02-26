/// <summary>
/// TEMP: Claims 3 districts on play-mode start so we can screenshot the
/// ownership colours and DistrictInfoPanel.  Remove before shipping.
/// </summary>
public static class MapTestHelper
{
    [UnityEngine.RuntimeInitializeOnLoadMethod(
        UnityEngine.RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        // Runs after all Awake()s, before all Start()s.
        // TerritoryManager has generated fresh districts; MapManager hasn't
        // spawned tiles yet — so RefreshAllTiles() in MapManager.Start() will
        // pick up the claims automatically.
        TerritoryManager.Instance?.ClaimDistrict("district_2_2",
            ClanManager.Instance?.ClanId ?? "player_clan");
        TerritoryManager.Instance?.ClaimDistrict("district_0_4", "enemy_clan");
        TerritoryManager.Instance?.ClaimDistrict("district_4_0", "enemy_clan");

        var go = new UnityEngine.GameObject("__MapTestRunner__");
        go.AddComponent<MapTestRunner>();
    }
}

/// Waits one frame (MapManager.Start finishes), then selects the center tile.
public class MapTestRunner : UnityEngine.MonoBehaviour
{
    System.Collections.IEnumerator Start()
    {
        yield return null;                                  // let MapManager.Start() run
        MapManager.Instance?.OnDistrictTapped("district_2_2");
        Destroy(gameObject);
    }
}
