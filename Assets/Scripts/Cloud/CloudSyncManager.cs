using System;
using System.Threading.Tasks;
using FlyLife.Cloud;
using UnityEngine;

/// <summary>
/// Orchestrates cloud save/load alongside the local SaveSystem.
/// Uses ICloudService (stub when no Firebase SDK, real when installed).
/// Attach to the GameManager GameObject or any DontDestroyOnLoad object.
/// </summary>
public class CloudSyncManager : MonoBehaviour
{
    public static CloudSyncManager Instance { get; private set; }

    ICloudService _cloud;
    string _playerId;
    bool _isSyncing;
    long _lastCloudSaveTicks;

    public bool CloudAvailable => _cloud?.IsAvailable ?? false;
    public string PlayerId => _playerId;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Generate or retrieve persistent player ID (pre-auth placeholder)
        _playerId = PlayerPrefs.GetString("FlyLife_PlayerId", "");
        if (string.IsNullOrEmpty(_playerId))
        {
            _playerId = Guid.NewGuid().ToString();
            PlayerPrefs.SetString("FlyLife_PlayerId", _playerId);
            PlayerPrefs.Save();
        }

        // Create the appropriate cloud service
#if FIREBASE_FIRESTORE
        _cloud = new FirestoreServiceImpl();
#else
        _cloud = new FirestoreServiceStub();
#endif

        Debug.Log($"[CloudSync] Initialized. PlayerId={_playerId}, Available={_cloud.IsAvailable}");
    }

    async void Start()
    {
        if (!FeatureFlags.CLOUD_SAVE_ENABLED)
        {
            Debug.Log("[CloudSync] Cloud save disabled via FeatureFlags.");
            return;
        }

#if FIREBASE_FIRESTORE
        if (_cloud is FirestoreServiceImpl impl)
            await impl.InitializeAsync();
#endif

        if (_cloud.IsAvailable)
        {
            _cloud.ListenToTerritory(OnTerritoryUpdatedFromCloud);
            await PullFromCloud();
        }
        else
        {
            Debug.Log("[CloudSync] Cloud not available — running locally only.");
        }
    }

    /// <summary>
    /// Push current player data to cloud. Called by SaveSystem.Save().
    /// Fire-and-forget — does not block the game.
    /// </summary>
    public async void PushToCloud()
    {
        if (!FeatureFlags.CLOUD_SAVE_ENABLED) return;
        if (!_cloud.IsAvailable || _isSyncing) return;
        _isSyncing = true;
        UIEventBus.Emit(UIEvent.CloudSyncStarted);

        try
        {
            var clanData = ClanManager.Instance?.GetSaveData();
            var resData = ResourceManager.Instance?.GetSaveData();
            var cloudData = CloudDataConverter.ToCloud(clanData, resData);

            await _cloud.SavePlayerAsync(_playerId, cloudData);
            _lastCloudSaveTicks = cloudData.LastSaveUtcTicks;

            UIEventBus.Emit(UIEvent.CloudSyncCompleted);
            Debug.Log("[CloudSync] Player data pushed to cloud.");
        }
        catch (Exception e)
        {
            UIEventBus.Emit(UIEvent.CloudSyncFailed);
            Debug.LogWarning($"[CloudSync] Push failed: {e.Message}");
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>
    /// Pull player data from cloud. Called on app launch.
    /// Compares timestamps — cloud wins if newer.
    /// </summary>
    public async Task PullFromCloud()
    {
        if (!_cloud.IsAvailable) return;

        try
        {
            var cloudData = await _cloud.LoadPlayerAsync(_playerId);
            if (cloudData == null)
            {
                Debug.Log("[CloudSync] No cloud save found. Using local data.");
                return;
            }

            // Compare timestamps — use cloud data if it's newer
            if (cloudData.LastSaveUtcTicks > _lastCloudSaveTicks)
            {
                var clanData = CloudDataConverter.ToClanSaveData(cloudData);
                var resData = CloudDataConverter.ToResourceSaveData(cloudData);

                ClanManager.Instance?.LoadSaveData(clanData);
                ResourceManager.Instance?.LoadSaveData(resData);

                _lastCloudSaveTicks = cloudData.LastSaveUtcTicks;
                Debug.Log("[CloudSync] Cloud data is newer — applied to local managers.");
            }
            else
            {
                Debug.Log("[CloudSync] Local data is current. No cloud update needed.");
            }

            // Also pull territory
            var territoryData = await _cloud.LoadTerritoryAsync();
            if (territoryData != null)
            {
                var localTerritory = CloudDataConverter.ToLocalTerritory(territoryData);
                TerritoryManager.Instance?.LoadSaveData(localTerritory);
                MapManager.Instance?.RefreshAllTiles();
                Debug.Log("[CloudSync] Territory loaded from cloud.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CloudSync] Pull failed: {e.Message}");
        }
    }

    /// <summary>
    /// Push territory changes to cloud. Called when district ownership changes.
    /// </summary>
    public async void PushTerritoryToCloud()
    {
        if (!FeatureFlags.CLOUD_SAVE_ENABLED) return;
        if (!_cloud.IsAvailable) return;

        try
        {
            var owners = TerritoryManager.Instance?.GetSaveData();
            if (owners == null) return;

            var cloudTerritory = CloudDataConverter.ToCloudTerritory(owners);
            await _cloud.SaveTerritoryAsync(cloudTerritory);
            Debug.Log("[CloudSync] Territory pushed to cloud.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CloudSync] Territory push failed: {e.Message}");
        }
    }

    void OnTerritoryUpdatedFromCloud(CloudTerritoryData data)
    {
        if (data == null) return;

        var localTerritory = CloudDataConverter.ToLocalTerritory(data);
        TerritoryManager.Instance?.LoadSaveData(localTerritory);
        MapManager.Instance?.RefreshAllTiles();

        UIEventBus.Emit(UIEvent.TerritoryUpdatedFromCloud);
        Debug.Log("[CloudSync] Territory updated from cloud listener.");
    }

    void OnDestroy()
    {
        _cloud?.StopListeningToTerritory();
    }
}
