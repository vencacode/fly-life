/// <summary>
/// All game-wide tuning constants in one place. Adjust here to balance the game.
/// </summary>
public static class Constants
{
    // --- Clan ---
    public const int    MAX_CLAN_SIZE           = 200;
    public const int    STARTING_FLY_COUNT      = 1;
    public const float  BASE_EGG_HATCH_TIME_SEC = 300f;   // 5 minutes
    public const int    EGGS_PER_LAY             = 5;
    public const float  FLY_LIFESPAN_SEC         = 86400f; // 24 hours real-time (scaled in game)

    // --- Roles distribution limits ---
    public const float  MAX_SCOUT_RATIO   = 0.5f;
    public const float  MAX_SOLDIER_RATIO = 0.5f;
    public const float  MAX_WORKER_RATIO  = 0.5f;

    // --- Poo ---
    public const float  POO_BASE_DURATION_SEC    = 3600f;  // 1 hour before drying
    public const float  POO_DISCOVERY_RADIUS_BASE = 5f;    // Unity units
    public const int    MAX_POOS_PER_DISTRICT    = 10;

    // --- Territory ---
    public const float  DISTRICT_CLAIM_THRESHOLD = 0.60f;  // 60% of poos to claim district
    public const float  DISTRICT_LOSS_THRESHOLD  = 0.40f;  // lose if you drop below 40%

    // --- Battle ---
    public const int    BATTLE_SLOTS            = 5;
    public const int    BOSS_BATTLE_SLOTS       = 7;
    public const float  BATTLE_ROUND_DELAY      = 0.5f;    // seconds between rounds
    public const float  BATTLE_ATTACK_DELAY     = 0.3f;    // seconds between attacks in a round
    public const float  ITEM_DROP_WINDOW_SEC    = 3f;      // seconds to tap a dropped item
    public const int    XP_PER_BATTLE_WIN       = 50;
    public const int    XP_PER_BATTLE_LOSS      = 10;
    public const int    XP_PER_LEVEL            = 100;     // XP needed per fly level
    public const int    CLAN_XP_PER_LEVEL       = 100;     // XP needed per clan level
    public const float  GEAR_DROP_CHANCE        = 0.4f;    // 40% chance per battle win
    public const float  BOSS_GEAR_DROP_CHANCE   = 1.0f;    // guaranteed drop from boss

    // --- Battle rewards ---
    public const int    BATTLE_WIN_COINS_MIN    = 10;
    public const int    BATTLE_WIN_COINS_MAX    = 30;
    public const int    BOSS_WIN_COINS_MIN      = 40;
    public const int    BOSS_WIN_COINS_MAX      = 80;

    // --- Leveling ---
    public const float  STAT_ATTACK_GROWTH      = 1.1f;    // attack multiplier per level
    public const float  STAT_DEFENSE_GROWTH     = 1.1f;    // defense multiplier per level
    public const float  STAT_HEALTH_GROWTH      = 1.15f;   // health multiplier per level

    // --- Training & Upgrades ---
    public const int    TRAINING_COST           = 50;      // PooCoins to train a fly
    public const int    TRAINING_XP             = 30;      // XP granted per training
    public const int    GEAR_UPGRADE_COST       = 100;     // PooCoins to upgrade gear +1

    // --- Economy ---
    public const int    STARTING_GEMS           = 50;
    public const int    STARTING_CLAN_COINS     = 100;
    public const int    AD_BOOSTER_COOLDOWN_SEC = 600;     // 10 min between rewarded ads per booster

    // --- Gem income (no IAP) ---
    public const int    GEMS_DAILY_LOGIN      = 5;
    public const int    GEMS_BOSS_KILL        = 10;
    public const int    GEMS_FIRST_DISTRICT   = 15;   // one-time tutorial reward
    public const int    GEMS_FULL_MAP_CLEAR   = 50;   // one-time reward for owning all districts

    // --- Booster costs ---
    public const int    BOOSTER_POO_RADAR_GEMS  = 50;
    public const int    BOOSTER_HATCH_GEMS      = 30;
    public const int    BOOSTER_FERTILITY_GEMS  = 25;
    public const int    BOOSTER_SHIELD_GEMS     = 30;

    // --- Promotion ---
    public const int    PROMOTION_LEVEL_REQ   = 10;
    public const int    PROMOTION_COST        = 500;     // PooCoins
    public const float  ELITE_STAT_MULTIPLIER = 1.25f;   // 25% stat boost

    // --- Breeding ---
    public const int   BREEDING_COST       = 300;    // PooCoins
    public const float BREEDING_STAT_BONUS = 0.10f;  // +10% on top of best parent stat
    public const int   BREEDING_MIN_LEVEL  = 5;      // both parents must be level 5+

    // --- Offline Progress ---
    public const float OFFLINE_COINS_PER_DISTRICT_PER_HOUR = 10f;
    public const float OFFLINE_MAX_HOURS = 8f;  // cap at 8 hours

    // --- Stat Respec ---
    public const int RESPEC_COST = 150;  // PooCoins

    // --- Difficulty ---
    public const int RING0_DIFFICULTY = 1;
    public const int RING1_DIFFICULTY = 1;  // inner ring: attackable from start
    public const int RING2_DIFFICULTY = 2;
    public const int RING3_DIFFICULTY = 3;
    public const int BOSS_DIFFICULTY  = 5;

    // --- Enemy AI ---
    public const float ENEMY_ATTACK_INTERVAL     = 300f;  // 5 min between attacks
    public const float ENEMY_CHANCE_VS_NEUTRAL   = 0.3f;
    public const float ENEMY_CHANCE_VS_PLAYER    = 0.15f;

    // --- IAP Product IDs ---
    public const string IAP_GEMS_100  = "com.flylife.gems100";
    public const string IAP_GEMS_500  = "com.flylife.gems500";
    public const string IAP_GEMS_1200 = "com.flylife.gems1200";
    public const string IAP_GEMS_3000 = "com.flylife.gems3000";

    // --- Scene names ---
    public const string SCENE_BOOTSTRAP        = "Bootstrap";
    public const string SCENE_MENU             = "MainMenu";
    public const string SCENE_CITY             = "MainMap";
    public const string SCENE_BATTLE           = "Battle";
    public const string SCENE_CLAN             = "ClanScreen";
}
