using UnityEngine;

/// <summary>
/// Manages rewarded ads. Currently a stub — simulates ad completion.
/// Replace with real Google Mobile Ads SDK when ready to ship.
/// </summary>
public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    /// <summary>
    /// Show a rewarded ad. onComplete fires when the user finishes watching.
    /// Currently stubs by completing immediately (no real ad SDK).
    /// </summary>
    public void ShowRewardedAd(System.Action onComplete)
    {
        Debug.Log("[AdManager] Rewarded ad shown (stub). Granting reward.");
        // TODO: Replace with real AdMob call:
        // rewardedAd.Show((reward) => onComplete?.Invoke());
        onComplete?.Invoke();
    }

    /// <summary>Grant double XP booster for next battle.</summary>
    public void GrantWarDrumsBooster()
    {
        ClanManager.Instance?.ActivateBooster(new ActiveBooster
        {
            BoosterDataId = "WarDrums",
            ActivatedAt   = Time.time,
            Duration      = 600f // 10 minutes
        });
        Debug.Log("[AdManager] WarDrums booster activated from ad reward.");
    }
}
