using System;
using System.Threading.Tasks;
using FlyLife.Cloud;

/// <summary>
/// Interface for cloud save/load operations.
/// Implemented by FirestoreServiceImpl (when Firebase SDK is available)
/// and FirestoreServiceStub (when it is not).
/// </summary>
public interface ICloudService
{
    bool IsAvailable { get; }
    Task SavePlayerAsync(string playerId, CloudPlayerData data);
    Task<CloudPlayerData> LoadPlayerAsync(string playerId);
    Task SaveTerritoryAsync(CloudTerritoryData data);
    Task<CloudTerritoryData> LoadTerritoryAsync();
    void ListenToTerritory(Action<CloudTerritoryData> onUpdate);
    void StopListeningToTerritory();
}

/// <summary>
/// No-op stub used when Firebase SDK is not installed.
/// All operations return immediately with no effect.
/// </summary>
public class FirestoreServiceStub : ICloudService
{
    public bool IsAvailable => false;

    public Task SavePlayerAsync(string playerId, CloudPlayerData data)
        => Task.CompletedTask;

    public Task<CloudPlayerData> LoadPlayerAsync(string playerId)
        => Task.FromResult<CloudPlayerData>(null);

    public Task SaveTerritoryAsync(CloudTerritoryData data)
        => Task.CompletedTask;

    public Task<CloudTerritoryData> LoadTerritoryAsync()
        => Task.FromResult<CloudTerritoryData>(null);

    public void ListenToTerritory(Action<CloudTerritoryData> onUpdate) { }
    public void StopListeningToTerritory() { }
}

#if FIREBASE_FIRESTORE
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine;

/// <summary>
/// Real Firestore implementation. Only compiles when FIREBASE_FIRESTORE is defined
/// (Player Settings > Scripting Define Symbols).
/// </summary>
public class FirestoreServiceImpl : ICloudService
{
    FirebaseFirestore _db;
    ListenerRegistration _territoryListener;

    public bool IsAvailable => _db != null;

    public async Task InitializeAsync()
    {
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status == DependencyStatus.Available)
        {
            _db = FirebaseFirestore.DefaultInstance;
            Debug.Log("[FirestoreService] Initialized.");
        }
        else
        {
            Debug.LogError($"[FirestoreService] Firebase dependency error: {status}");
        }
    }

    public async Task SavePlayerAsync(string playerId, CloudPlayerData data)
    {
        if (_db == null) return;
        var docRef = _db.Collection("players").Document(playerId);
        await docRef.SetAsync(data);
        Debug.Log($"[FirestoreService] Player data saved for {playerId}.");
    }

    public async Task<CloudPlayerData> LoadPlayerAsync(string playerId)
    {
        if (_db == null) return null;
        var docRef = _db.Collection("players").Document(playerId);
        var snapshot = await docRef.GetSnapshotAsync();
        if (!snapshot.Exists) return null;
        return snapshot.ConvertTo<CloudPlayerData>();
    }

    public async Task SaveTerritoryAsync(CloudTerritoryData data)
    {
        if (_db == null) return;
        var docRef = _db.Collection("world").Document("territory");
        // Use a transaction to merge territory changes safely
        await _db.RunTransactionAsync(async transaction =>
        {
            var snapshot = await transaction.GetSnapshotAsync(docRef);
            var current = snapshot.Exists
                ? snapshot.ConvertTo<CloudTerritoryData>()
                : new CloudTerritoryData { Districts = new() };

            // Merge: overwrite districts that have owners
            if (data.Districts != null)
                foreach (var kv in data.Districts)
                    current.Districts[kv.Key] = kv.Value;

            current.LastUpdateUtcTicks = data.LastUpdateUtcTicks;
            transaction.Set(docRef, current);
        });
        Debug.Log("[FirestoreService] Territory saved.");
    }

    public async Task<CloudTerritoryData> LoadTerritoryAsync()
    {
        if (_db == null) return null;
        var docRef = _db.Collection("world").Document("territory");
        var snapshot = await docRef.GetSnapshotAsync();
        if (!snapshot.Exists) return null;
        return snapshot.ConvertTo<CloudTerritoryData>();
    }

    public void ListenToTerritory(Action<CloudTerritoryData> onUpdate)
    {
        if (_db == null) return;
        var docRef = _db.Collection("world").Document("territory");
        _territoryListener = docRef.Listen(snapshot =>
        {
            if (snapshot.Exists)
            {
                var data = snapshot.ConvertTo<CloudTerritoryData>();
                onUpdate?.Invoke(data);
            }
        });
        Debug.Log("[FirestoreService] Territory listener started.");
    }

    public void StopListeningToTerritory()
    {
        _territoryListener?.Stop();
        _territoryListener = null;
    }
}
#endif
