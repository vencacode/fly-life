using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bottom panel shown when player taps a district.
/// Shows district info and action buttons.
/// </summary>
public class DistrictInfoPanel : MonoBehaviour
{
    [Header("Labels")]
    public TextMeshProUGUI DistrictNameText;
    public TextMeshProUGUI OwnerText;
    public TextMeshProUGUI PooCountText;
    public TextMeshProUGUI BonusText;

    [Header("Buttons")]
    public Button SendFliesButton;
    public Button AttackButton;
    public Button ClaimButton;

    District _current;

    void Awake()
    {
        gameObject.SetActive(false);
        SendFliesButton?.onClick.AddListener(OnSendFlies);
        AttackButton?.onClick.AddListener(OnAttack);
        ClaimButton?.onClick.AddListener(OnClaim);
    }

    public void Show(District d)
    {
        _current = d;
        gameObject.SetActive(true);

        DistrictNameText.text = d.Name;

        bool isPlayer = d.OwnerId == "player";
        bool isEnemy  = !string.IsNullOrEmpty(d.OwnerId) && !isPlayer;
        bool isNeutral = string.IsNullOrEmpty(d.OwnerId);

        OwnerText.text    = isPlayer ? "🟢 Yours" : isEnemy ? "🔴 Enemy" : "⚪ Unclaimed";
        PooCountText.text = $"💩 {d.PooIds?.Count ?? 0} poos spotted";
        BonusText.text    = GetBonusText(d);

        SendFliesButton?.gameObject.SetActive(isPlayer);
        AttackButton?.gameObject.SetActive(isEnemy);
        ClaimButton?.gameObject.SetActive(isNeutral);
    }

    public void Hide() => gameObject.SetActive(false);

    string GetBonusText(District d)
    {
        if (string.IsNullOrEmpty(d.OwnerId)) return "";
        return d.OwnerId == "player"
            ? "✨ +10% egg hatch speed"
            : "⚠️ Enemy gets +10% resources";
    }

    void OnSendFlies()
    {
        if (_current == null) return;
        Debug.Log($"[InfoPanel] Send flies to {_current.Name}");
        // TODO: open clan deployment screen
    }

    void OnAttack()
    {
        if (_current == null) return;
        Debug.Log($"[InfoPanel] Attack {_current.Name}");
        // TODO: trigger battle scene
        // SceneManager.LoadScene("Battle");
    }

    void OnClaim()
    {
        if (_current == null) return;
        Debug.Log($"[InfoPanel] Claim {_current.Name}");
        TerritoryManager.Instance?.ClaimDistrict(_current.Id, "player");
        MapManager.Instance?.RefreshAllTiles();
        Show(_current); // refresh panel
    }
}
