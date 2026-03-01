using System;
using System.Collections.Generic;

/// <summary>
/// Defines seasonal events with dates, bonuses, and rewards.
/// </summary>
public static class SeasonalEventRegistry
{
    public struct EventDef
    {
        public string EventId;
        public string DisplayName;
        public string Description;
        public int StartMonth, StartDay;      // recurring annually
        public int DurationDays;
        public float PooRateMultiplier;       // applied globally during event
        public float XPMultiplier;
        public float CoinMultiplier;
        public string RewardSkinId;           // unlocked for participating
        public string BannerColorHex;
    }

    static readonly List<EventDef> _events = new()
    {
        new EventDef
        {
            EventId = "poo_year", DisplayName = "Poo Year Festival",
            Description = "Ring in the Poo Year! Double poo rate and +50% coins for 3 days.",
            StartMonth = 1, StartDay = 1, DurationDays = 3,
            PooRateMultiplier = 2.0f, XPMultiplier = 1.0f, CoinMultiplier = 1.5f,
            RewardSkinId = "candy", BannerColorHex = "#FFD700"
        },
        new EventDef
        {
            EventId = "summer_swarm", DisplayName = "Summer Swarm",
            Description = "Summer heat means more flies! +50% XP and double hatch speed.",
            StartMonth = 6, StartDay = 21, DurationDays = 7,
            PooRateMultiplier = 1.0f, XPMultiplier = 1.5f, CoinMultiplier = 1.0f,
            RewardSkinId = "neon", BannerColorHex = "#39FF14"
        },
        new EventDef
        {
            EventId = "spooky_swarm", DisplayName = "Spooky Swarm",
            Description = "Haunted poos and ghostly flies. +75% XP from battles.",
            StartMonth = 10, StartDay = 28, DurationDays = 5,
            PooRateMultiplier = 1.3f, XPMultiplier = 1.75f, CoinMultiplier = 1.0f,
            RewardSkinId = "shadow", BannerColorHex = "#1A1A2E"
        },
    };

    /// <summary>Returns the currently active event, or null if none.</summary>
    public static EventDef? GetActiveEvent()
    {
        var now = DateTime.Now;
        foreach (var e in _events)
        {
            var start = new DateTime(now.Year, e.StartMonth, e.StartDay);
            var end = start.AddDays(e.DurationDays);
            if (now >= start && now < end) return e;
        }
        return null;
    }

    public static IReadOnlyList<EventDef> AllEvents => _events;
}
