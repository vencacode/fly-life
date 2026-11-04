using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// In-app purchase facade. For the first playable build IAP is off
/// (FeatureFlags.IAP_ENABLED = false) and every call is a graceful no-op.
///
/// The previous reflection-based integration targeted the legacy
/// UnityPurchasing/ConfigurationBuilder API, which IAP 5.x marks obsolete and
/// which never actually initialized the store. Real integration against the
/// IAP 5 StoreController API is Phase 10 Task 66; it should plug into
/// InitializeStore() / PurchaseProduct() and call OnPurchaseComplete().
/// </summary>
public class IAPManager : MonoBehaviour
{
    public static IAPManager Instance { get; private set; }

    public bool IsInitialized { get; private set; }

    // Product ID → gem amount
    static readonly Dictionary<string, int> _products = new()
    {
        [Constants.IAP_GEMS_100]  = 100,
        [Constants.IAP_GEMS_500]  = 500,
        [Constants.IAP_GEMS_1200] = 1200,
        [Constants.IAP_GEMS_3000] = 3000,
    };

    // Product ID → display price (would be replaced by localized store prices)
    readonly Dictionary<string, string> _prices = new()
    {
        [Constants.IAP_GEMS_100]  = "$0.99",
        [Constants.IAP_GEMS_500]  = "$4.99",
        [Constants.IAP_GEMS_1200] = "$9.99",
        [Constants.IAP_GEMS_3000] = "$19.99",
    };

    public static IReadOnlyDictionary<string, int> Products => _products;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        if (!FeatureFlags.IAP_ENABLED)
        {
            Debug.Log("[IAPManager] IAP disabled via FeatureFlags.");
            return;
        }
        InitializeStore();
    }

    void InitializeStore()
    {
        // Store SDK not integrated yet (Phase 10 Task 66).
        Debug.LogWarning("[IAPManager] IAP_ENABLED is true but no store integration exists yet.");
        IsInitialized = false;
    }

    public void PurchaseProduct(string productId)
    {
        if (!FeatureFlags.IAP_ENABLED)
        {
            Debug.Log("[IAPManager] IAP disabled.");
            ToastManager.Instance?.ShowToast("Purchases not available yet.", ToastType.Warning);
            return;
        }
        if (!IsInitialized)
        {
            Debug.LogWarning("[IAPManager] Cannot purchase — store not initialized.");
#if UNITY_EDITOR
            GrantGems(productId); // editor-only test path
#else
            ToastManager.Instance?.ShowToast("Store unavailable. Try again later.", ToastType.Warning);
#endif
        }
    }

    void GrantGems(string productId)
    {
        if (!_products.TryGetValue(productId, out int gems)) return;
        ResourceManager.Instance?.AddGems(gems);
        AnalyticsManager.Instance?.TrackGemPurchase(productId, gems);
        Debug.Log($"[IAPManager] Granted {gems} gems for product {productId}");
        ToastManager.Instance?.ShowToast($"+{gems} Gems purchased!", ToastType.Reward);
    }

    /// <summary>Call from store callback after successful purchase.</summary>
    public void OnPurchaseComplete(string productId)
    {
        GrantGems(productId);
    }

    public string GetPrice(string productId)
        => _prices.TryGetValue(productId, out var p) ? p : "???";

    public int GetGemAmount(string productId)
        => _products.TryGetValue(productId, out var g) ? g : 0;
}
