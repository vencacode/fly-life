using UnityEngine;
using TMPro;

/// <summary>
/// Top HUD bar showing all 4 resources.
/// Subscribes to UIEventBus for live updates.
/// </summary>
public class ResourceHUD : MonoBehaviour
{
    public TextMeshProUGUI PooResourcesText;
    public TextMeshProUGUI TrashResourcesText;
    public TextMeshProUGUI ClanCoinsText;
    public TextMeshProUGUI GemsText;

    void Start()
    {
        UIEventBus.OnResourceChanged += OnResourceChanged;
        RefreshAll();
    }

    void OnDestroy() => UIEventBus.OnResourceChanged -= OnResourceChanged;

    void RefreshAll()
    {
        if (ResourceManager.Instance == null) return;
        UpdateText(PooResourcesText,   ResourceManager.Instance.PooResources);
        UpdateText(TrashResourcesText, ResourceManager.Instance.TrashResources);
        UpdateText(ClanCoinsText,      ResourceManager.Instance.ClanCoins);
        UpdateText(GemsText,           ResourceManager.Instance.Gems);
    }

    void OnResourceChanged(string type, int newVal)
    {
        switch (type)
        {
            case "PooResources":   UpdateText(PooResourcesText,   newVal); break;
            case "TrashResources": UpdateText(TrashResourcesText, newVal); break;
            case "ClanCoins":      UpdateText(ClanCoinsText,      newVal); break;
            case "Gems":           UpdateText(GemsText,           newVal); break;
        }
    }

    void UpdateText(TextMeshProUGUI label, int val)
    {
        if (label) label.text = val >= 1000 ? $"{val/1000f:0.#}k" : val.ToString();
    }
}
