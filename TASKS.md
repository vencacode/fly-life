# Fly Life — Task List for Claude Code

Each task is self-contained. Complete them in order within each phase.
After each task, run `.\unity-cmd.ps1 '{"type": "refresh"}' -Timeout 120` and confirm zero errors before moving on.

---

## PHASE 2 — Playable Core Loop

### TASK 01 — Bootstrap → MainMap Scene Flow

**Goal:** GameManager persists from Bootstrap into MainMap. No more hacked-in local managers.

**Problem:** Currently MainMap has TerritoryManager, ClanManager, ResourceManager added locally as a workaround. When Bootstrap loads MainMap, DontDestroyOnLoad GameManager will conflict with these duplicates.

**Steps:**
1. Open `Assets/Scenes/MainMap.unity`
2. Remove these components from the `MapManager` GameObject: `TerritoryManager`, `ResourceManager`, `ClanManager` (keep `MapManager`, `PooManager`)
3. Open `Assets/Scenes/Bootstrap.unity`
4. Add a script `BootstrapLoader.cs` (create it) to the Bootstrap scene on an empty GameObject called `Loader`:
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    void Start()
    {
        // GameManager.Awake() has already run and called DontDestroyOnLoad
        // Just load the main scene
        SceneManager.LoadScene("MainMap");
    }
}
```
5. Verify the GameManager prefab in Bootstrap has all managers: `ClanManager`, `TerritoryManager`, `ResourceManager`, `PooManager`, `BattleManager`, `AudioManager`
6. In `MapManager.cs`, update `Start()` to get PooManager from GameManager if local one is missing:
```csharp
void Start()
{
    // Fall back to GameManager's PooManager if not set locally
    if (PooManager.Instance == null)
        Debug.LogWarning("[MapManager] PooManager not found — ensure Bootstrap scene loaded first.");
    SpawnTiles();
    RefreshAllTiles();
    UIEventBus.OnResourceChanged += OnResourceChanged;
}
```
7. Test: Play from Bootstrap scene. Should load MainMap with all managers intact and zero NullReferenceExceptions.

---

### TASK 02 — District Difficulty & Lock System

**Goal:** Districts have difficulty levels. Player can only attack districts within their fly level range.

**Files to modify:** `TerritoryManager.cs`, `District` class, `DistrictTile.cs`, `DistrictInfoPanel.cs`

**Steps:**

1. Add fields to the `District` class in `TerritoryManager.cs`:
```csharp
public int  DifficultyLevel;   // 1-6
public int  Ring;              // 0=center, 1=inner, 2=outer
public bool IsLocked(int playerLevel) => playerLevel < DifficultyLevel;
```

2. In `TerritoryManager.GenerateDistricts()`, assign Ring and DifficultyLevel based on distance from center (2,2):
```csharp
int ring = Mathf.Max(Mathf.Abs(x - 2), Mathf.Abs(y - 2)); // Chebyshev distance
d.Ring = ring;
d.DifficultyLevel = ring == 0 ? 1 : ring == 1 ? 3 : 5;
```

3. In `TerritoryManager.GenerateDistricts()`, assign starter enemies to all non-center districts:
```csharp
if (ring > 0) d.OwnerId = $"enemy_clan_{ring}"; // enemy_clan_1 or enemy_clan_2
```

4. Add a color for Locked districts in `DistrictTile.cs`:
```csharp
public Color LockedColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
```

5. In `DistrictTile.Refresh()`, check lock state:
```csharp
int playerLevel = ClanManager.Instance?.ClanLevel ?? 1;
bool locked = d.IsLocked(playerLevel);
if (locked) c = LockedColor;
```

6. In `DistrictInfoPanel.Show()`, show difficulty and lock state:
```csharp
// Add a DifficultyText label to the panel
DifficultyText.text = $"Difficulty: {d.DifficultyLevel} | Ring {d.Ring}";
bool locked = d.IsLocked(ClanManager.Instance?.ClanLevel ?? 1);
AttackButton?.gameObject.SetActive(isEnemy && !locked);
ClaimButton?.gameObject.SetActive(isNeutral && !locked);
if (locked) BonusText.text = $"🔒 Requires level {d.DifficultyLevel}";
```

7. Add `DifficultyText` TextMeshProUGUI field to `DistrictInfoPanel.cs` and wire it in Unity.

8. In `DistrictInfoPanel.GetBonusText()`, replace the stale egg hatch reference (egg hatching is cut from v1):
```csharp
string GetBonusText(District d)
{
    if (string.IsNullOrEmpty(d.OwnerId)) return "";
    return d.OwnerId == (ClanManager.Instance?.ClanId ?? "player_clan")
        ? "✨ +5 PooCoins/min income"
        : "⚠️ Enemy gets +10% resources";
}
```

---

### TASK 03 — Battle Scene Setup

**Goal:** Tapping Attack or Claim button on an available district loads the Battle scene and starts a fight.

**Files:** `BattleManager.cs`, `DistrictInfoPanel.cs`, new `BattleSceneController.cs`, new `BattleUI.cs` (stub)

**Prerequisites:** Ensure these exist before starting. If missing, create them first:
- `Constants.BATTLE_SLOTS` (int, value 5) in `Constants.cs`
- `FlyInstance` class with fields: `string InstanceId`, `FlyRole Role`, `int Level`, `int Attack`, `int Defense`, `int Speed`, `int MaxHealth`, `int CurrentHealth`, `int XP`, `bool IsAlive => CurrentHealth > 0`
- `FlyRole` enum: `Scout, Soldier, Worker`

**Steps:**

0. (If missing) Add to `Constants.cs`:
```csharp
public const int BATTLE_SLOTS = 5;
```
   (If missing) Create `Assets/Scripts/Data/FlyInstance.cs`:
```csharp
[System.Serializable]
public class FlyInstance
{
    public string InstanceId;
    public FlyRole Role;
    public int Level = 1;
    public int Attack = 10;
    public int Defense = 5;
    public int Speed = 5;
    public int MaxHealth = 50;
    public int CurrentHealth = 50;
    public int XP = 0;
    public bool IsAlive => CurrentHealth > 0;
}

public enum FlyRole { Scout, Soldier, Worker }
```

1. Create a stub `Assets/Scripts/Battle/BattleUI.cs` so `BattleSceneController` compiles (full implementation in Task 04):
```csharp
using UnityEngine;

/// <summary>Stub — full implementation in Task 04.</summary>
public class BattleUI : MonoBehaviour
{
    public void Initialize() { }
    public void ShowResult(bool playerWon) { }
}
```

2. Create `Assets/Scripts/Battle/BattleSceneController.cs`:
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Entry point for the Battle scene.
/// Reads pending battle data from BattleManager and kicks off the fight.
/// </summary>
public class BattleSceneController : MonoBehaviour
{
    public BattleUI UI;

    void Start()
    {
        if (BattleManager.Instance == null)
        {
            Debug.LogError("[BattleScene] No BattleManager found. Loading Bootstrap.");
            SceneManager.LoadScene("Bootstrap");
            return;
        }
        UI?.Initialize();
        BattleManager.Instance.ExecutePendingBattle(OnBattleComplete);
    }

    void OnBattleComplete(bool playerWon, string districtId)
    {
        UI?.ShowResult(playerWon);
        // After 2 seconds, return to map
        Invoke(nameof(ReturnToMap), 2.5f);
    }

    void ReturnToMap() => GameManager.Instance?.LoadScene("MainMap");
}
```

2. Modify `BattleManager.cs`:
   - Add `public string PendingDistrictId { get; private set; }`
   - Add `public void PrepareBattle(string districtId, List<FlyInstance> enemies)` that stores the pending battle
   - Add `public void ExecutePendingBattle(System.Action<bool, string> onComplete)` that runs the coroutine and calls callback

3. Modify `DistrictInfoPanel.OnAttack()` and `OnClaim()`:
```csharp
void OnAttack()
{
    if (_current == null) return;
    var enemies = GenerateEnemyFlies(_current.DifficultyLevel);
    BattleManager.Instance?.PrepareBattle(_current.Id, enemies);
    GameManager.Instance?.LoadScene("Battle");
}

void OnClaim() // same flow for neutral districts
{
    if (_current == null) return;
    var enemies = GenerateEnemyFlies(_current.DifficultyLevel);
    BattleManager.Instance?.PrepareBattle(_current.Id, enemies);
    GameManager.Instance?.LoadScene("Battle");
}

List<FlyInstance> GenerateEnemyFlies(int difficulty)
{
    var enemies = new List<FlyInstance>();
    for (int i = 0; i < Constants.BATTLE_SLOTS; i++)
        enemies.Add(new FlyInstance {
            InstanceId = $"enemy_{i}",
            Role = FlyRole.Soldier,
            Level = difficulty,
            Attack = 5 * difficulty,
            Defense = 3 * difficulty,
            Speed = 5,
            MaxHealth = 20 * difficulty,
            CurrentHealth = 20 * difficulty
        });
    return enemies;
}
```

4. Open `Assets/Scenes/Battle.unity` in Unity and set it up:
```powershell
.\unity-cmd.ps1 '[
  {"type": "open-scene", "path": "Assets/Scenes/Battle.unity"},
  {"type": "create", "path": "BattleController", "components": ["BattleSceneController"]},
  {"type": "create", "path": "UI", "components": ["Canvas"]},
  {"type": "save-scene"}
]'
```

---

### TASK 04 — Battle UI

**Goal:** Simple battle screen showing player flies vs enemy flies, round results, and win/loss screen.

**Requires:** Task 03 (BattleSceneController, FlyInstance, BattleManager updates)

**Files:** Replace stub `BattleUI.cs` with full implementation

**Replace `Assets/Scripts/Battle/BattleUI.cs` (overwrites stub from Task 03):**
```csharp
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BattleUI : MonoBehaviour
{
    [Header("Labels")]
    public TextMeshProUGUI RoundText;
    public TextMeshProUGUI LogText;
    public TextMeshProUGUI ResultText;

    [Header("Fly Slots — Player (5 slots)")]
    public TextMeshProUGUI[] PlayerSlotLabels;  // array of 5

    [Header("Fly Slots — Enemy (5 slots)")]
    public TextMeshProUGUI[] EnemySlotLabels;   // array of 5

    [Header("Result Panel")]
    public GameObject ResultPanel;
    public Button ReturnButton;

    public void Initialize()
    {
        if (ResultPanel) ResultPanel.SetActive(false);
        LogText.text = "Battle starting...";

        UIEventBus.On(UIEvent.BattleRoundStart, OnRoundStart);
        UIEventBus.On(UIEvent.BattleAttack,     OnAttack);
        UIEventBus.On(UIEvent.BattleStateChanged, OnStateChanged);
    }

    void OnDestroy()
    {
        UIEventBus.Off(UIEvent.BattleRoundStart, OnRoundStart);
        UIEventBus.Off(UIEvent.BattleAttack,     OnAttack);
        UIEventBus.Off(UIEvent.BattleStateChanged, OnStateChanged);
    }

    void OnRoundStart(object data)
    {
        if (RoundText) RoundText.text = $"Round {data}";
        RefreshSlots();
    }

    void OnAttack(object data)
    {
        if (data is not (FlyInstance attacker, FlyInstance target, int dmg)) return;
        AppendLog($"{attacker.Role} hits {target.Role} for {dmg} dmg (HP: {target.CurrentHealth}/{target.MaxHealth})");
        RefreshSlots();
    }

    void OnStateChanged(object data) { }

    void RefreshSlots()
    {
        var bm = BattleManager.Instance;
        if (bm == null) return;
        for (int i = 0; i < 5; i++)
        {
            var p = i < bm.PlayerSlots.Count ? bm.PlayerSlots[i] : null;
            var e = i < bm.EnemySlots.Count  ? bm.EnemySlots[i]  : null;
            if (PlayerSlotLabels != null && i < PlayerSlotLabels.Length && PlayerSlotLabels[i])
                PlayerSlotLabels[i].text = p == null ? "---" : $"{p.Role} Lv{p.Level}\nHP:{p.CurrentHealth}/{p.MaxHealth}";
            if (EnemySlotLabels != null && i < EnemySlotLabels.Length && EnemySlotLabels[i])
                EnemySlotLabels[i].text  = e == null ? "---" : $"{e.Role} Lv{e.Level}\nHP:{e.CurrentHealth}/{e.MaxHealth}";
        }
    }

    void AppendLog(string line)
    {
        if (!LogText) return;
        LogText.text += "\n" + line;
        // Keep last 8 lines
        var lines = LogText.text.Split('\n');
        if (lines.Length > 8)
            LogText.text = string.Join("\n", lines[^8..]);
    }

    public void ShowResult(bool playerWon)
    {
        if (ResultPanel) ResultPanel.SetActive(true);
        if (ResultText) ResultText.text = playerWon ? "VICTORY! 🎉" : "DEFEATED 😢";
    }
}
```

**Wire in Unity:** Build the Battle scene UI hierarchy via unity-cmd with Canvas → PlayerPanel, EnemyPanel, LogPanel, ResultPanel. Wire all TMP labels and connect to BattleUI component.

---

### TASK 05 — Fly XP + Level Up After Battle

**Goal:** After a battle win, player flies gain XP. Individual fly levels up every 100 XP. Stats increase. All flies heal to full after battle (no permadeath).

**Requires:** Task 03 (BattleManager, FlyInstance), Task 04 (BattleUI)

**Files:** `ClanManager.cs`, `BattleManager.cs`

**Steps:**

1. Add post-battle heal to `ClanManager.cs` — call this after every battle regardless of outcome:
```csharp
public void HealAllFlies()
{
    foreach (var fly in _flies)
        fly.CurrentHealth = fly.MaxHealth;
}
```

2. Add per-fly XP method to `ClanManager.cs`:
```csharp
public void AwardBattleXP(bool won)
{
    int xpPerFly = won ? 50 : 10;
    foreach (var fly in _flies.Where(f => f.Role == FlyRole.Soldier && f.IsAlive))
    {
        fly.XP += xpPerFly;
        int newLevel = Mathf.FloorToInt(fly.XP / 100f) + 1;
        if (newLevel > fly.Level)
        {
            fly.Level = newLevel;
            // Scale stats with level
            fly.Attack        = Mathf.RoundToInt(fly.Attack  * 1.1f);
            fly.Defense       = Mathf.RoundToInt(fly.Defense * 1.1f);
            fly.MaxHealth     = Mathf.RoundToInt(fly.MaxHealth * 1.15f);
            fly.CurrentHealth = fly.MaxHealth; // full heal on level up
            Debug.Log($"[ClanManager] {fly.Role} levelled up to {fly.Level}! 🪰");
            UIEventBus.Emit(UIEvent.LevelUp, fly);
        }
    }
    // Also award clan-level XP
    AddXP(won ? 50 : 10);
}
```

2. In `BattleManager.EndBattle()`, replace `ClanManager.Instance?.AddXP(xp)` with:
```csharp
ClanManager.Instance?.AwardBattleXP(playerWon);
ClanManager.Instance?.HealAllFlies(); // no permadeath — full heal after every battle
```

3. In `BattleSceneController.OnBattleComplete()`, after result:
```csharp
// If won, claim the district
if (playerWon && !string.IsNullOrEmpty(BattleManager.Instance.PendingDistrictId))
    TerritoryManager.Instance?.ClaimDistrict(
        BattleManager.Instance.PendingDistrictId,
        ClanManager.Instance?.ClanId ?? "player_clan");
```

---

### TASK 06 — Currency Simplification

**Goal:** Remove TrashResources. Rename PooResources → PooCoins throughout. Keep Gems.

**Requires:** Task 02 (DistrictInfoPanel bonus text already updated)

**Files:** `ResourceManager.cs`, `Constants.cs`, `ResourceHUD.cs`, `SaveSystem.cs`

**Steps:**

1. In `ResourceManager.cs`:
   - Delete `TrashResources`, `AddTrashResources()`, `SpendTrashResources()`
   - Rename `PooResources` → `PooCoins` and all related methods
   - Update `SaveData` class

2. In `ResourceHUD.cs`:
   - Remove `TrashResourcesText` field
   - Update label text: `PooCoins` and `Gems` only

3. In `UIEventBus` / `UIEventBus.OnResourceChanged`, update any "PooResources" string to "PooCoins"

4. In `Constants.cs`, remove `BOOSTER_POO_RADAR_GEMS` etc that reference TrashResources

5. In `BoosterData.cs`, remove `TrashResourceCost` field

6. In `SaveSystem.cs`, update SaveFile.Resources to match new ResourceManager.SaveData

7. In the Unity scene, remove TrashResourcesText label from ResourceHUD in the Inspector

---

### TASK 07 — Save/Load End-to-End

**Goal:** Game state fully persists across sessions. Test: claim a district, quit, reopen — district still yours.

**Requires:** Task 01 (Bootstrap flow), Task 05 (fly XP data), Task 06 (PooCoins currency)

**Files:** `SaveSystem.cs`, `GameManager.cs`

**Steps:**

1. Verify `SaveSystem.Save()` captures: clan data, resource data, district ownership
2. Verify `SaveSystem.Load()` restores all three correctly
3. In `GameManager.Start()`, ensure load happens AFTER all managers have Awake'd:
```csharp
IEnumerator Start()
{
    yield return null; // wait one frame for all Awake() calls
    SaveSystem.Load();
    AudioManager.Instance?.PlayMusic(AudioManager.Track.Menu);
    GetComponent<BootstrapLoader>()?.BeginLoad(); // if on same GO
}
```
4. Add `DistrictTile` refresh after load so map reflects saved ownership
5. Test manually: claim district_2_2, quit play mode, re-enter play mode, confirm it's still green

---

## PHASE 3 — RPG Depth

### TASK 08 — Gear Drop System

**Goal:** Winning a battle has a 40% chance to drop one EquipmentData item. Player can equip it to a fly.

**Requires:** Task 03 (BattleManager.EndBattle), Task 05 (FlyInstance with stats), Task 07 (SaveSystem)

**Files:** new `InventoryManager.cs`, new `EquipmentData.cs` (if not exists), modify `BattleManager.cs`, modify `SaveSystem.cs`

**Steps:**

1. Create `Assets/Scripts/Data/EquipmentData.cs` (ScriptableObject definition):
```csharp
using UnityEngine;

public enum EquipmentSlot { Weapon, Head, Offhand, Feet }
public enum EquipmentAbility { None, AOESplash, ReflectDamage, BlockFirstHit, ActFirst }

[CreateAssetMenu(fileName = "NewEquipment", menuName = "FlyLife/Equipment Data")]
public class EquipmentData : ScriptableObject
{
    public string ItemName;
    public EquipmentSlot Slot;
    public int AttackBonus;
    public int DefenseBonus;
    public int SpeedBonus;
    public EquipmentAbility Ability;
    [Range(0f, 1f)] public float AbilityChance = 1f;
    [TextArea] public string Description;
}
```

2. Create `Assets/Scripts/Managers/InventoryManager.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("All equipment definitions (assign in Inspector)")]
    public EquipmentData[] AllEquipment;

    // Runtime state
    List<string> _ownedItemIds = new();
    Dictionary<string, string> _equipped = new(); // flyId → itemId

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void AddItem(string itemId)
    {
        _ownedItemIds.Add(itemId);
        Debug.Log($"[Inventory] Added {itemId}. Total items: {_ownedItemIds.Count}");
        UIEventBus.Emit(UIEvent.ResourceChanged, null);
    }

    public List<string> GetOwnedItems() => new(_ownedItemIds);

    public EquipmentData GetEquipmentData(string itemId) =>
        AllEquipment?.FirstOrDefault(e => e.name == itemId);

    public bool EquipItem(string flyId, string itemId)
    {
        if (!_ownedItemIds.Contains(itemId)) return false;
        var data = GetEquipmentData(itemId);
        if (data == null) return false;

        // Unequip any item in same slot on same fly
        var currentlyEquipped = GetEquippedItem(flyId);
        if (currentlyEquipped != null)
            _equipped.Remove(flyId);

        _equipped[flyId] = itemId;
        Debug.Log($"[Inventory] Equipped {itemId} on fly {flyId}");
        return true;
    }

    public string GetEquippedItem(string flyId) =>
        _equipped.TryGetValue(flyId, out var id) ? id : null;

    /// <summary>Apply gear bonuses to a FlyInstance before battle.</summary>
    public void ApplyGearStats(FlyInstance fly)
    {
        var itemId = GetEquippedItem(fly.InstanceId);
        if (itemId == null) return;
        var data = GetEquipmentData(itemId);
        if (data == null) return;
        fly.Attack  += data.AttackBonus;
        fly.Defense += data.DefenseBonus;
        fly.Speed   += data.SpeedBonus;
    }

    // --- Save/Load ---
    [Serializable]
    public class SaveData
    {
        public List<string> OwnedItemIds = new();
        public List<string> EquippedKeys = new();
        public List<string> EquippedValues = new();
    }

    public SaveData GetSaveData()
    {
        var sd = new SaveData { OwnedItemIds = new(_ownedItemIds) };
        foreach (var kvp in _equipped)
        {
            sd.EquippedKeys.Add(kvp.Key);
            sd.EquippedValues.Add(kvp.Value);
        }
        return sd;
    }

    public void LoadSaveData(SaveData sd)
    {
        if (sd == null) return;
        _ownedItemIds = new(sd.OwnedItemIds);
        _equipped.Clear();
        for (int i = 0; i < sd.EquippedKeys.Count && i < sd.EquippedValues.Count; i++)
            _equipped[sd.EquippedKeys[i]] = sd.EquippedValues[i];
    }
}
```

3. Add `InventoryManager` component to GameManager prefab:
```powershell
.\unity-cmd.ps1 '[
  {"type": "inspect", "path": "GameManager", "lens": "scripts"},
  {"type": "save-scene"}
]'
```

4. Create 4 EquipmentData ScriptableObjects in `Assets/ScriptableObjects/Equipment/`:

| Asset name | ItemName | Slot | Attack | Defense | Speed | Ability | AbilityChance |
|---|---|---|---|---|---|---|---|
| PooThrower | Poo Thrower | Weapon | 8 | 0 | 0 | AOESplash | 0.3 |
| NailHelmet | Nail Helmet | Head | 0 | 6 | 0 | ReflectDamage | 0.2 |
| BottleCapShield | Bottle Cap Shield | Offhand | 0 | 10 | 0 | BlockFirstHit | 1.0 |
| SugarCubeBoots | Sugar Cube Boots | Feet | 0 | 0 | 8 | ActFirst | 1.0 |

Create via scratch pad:
```powershell
.\unity-cmd.ps1 '[
  {"type": "scratch", "code": "
    var assets = new (string name, string itemName, int slot, int atk, int def, int spd, int ability, float chance)[] {
      (\"PooThrower\", \"Poo Thrower\", 0, 8, 0, 0, 1, 0.3f),
      (\"NailHelmet\", \"Nail Helmet\", 1, 0, 6, 0, 2, 0.2f),
      (\"BottleCapShield\", \"Bottle Cap Shield\", 2, 0, 10, 0, 3, 1f),
      (\"SugarCubeBoots\", \"Sugar Cube Boots\", 3, 0, 0, 8, 4, 1f)
    };
    foreach (var a in assets) {
      var eq = ScriptableObject.CreateInstance<EquipmentData>();
      eq.ItemName = a.itemName;
      eq.Slot = (EquipmentSlot)a.slot;
      eq.AttackBonus = a.atk; eq.DefenseBonus = a.def; eq.SpeedBonus = a.spd;
      eq.Ability = (EquipmentAbility)a.ability; eq.AbilityChance = a.chance;
      AssetDatabase.CreateAsset(eq, $\"Assets/ScriptableObjects/Equipment/{a.name}.asset\");
    }
    AssetDatabase.SaveAssets();
    return \"Created 4 equipment assets\";
  "}
]'
```
Assign all 4 to `InventoryManager.AllEquipment` array in GameManager prefab Inspector.

5. In `BattleManager.cs`, add drop table field and drop logic to `EndBattle()`:
```csharp
[Header("Gear Drops")]
public EquipmentData[] PossibleDrops; // assign in Inspector

// Inside EndBattle(), after AwardBattleXP:
if (playerWon && PossibleDrops != null && PossibleDrops.Length > 0 && Random.value < 0.4f)
{
    var drop = PossibleDrops[Random.Range(0, PossibleDrops.Length)];
    InventoryManager.Instance?.AddItem(drop.name);
    UIEventBus.Emit(UIEvent.BattleItemDropped, drop);
    Debug.Log($"[Battle] Gear drop: {drop.ItemName}!");
}
```
Assign all 4 EquipmentData assets to `BattleManager.PossibleDrops` in Inspector.

6. Apply gear stats before battle in `BattleManager.ExecutePendingBattle()`:
```csharp
// Before combat starts, apply gear bonuses to player flies
foreach (var fly in PlayerSlots)
    InventoryManager.Instance?.ApplyGearStats(fly);
```

7. Wire `InventoryManager.SaveData` into `SaveSystem.cs`:
```csharp
// In SaveFile class:
public InventoryManager.SaveData Inventory;

// In SaveSystem.Save():
file.Inventory = InventoryManager.Instance?.GetSaveData();

// In SaveSystem.Load():
InventoryManager.Instance?.LoadSaveData(file.Inventory);
```

8. Test: win a battle → check console for gear drop log → verify item persists after save/load.

---

### TASK 09 — PooData ScriptableObjects

**Goal:** Create all 7 poo types as real assets so PooManager can spawn them with correct stats.

**Requires:** None (standalone data assets)

**Files:** `PooData.cs` (verify exists), new ScriptableObject assets

**Steps:**

1. Verify `Assets/Scripts/Data/PooData.cs` exists with `[CreateAssetMenu]` and these fields:
```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "NewPoo", menuName = "FlyLife/Poo Data")]
public class PooData : ScriptableObject
{
    public string PooName;
    public int FertilityMin;
    public int FertilityMax;
    public int SmellMin;
    public int SmellMax;
    public float DurationSeconds;
    public int SpawnWeight;
}
```
If it differs, update to match. If missing, create it.

2. Create 7 PooData assets in `Assets/ScriptableObjects/Poos/` via scratch pad:
```powershell
.\unity-cmd.ps1 '[
  {"type": "scratch", "code": "
    string dir = \"Assets/ScriptableObjects/Poos\";
    if (!AssetDatabase.IsValidFolder(dir))
      AssetDatabase.CreateFolder(\"Assets/ScriptableObjects\", \"Poos\");

    var poos = new (string name, int fMin, int fMax, int sMin, int sMax, float dur, int weight)[] {
      (\"DogPoo\",      30, 60,  40, 70,  3600f,  30),
      (\"BirdPoo\",     10, 30,  20, 50,  1800f,  25),
      (\"HorsePoo\",    60, 90,  60, 90,  7200f,  10),
      (\"CatPoo\",      20, 40,  50, 80,  2700f,  20),
      (\"Compost\",     80, 100, 10, 30,  14400f,  5),
      (\"BabyDiaper\",  40, 70,  70, 100, 3600f,   8),
      (\"HumanPoo\",    50, 80,  80, 100, 5400f,   2)
    };

    foreach (var p in poos) {
      var pd = ScriptableObject.CreateInstance<PooData>();
      pd.PooName = p.name;
      pd.FertilityMin = p.fMin; pd.FertilityMax = p.fMax;
      pd.SmellMin = p.sMin; pd.SmellMax = p.sMax;
      pd.DurationSeconds = p.dur; pd.SpawnWeight = p.weight;
      AssetDatabase.CreateAsset(pd, $\"{dir}/{p.name}.asset\");
    }
    AssetDatabase.SaveAssets();
    return \"Created 7 poo data assets\";
  "}
]'
```

3. In `PooManager.cs`, add array field and weighted spawn method:
```csharp
[Header("Poo Types")]
public PooData[] AllPooTypes; // assign all 7 in Inspector

public PooData GetRandomPooType()
{
    if (AllPooTypes == null || AllPooTypes.Length == 0) return null;
    int totalWeight = 0;
    foreach (var p in AllPooTypes) totalWeight += p.SpawnWeight;
    int roll = Random.Range(0, totalWeight);
    int cumulative = 0;
    foreach (var p in AllPooTypes)
    {
        cumulative += p.SpawnWeight;
        if (roll < cumulative) return p;
    }
    return AllPooTypes[^1];
}
```

4. Update `PooManager.SpawnPoo()` (or equivalent) to use `GetRandomPooType()` instead of hardcoded values:
```csharp
var pooType = GetRandomPooType();
if (pooType == null) return;
// Use pooType.PooName, pooType.FertilityMin, etc. when creating the poo instance
```

5. Assign all 7 assets to `PooManager.AllPooTypes` in GameManager prefab Inspector.

6. Test: play from Bootstrap → verify console shows varied poo types spawning with different names.

---

### TASK 10 — FlyRoleData ScriptableObjects

**Goal:** Create Scout, Soldier, Worker role assets. Wire to ClanManager so flies spawn with real stats.

**Requires:** Task 03 (FlyInstance, FlyRole, GenerateEnemyFlies)

**Files:** new `FlyRoleData.cs`, modify `ClanManager.cs`, modify `DistrictInfoPanel.cs`

**Steps:**

1. Create `Assets/Scripts/Data/FlyRoleData.cs`:
```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "NewFlyRole", menuName = "FlyLife/Fly Role Data")]
public class FlyRoleData : ScriptableObject
{
    public FlyRole Role;
    public int BaseAttack;
    public int BaseDefense;
    public int BaseSpeed;
    public int BaseHealth;
    [Header("Special ability multiplier")]
    public float SpecialMultiplier = 1f;
    [TextArea] public string Description;
}
```

2. Create 3 FlyRoleData assets in `Assets/ScriptableObjects/Roles/` via scratch pad:
```powershell
.\unity-cmd.ps1 '[
  {"type": "scratch", "code": "
    string dir = \"Assets/ScriptableObjects/Roles\";
    if (!AssetDatabase.IsValidFolder(dir))
      AssetDatabase.CreateFolder(\"Assets/ScriptableObjects\", \"Roles\");

    var roles = new (string name, int role, int atk, int def, int spd, int hp, float special, string desc)[] {
      (\"Scout\",   0, 8,  5,  15, 40, 2.0f, \"Fast striker. 2x poo find rate.\"),
      (\"Soldier\", 1, 15, 12, 8,  80, 1.5f, \"Tank. 1.5x attack multiplier.\"),
      (\"Worker\",  2, 5,  8,  6,  60, 2.0f, \"Support. Heals allies between rounds.\")
    };

    foreach (var r in roles) {
      var rd = ScriptableObject.CreateInstance<FlyRoleData>();
      rd.Role = (FlyRole)r.role;
      rd.BaseAttack = r.atk; rd.BaseDefense = r.def;
      rd.BaseSpeed = r.spd; rd.BaseHealth = r.hp;
      rd.SpecialMultiplier = r.special; rd.Description = r.desc;
      AssetDatabase.CreateAsset(rd, $\"{dir}/{r.name}.asset\");
    }
    AssetDatabase.SaveAssets();
    return \"Created 3 fly role assets\";
  "}
]'
```

3. Add role data field to `ClanManager.cs`:
```csharp
[Header("Role Definitions")]
public FlyRoleData[] AllRoles; // assign Scout, Soldier, Worker in Inspector
```

4. Update `ClanManager` fly creation to use `FlyRoleData` when spawning initial flies:
```csharp
public FlyInstance CreateFly(FlyRole role)
{
    var roleData = AllRoles?.FirstOrDefault(r => r.Role == role);
    return new FlyInstance
    {
        InstanceId = $"fly_{System.Guid.NewGuid():N}",
        Role = role,
        Level = 1,
        Attack = roleData?.BaseAttack ?? 10,
        Defense = roleData?.BaseDefense ?? 5,
        Speed = roleData?.BaseSpeed ?? 5,
        MaxHealth = roleData?.BaseHealth ?? 50,
        CurrentHealth = roleData?.BaseHealth ?? 50,
        XP = 0
    };
}
```
Update any existing fly initialization code to call `CreateFly()`.

5. Refactor `DistrictInfoPanel.GenerateEnemyFlies()` to use `FlyRoleData`:
```csharp
List<FlyInstance> GenerateEnemyFlies(int difficulty)
{
    var roles = ClanManager.Instance?.AllRoles;
    var enemies = new List<FlyInstance>();
    for (int i = 0; i < Constants.BATTLE_SLOTS; i++)
    {
        var role = roles != null && roles.Length > 0
            ? roles[i % roles.Length]
            : null;
        enemies.Add(new FlyInstance {
            InstanceId = $"enemy_{i}",
            Role = role?.Role ?? FlyRole.Soldier,
            Level = difficulty,
            Attack = (role?.BaseAttack ?? 10) * difficulty,
            Defense = (role?.BaseDefense ?? 5) * difficulty,
            Speed = role?.BaseSpeed ?? 5,
            MaxHealth = (role?.BaseHealth ?? 50) * difficulty,
            CurrentHealth = (role?.BaseHealth ?? 50) * difficulty
        });
    }
    return enemies;
}
```

6. Assign all 3 role assets to `ClanManager.AllRoles` in GameManager prefab Inspector.

7. Test: start a battle → verify enemies have varied roles (Scout/Soldier/Worker cycling) with different stat profiles in BattleUI.

---

### TASK 11 — Enemy Clan AI (Districts Fight Back)

**Goal:** Every 5 minutes (real-time), enemy clans attempt to re-capture adjacent neutral or player districts.

**Requires:** Task 02 (District with GridX/GridY, OwnerId), Task 07 (save system for persistence)

**Files:** new `EnemyAIManager.cs`, modify GameManager prefab

**Steps:**

1. Create `Assets/Scripts/Managers/EnemyAIManager.cs`:
```csharp
using System.Collections;
using System.Linq;
using UnityEngine;

public class EnemyAIManager : MonoBehaviour
{
    public static EnemyAIManager Instance { get; private set; }
    public float AttackIntervalSeconds = 300f; // 5 minutes

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
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

        var enemyDistricts = tm.Districts.Values
            .Where(d => !string.IsNullOrEmpty(d.OwnerId) && d.OwnerId.StartsWith("enemy_clan"))
            .ToList();

        foreach (var enemy in enemyDistricts)
        {
            var adjacents = tm.Districts.Values.Where(d =>
                Mathf.Abs(d.GridX - enemy.GridX) <= 1 &&
                Mathf.Abs(d.GridY - enemy.GridY) <= 1 &&
                d.Id != enemy.Id &&
                (string.IsNullOrEmpty(d.OwnerId) || d.OwnerId == playerClanId)
            ).ToList();

            if (adjacents.Count == 0) continue;
            var target = adjacents[Random.Range(0, adjacents.Count)];

            // 30% chance vs neutral, 15% chance vs player
            float chance = string.IsNullOrEmpty(target.OwnerId) ? 0.3f : 0.15f;
            if (Random.value < chance)
            {
                bool wasPlayerOwned = target.OwnerId == playerClanId;
                target.OwnerId = enemy.OwnerId;
                MapManager.Instance?.RefreshAllTiles();
                if (wasPlayerOwned)
                    UIEventBus.Emit(UIEvent.DistrictLost, target);
                Debug.Log($"[EnemyAI] {enemy.OwnerId} captured {target.Name}");
            }
        }
    }
}
```

2. Add `EnemyAIManager` component to GameManager prefab:
```powershell
.\unity-cmd.ps1 '[
  {"type": "inspect", "path": "GameManager", "lens": "scripts"},
  {"type": "save-scene"}
]'
```

3. Ensure `UIEvent.DistrictLost` exists in UIEvent enum. If missing, add it.

4. Add a notification in MainMap when a district is lost. In `MapManager.cs` or a new `NotificationPopup.cs`:
```csharp
void OnEnable()
{
    UIEventBus.On(UIEvent.DistrictLost, OnDistrictLost);
}

void OnDisable()
{
    UIEventBus.Off(UIEvent.DistrictLost, OnDistrictLost);
}

void OnDistrictLost(object data)
{
    if (data is District d)
        Debug.LogWarning($"[Map] District lost: {d.Name} was captured by enemy!");
    // TODO Phase 4: show toast notification to player
}
```

5. Test: set `AttackIntervalSeconds = 10f` temporarily → play → wait 10s → check console for enemy capture logs. Reset to 300f after testing.

---

### TASK 12 — Poo Resource Income

**Goal:** Each owned district generates 5 PooCoins per minute passively.

**Requires:** Task 06 (PooCoins currency), Task 02 (district ownership)

**Files:** modify `ResourceManager.cs`, modify `TerritoryManager.cs`

**Steps:**

1. In `TerritoryManager.cs`, add helper method if missing:
```csharp
public List<District> GetPlayerDistricts()
{
    string playerClanId = ClanManager.Instance?.ClanId ?? "player_clan";
    return Districts.Values.Where(d => d.OwnerId == playerClanId).ToList();
}
```

2. In `ResourceManager.cs`, add passive income in `Update()`:
```csharp
[Header("Passive income")]
public float IncomeIntervalSeconds = 60f;
public int IncomePerDistrict = 5;
float _nextIncomeTime;

void Update()
{
    if (Time.time >= _nextIncomeTime)
    {
        int districtCount = TerritoryManager.Instance?.GetPlayerDistricts().Count ?? 0;
        if (districtCount > 0)
        {
            int income = districtCount * IncomePerDistrict;
            AddPooCoins(income);
            Debug.Log($"[ResourceManager] Passive income: +{income} PooCoins from {districtCount} districts.");
        }
        _nextIncomeTime = Time.time + IncomeIntervalSeconds;
    }
}
```

3. Test: claim center district → wait 60s → verify PooCoins increase by 5 in console log and HUD.

---

### TASK 12B — ResourceHUD Wired to Runtime Values

**Goal:** HUD shows real PooCoins and Gems counts, updating live as resources change.

**Requires:** Task 06 (PooCoins/Gems in ResourceManager), Task 12 (passive income)

**Files:** modify `ResourceHUD.cs`

**Steps:**

1. In `ResourceHUD.cs`, ensure it has these fields:
```csharp
public TextMeshProUGUI PooCoinsText;
public TextMeshProUGUI GemsText;
```

2. Replace any existing update logic with:
```csharp
void Update()
{
    if (ResourceManager.Instance == null) return;
    PooCoinsText.text  = $"💰 {ResourceManager.Instance.PooCoins}";
    GemsText.text      = $"💎 {ResourceManager.Instance.Gems}";
}
```

3. Remove any old labels (TrashResourcesText, PooResourcesText) if still present in scene.

4. Wire TMP labels in MainMap scene:
```powershell
.\unity-cmd.ps1 '[
  {"type": "inspect", "path": "HUD/ResourceHUD", "lens": "scripts"},
  {"type": "save-scene"}
]'
```

5. Test: play from Bootstrap → claim district → wait for income tick → verify HUD numbers update live.

---

## PHASE 4 — Retention & Monetization

### TASK 13 — Daily Login Bonus

**Goal:** First open of each day awards 50 PooCoins + 5 Gems.

**Requires:** Task 06 (PooCoins/Gems), Task 07 (SaveSystem)

**Modify `SaveSystem.cs`:** Add `LastLoginDate` string to SaveFile.
**Modify `GameManager.Start()`:** After load, check if date changed → award bonus → show popup.

---

### TASK 14 — Rewarded Ad Integration (AdMob)

**Goal:** "Watch Ad" button gives Double XP booster for next battle.

**Requires:** Task 03 (battle flow), Task 05 (XP system)

**Steps:**
1. Install Google Mobile Ads Unity Plugin
2. Create `Assets/Scripts/Managers/AdManager.cs` with `ShowRewardedAd(System.Action onComplete)`
3. Add "Watch Ad" button to DistrictInfoPanel (visible before a fight)
4. On ad complete: activate `BoosterType.WarDrums` for 1 battle via `ClanManager.ActivateBooster()`

---

### TASK 15 — Clan Screen UI

**Goal:** Dedicated screen showing all flies, their levels, gear, and role assignment buttons.

**Requires:** Task 05 (fly levels/XP), Task 08 (gear/inventory), Task 10 (FlyRoleData)

**Steps:**
1. Create `Assets/Scenes/ClanScreen.unity`
2. Create `Assets/Scripts/UI/ClanScreenController.cs`
3. Scrollable list of FlyInstance cards: name (role+level), HP bar, equipped gear slots, Reassign button
4. Add "Clan" button to MainMap HUD → `GameManager.Instance.LoadScene("ClanScreen")`

---

### TASK 16 — Level-Up Visual Feedback

**Goal:** When a fly levels up, show a brief animated popup on screen.

**Requires:** Task 05 (UIEventBus.LevelUp event)

**Steps:**
1. Create `LevelUpPopup.cs` — appears at screen center, shows "LEVEL UP! Scout → Lv 3", fades out after 1.5s
2. Subscribe to `UIEventBus.LevelUp` in the popup controller
3. Add to Battle scene UI

---

### TASK 17 — Boss Districts

**Goal:** 4 corner districts (0,0), (0,4), (4,0), (4,4) are Boss districts with unique gear drops.

**Requires:** Task 02 (district difficulty), Task 03 (battle flow), Task 08 (gear drops)

**Modify `TerritoryManager.GenerateDistricts()`:**
```csharp
bool isBoss = (x == 0 || x == 4) && (y == 0 || y == 4);
d.IsBoss = isBoss;
d.DifficultyLevel = isBoss ? 6 : (ring == 0 ? 1 : ring == 1 ? 3 : 5);
```

**In `BattleManager`:** Boss fights have 7 enemy flies instead of 5, guaranteed gear drop.

---

## PHASE 5 — Polish & Ship

### TASK 18 — District Tile Art

**Goal:** Replace flat grey squares with themed sprites.
- Ring 0 (center): Park/garden theme (green)
- Ring 1 (inner): Urban/city theme (brown/grey)  
- Ring 2 (outer): Industrial theme (dark grey/rust)

Create 3 sprite variants of the tile background. Assign in `DistrictTile` based on `d.Ring`.

---

### TASK 19 — Poo Marker Sprites

**Goal:** Replace `[POO]` text with actual poo emoji sprite or simple brown circle sprite.

Create a simple poo sprite (brown circle with eyes). Assign to `PooMarker.Icon` SpriteRenderer.
Install TMP Emoji Atlas if using emoji: Window → TextMeshPro → Sprite Asset Creator.

---

### TASK 20 — Battle Hit Animations

**Goal:** When a fly takes damage, flash the slot red briefly.

In `BattleUI.OnAttack()`: flash the target slot Image component red for 0.2 seconds using a Coroutine.

---

### TASK 21 — Sound Effects

**Goal:** AudioManager plays sounds for: battle hit, level up, district capture, button tap.

**Steps:**
1. Add AudioClip fields to `AudioManager.cs`: `HitClip`, `LevelUpClip`, `CaptureClip`, `ButtonClip`
2. Add `PlaySFX(AudioClip)` method using a pooled AudioSource
3. Subscribe to UIEvents and play appropriate clips

---

### TASK 22 — Android Build

**Goal:** Working APK for Android testing.

**Steps:**
1. File → Build Settings → Android → Switch Platform
2. Edit → Project Settings → Player → Android:
   - Bundle ID: `com.vencacode.flylife`
   - Min API: 26 (Android 8.0)
   - Target API: 34 (Android 14)
   - Scripting Backend: IL2CPP
   - Architecture: ARMv7 + ARM64
3. Install Android Build Support module via Unity Hub if missing
4. Build APK → test on physical device
5. Verify: scene loading, save/load, touch input, performance (target 60fps)

---

## PHASE 9 — First Playable

### TASK 51 — Disable Ads & IAP at Runtime

**Goal:** All ad and IAP code paths are gated behind feature flags. "Watch Ad" buttons hidden. Gem purchases disabled. Game is fully playable without any SDK.

**Requires:** Task 14 (AdManager), Task 36 (IAPManager)

**Files:** new `FeatureFlags.cs`, modify `AdManager.cs`, modify `IAPManager.cs`, modify any UI that shows ad/IAP buttons

**Steps:**

1. Create `Assets/Scripts/Config/FeatureFlags.cs`:
```csharp
using UnityEngine;

/// <summary>
/// Central toggle for features that aren't ready for the first playable build.
/// Flip these to true when real SDKs are integrated.
/// </summary>
public static class FeatureFlags
{
    public const bool ADS_ENABLED = false;
    public const bool IAP_ENABLED = false;
    public const bool ANALYTICS_ENABLED = false;
    public const bool CLOUD_SAVE_ENABLED = false;
    public const bool PUSH_NOTIFICATIONS_ENABLED = false;
}
```

2. In `AdManager.cs`, gate all ad calls:
```csharp
public void ShowRewardedAd(System.Action onComplete)
{
    if (!FeatureFlags.ADS_ENABLED)
    {
        Debug.Log("[AdManager] Ads disabled — granting reward directly.");
        onComplete?.Invoke(); // give reward without ad in dev builds
        return;
    }
    // ... existing ad code
}
```

3. In `IAPManager.cs`, gate all purchase calls:
```csharp
public void PurchaseGemPack(string packId)
{
    if (!FeatureFlags.IAP_ENABLED)
    {
        Debug.Log("[IAPManager] IAP disabled.");
        ToastManager.Instance?.ShowToast("Purchases not available yet.");
        return;
    }
    // ... existing IAP code
}
```

4. Hide "Watch Ad" button in `DistrictInfoPanel.cs`:
```csharp
// In Show():
WatchAdButton?.gameObject.SetActive(FeatureFlags.ADS_ENABLED);
```

5. Hide Gem purchase buttons wherever they appear (Settings, Clan Screen, Skin shop):
```csharp
BuyGemsButton?.gameObject.SetActive(FeatureFlags.IAP_ENABLED);
```

6. Search entire codebase for any direct AdMob/IAP SDK calls not behind the flag:
```powershell
# Run in project root:
grep -rn "GoogleMobileAds\|UnityPurchasing\|AdRequest\|BuyProduct" Assets/Scripts/ --include="*.cs"
```
Wrap every match in `if (FeatureFlags.X_ENABLED)` or early-return.

7. Test: play full session → no ad popups, no purchase prompts, no SDK-related errors in console.

---

### TASK 52 — Gem Economy Rebalance

**Goal:** Player can earn enough Gems from gameplay alone to buy skins and boosters. No purchase path needed.

**Requires:** Task 51 (IAP disabled), Task 43 (Achievements), Task 44 (Daily Missions), Task 17 (Boss Districts)

**Files:** modify `Constants.cs`, modify `AchievementManager.cs`, modify `DailyMissionManager.cs`, modify `BattleManager.cs`

**Steps:**

1. Add Gem reward constants to `Constants.cs`:
```csharp
// Gem income (no IAP)
public const int GEMS_DAILY_LOGIN      = 5;   // already exists, verify
public const int GEMS_BOSS_KILL        = 10;
public const int GEMS_ACHIEVEMENT      = 5;   // per achievement completed
public const int GEMS_DAILY_MISSION    = 3;   // per mission completed
public const int GEMS_FIRST_DISTRICT   = 15;  // one-time tutorial reward
public const int GEMS_FULL_MAP_CLEAR   = 50;  // one-time reward
```

2. In `BattleManager.EndBattle()`, award Gems for boss kills:
```csharp
if (playerWon)
{
    var district = TerritoryManager.Instance?.GetDistrict(PendingDistrictId);
    if (district != null && district.IsBoss)
    {
        ResourceManager.Instance?.AddGems(Constants.GEMS_BOSS_KILL);
        ToastManager.Instance?.ShowToast($"💎 +{Constants.GEMS_BOSS_KILL} Gems (Boss defeated!)");
    }
}
```

3. In `AchievementManager.cs`, award Gems on achievement unlock:
```csharp
void UnlockAchievement(string id)
{
    // ... existing unlock logic
    ResourceManager.Instance?.AddGems(Constants.GEMS_ACHIEVEMENT);
    ToastManager.Instance?.ShowToast($"🏆 Achievement unlocked! +{Constants.GEMS_ACHIEVEMENT} 💎");
}
```

4. In `DailyMissionManager.cs`, award Gems on mission complete:
```csharp
void CompleteMission(string missionId)
{
    // ... existing complete logic
    ResourceManager.Instance?.AddGems(Constants.GEMS_DAILY_MISSION);
}
```

5. Verify skin prices are achievable. Check `Constants.cs` for skin costs — a player who plays daily should afford 1 skin per week. Rough math: 5 (login) + 3×3 (missions) + 5 (achievement avg) = ~19 Gems/day. If skins cost 100 Gems, that's ~5 days. Adjust if needed.

6. Verify `ResourceHUD` shows Gems. Verify Gems persist through save/load.

7. Test: fresh save → play through tutorial → kill boss → complete achievement → verify Gems accumulate → buy a skin → verify it works.

---

### TASK 53 — Stub Removal Audit

**Goal:** Find every reflection-based stub and mock. Replace with either real logic or a clean no-op that won't crash on device.

**Requires:** Task 51 (FeatureFlags)

**Files:** multiple — audit-driven

**Steps:**

1. Search for all reflection-based stubs:
```powershell
grep -rn "reflection\|GetType()\|Invoke(\|MethodInfo\|Assembly.Load\|Type.GetType" Assets/Scripts/ --include="*.cs"
```

2. Search for TODO/STUB/MOCK markers:
```powershell
grep -rn "TODO\|STUB\|MOCK\|HACK\|FIXME\|NotImplemented\|throw new" Assets/Scripts/ --include="*.cs"
```

3. For each stub found, decide:
   - **SDK stub** (AdMob, Firebase, IAP, Play Games) → wrap in `FeatureFlags` check, return graceful no-op
   - **Logic stub** (unfinished game logic) → implement for real or remove the feature entry point
   - **Test stub** (debug helpers) → wrap in `#if UNITY_EDITOR` or remove

4. For SDK stubs specifically, ensure the pattern is:
```csharp
public void SomeSDKCall()
{
    if (!FeatureFlags.RELEVANT_FLAG)
    {
        Debug.Log("[Manager] Feature disabled, skipping.");
        return;
    }
    // Real SDK call here (for when SDK is integrated later)
}
```

5. Verify no stub throws an exception at runtime. Every code path must either work or silently skip.

6. Test: play full game loop with no exceptions in console. Check `adb logcat` for any reflection errors.

---

### TASK 54 — Scene Flow Audit

**Goal:** Every scene transition works. No dead ends. Every back button returns somewhere. Player can't get stuck.

**Requires:** All previous tasks

**Files:** audit across all scene controllers

**Steps:**

1. Map all scenes and transitions. Expected flow:
```
Bootstrap → MainMap ↔ Battle
                    ↔ ClanScreen
                    ↔ Settings
                    ↔ BossRush
```
Verify each arrow works in both directions.

2. Check every scene controller for a back/return path:
```powershell
grep -rn "LoadScene\|SceneManager" Assets/Scripts/ --include="*.cs"
```
Every scene except Bootstrap must have a way back to MainMap.

3. Check for missing null guards on scene entry — if a manager is missing (e.g. player somehow loads Battle directly), it should redirect to Bootstrap:
```csharp
void Start()
{
    if (GameManager.Instance == null)
    {
        Debug.LogError("[SceneController] No GameManager — redirecting to Bootstrap.");
        SceneManager.LoadScene("Bootstrap");
        return;
    }
}
```
Add this pattern to: `BattleSceneController`, `ClanScreenController`, `SettingsScreenController`, `BossRushController`, any other scene entry points.

4. Verify all scenes are in Build Settings:
```powershell
.\unity-cmd.ps1 '{"type": "build-settings"}'
```
Expected: Bootstrap (0), MainMap (1), Battle (2), ClanScreen (3), Settings (4), any others.

5. Test each transition manually:
   - MainMap → tap district → Attack → Battle → win → MainMap
   - MainMap → tap district → Attack → Battle → lose → MainMap
   - MainMap → Clan button → ClanScreen → Back → MainMap
   - MainMap → Settings → Back → MainMap
   - MainMap → Settings → Reset → Bootstrap → Tutorial → MainMap
   - Cold start (kill app) → Bootstrap → MainMap (with saved state)

6. Fix any dead ends or crashes found.

---

### TASK 55 — Save System Stress Test

**Goal:** Save system handles edge cases: corrupt files, fresh install, mid-battle quit, version upgrades.

**Requires:** Task 07 (SaveSystem)

**Files:** modify `SaveSystem.cs`

**Steps:**

1. Add corrupt save recovery:
```csharp
public static void Load()
{
    string path = Application.persistentDataPath + "/save.json";
    if (!System.IO.File.Exists(path))
    {
        Debug.Log("[SaveSystem] No save file — starting fresh.");
        CreateDefaultSave();
        return;
    }

    try
    {
        string json = System.IO.File.ReadAllText(path);
        var file = JsonUtility.FromJson<SaveFile>(json);
        if (file == null) throw new System.Exception("Deserialized to null");
        ApplySaveFile(file);
    }
    catch (System.Exception e)
    {
        Debug.LogError($"[SaveSystem] Corrupt save: {e.Message}. Backing up and starting fresh.");
        // Backup corrupt file
        string backup = path + ".corrupt." + System.DateTime.Now.Ticks;
        System.IO.File.Copy(path, backup);
        System.IO.File.Delete(path);
        CreateDefaultSave();
    }
}
```

2. Add save version field for future migration:
```csharp
[System.Serializable]
public class SaveFile
{
    public int SaveVersion = 1;
    // ... existing fields
}

// In Load(), after deserialize:
if (file.SaveVersion < CURRENT_SAVE_VERSION)
    MigrateSave(file);
```

3. Add auto-save on key events to prevent data loss:
```csharp
// Call Save() after:
// - Battle complete (win or lose)
// - District claimed/lost
// - Gear equipped
// - Fly trained
// - Settings changed
// - OnApplicationPause(true)
// - OnApplicationQuit()
```

4. In `GameManager.cs`:
```csharp
void OnApplicationPause(bool paused)
{
    if (paused) SaveSystem.Save();
}

void OnApplicationQuit()
{
    SaveSystem.Save();
}
```

5. Test scenarios:
   - Delete save file → launch → verify clean start, no crash
   - Replace save file with `{garbage}` → launch → verify backup created, clean start
   - Win battle → force-kill app (alt+F4 / swipe away) → relaunch → verify district saved
   - Play for 5 min → check save file size is reasonable (< 100KB)

---

### TASK 56 — UI/UX Pass

**Goal:** No overlapping text, all buttons reachable on mobile, touch targets ≥ 48dp, safe area respected.

**Requires:** All UI tasks complete

**Files:** modify various UI scripts and scene objects

**Steps:**

1. Add safe area handler to all canvases. Create `Assets/Scripts/UI/SafeAreaHandler.cs`:
```csharp
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaHandler : MonoBehaviour
{
    RectTransform _rect;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void ApplySafeArea()
    {
        var safeArea = Screen.safeArea;
        var min = safeArea.position;
        var max = safeArea.position + safeArea.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        _rect.anchorMin = min;
        _rect.anchorMax = max;
    }
}
```
Attach to the root content panel (child of Canvas) in every scene.

2. Audit touch target sizes. Minimum 48×48dp (approximately 48×48 Unity units on a 160dpi baseline). Check:
   - All buttons in DistrictInfoPanel
   - Fly cards in selection screen and Clan Screen
   - Settings sliders
   - Map district tiles
   - Toast dismiss areas

3. Check text scaling. Set TextMeshPro auto-size with min 12pt on all labels that might overflow:
```csharp
label.enableAutoSizing = true;
label.fontSizeMin = 12;
label.fontSizeMax = 24;
```

4. Test at multiple resolutions:
```powershell
# In Unity: Game view → add custom resolutions:
# 1080x1920 (standard phone)
# 1080x2400 (tall phone, Samsung S21+)
# 720x1280 (low-end)
# 1200x1920 (tablet)
```
Verify nothing overlaps or gets cut off at any resolution.

5. Verify panel layering — no panel hidden behind another. Check Canvas sort orders.

6. Test: screenshot at each resolution → review for clipping, overlap, unreachable buttons.

---

### TASK 57 — Balance Tuning Pass

**Goal:** Single pass through all Constants to make the game feel right. Difficulty ramps smoothly, economy doesn't stall or flood.

**Requires:** All gameplay tasks

**Files:** modify `Constants.cs`, modify `TerritoryManager.cs`

**Steps:**

1. Review and set all balance constants in one file. Create or update `Constants.cs`:
```csharp
public static class Constants
{
    // --- Battle ---
    public const int BATTLE_SLOTS = 5;
    public const int BOSS_BATTLE_SLOTS = 7;
    public const float BATTLE_ROUND_DELAY = 0.8f;  // seconds between rounds
    public const int XP_WIN = 50;
    public const int XP_LOSE = 10;

    // --- Economy ---
    public const int INCOME_PER_DISTRICT_PER_MIN = 5;
    public const int BATTLE_WIN_COINS = 20;
    public const int TRAINING_COST = 50;
    public const int TRAINING_XP = 30;
    public const int GEAR_UPGRADE_COST = 100;
    public const float GEAR_DROP_CHANCE = 0.4f;
    public const float BOSS_GEAR_DROP_CHANCE = 1.0f;

    // --- Gems (no IAP) ---
    public const int GEMS_DAILY_LOGIN = 5;
    public const int GEMS_BOSS_KILL = 10;
    public const int GEMS_ACHIEVEMENT = 5;
    public const int GEMS_DAILY_MISSION = 3;
    public const int SKIN_COST_GEMS = 80;

    // --- Difficulty ---
    public const int RING0_DIFFICULTY = 1;
    public const int RING1_DIFFICULTY = 3;
    public const int RING2_DIFFICULTY = 5;
    public const int RING3_DIFFICULTY = 7;  // 7x7 map
    public const int BOSS_DIFFICULTY = 6;

    // --- Enemy AI ---
    public const float ENEMY_ATTACK_INTERVAL = 300f;  // 5 min
    public const float ENEMY_CHANCE_VS_NEUTRAL = 0.3f;
    public const float ENEMY_CHANCE_VS_PLAYER = 0.15f;

    // --- Leveling ---
    public const int XP_PER_LEVEL = 100;
    public const float STAT_ATTACK_GROWTH = 1.1f;   // per level
    public const float STAT_DEFENSE_GROWTH = 1.1f;
    public const float STAT_HEALTH_GROWTH = 1.15f;
}
```

2. Replace all hardcoded magic numbers in the codebase with these constants:
```powershell
# Find hardcoded numbers that should be constants:
grep -rn "0\.4f\|300f\|0\.3f\|0\.15f\| 50;\| 100;\| 5 \*\|1\.1f\|1\.15f" Assets/Scripts/ --include="*.cs"
```
Replace each match with the corresponding `Constants.X` reference.

3. Verify the progression feels right. Target session:
   - Minutes 0-2: tutorial, claim center (free)
   - Minutes 2-5: fight 1-2 Ring 1 districts, first level up
   - Minutes 5-10: explore Ring 1, get first gear drop
   - Minutes 10-20: push into Ring 2, need to train flies
   - Minutes 20-30: tackle first boss, get legendary gear
   - After 30 min: player has 5-8 districts, level 3-4 flies, 1-2 gear pieces

4. If pacing feels off, adjust the constants and re-test. Document final values in comments.

---

### TASK 58 — Placeholder Art Audit

**Goal:** List every placeholder asset in the game. Output a manifest so art can be commissioned or created.

**Requires:** Task 18-21 (art tasks, which used procedural/placeholder art)

**Files:** new `ART_MANIFEST.md` in project root

**Steps:**

1. Scan for placeholder markers in code:
```powershell
grep -rn "\[POO\]\|placeholder\|TODO.*art\|TODO.*sprite\|grey\|gray.*square\|procedural\|Texture2D(1" Assets/Scripts/ --include="*.cs"
```

2. Scan for procedurally generated textures (likely placeholders):
```powershell
grep -rn "new Texture2D\|SetPixel\|Apply()\|CreateSprite" Assets/Scripts/ --include="*.cs"
```

3. Check all sprite references in scenes:
```powershell
.\unity-cmd.ps1 '{"type": "inspect", "path": "", "lens": "sprites"}'
```

4. Create `ART_MANIFEST.md`:
```markdown
# Art Asset Manifest — Fly Life

## Sprites Needed

### District Tiles
| Asset | Current State | Size | Notes |
|---|---|---|---|
| tile_park.png | Procedural green square | 128×128 | Ring 0, garden theme |
| tile_urban.png | Procedural grey square | 128×128 | Ring 1, city theme |
| tile_industrial.png | Procedural dark square | 128×128 | Ring 2, factory theme |
| tile_boss.png | Procedural red square | 128×128 | Corner boss districts |

### Fly Characters
| Asset | Current State | Size | Notes |
|---|---|---|---|
| fly_scout_idle.png | Procedural | 64×64 | Thin, fast-looking |
| fly_soldier_idle.png | Procedural | 64×64 | Bulky, armored |
| fly_worker_idle.png | Procedural | 64×64 | Round, healer look |
| fly_*_battle.png | Same as idle | 64×64 | Battle stance variant |

### Poo Types (7)
| Asset | Current | Size |
|---|---|---|
| poo_dog.png | Brown circle | 32×32 |
| poo_bird.png | Brown circle | 32×32 |
| ... (all 7 types) | | |

### UI
| Asset | Current | Notes |
|---|---|---|
| icon_poocoin.png | 💰 emoji | Currency icon |
| icon_gem.png | 💎 emoji | Currency icon |
| btn_attack.png | Default Unity button | Red theme |
| btn_claim.png | Default Unity button | Green theme |
| app_icon.png | None | 512×512, Play Store |

### Audio
| Asset | Current | Duration |
|---|---|---|
| music_menu.ogg | None/placeholder | 30-60s loop |
| music_battle.ogg | None/placeholder | 30-60s loop |
| sfx_hit.ogg | Placeholder beep | <1s |
| sfx_levelup.ogg | Placeholder beep | <1s |
| sfx_capture.ogg | Placeholder beep | <1s |
| sfx_button.ogg | Placeholder beep | <1s |
```

5. Count totals and estimate effort at bottom of manifest.

---

### TASK 59 — Error Handling Pass

**Goal:** No NullReferenceExceptions in any code path. Every manager singleton access is guarded. Every UI element is null-checked.

**Requires:** All previous tasks

**Files:** audit across all scripts

**Steps:**

1. Find all unguarded singleton access:
```powershell
grep -rn "\.Instance\." Assets/Scripts/ --include="*.cs" | grep -v "?.\|!= null\|== null"
```
Every result should be changed to use `?.` or a null check.

2. Find all unguarded UI element access:
```powershell
grep -rn "\.text = \|\.SetActive\|\.gameObject" Assets/Scripts/ --include="*.cs" | grep -v "?\.\|if ("
```
Wrap in null checks or use `?.` operator.

3. Add try-catch to critical startup paths:
```csharp
// In GameManager.Start() or similar:
try
{
    SaveSystem.Load();
}
catch (System.Exception e)
{
    Debug.LogError($"[GameManager] Save load failed: {e.Message}");
    // Continue with defaults
}
```

4. Verify every coroutine has a null guard on its dependencies at the top.

5. Add `[SerializeField]` validation — log warnings for unassigned Inspector fields:
```csharp
void OnValidate()
{
    if (SomeRequiredField == null)
        Debug.LogWarning($"[{GetType().Name}] SomeRequiredField is not assigned!", this);
}
```
Add to key MonoBehaviours: `BattleSceneController`, `DistrictInfoPanel`, `ClanScreenController`, `MapManager`.

6. Test: play full loop → check console for zero errors/warnings (except expected debug logs).

---

### TASK 60 — Android Device Build & Test

**Goal:** Signed APK runs on 2+ real devices without crashes. Full game loop works on touch input.

**Requires:** All Tasks 51-59

**Files:** modify `ProjectSettings/`, build scripts

**Steps:**

1. Update build settings:
```csharp
// Via unity-cmd or Editor:
// Player Settings → Android:
//   Package Name: com.vencacode.flylife
//   Min API Level: 26 (Android 8.0)
//   Target API Level: 34 (Android 14)
//   Scripting Backend: IL2CPP
//   Target Architectures: ARM64 (drop ARMv7 unless targeting very old devices)
//   Install Location: Automatic
```

2. Create keystore for signing (if not already):
```powershell
# In Unity: Edit → Project Settings → Player → Publishing Settings
# Create New Keystore → set password → create key alias
# Store keystore file OUTSIDE the repo
```

3. Build AAB (Android App Bundle):
```
File → Build Settings → Android → Build
Output: flylife-firstplayable.aab
```

4. Also build APK for direct install testing:
```
File → Build Settings → Android → Switch to APK → Build
Output: flylife-firstplayable.apk
```

5. Install and test on Device 1 (target: mid-range, e.g. Pixel 6a or Samsung A54):
```powershell
adb install flylife-firstplayable.apk
```
Test checklist:
   - [ ] App launches without crash
   - [ ] Tutorial plays through
   - [ ] District tap → info panel → attack → battle → win → return
   - [ ] Clan screen opens, fly selection works
   - [ ] Gear equip works
   - [ ] Settings screen (volume sliders, reset)
   - [ ] Save persists after kill + reopen
   - [ ] Map zoom and pan smooth
   - [ ] No "Watch Ad" buttons visible
   - [ ] Gem income works (boss kill, daily login)
   - [ ] 60fps maintained during battle
   - [ ] No ANR (Application Not Responding) dialogs

6. Test on Device 2 (low-end or different screen size):
   - Repeat checklist above
   - Check safe area on devices with notch/punch hole

7. Collect `adb logcat` output during testing:
```powershell
adb logcat -s Unity | tee device_test_log.txt
```
Fix any errors found.

8. If all tests pass, tag the build:
```powershell
git tag v0.1.0-firstplayable
```

---

## Notes for Claude Code

- Always run `.\unity-cmd.ps1 '{"type": "refresh"}' -Timeout 120` after editing any .cs file
- Use `.\unity-cmd.ps1 '{"type": "errors"}'` to check compilation status
- Use scratch pad (`BridgeScratch.cs` + `{"type": "scratch"}`) for complex scene setup
- Save scenes after every unity-cmd batch: `{"type": "save-scene"}`
- When in doubt about scene state: `{"type": "scene"}` or `{"type": "inspect", "path": "...", "lens": "scripts"}`
- Screenshots via `{"type": "screenshot", "delay": 2}` to verify visual output
