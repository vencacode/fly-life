using UnityEngine;

[CreateAssetMenu(fileName = "NewFlyRole", menuName = "FlyLife/FlyRoleData")]
public class FlyRoleData : ScriptableObject
{
    [Header("Identity")]
    public string RoleName;
    public FlyRole Role;
    public Sprite Icon;
    [TextArea(1,3)] public string Description;

    [Header("Base Stats")]
    public int BaseAttack   = 10;
    public int BaseDefense  = 10;
    public int BaseSpeed    = 10;
    public int BaseHealth   = 50;

    [Header("Role Bonus")]
    [Tooltip("Scout: poo find speed multiplier")]
    public float ScoutFindMultiplier   = 1.0f;
    [Tooltip("Worker: egg hatch efficiency multiplier")]
    public float WorkerHatchMultiplier = 1.0f;
    [Tooltip("Soldier: battle attack multiplier")]
    public float SoldierAttackMultiplier = 1.0f;
}

[System.Serializable]
public class FlyInstance
{
    public string   InstanceId;
    public FlyRole  Role;
    public int      Level = 1;
    public int      XP = 0;
    public int      Attack;
    public int      Defense;
    public int      Speed;
    public int      CurrentHealth;
    public int      MaxHealth;
    public string   SkinId;               // null/empty = default skin
    public string   DeployedDistrictId;   // null/empty = available, otherwise deployed to this district
    public string   EquippedWeaponId;
    public string   EquippedHeadId;
    public string   EquippedBodyId;
    public string   EquippedFeetId;
    public string   EquippedWingsId;
    public string   EquippedOffhandId;
    public string   EquippedAccessoryId;  // 7th slot (elite only)
    public bool     IsElite;              // promoted to elite tier
    public bool     IsAlive    => CurrentHealth > 0;
    public bool     IsDeployed => !string.IsNullOrEmpty(DeployedDistrictId);
}

public enum FlyRole { Scout, Soldier, Worker }
