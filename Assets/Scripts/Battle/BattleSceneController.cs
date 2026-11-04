using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Entry point for the Battle scene.
/// Reads pending battle data from BattleManager and kicks off the fight.
/// </summary>
public class BattleSceneController : MonoBehaviour
{
    void Start()
    {
        if (BattleManager.Instance == null)
        {
            Debug.LogError("[BattleScene] No BattleManager found. Loading Bootstrap.");
            SceneManager.LoadScene("Bootstrap");
            return;
        }

        // If no battle was prepared (e.g. entered scene directly), generate fallback enemies
        if (BattleContext.EnemyFlies == null || BattleContext.EnemyFlies.Count == 0)
        {
            Debug.LogWarning("[BattleScene] No pending battle data — generating fallback enemies.");
            var enemies = BattleManager.GenerateEnemyFlies(Constants.BATTLE_SLOTS);
            BattleManager.Instance.PrepareBattle("", enemies);
        }

        BattleManager.Instance.ExecutePendingBattle(OnBattleComplete);
    }

    void OnBattleComplete(bool playerWon, string districtId)
    {
        if (playerWon && !string.IsNullOrEmpty(districtId))
        {
            string clanId = ClanManager.Instance?.ClanId ?? "player_clan";
            int ownedBefore = TerritoryManager.Instance?.GetPlayerDistricts().Count ?? 0;

            TerritoryManager.Instance?.ClaimDistrict(districtId, clanId);

            int ownedAfter = TerritoryManager.Instance?.GetPlayerDistricts().Count ?? 0;

            // First district ever captured — bonus gems
            if (ownedBefore == 0 && ownedAfter > 0)
            {
                ResourceManager.Instance?.AddGems(Constants.GEMS_FIRST_DISTRICT);
                ToastManager.Instance?.ShowToast($"+{Constants.GEMS_FIRST_DISTRICT} Gems (First conquest!)", ToastType.Reward);
            }

            // All districts captured — one-time reward
            int totalDistricts = TerritoryManager.Instance?.TotalDistricts ?? 25;
            if (ownedAfter >= totalDistricts && ownedBefore < totalDistricts)
            {
                ResourceManager.Instance?.AddGems(Constants.GEMS_FULL_MAP_CLEAR);
                ToastManager.Instance?.ShowToast($"+{Constants.GEMS_FULL_MAP_CLEAR} Gems (World Domination!)", ToastType.Reward);
            }
        }

        // Return to map after delay (BattleUI shows result overlay)
        Invoke(nameof(ReturnToMap), 2.5f);
    }

    void ReturnToMap()
    {
        GameManager.Instance?.LoadScene("MainMap");
    }
}
