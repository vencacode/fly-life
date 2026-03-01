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
        // If won, claim the district
        if (playerWon && !string.IsNullOrEmpty(districtId))
            TerritoryManager.Instance?.ClaimDistrict(
                districtId,
                ClanManager.Instance?.ClanId ?? "player_clan");

        // Return to map after delay (BattleUI shows result overlay)
        Invoke(nameof(ReturnToMap), 2.5f);
    }

    void ReturnToMap()
    {
        GameManager.Instance?.LoadScene("MainMap");
    }
}
