using System;
using System.Collections.Generic;

/// <summary>
/// Simple event bus for decoupled UI ↔ game logic communication.
/// Usage:
///   UIEventBus.On(UIEvent.ResourceChanged, OnResourceChanged);
///   UIEventBus.Emit(UIEvent.ResourceChanged, null);
///   UIEventBus.Off(UIEvent.ResourceChanged, OnResourceChanged);
/// </summary>
public static class UIEventBus
{
    // Typed event for resource changes: (resourceName, newValue)
    public static event Action<string, int> OnResourceChanged;
    public static void EmitResourceChanged(string resourceName, int newValue)
        => OnResourceChanged?.Invoke(resourceName, newValue);

    private static readonly Dictionary<UIEvent, Action<object>> _listeners = new();

    public static void On(UIEvent evt, Action<object> callback)
    {
        if (!_listeners.ContainsKey(evt)) _listeners[evt] = null;
        _listeners[evt] += callback;
    }

    public static void Off(UIEvent evt, Action<object> callback)
    {
        if (_listeners.ContainsKey(evt)) _listeners[evt] -= callback;
    }

    public static void Emit(UIEvent evt, object data = null)
    {
        _listeners.GetValueOrDefault(evt)?.Invoke(data);
    }
}

public enum UIEvent
{
    ResourceChanged,
    ClanUpdated,
    DistrictCaptured,
    DistrictLost,
    BattleRoundStart,
    BattleAttack,
    BattleItemDropped,
    BattleItemGrabbed,
    BattleStateChanged,
    BattleEnded,
    PooDiscovered,
    EggHatched,
    LevelUp,
    BoosterActivated
}
