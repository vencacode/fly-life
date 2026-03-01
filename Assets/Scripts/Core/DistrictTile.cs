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
    public Color LockedColor    = new Color(0.2f, 0.2f, 0.2f, 0.8f);

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

        var theme = DistrictThemeData.Get(d.Theme);
        string playerClanId = ClanManager.Instance?.ClanId ?? "player_clan";
        bool isPlayer = d.OwnerId == playerClanId;
        bool isEnemy = !string.IsNullOrEmpty(d.OwnerId) && !isPlayer;

        // Lock check
        int playerLevel = ClanManager.Instance?.ClanLevel ?? 1;
        bool locked = d.IsLocked(playerLevel);

        // Theme-based coloring: owned = full brightness, enemy = red tint, neutral = dimmed, locked = dark
        Color c;
        if (locked)
            c = LockedColor;
        else if (isPlayer)
            c = theme.ThemeColor;
        else if (isEnemy)
            c = Color.Lerp(theme.ThemeColor, EnemyColor, 0.5f);
        else
            c = new Color(theme.ThemeColor.r, theme.ThemeColor.g, theme.ThemeColor.b, 0.5f);

        // Apply ring-based tile sprite
        if (Background)
        {
            Background.sprite = TileArtGenerator.GetTileSprite(d.Ring);
            Background.color = _selected ? SelectedColor : c;
        }

        // Name label: icon + name (+ BOSS tag for boss districts)
        string bossTag = d.IsBoss ? " [BOSS]" : "";
        if (NameLabel) NameLabel.text = $"{theme.Icon} {d.Name}{bossTag}";

        if (StatsLabel)
        {
            int pooCount = d.PooIds?.Count ?? 0;
            int flyCount = ClanManager.Instance?.GetDeployedCount(d.Id) ?? 0;
            string parts = "";
            if (flyCount > 0) parts += $"{flyCount} fly";
            if (pooCount > 0) parts += (parts.Length > 0 ? " | " : "") + $"{pooCount} poo";
            StatsLabel.text = parts;
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
