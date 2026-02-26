using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Visual tile for a district on the city map.
/// Shows ownership color, name, and handles tap-to-select.
/// </summary>
public class DistrictTile : MonoBehaviour, IPointerClickHandler
{
    [Header("Visuals")]
    public SpriteRenderer Background;
    public SpriteRenderer Border;
    public TextMeshPro NameLabel;
    public TextMeshPro StatsLabel;

    [Header("Colors")]
    public Color PlayerColor    = new Color(0.2f, 0.8f, 0.3f, 0.9f);
    public Color EnemyColor     = new Color(0.9f, 0.2f, 0.2f, 0.9f);
    public Color NeutralColor   = new Color(0.6f, 0.6f, 0.6f, 0.6f);
    public Color SelectedColor  = new Color(1f,   0.9f, 0.2f, 1.0f);

    public string DistrictId { get; private set; }
    bool _selected;

    public void Setup(string districtId, string displayName)
    {
        DistrictId = districtId;
        if (NameLabel) NameLabel.text = displayName;
        Refresh();
    }

    public void Refresh()
    {
        if (TerritoryManager.Instance == null) return;
        if (!TerritoryManager.Instance.Districts.TryGetValue(DistrictId, out var d)) return;

        Color c = NeutralColor;
        if (d.OwnerId == (ClanManager.Instance?.ClanId ?? "player_clan")) c = PlayerColor;
        else if (!string.IsNullOrEmpty(d.OwnerId)) c = EnemyColor;

        if (Background) Background.color = _selected ? SelectedColor : c;
        if (StatsLabel)
        {
            int pooCount = d.PooIds?.Count ?? 0;
            StatsLabel.text = pooCount > 0 ? $"{pooCount} 💩" : "";
        }
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        Refresh();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        MapManager.Instance?.OnDistrictTapped(DistrictId);
    }
}
