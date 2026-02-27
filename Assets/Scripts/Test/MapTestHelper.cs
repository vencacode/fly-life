/// <summary>
/// TEMP: Auto-bootstraps the map test scene on play-mode start.
/// Creates UI if missing, then selects center tile.  Remove before shipping.
/// </summary>
public static class MapTestHelper
{
    [UnityEngine.RuntimeInitializeOnLoadMethod(
        UnityEngine.RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        // Ensure UI canvas + DistrictInfoPanel exist
        var bootstrap = new UnityEngine.GameObject("__MapSceneBootstrap__");
        bootstrap.AddComponent<MapSceneBootstrap>();

        // Auto-select center tile after one frame
        var runner = new UnityEngine.GameObject("__MapTestRunner__");
        runner.AddComponent<MapTestRunner>();
    }
}

/// Waits one frame (MapManager.Start finishes), then selects the center tile.
public class MapTestRunner : UnityEngine.MonoBehaviour
{
    System.Collections.IEnumerator Start()
    {
        yield return null;
        MapManager.Instance?.OnDistrictTapped("district_2_2");
        UnityEngine.Debug.Log("[MapTestRunner] Auto-selected district_2_2 for testing.");
        Destroy(gameObject);
    }
}
