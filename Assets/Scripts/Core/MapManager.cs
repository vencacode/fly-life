using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
        if (PooManager.Instance == null)
            Debug.LogWarning("[MapManager] PooManager not found — ensure Bootstrap scene loaded first.");
        SpawnTiles();
        RefreshAllTiles();
        UIEventBus.OnResourceChanged += OnResourceChanged;
    }

    void OnDestroy() => UIEventBus.OnResourceChanged -= OnResourceChanged;

    void Update()
    {
        // Detect tap/click on empty space to close panel
        if (_selectedDistrictId == null) return;

        bool tapped = false;
        Vector2 screenPos = Vector2.zero;

        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            tapped = true;
            screenPos = Input.GetTouch(0).position;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            tapped = true;
            screenPos = Input.mousePosition;
        }

        if (!tapped) return;

        // Ignore if tap is over UI (buttons, panels, etc.)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (Input.touchCount > 0 && EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;

        // Raycast into world — if no tile collider hit, deselect
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector2 worldPos = cam.ScreenToWorldPoint(screenPos);
        var hit = Physics2D.OverlapPoint(worldPos);
        if (hit == null)
            DeselectCurrent();
    }

    public void DeselectCurrent()
    {
        if (_selectedDistrictId != null && _tiles.TryGetValue(_selectedDistrictId, out var prev))
            prev.SetSelected(false);
        _selectedDistrictId = null;
        InfoPanel?.Hide();
    }

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
        if (!_tiles.TryGetValue(districtId, out var tile))
        {
            Debug.LogWarning($"[MapManager] Tile '{districtId}' not found in _tiles dictionary.");
            return;
        }
        tile.SetSelected(true);

        if (TerritoryManager.Instance != null &&
            TerritoryManager.Instance.Districts.TryGetValue(districtId, out var d))
            InfoPanel?.Show(d);
    }

    void OnResourceChanged(string type, int newVal) => RefreshAllTiles();

#if UNITY_EDITOR
    void OnValidate()
    {
        if (DistrictTilePrefab == null) Debug.LogWarning("[MapManager] DistrictTilePrefab not assigned!", this);
    }
#endif
}
