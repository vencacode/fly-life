using UnityEngine;

[CreateAssetMenu(fileName = "NewPoo", menuName = "FlyLife/PooData")]
public class PooData : ScriptableObject
{
    [Header("Identity")]
    public string PooName;
    public string Description;
    public Sprite Icon;
    public PooType Type;

    [Header("Attributes (randomised within range on spawn)")]
    [Range(1, 100)] public int FertilityMin   = 10;
    [Range(1, 100)] public int FertilityMax   = 40;
    [Range(1, 100)] public int ConsistencyMin = 20;
    [Range(1, 100)] public int ConsistencyMax = 60;
    [Range(1, 100)] public int SmellMin       = 10;
    [Range(1, 100)] public int SmellMax       = 50;
    [Range(5, 95)]  public int LeftoversMin   = 20;
    [Range(5, 95)]  public int LeftoversMax   = 80;

    [Header("Duration")]
    public float BaseDurationSeconds = 3600f;

    [Header("Rarity")]
    public PooRarity Rarity = PooRarity.Common;
    [Range(0f, 1f)] public float SpawnWeight = 0.5f;

    [Header("Funny lines on discovery")]
    [TextArea(2, 4)] public string[] DiscoveryQuotes;
}

[System.Serializable]
public class PooInstance
{
    public string   PooDataId;
    public int      Fertility;
    public int      Consistency;
    public int      Smell;
    public int      Leftovers;
    public float    SpawnTime;
    public float    Duration;
    public string   OwnerId;
    public UnityEngine.Vector2 WorldPosition;
    public string   DistrictId;

    public bool IsDry => UnityEngine.Time.time - SpawnTime >= Duration;
    public bool IsOwned => !string.IsNullOrEmpty(OwnerId);
    public int ResourceYield => UnityEngine.Mathf.RoundToInt((100 - Leftovers) / 10f);
}

public enum PooType   { Dog, Bird, Horse, Human, Cat, Compost, BabyDiaper }
public enum PooRarity { Common, Uncommon, Rare, Epic, Legendary }
