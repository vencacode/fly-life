using UnityEngine;

/// <summary>
/// Schedules local push notifications on Android:
/// - Daily login reminder (24h after last open)
/// - District under attack alerts
/// Requires com.unity.mobile.notifications package.
/// Uses reflection to avoid hard compile dependency — works with or without the package.
/// </summary>
public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    const string CHANNEL_ID = "flylife_main";
    static bool _packageAvailable;
    static System.Type _channelType;
    static System.Type _notificationType;
    static System.Type _centerType;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DetectPackage();
    }

    void Start()
    {
        if (!FeatureFlags.PUSH_NOTIFICATIONS_ENABLED)
        {
            Debug.Log("[NotificationManager] Push notifications disabled via FeatureFlags.");
            return;
        }
        if (!_packageAvailable)
        {
            Debug.Log("[NotificationManager] Mobile Notifications package not installed. Notifications disabled.");
            return;
        }
        RegisterChannel();
        CancelAll();
        ScheduleDailyReminder();
    }

    static void DetectPackage()
    {
        _centerType = System.Type.GetType("Unity.Notifications.Android.AndroidNotificationCenter, Unity.Notifications.Android");
        _channelType = System.Type.GetType("Unity.Notifications.Android.AndroidNotificationChannel, Unity.Notifications.Android");
        _notificationType = System.Type.GetType("Unity.Notifications.Android.AndroidNotification, Unity.Notifications.Android");
        _packageAvailable = _centerType != null && _channelType != null && _notificationType != null;
    }

    void RegisterChannel()
    {
        if (!_packageAvailable) return;
        try
        {
            var channel = System.Activator.CreateInstance(_channelType);
            _channelType.GetProperty("Id")?.SetValue(channel, CHANNEL_ID);
            _channelType.GetProperty("Name")?.SetValue(channel, "Fly Life");
            _channelType.GetProperty("Description")?.SetValue(channel, "Game notifications");
            // Importance.Default = 3
            var importanceType = System.Type.GetType("Unity.Notifications.Android.Importance, Unity.Notifications.Android");
            if (importanceType != null)
                _channelType.GetProperty("Importance")?.SetValue(channel, System.Enum.ToObject(importanceType, 3));

            var method = _centerType.GetMethod("RegisterNotificationChannel",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            method?.Invoke(null, new[] { channel });
            Debug.Log("[NotificationManager] Channel registered.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NotificationManager] Channel registration failed: {e.Message}");
        }
    }

    void CancelAll()
    {
        if (!_packageAvailable) return;
        try
        {
            var method = _centerType.GetMethod("CancelAllNotifications",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            method?.Invoke(null, null);
        }
        catch { /* ignore */ }
    }

    void SendNotification(string title, string text, System.DateTime fireTime)
    {
        if (!_packageAvailable) return;
        try
        {
            var notif = System.Activator.CreateInstance(_notificationType);
            _notificationType.GetProperty("Title")?.SetValue(notif, title);
            _notificationType.GetProperty("Text")?.SetValue(notif, text);
            _notificationType.GetProperty("FireTime")?.SetValue(notif, fireTime);
            _notificationType.GetProperty("SmallIcon")?.SetValue(notif, "icon_small");

            var method = _centerType.GetMethod("SendNotification",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                null,
                new System.Type[] { _notificationType, typeof(string) },
                null);
            method?.Invoke(null, new[] { notif, CHANNEL_ID });
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NotificationManager] Send failed: {e.Message}");
        }
    }

    /// <summary>Schedule a "come back" notification 24h from now.</summary>
    public void ScheduleDailyReminder()
    {
        if (!FeatureFlags.PUSH_NOTIFICATIONS_ENABLED) return;
        SendNotification(
            "Your flies miss you!",
            "Districts are generating PooCoins. Come collect!",
            System.DateTime.Now.AddHours(24));
        Debug.Log("[NotificationManager] Daily reminder scheduled for 24h from now.");
    }

    /// <summary>Fire a near-instant notification that a district is under attack.</summary>
    public void NotifyDistrictUnderAttack(string districtName)
    {
        if (!FeatureFlags.PUSH_NOTIFICATIONS_ENABLED) return;
        SendNotification(
            "District under attack!",
            $"{districtName} is being invaded by enemies!",
            System.DateTime.Now.AddSeconds(1));
        Debug.Log($"[NotificationManager] Attack notification sent for {districtName}.");
    }
}
