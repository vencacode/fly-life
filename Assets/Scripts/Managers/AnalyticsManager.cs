using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wraps Unity Analytics via reflection — compiles with or without the package.
/// Subscribes to UIEventBus to automatically fire analytics events.
/// </summary>
public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    static bool _analyticsAvailable;
    static Type _analyticsType;

    // Cached delegates for clean unsubscribe
    Action<object> _onBattleEnded;
    Action<object> _onDistrictCaptured;
    Action<object> _onLevelUp;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        DetectPackage();
    }

    void Start()
    {
        if (!_analyticsAvailable)
        {
            Debug.Log("[AnalyticsManager] Unity Analytics not detected. Analytics disabled.");
        }

        // Track first open
        TrackEvent("first_open", new Dictionary<string, object>
        {
            ["platform"] = Application.platform.ToString(),
            ["version"] = Application.version
        });

        // Subscribe to events
        _onBattleEnded = data => OnBattleEnded(data);
        _onDistrictCaptured = data => OnDistrictCaptured(data);
        _onLevelUp = data => OnLevelUp(data);

        UIEventBus.On(UIEvent.BattleEnded, _onBattleEnded);
        UIEventBus.On(UIEvent.DistrictCaptured, _onDistrictCaptured);
        UIEventBus.On(UIEvent.LevelUp, _onLevelUp);

        Debug.Log("[AnalyticsManager] Analytics initialized and subscribed to events.");
    }

    void OnDisable()
    {
        if (_onBattleEnded != null)      UIEventBus.Off(UIEvent.BattleEnded, _onBattleEnded);
        if (_onDistrictCaptured != null)  UIEventBus.Off(UIEvent.DistrictCaptured, _onDistrictCaptured);
        if (_onLevelUp != null)           UIEventBus.Off(UIEvent.LevelUp, _onLevelUp);
    }

    static void DetectPackage()
    {
        // Try Unity Analytics legacy
        _analyticsType = Type.GetType("UnityEngine.Analytics.Analytics, UnityEngine.UnityAnalyticsModule");
        _analyticsAvailable = _analyticsType != null;
        if (!_analyticsAvailable)
        {
            // Try newer namespace
            _analyticsType = Type.GetType("Unity.Services.Analytics.AnalyticsService, Unity.Services.Analytics");
            _analyticsAvailable = _analyticsType != null;
        }
    }

    /// <summary>Track a custom event with optional parameters.</summary>
    public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        if (!_analyticsAvailable)
        {
            Debug.Log($"[Analytics] (log only) Event: {eventName}");
            return;
        }
        try
        {
            var customEvent = _analyticsType.GetMethod("CustomEvent",
                new[] { typeof(string), typeof(Dictionary<string, object>) });
            if (customEvent != null)
            {
                customEvent.Invoke(null, new object[] { eventName, parameters ?? new Dictionary<string, object>() });
                Debug.Log($"[Analytics] Event: {eventName}");
                return;
            }
            Debug.Log($"[Analytics] (log only) Event: {eventName}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Analytics] Failed to track '{eventName}': {e.Message}");
        }
    }

    // ---- Event handlers ----

    void OnBattleEnded(object data)
    {
        bool won = data is bool b && b;
        TrackEvent(won ? "battle_won" : "battle_lost", new Dictionary<string, object>
        {
            ["clan_level"] = ClanManager.Instance?.ClanLevel ?? 0,
            ["total_flies"] = ClanManager.Instance?.TotalFlies ?? 0
        });
    }

    void OnDistrictCaptured(object data)
    {
        string districtName = data is District d ? d.Name : "unknown";
        TrackEvent("district_captured", new Dictionary<string, object>
        {
            ["district_name"] = districtName,
            ["total_districts"] = TerritoryManager.Instance?.GetPlayerDistricts().Count ?? 0
        });
    }

    void OnLevelUp(object data)
    {
        int level = data is FlyInstance fly ? fly.Level : (ClanManager.Instance?.ClanLevel ?? 0);
        TrackEvent("level_up", new Dictionary<string, object>
        {
            ["new_level"] = level
        });
    }

    /// <summary>Track tutorial completion (call from TutorialManager).</summary>
    public void TrackTutorialComplete()
    {
        TrackEvent("tutorial_complete", new Dictionary<string, object>
        {
            ["time_since_start"] = Time.realtimeSinceStartup
        });
    }

    /// <summary>Track gem purchase (call from IAPManager).</summary>
    public void TrackGemPurchase(string productId, int gemAmount)
    {
        TrackEvent("gem_purchase", new Dictionary<string, object>
        {
            ["product_id"] = productId,
            ["gem_amount"] = gemAmount
        });
    }
}
