using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Calculates and stores player score.
/// In a real game this would sync with a server — here we track local + simulated rivals.
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    [System.Serializable]
    public class LeaderboardEntry
    {
        public string PlayerName;
        public string ClanName;
        public int Score;
        public int Districts;
        public int ClanLevel;
        public int TotalFlies;
        public bool IsPlayer;
    }

    List<LeaderboardEntry> _entries = new();
    public IReadOnlyList<LeaderboardEntry> Entries => _entries;
    public int PlayerScore { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        GenerateRivals();
        InvokeRepeating(nameof(RefreshScore), 2f, 30f);
    }

    public void RefreshScore()
    {
        int districts = TerritoryManager.Instance?.GetPlayerDistricts().Count ?? 0;
        int clanLevel = ClanManager.Instance?.ClanLevel ?? 1;
        int totalFlies = ClanManager.Instance?.TotalFlies ?? 0;

        PlayerScore = (districts * 100) + (clanLevel * 50) + (totalFlies * 10);

        var playerEntry = _entries.FirstOrDefault(e => e.IsPlayer);
        if (playerEntry != null)
        {
            playerEntry.Score = PlayerScore;
            playerEntry.Districts = districts;
            playerEntry.ClanLevel = clanLevel;
            playerEntry.TotalFlies = totalFlies;
            playerEntry.ClanName = ClanManager.Instance?.ClanName ?? "The Fly Lifers";
        }

        _entries.Sort((a, b) => b.Score.CompareTo(a.Score));
        UIEventBus.Emit(UIEvent.LeaderboardUpdated);
    }

    void GenerateRivals()
    {
        _entries.Add(new LeaderboardEntry
        {
            PlayerName = "You",
            ClanName = ClanManager.Instance?.ClanName ?? "The Fly Lifers",
            Score = 0, Districts = 0, ClanLevel = 1, TotalFlies = 1,
            IsPlayer = true
        });

        string[] names = {
            "Lord Buzzkill", "Queen Maggot", "Sir Poopsalot", "The Dung Beetles",
            "Swarm Master", "Captain Compost", "Baron Von Stink", "Lady Larva",
            "Professor Poo", "General Gnat"
        };
        string[] clans = {
            "Dung Dynasty", "Maggot Militia", "Poo Pirates", "Beetle Brigade",
            "Swarm Squad", "Compost Crew", "Stink Syndicate", "Larva Legion",
            "Poo Professors", "Gnat Gang"
        };
        var rng = new System.Random(42);
        for (int i = 0; i < names.Length; i++)
        {
            int d = rng.Next(0, 6);
            int lvl = rng.Next(1, 12);
            int flies = rng.Next(1, 30);
            _entries.Add(new LeaderboardEntry
            {
                PlayerName = names[i],
                ClanName = clans[i],
                Score = (d * 100) + (lvl * 50) + (flies * 10),
                Districts = d,
                ClanLevel = lvl,
                TotalFlies = flies,
                IsPlayer = false
            });
        }

        _entries.Sort((a, b) => b.Score.CompareTo(a.Score));
    }
}
