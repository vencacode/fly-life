using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// Every 5 minutes, enemy clans attempt to re-capture adjacent neutral or player districts.
/// </summary>
public class EnemyAIManager : MonoBehaviour
{
    public static EnemyAIManager Instance { get; private set; }
    public float AttackIntervalSeconds = Constants.ENEMY_ATTACK_INTERVAL;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start() => StartCoroutine(EnemyAttackLoop());

    IEnumerator EnemyAttackLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(AttackIntervalSeconds);
            TryEnemyAttack();
        }
    }

    void TryEnemyAttack()
    {
        var tm = TerritoryManager.Instance;
        if (tm == null) return;
        string playerClanId = ClanManager.Instance?.ClanId ?? "player_clan";

        // Find enemy-owned districts adjacent to player or neutral districts
        var enemyDistricts = tm.Districts.Values
            .Where(d => !string.IsNullOrEmpty(d.OwnerId) && d.OwnerId.StartsWith("enemy_clan"))
            .ToList();

        foreach (var enemy in enemyDistricts)
        {
            var adjacents = tm.Districts.Values.Where(d =>
                Mathf.Abs(d.GridX - enemy.GridX) <= 1 &&
                Mathf.Abs(d.GridY - enemy.GridY) <= 1 &&
                d.Id != enemy.Id &&
                d.Ring > 1 && // protect starter area (ring 0-1) from enemy takeover
                (string.IsNullOrEmpty(d.OwnerId) || d.OwnerId == playerClanId)
            ).ToList();

            if (adjacents.Count == 0) continue;
            var target = adjacents[Random.Range(0, adjacents.Count)];

            // 30% chance enemy captures neutral, 15% against player
            float chance = string.IsNullOrEmpty(target.OwnerId)
                ? Constants.ENEMY_CHANCE_VS_NEUTRAL
                : Constants.ENEMY_CHANCE_VS_PLAYER;
            if (Random.value < chance)
            {
                bool wasPlayerOwned = target.OwnerId == playerClanId;
                target.OwnerId = enemy.OwnerId;
                MapManager.Instance?.RefreshAllTiles();
                if (wasPlayerOwned)
                {
                    UIEventBus.Emit(UIEvent.DistrictLost, target);
                    NotificationManager.Instance?.NotifyDistrictUnderAttack(target.Name);
                }
                Debug.Log($"[EnemyAI] {enemy.OwnerId} captured {target.Name}");
            }
        }
    }
}
