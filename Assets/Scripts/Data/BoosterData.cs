using UnityEngine;

[CreateAssetMenu(fileName = "NewBooster", menuName = "FlyLife/BoosterData")]
public class BoosterData : ScriptableObject
{
    [Header("Identity")]
    public string BoosterName;
    public Sprite Icon;
    [TextArea(1,3)] public string Description;
    public BoosterType Type;

    [Header("Effect")]
    public float Multiplier   = 2.0f;
    public float DurationSec  = 3600f;

    [Header("Cost")]
    public int GemCost         = 30;
    public bool CanWatchAd     = true;
    public int AdCooldownSec   = 600;   // 10 min between uses via ad

    [Header("Unlock")]
    public int PooCoinCost = 0;
}

[System.Serializable]
public class ActiveBooster
{
    public string BoosterDataId;
    public float  ActivatedAt;   // Time.time
    public float  Duration;
    public bool   IsActive => UnityEngine.Time.time - ActivatedAt < Duration;
    public float  RemainingSeconds => Duration - (UnityEngine.Time.time - ActivatedAt);
}

public enum BoosterType
{
    PooRadar,       // smell radius
    HatchSpeed,     // eggs hatch faster
    FertilitySpike, // more eggs per lay
    WarDrums,       // battle attack bonus
    ScoutTraining,  // scouts find poo faster
    TerritoryShield // can't be attacked
}
