using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game controller. Singleton. Owns game state and coordinates all managers.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Managers (auto-assigned if null)")]
    public PooManager PooManager;
    public ClanManager ClanManager;
    public TerritoryManager TerritoryManager;
    public ResourceManager ResourceManager;
    public BattleManager BattleManager;
    public CloudSyncManager CloudSyncManager;

    public GameState State { get; private set; } = GameState.MainMenu;

    /// <summary>
    /// Ensures GameManager exists regardless of which scene is entered first.
    /// Runs before any scene Awake — handles editor play-from-any-scene.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoBootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("GameManager (Auto)");
        go.AddComponent<GameManager>();  // Awake sets Instance + DDOL
        go.AddComponent<PooManager>();
        go.AddComponent<ClanManager>();
        go.AddComponent<TerritoryManager>();
        go.AddComponent<ResourceManager>();
        go.AddComponent<BattleManager>();
        go.AddComponent<InventoryManager>();
        go.AddComponent<EnemyAIManager>();
        go.AddComponent<AdManager>();
        go.AddComponent<TutorialManager>();
        go.AddComponent<ToastManager>();
        go.AddComponent<SettingsScreenController>();
        go.AddComponent<FlySelectionUI>();
        go.AddComponent<GearEquipPopup>();
        go.AddComponent<NotificationManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        AutoAssignManagers();
    }

    IEnumerator Start()
    {
        yield return null; // wait one frame for all managers' Awake()/Start()
        SaveSystem.Load();
        CheckDailyLoginBonus();
        AudioManager.Instance?.PlayMusic(AudioManager.Track.Menu);

        // Start tutorial for first-time players (after MainMap loads)
        if (!SaveSystem.TutorialComplete)
            TutorialManager.Instance?.StartTutorial();
    }

    void CheckDailyLoginBonus()
    {
        string today = System.DateTime.Now.ToString("yyyy-MM-dd");
        if (SaveSystem.LastLoginDate == today) return;

        SaveSystem.LastLoginDate = today;
        ResourceManager.Instance?.AddPooCoins(50);
        ResourceManager.Instance?.AddGems(5);
        Debug.Log("[GameManager] Daily login bonus: +50 PooCoins, +5 Gems!");
        UIEventBus.Emit(UIEvent.DailyLoginBonus);
    }

    void AutoAssignManagers()
    {
        PooManager      ??= GetComponentInChildren<PooManager>();
        ClanManager     ??= GetComponentInChildren<ClanManager>();
        TerritoryManager??= GetComponentInChildren<TerritoryManager>();
        ResourceManager ??= GetComponentInChildren<ResourceManager>();
        BattleManager   ??= GetComponentInChildren<BattleManager>();
        CloudSyncManager??= GetComponentInChildren<CloudSyncManager>();
    }

    public void SetState(GameState newState)
    {
        State = newState;
        Debug.Log($"[GameManager] State → {newState}");
    }

    public void LoadScene(string sceneName)
    {
        SaveSystem.Save();
        SceneManager.LoadScene(sceneName);
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            SaveSystem.Save();
            NotificationManager.Instance?.ScheduleDailyReminder();
        }
    }

    void OnApplicationQuit()
    {
        SaveSystem.Save();
    }
}

public enum GameState
{
    MainMenu,
    CityMap,
    ClanScreen,
    Battle,
    Inventory,
    Shop
}