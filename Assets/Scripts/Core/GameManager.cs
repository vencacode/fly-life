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

    public GameState State { get; private set; } = GameState.MainMenu;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        AutoAssignManagers();
    }

    void Start()
    {
        SaveSystem.Load();
        AudioManager.Instance?.PlayMusic(AudioManager.Track.Menu);
    }

    void AutoAssignManagers()
    {
        PooManager      ??= GetComponentInChildren<PooManager>();
        ClanManager     ??= GetComponentInChildren<ClanManager>();
        TerritoryManager??= GetComponentInChildren<TerritoryManager>();
        ResourceManager ??= GetComponentInChildren<ResourceManager>();
        BattleManager   ??= GetComponentInChildren<BattleManager>();
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
        if (paused) SaveSystem.Save();
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