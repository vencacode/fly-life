using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestrates the MainMap scene: spawns district tiles,
/// handles selection, triggers battles and poo spawning.
/// </summary>
public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [Header("References")]
    public GameObject DistrictTilePrefab;
    public Transform  TilesParent;
    public DistrictInfoPanel InfoPanel;

    [Header("Layout")]
    public float TileSize   = 10f;
    public float TileGap    = 0.3f;

    Dictionary<string, DistrictTile> _tiles = new();
    string _selectedDistrictId;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        SpawnTiles();
        RefreshAllTiles();
        UIEventBus.OnResourceChanged += OnResourceChanged;
    }

    void OnDestroy() => UIEventBus.OnResourceChanged -= OnResourceChanged;

    void SpawnTiles()
    {
        if (TerritoryManager.Instance == null)
        {
            Debug.LogError("[MapManager] TerritoryManager not found.");
            return;
        }

        foreach (var kvp in TerritoryManager.Instance.Districts)
        {
            var d = kvp.Value;
            float step = TileSize + TileGap;
            Vector3 pos = new Vector3(d.GridX * step, d.GridY * step, 0f);

            GameObject go = Instantiate(DistrictTilePrefab, pos, Quaternion.identity, TilesParent);
            go.name = $"Tile_{d.Id}";

            var tile = go.GetComponent<DistrictTile>();
            tile.Setup(d.Id, d.Name);
            _tiles[d.Id] = tile;
        }
    }

    public void RefreshAllTiles()
    {
        foreach (var t in _tiles.Values) t.Refresh();
    }

    public void OnDistrictTapped(string districtId)
    {
        // Deselect previous
        if (_selectedDistrictId != null && _tiles.TryGetValue(_selectedDistrictId, out var prev))
            prev.SetSelected(false);

        if (_selectedDistrictId == districtId)
        {
            // Tap same tile twice — deselect
            _selectedDistrictId = null;
            InfoPanel?.Hide();
            return;
        }

        _selectedDistrictId = districtId;
        _tiles[districtId].SetSelected(true);

        if (TerritoryManager.Instance.Districts.TryGetValue(districtId, out var d))
            InfoPanel?.Show(d);
    }

    void OnResourceChanged(string type, int newVal) => RefreshAllTiles();
}
