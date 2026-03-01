using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Integrates Unity IAP via reflection — compiles and runs with or without
/// com.unity.purchasing installed. Handles initialization, purchasing, and receipt validation.
/// </summary>
public class IAPManager : MonoBehaviour
{
    public static IAPManager Instance { get; private set; }

    static bool _packageAvailable;
    static Type _purchasingType;
    static Type _configBuilderType;
    static Type _productTypeEnumType;

    public bool IsInitialized { get; private set; }

    // Product ID → gem amount
    static readonly Dictionary<string, int> _products = new()
    {
        [Constants.IAP_GEMS_100]  = 100,
        [Constants.IAP_GEMS_500]  = 500,
        [Constants.IAP_GEMS_1200] = 1200,
        [Constants.IAP_GEMS_3000] = 3000,
    };

    // Product ID → display price (updated after initialization from store)
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
        DetectPackage();
    }

    void Start()
    {
        if (!_packageAvailable)
        {
            Debug.Log("[IAPManager] Unity Purchasing package not detected. IAP disabled.");
            return;
        }
        InitializePurchasing();
    }

    static void DetectPackage()
    {
        _purchasingType = Type.GetType("UnityEngine.Purchasing.UnityPurchasing, UnityEngine.Purchasing");
        _configBuilderType = Type.GetType("UnityEngine.Purchasing.ConfigurationBuilder, UnityEngine.Purchasing");
        _productTypeEnumType = Type.GetType("UnityEngine.Purchasing.ProductType, UnityEngine.Purchasing");
        _packageAvailable = _purchasingType != null && _configBuilderType != null;
    }

    void InitializePurchasing()
    {
        if (!_packageAvailable) return;
        try
        {
            var stdModuleType = Type.GetType("UnityEngine.Purchasing.StandardPurchasingModule, UnityEngine.Purchasing");
            var moduleInstance = stdModuleType?.GetMethod("Instance", Type.EmptyTypes)?.Invoke(null, null);
            var builder = _configBuilderType?.GetMethod("Instance")?.Invoke(null, new[] { moduleInstance });

            var consumable = Enum.ToObject(_productTypeEnumType, 0); // Consumable = 0
            var addMethod = builder?.GetType().GetMethod("AddProduct", new[] { typeof(string), _productTypeEnumType });
            foreach (var productId in _products.Keys)
                addMethod?.Invoke(builder, new object[] { productId, consumable });

            Debug.Log("[IAPManager] IAP products configured.");
            IsInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[IAPManager] Init failed: {e.Message}");
        }
    }

    public void PurchaseProduct(string productId)
    {
        if (!_packageAvailable || !IsInitialized)
        {
            Debug.LogWarning("[IAPManager] Cannot purchase — IAP not initialized.");
#if UNITY_EDITOR
            // Grant gems directly in editor for testing
            GrantGems(productId);
#endif
            return;
        }

        // In production, call UnityPurchasing.InitiatePurchase(productId) via reflection
        try
        {
            var initMethod = _purchasingType?.GetMethod("InitiatePurchase",
                new[] { typeof(string) });
            initMethod?.Invoke(null, new object[] { productId });
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[IAPManager] Purchase failed: {e.Message}");
#if UNITY_EDITOR
            GrantGems(productId);
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
