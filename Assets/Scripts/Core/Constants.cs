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
    public const float  ITEM_DROP_WINDOW_SEC    = 3f;      // seconds to tap a dropped item
    public const int    XP_PER_BATTLE_WIN       = 50;
    public const int    XP_PER_BATTLE_LOSS      = 10;

    // --- Training & Upgrades ---
    public const int    TRAINING_COST           = 50;      // PooCoins to train a fly
    public const int    TRAINING_XP             = 30;      // XP granted per training
    public const int    GEAR_UPGRADE_COST       = 100;     // PooCoins to upgrade gear +1

    // --- Economy ---
    public const int    STARTING_GEMS           = 50;
    public const int    STARTING_CLAN_COINS     = 100;
    public const int    AD_BOOSTER_COOLDOWN_SEC = 600;     // 10 min between rewarded ads per booster

    // --- Booster costs ---
    public const int    BOOSTER_POO_RADAR_GEMS  = 50;
    public const int    BOOSTER_HATCH_GEMS      = 30;
    public const int    BOOSTER_FERTILITY_GEMS  = 25;
    public const int    BOOSTER_SHIELD_GEMS     = 30;

    // --- Scene names ---
    public const string SCENE_MENU             = "MainMenu";
    public const string SCENE_CITY             = "MainMap";
    public const string SCENE_BATTLE           = "Battle";
    public const string SCENE_CLAN             = "ClanScreen";
}
