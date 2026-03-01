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

## PHASE 6 — Game Feel & Onboarding

### TASK 23 — Tutorial Flow

**Goal:** First-time player is guided through claiming center district in a scripted sequence. Subsequent sessions skip tutorial.

**Requires:** Task 01 (Bootstrap), Task 03 (Battle), Task 07 (SaveSystem)

**Files:** new `TutorialManager.cs`, modify `GameManager.cs`, modify `SaveSystem.cs`, modify `DistrictInfoPanel.cs`

**Steps:**

1. Add `bool TutorialComplete` to `SaveFile` in `SaveSystem.cs`.

2. Create `Assets/Scripts/Managers/TutorialManager.cs`:
```csharp
using System.Collections;
using UnityEngine;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI")]
    public GameObject OverlayPanel;       // full-screen semi-transparent
    public TextMeshProUGUI DialogueText;
    public GameObject TapToContinue;
    public GameObject PointerArrow;       // arrow sprite pointing at target

    bool _waitingForTap;
    bool _active;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void StartTutorial()
    {
        _active = true;
        StartCoroutine(TutorialSequence());
    }

    IEnumerator TutorialSequence()
    {
        // Step 1: Welcome
        yield return ShowDialogue("Welcome, Commander! Your fly clan needs territory. Let's take our first district.");

        // Step 2: Point at center tile
        PointerArrow?.SetActive(true);
        yield return ShowDialogue("Tap the center district to inspect it.");

        // Step 3: Wait for player to open DistrictInfoPanel on center district
        yield return new WaitUntil(() => DistrictInfoPanel.ActiveDistrict?.Id == "district_2_2");
        PointerArrow?.SetActive(false);

        // Step 4: Point at Claim/Attack button
        yield return ShowDialogue("Now tap 'Attack' to fight for this district!");

        // Step 5: Wait for battle scene load
        yield return new WaitUntil(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Battle");

        // Step 6: Battle runs automatically — wait for return to MainMap
        yield return new WaitUntil(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMap");

        // Step 7: Congrats
        yield return ShowDialogue("Victory! This district is now yours. It generates PooCoins every minute. Keep conquering!");

        // Done
        _active = false;
        OverlayPanel?.SetActive(false);
        SaveSystem.Instance?.MarkTutorialComplete();
    }

    IEnumerator ShowDialogue(string text)
    {
        OverlayPanel?.SetActive(true);
        if (DialogueText) DialogueText.text = text;
        TapToContinue?.SetActive(true);
        _waitingForTap = true;
        yield return new WaitUntil(() => !_waitingForTap);
        TapToContinue?.SetActive(false);
    }

    void Update()
    {
        if (_waitingForTap && Input.GetMouseButtonDown(0))
            _waitingForTap = false;
    }

    public bool IsActive => _active;
}
```

3. Add `public static District ActiveDistrict` to `DistrictInfoPanel.cs`:
```csharp
public static District ActiveDistrict { get; private set; }

public void Show(District d)
{
    ActiveDistrict = d;
    // ... rest of existing Show() code
}

public void Hide()
{
    ActiveDistrict = null;
    gameObject.SetActive(false);
}
```

4. In `GameManager.cs`, after load check tutorial state:
```csharp
// After SaveSystem.Load() completes and MainMap is loaded:
if (!SaveSystem.Instance.IsTutorialComplete())
    TutorialManager.Instance?.StartTutorial();
```

5. Add `MarkTutorialComplete()` and `IsTutorialComplete()` to `SaveSystem.cs`:
```csharp
public void MarkTutorialComplete()
{
    _currentFile.TutorialComplete = true;
    Save();
}

public bool IsTutorialComplete() => _currentFile?.TutorialComplete ?? false;
```

6. Build tutorial UI in MainMap scene:
```powershell
.\unity-cmd.ps1 '[
  {"type": "create", "path": "TutorialCanvas/OverlayPanel", "components": ["Canvas", "Image"]},
  {"type": "create", "path": "TutorialCanvas/OverlayPanel/DialogueText", "components": ["TextMeshProUGUI"]},
  {"type": "create", "path": "TutorialCanvas/OverlayPanel/TapToContinue", "components": ["TextMeshProUGUI"]},
  {"type": "create", "path": "TutorialCanvas/PointerArrow", "components": ["Image"]},
  {"type": "create", "path": "TutorialCanvas/TutorialManager", "components": ["TutorialManager"]},
  {"type": "save-scene"}
]'
```

7. Test: delete save file → play from Bootstrap → verify tutorial sequence runs → complete it → restart → verify tutorial is skipped.

---

### TASK 24 — Toast Notification System

**Goal:** In-game toast popups for: district lost, gear dropped, level up, income received. Slide in from top, auto-dismiss after 3s.

**Requires:** Task 11 (UIEvent.DistrictLost), Task 08 (UIEvent.BattleItemDropped), Task 05 (UIEvent.LevelUp)

**Files:** new `ToastManager.cs`, new `ToastItem.cs`

**Steps:**

1. Create `Assets/Scripts/UI/ToastManager.cs`:
```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ToastManager : MonoBehaviour
{
    public static ToastManager Instance { get; private set; }

    [Header("Config")]
    public GameObject ToastPrefab;
    public Transform ToastContainer;  // vertical layout group at top of screen
    public int MaxVisible = 3;
    public float DisplayDuration = 3f;

    readonly Queue<GameObject> _active = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        UIEventBus.On(UIEvent.DistrictLost, d => ShowToast($"⚠️ District lost: {((District)d).Name}!"));
        UIEventBus.On(UIEvent.BattleItemDropped, d => ShowToast($"🎁 Gear drop: {((EquipmentData)d).ItemName}!"));
        UIEventBus.On(UIEvent.LevelUp, d => {
            var f = (FlyInstance)d;
            ShowToast($"⬆️ {f.Role} levelled up to {f.Level}!");
        });
    }

    public void ShowToast(string message)
    {
        if (ToastPrefab == null || ToastContainer == null) return;
        var go = Instantiate(ToastPrefab, ToastContainer);
        var label = go.GetComponentInChildren<TextMeshProUGUI>();
        if (label) label.text = message;
        _active.Enqueue(go);

        // Remove excess
        while (_active.Count > MaxVisible)
        {
            var old = _active.Dequeue();
            if (old) Destroy(old);
        }

        StartCoroutine(DismissAfterDelay(go));
    }

    IEnumerator DismissAfterDelay(GameObject go)
    {
        yield return new WaitForSeconds(DisplayDuration);
        if (go != null)
        {
            // Fade out via CanvasGroup
            var cg = go.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                float t = 0.3f;
                while (t > 0)
                {
                    t -= Time.deltaTime;
                    cg.alpha = t / 0.3f;
                    yield return null;
                }
            }
            Destroy(go);
        }
    }
}
```

2. Create toast prefab: a horizontal layout with Image (icon area) + TMP label + CanvasGroup for fade:
```powershell
.\unity-cmd.ps1 '[
  {"type": "create", "path": "ToastPrefab", "components": ["CanvasGroup", "HorizontalLayoutGroup", "Image"]},
  {"type": "create", "path": "ToastPrefab/Label", "components": ["TextMeshProUGUI"]},
  {"type": "save-prefab", "path": "Assets/Prefabs/UI/ToastPrefab.prefab", "source": "ToastPrefab"}
]'
```

3. Add `ToastManager` + `ToastContainer` (VerticalLayoutGroup, top-anchored) to MainMap canvas:
```powershell
.\unity-cmd.ps1 '[
  {"type": "create", "path": "HUD/ToastContainer", "components": ["VerticalLayoutGroup"]},
  {"type": "create", "path": "HUD/ToastManager", "components": ["ToastManager"]},
  {"type": "save-scene"}
]'
```
Wire `ToastPrefab` and `ToastContainer` references in Inspector.

4. Test: win a battle that drops gear → verify toast appears at top. Lose a district to enemy AI (set interval to 10s) → verify warning toast.

---

### TASK 25 — Settings Screen

**Goal:** Accessible from MainMap HUD. Controls: SFX volume slider, Music volume slider, Reset Progress button (with confirmation), Credits text.

**Requires:** Task 21 (AudioManager with SFX), Task 07 (SaveSystem)

**Files:** new `SettingsScreenController.cs`, modify `AudioManager.cs`

**Steps:**

1. Create `Assets/Scripts/UI/SettingsScreenController.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsScreenController : MonoBehaviour
{
    [Header("Audio")]
    public Slider SFXSlider;
    public Slider MusicSlider;

    [Header("Reset")]
    public Button ResetButton;
    public GameObject ConfirmPanel;
    public Button ConfirmYes;
    public Button ConfirmNo;

    [Header("Navigation")]
    public Button BackButton;

    void Start()
    {
        SFXSlider.value = AudioManager.Instance?.SFXVolume ?? 1f;
        MusicSlider.value = AudioManager.Instance?.MusicVolume ?? 1f;

        SFXSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetSFXVolume(v));
        MusicSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetMusicVolume(v));

        ResetButton.onClick.AddListener(() => ConfirmPanel?.SetActive(true));
        ConfirmYes.onClick.AddListener(OnResetConfirmed);
        ConfirmNo.onClick.AddListener(() => ConfirmPanel?.SetActive(false));
        BackButton.onClick.AddListener(() => GameManager.Instance?.LoadScene("MainMap"));

        ConfirmPanel?.SetActive(false);
    }

    void OnResetConfirmed()
    {
        SaveSystem.Instance?.DeleteSave();
        // Reload from scratch
        GameManager.Instance?.LoadScene("Bootstrap");
    }
}
```

2. Add volume control methods to `AudioManager.cs`:
```csharp
public float SFXVolume { get; private set; } = 1f;
public float MusicVolume { get; private set; } = 1f;

public void SetSFXVolume(float v)
{
    SFXVolume = Mathf.Clamp01(v);
    // Apply to all SFX AudioSources
    PlayerPrefs.SetFloat("SFXVolume", SFXVolume);
}

public void SetMusicVolume(float v)
{
    MusicVolume = Mathf.Clamp01(v);
    if (_musicSource) _musicSource.volume = MusicVolume;
    PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
}

void Start()
{
    SFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
}
```

3. Add `DeleteSave()` to `SaveSystem.cs`:
```csharp
public void DeleteSave()
{
    string path = Application.persistentDataPath + "/save.json";
    if (System.IO.File.Exists(path))
        System.IO.File.Delete(path);
    Debug.Log("[SaveSystem] Save deleted.");
}
```

4. Create `Assets/Scenes/Settings.unity` and build UI:
```powershell
.\unity-cmd.ps1 '[
  {"type": "create-scene", "path": "Assets/Scenes/Settings.unity"},
  {"type": "create", "path": "Canvas/SFXLabel", "components": ["TextMeshProUGUI"]},
  {"type": "create", "path": "Canvas/SFXSlider", "components": ["Slider"]},
  {"type": "create", "path": "Canvas/MusicLabel", "components": ["TextMeshProUGUI"]},
  {"type": "create", "path": "Canvas/MusicSlider", "components": ["Slider"]},
  {"type": "create", "path": "Canvas/ResetButton", "components": ["Button", "TextMeshProUGUI"]},
  {"type": "create", "path": "Canvas/ConfirmPanel", "components": ["Image"]},
  {"type": "create", "path": "Canvas/ConfirmPanel/YesButton", "components": ["Button", "TextMeshProUGUI"]},
  {"type": "create", "path": "Canvas/ConfirmPanel/NoButton", "components": ["Button", "TextMeshProUGUI"]},
  {"type": "create", "path": "Canvas/BackButton", "components": ["Button", "TextMeshProUGUI"]},
  {"type": "create", "path": "Canvas/Credits", "components": ["TextMeshProUGUI"]},
  {"type": "create", "path": "Controller", "components": ["SettingsScreenController"]},
  {"type": "save-scene"}
]'
```

5. Add Settings scene to Build Settings. Add ⚙️ button to MainMap HUD → `GameManager.Instance.LoadScene("Settings")`.

6. Test: open settings → drag sliders → verify volume changes. Tap Reset → confirm → verify game restarts from tutorial.

---

### TASK 26 — Battle Fly Selection Screen

**Goal:** Before battle starts, player picks which flies (up to 5) to send. Shows fly cards with role, level, HP, equipped gear.

**Requires:** Task 03 (battle flow), Task 08 (gear), Task 10 (FlyRoleData)

**Files:** new `FlySelectionUI.cs`, modify `DistrictInfoPanel.cs`, modify `BattleManager.cs`

**Steps:**

1. Create `Assets/Scripts/UI/FlySelectionUI.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FlySelectionUI : MonoBehaviour
{
    public static FlySelectionUI Instance { get; private set; }

    [Header("UI")]
    public GameObject Panel;
    public Transform FlyCardContainer;  // vertical scroll content
    public GameObject FlyCardPrefab;
    public Button ConfirmButton;
    public Button CancelButton;
    public TextMeshProUGUI SelectedCountText;

    readonly List<FlyInstance> _selected = new();
    System.Action<List<FlyInstance>> _onConfirm;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Panel?.SetActive(false);
        ConfirmButton?.onClick.AddListener(OnConfirm);
        CancelButton?.onClick.AddListener(OnCancel);
    }

    public void Show(System.Action<List<FlyInstance>> onConfirm)
    {
        _onConfirm = onConfirm;
        _selected.Clear();
        Panel?.SetActive(true);

        // Clear old cards
        foreach (Transform child in FlyCardContainer)
            Destroy(child.gameObject);

        // Build fly cards
        var flies = ClanManager.Instance?.GetAllFlies()
            .Where(f => f.IsAlive)
            .OrderByDescending(f => f.Level)
            .ToList();

        if (flies == null) return;
        foreach (var fly in flies)
        {
            var card = Instantiate(FlyCardPrefab, FlyCardContainer);
            var label = card.GetComponentInChildren<TextMeshProUGUI>();
            var equippedItem = InventoryManager.Instance?.GetEquippedItem(fly.InstanceId);
            string gearText = equippedItem != null ? $" [{equippedItem}]" : "";
            if (label) label.text = $"{fly.Role} Lv{fly.Level} | HP:{fly.MaxHealth} ATK:{fly.Attack}{gearText}";

            var toggle = card.GetComponentInChildren<Toggle>();
            if (toggle)
            {
                var capturedFly = fly;
                toggle.isOn = false;
                toggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn && _selected.Count < Constants.BATTLE_SLOTS)
                        _selected.Add(capturedFly);
                    else if (isOn)
                        toggle.isOn = false; // at max
                    else
                        _selected.Remove(capturedFly);
                    UpdateCountText();
                });
            }
        }
        UpdateCountText();
    }

    void UpdateCountText()
    {
        if (SelectedCountText)
            SelectedCountText.text = $"{_selected.Count}/{Constants.BATTLE_SLOTS} selected";
        if (ConfirmButton)
            ConfirmButton.interactable = _selected.Count > 0;
    }

    void OnConfirm()
    {
        Panel?.SetActive(false);
        _onConfirm?.Invoke(new List<FlyInstance>(_selected));
    }

    void OnCancel()
    {
        Panel?.SetActive(false);
        _selected.Clear();
    }
}
```

2. Create FlyCard prefab with Toggle + Label:
```powershell
.\unity-cmd.ps1 '[
  {"type": "create", "path": "FlyCardPrefab", "components": ["Toggle", "HorizontalLayoutGroup", "Image"]},
  {"type": "create", "path": "FlyCardPrefab/Label", "components": ["TextMeshProUGUI"]},
  {"type": "save-prefab", "path": "Assets/Prefabs/UI/FlyCardPrefab.prefab", "source": "FlyCardPrefab"}
]'
```

3. Add `GetAllFlies()` to `ClanManager.cs` if missing:
```csharp
public List<FlyInstance> GetAllFlies() => new(_flies);
```

4. Modify `DistrictInfoPanel.OnAttack()` and `OnClaim()` to go through selection first:
```csharp
void OnAttack()
{
    if (_current == null) return;
    FlySelectionUI.Instance?.Show(selectedFlies =>
    {
        var enemies = GenerateEnemyFlies(_current.DifficultyLevel);
        BattleManager.Instance?.PrepareBattle(_current.Id, enemies, selectedFlies);
        GameManager.Instance?.LoadScene("Battle");
    });
}

void OnClaim()
{
    if (_current == null) return;
    FlySelectionUI.Instance?.Show(selectedFlies =>
    {
        var enemies = GenerateEnemyFlies(_current.DifficultyLevel);
        BattleManager.Instance?.PrepareBattle(_current.Id, enemies, selectedFlies);
        GameManager.Instance?.LoadScene("Battle");
    });
}
```

5. Update `BattleManager.PrepareBattle()` to accept player fly list:
```csharp
public void PrepareBattle(string districtId, List<FlyInstance> enemies, List<FlyInstance> playerFlies)
{
    PendingDistrictId = districtId;
    EnemySlots = enemies;
    PlayerSlots = playerFlies;
}
```

6. Add `FlySelectionUI` panel to MainMap scene. Wire prefab and container references.

7. Test: tap enemy district → fly selection appears → pick 3 flies → confirm → battle loads with those 3 flies vs 5 enemies.

---

### TASK 27 — Gear Equip UI in Clan Screen

**Goal:** In Clan Screen, player taps a fly → sees gear slot → taps slot → picks from owned unequipped gear → equips it.

**Requires:** Task 15 (Clan Screen), Task 08 (InventoryManager)

**Files:** modify `ClanScreenController.cs`, new `GearEquipPopup.cs`

**Steps:**

1. Create `Assets/Scripts/UI/GearEquipPopup.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GearEquipPopup : MonoBehaviour
{
    public static GearEquipPopup Instance { get; private set; }

    public GameObject Panel;
    public Transform ItemListContainer;
    public GameObject ItemButtonPrefab;
    public Button CloseButton;
    public TextMeshProUGUI FlyNameLabel;

    string _targetFlyId;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Panel?.SetActive(false);
        CloseButton?.onClick.AddListener(() => Panel?.SetActive(false));
    }

    public void Show(FlyInstance fly)
    {
        _targetFlyId = fly.InstanceId;
        if (FlyNameLabel) FlyNameLabel.text = $"Equip gear for {fly.Role} Lv{fly.Level}";
        Panel?.SetActive(true);

        // Clear old items
        foreach (Transform child in ItemListContainer)
            Destroy(child.gameObject);

        // Show owned items
        var inv = InventoryManager.Instance;
        if (inv == null) return;
        var owned = inv.GetOwnedItems();

        foreach (var itemId in owned.Distinct())
        {
            var data = inv.GetEquipmentData(itemId);
            if (data == null) continue;

            var btn = Instantiate(ItemButtonPrefab, ItemListContainer);
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label) label.text = $"{data.ItemName} | ATK+{data.AttackBonus} DEF+{data.DefenseBonus} SPD+{data.SpeedBonus}";

            var capturedId = itemId;
            btn.GetComponent<Button>()?.onClick.AddListener(() =>
            {
                inv.EquipItem(_targetFlyId, capturedId);
                Panel?.SetActive(false);
                // Refresh clan screen
                ClanScreenController.Instance?.RefreshFlyList();
            });
        }
    }
}
```

2. In `ClanScreenController.cs`, add equip button per fly card:
```csharp
public static ClanScreenController Instance { get; private set; }

void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
}

// In fly card creation loop, add:
var equipBtn = card.transform.Find("EquipButton")?.GetComponent<Button>();
var capturedFly = fly;
equipBtn?.onClick.AddListener(() => GearEquipPopup.Instance?.Show(capturedFly));

// Show currently equipped item on card:
var equipped = InventoryManager.Instance?.GetEquippedItem(fly.InstanceId);
var gearLabel = card.transform.Find("GearLabel")?.GetComponent<TextMeshProUGUI>();
if (gearLabel) gearLabel.text = equipped != null
    ? $"⚔️ {InventoryManager.Instance.GetEquipmentData(equipped)?.ItemName ?? equipped}"
    : "No gear";

public void RefreshFlyList()
{
    // Re-populate fly list — call existing populate method
}
```

3. Add `GearEquipPopup` to ClanScreen scene. Wire prefab and container.

4. Test: open Clan Screen → tap equip on a fly → select Poo Thrower → verify gear label updates → enter battle → verify Attack bonus applied.

---

### TASK 28 — Fly Training (Spend PooCoins for XP)

**Goal:** In Clan Screen, "Train" button on each fly. Costs 50 PooCoins, awards 30 XP to that fly.

**Requires:** Task 15 (Clan Screen), Task 05 (fly XP), Task 06 (PooCoins)

**Files:** modify `ClanScreenController.cs`, modify `ClanManager.cs`

**Steps:**

1. Add constants:
```csharp
// In Constants.cs
public const int TRAINING_COST = 50;    // PooCoins
public const int TRAINING_XP   = 30;
```

2. Add training method to `ClanManager.cs`:
```csharp
public bool TrainFly(string flyId)
{
    var fly = _flies.FirstOrDefault(f => f.InstanceId == flyId);
    if (fly == null) return false;
    if (ResourceManager.Instance == null || !ResourceManager.Instance.SpendPooCoins(Constants.TRAINING_COST))
        return false;

    fly.XP += Constants.TRAINING_XP;
    int newLevel = Mathf.FloorToInt(fly.XP / 100f) + 1;
    if (newLevel > fly.Level)
    {
        fly.Level = newLevel;
        fly.Attack    = Mathf.RoundToInt(fly.Attack  * 1.1f);
        fly.Defense   = Mathf.RoundToInt(fly.Defense * 1.1f);
        fly.MaxHealth = Mathf.RoundToInt(fly.MaxHealth * 1.15f);
        fly.CurrentHealth = fly.MaxHealth;
        UIEventBus.Emit(UIEvent.LevelUp, fly);
    }
    Debug.Log($"[ClanManager] Trained {fly.Role}: +{Constants.TRAINING_XP} XP (now {fly.XP})");
    return true;
}
```

3. In `ClanScreenController.cs`, add Train button per fly card:
```csharp
var trainBtn = card.transform.Find("TrainButton")?.GetComponent<Button>();
var capturedFly = fly;
trainBtn?.onClick.AddListener(() =>
{
    if (ClanManager.Instance?.TrainFly(capturedFly.InstanceId) == true)
        RefreshFlyList();
    else
        ToastManager.Instance?.ShowToast("Not enough PooCoins!");
});

// Show cost on button label
var trainLabel = trainBtn?.GetComponentInChildren<TextMeshProUGUI>();
if (trainLabel) trainLabel.text = $"Train ({Constants.TRAINING_COST} 💰)";
```

4. Test: earn 100+ PooCoins → open Clan Screen → train a fly twice → verify XP increases and level up triggers if threshold crossed.

---

### TASK 29 — Gear Upgrade (Spend PooCoins for +1 Stats)

**Goal:** In Gear Equip popup, "Upgrade" button on equipped gear. Costs 100 PooCoins, permanently adds +1 to the gear's primary stat.

**Requires:** Task 27 (GearEquipPopup), Task 08 (InventoryManager)

**Files:** modify `InventoryManager.cs`, modify `GearEquipPopup.cs`

**Steps:**

1. Add upgrade tracking to `InventoryManager.cs`:
```csharp
Dictionary<string, int> _upgradeLevels = new(); // itemId → upgrade count

public int GetUpgradeLevel(string itemId) =>
    _upgradeLevels.TryGetValue(itemId, out var lvl) ? lvl : 0;

public bool UpgradeItem(string itemId)
{
    if (!_ownedItemIds.Contains(itemId)) return false;
    int cost = Constants.GEAR_UPGRADE_COST;
    if (ResourceManager.Instance == null || !ResourceManager.Instance.SpendPooCoins(cost))
        return false;

    _upgradeLevels.TryGetValue(itemId, out int current);
    _upgradeLevels[itemId] = current + 1;
    Debug.Log($"[Inventory] Upgraded {itemId} to +{current + 1}");
    return true;
}
```

2. Update `ApplyGearStats` to include upgrade bonuses:
```csharp
public void ApplyGearStats(FlyInstance fly)
{
    var itemId = GetEquippedItem(fly.InstanceId);
    if (itemId == null) return;
    var data = GetEquipmentData(itemId);
    if (data == null) return;
    int upgrades = GetUpgradeLevel(itemId);
    fly.Attack  += data.AttackBonus  + (data.AttackBonus  > 0 ? upgrades : 0);
    fly.Defense += data.DefenseBonus + (data.DefenseBonus > 0 ? upgrades : 0);
    fly.Speed   += data.SpeedBonus  + (data.SpeedBonus   > 0 ? upgrades : 0);
}
```

3. Add upgrade data to save/load in `InventoryManager.SaveData`:
```csharp
public List<string> UpgradeKeys = new();
public List<int> UpgradeValues = new();
```
Wire in `GetSaveData()` and `LoadSaveData()`.

4. Add constant:
```csharp
// In Constants.cs
public const int GEAR_UPGRADE_COST = 100;
```

5. In `GearEquipPopup.cs`, show upgrade button for currently equipped item:
```csharp
// After showing item list, if fly has equipped gear, show upgrade option:
var equippedId = inv.GetEquippedItem(_targetFlyId);
if (equippedId != null)
{
    var eqData = inv.GetEquipmentData(equippedId);
    int lvl = inv.GetUpgradeLevel(equippedId);
    // Add upgrade button at top of panel
    var upgradeLabel = $"Upgrade {eqData?.ItemName} (+{lvl}) — {Constants.GEAR_UPGRADE_COST} 💰";
    // ... create button, wire onClick to inv.UpgradeItem(equippedId)
}
```

6. Test: equip Poo Thrower → upgrade it → verify Attack bonus is now 9 (8 base + 1) → verify persists after save/load.

---

### TASK 30 — Map Zoom & Pan

**Goal:** Player can pinch-zoom and drag the map on mobile. Map content scales, camera stays within bounds.

**Requires:** Task 01 (MainMap scene)

**Files:** new `MapCameraController.cs`

**Steps:**

1. Create `Assets/Scripts/Map/MapCameraController.cs`:
```csharp
using UnityEngine;

public class MapCameraController : MonoBehaviour
{
    [Header("Zoom")]
    public float MinZoom = 3f;
    public float MaxZoom = 8f;
    public float ZoomSpeed = 0.5f;

    [Header("Pan Bounds")]
    public Vector2 MinBounds = new(-2f, -2f);
    public Vector2 MaxBounds = new(6f, 6f);

    Camera _cam;
    Vector3 _dragOrigin;
    bool _isDragging;

    void Start() => _cam = Camera.main;

    void Update()
    {
        if (_cam == null) return;

        // Pinch zoom (mobile)
        if (Input.touchCount == 2)
        {
            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);
            float prevDist = (t0.position - t0.deltaPosition - (t1.position - t1.deltaPosition)).magnitude;
            float currDist = (t0.position - t1.position).magnitude;
            float delta = prevDist - currDist;
            _cam.orthographicSize = Mathf.Clamp(
                _cam.orthographicSize + delta * ZoomSpeed * Time.deltaTime,
                MinZoom, MaxZoom);
            _isDragging = false;
            return;
        }

        // Scroll wheel zoom (editor)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
            _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize - scroll * 2f, MinZoom, MaxZoom);

        // Pan (single finger/mouse drag)
        if (Input.GetMouseButtonDown(0))
        {
            _dragOrigin = _cam.ScreenToWorldPoint(Input.mousePosition);
            _isDragging = true;
        }

        if (Input.GetMouseButton(0) && _isDragging)
        {
            Vector3 diff = _dragOrigin - _cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPos = _cam.transform.position + diff;
            newPos.x = Mathf.Clamp(newPos.x, MinBounds.x, MaxBounds.x);
            newPos.y = Mathf.Clamp(newPos.y, MinBounds.y, MaxBounds.y);
            newPos.z = _cam.transform.position.z;
            _cam.transform.position = newPos;
        }

        if (Input.GetMouseButtonUp(0))
            _isDragging = false;
    }
}
```

2. Attach `MapCameraController` to Main Camera in MainMap scene.

3. Set bounds based on your 5×5 grid tile positions. Adjust `MinBounds`/`MaxBounds` to match actual world coordinates.

4. Test: play on device → pinch to zoom → drag to pan → verify camera stays within map bounds. Also verify district taps still register correctly at different zoom levels.

---

### TASK 31 — Push Notifications

**Goal:** Local push notifications for: "Your district is under attack!" (when enemy AI captures) and daily login reminder (if not opened in 24h).

**Requires:** Task 11 (EnemyAI), Task 13 (daily login), Task 22 (Android build)

**Files:** new `NotificationManager.cs`

**Steps:**

1. Install Unity Mobile Notifications package:
```
Window → Package Manager → Unity Registry → Mobile Notifications → Install
```

2. Create `Assets/Scripts/Managers/NotificationManager.cs`:
```csharp
using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
#if UNITY_ANDROID
        // Create notification channel
        var channel = new AndroidNotificationChannel
        {
            Id = "flylife_main",
            Name = "Fly Life",
            Importance = Importance.Default,
            Description = "Game notifications"
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);

        // Cancel old notifications on app open
        AndroidNotificationCenter.CancelAllNotifications();

        // Schedule daily reminder for 24h from now
        ScheduleDailyReminder();
#endif
    }

    public void ScheduleDailyReminder()
    {
#if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = "Your flies miss you! 🪰",
            Text = "Districts are generating PooCoins. Come collect!",
            FireTime = System.DateTime.Now.AddHours(24),
            SmallIcon = "icon_small",
            LargeIcon = "icon_large"
        };
        AndroidNotificationCenter.SendNotification(notification, "flylife_main");
#endif
    }

    public void NotifyDistrictUnderAttack(string districtName)
    {
#if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = "District under attack! ⚠️",
            Text = $"{districtName} is being invaded by enemies!",
            FireTime = System.DateTime.Now.AddSeconds(1),
            SmallIcon = "icon_small"
        };
        AndroidNotificationCenter.SendNotification(notification, "flylife_main");
#endif
    }
}
```

3. Wire into `EnemyAIManager.TryEnemyAttack()` — when a player district is captured while app is backgrounded:
```csharp
// This only fires while app is running, but schedule a "check back" notification
// for when player is away. In OnApplicationPause(true):
NotificationManager.Instance?.ScheduleDailyReminder();
```

4. Add `NotificationManager` to GameManager prefab.

5. Test on Android device: open game → background it → wait for notification after 24h (or temporarily set to 30 seconds for testing).

---

## PHASE 7 — Content Expansion & Monetisation

### TASK 35 — Cosmetic Fly Skins

**Goal:** Players can unlock and apply cosmetic skins to individual flies. Skins are purely visual and do not affect stats. Some skins are earned through gameplay, others purchasable with Gems.

**Files to create:** `Assets/Scripts/Data/SkinRegistry.cs`, `Assets/Scripts/UI/SkinSelectPopup.cs`
**Files to modify:** `Assets/Scripts/Data/FlyData.cs`, `Assets/Scripts/Managers/ClanManager.cs`, `Assets/Scripts/Utils/SaveSystem.cs`, `Assets/Scripts/UI/ClanScreenController.cs`, `Assets/Scripts/Core/GameManager.cs`

**Steps:**

1. Add `SkinId` field to `FlyInstance` in `FlyData.cs`:
```csharp
public string SkinId;           // null/empty = default skin
```

2. Create `Assets/Scripts/Data/SkinRegistry.cs`:
```csharp
using System.Collections.Generic;

/// <summary>
/// Static registry of all cosmetic fly skins.
/// Skins are purely visual — no stat changes.
/// </summary>
public static class SkinRegistry
{
    public struct SkinDef
    {
        public string SkinId;
        public string DisplayName;
        public string Description;
        public string ColorHex;        // Tint color for fly sprite
        public SkinSource Source;
        public int GemCost;            // 0 if not purchasable
        public string UnlockCondition; // human-readable
    }

    public enum SkinSource { Default, GemShop, BattleReward, LevelReward, Seasonal }

    static readonly Dictionary<string, SkinDef> _skins = new()
    {
        ["default"] = new SkinDef
        {
            SkinId = "default", DisplayName = "Classic Fly", Description = "The OG. Nothing fancy.",
            ColorHex = "#888888", Source = SkinSource.Default, GemCost = 0,
            UnlockCondition = "Already unlocked"
        },
        ["golden"] = new SkinDef
        {
            SkinId = "golden", DisplayName = "Golden Fly", Description = "Dripping in PooGold.",
            ColorHex = "#FFD700", Source = SkinSource.GemShop, GemCost = 100,
            UnlockCondition = "Purchase for 100 Gems"
        },
        ["neon"] = new SkinDef
        {
            SkinId = "neon", DisplayName = "Neon Buzz", Description = "Glow in the dark. Rave fly.",
            ColorHex = "#39FF14", Source = SkinSource.GemShop, GemCost = 75,
            UnlockCondition = "Purchase for 75 Gems"
        },
        ["royal"] = new SkinDef
        {
            SkinId = "royal", DisplayName = "Royal Purple", Description = "Bow before the king fly.",
            ColorHex = "#7B2FBE", Source = SkinSource.GemShop, GemCost = 150,
            UnlockCondition = "Purchase for 150 Gems"
        },
        ["crimson"] = new SkinDef
        {
            SkinId = "crimson", DisplayName = "Crimson Warrior", Description = "Battle-hardened and fierce.",
            ColorHex = "#DC143C", Source = SkinSource.BattleReward, GemCost = 0,
            UnlockCondition = "Win 50 battles"
        },
        ["ice"] = new SkinDef
        {
            SkinId = "ice", DisplayName = "Frost Wing", Description = "Cool as a frozen turd.",
            ColorHex = "#87CEEB", Source = SkinSource.LevelReward, GemCost = 0,
            UnlockCondition = "Reach Clan Level 10"
        },
        ["shadow"] = new SkinDef
        {
            SkinId = "shadow", DisplayName = "Shadow Fly", Description = "Moves unseen. Spooky.",
            ColorHex = "#1A1A2E", Source = SkinSource.GemShop, GemCost = 200,
            UnlockCondition = "Purchase for 200 Gems"
        },
        ["candy"] = new SkinDef
        {
            SkinId = "candy", DisplayName = "Candy Coat", Description = "Sweet and sticky.",
            ColorHex = "#FF69B4", Source = SkinSource.Seasonal, GemCost = 0,
            UnlockCondition = "Seasonal event reward"
        },
    };

    public static SkinDef? Get(string skinId)
        => _skins.TryGetValue(skinId ?? "default", out var def) ? def : null;

    public static IReadOnlyDictionary<string, SkinDef> All => _skins;

    public static List<SkinDef> GetShopSkins()
    {
        var list = new List<SkinDef>();
        foreach (var kv in _skins)
            if (kv.Value.Source == SkinSource.GemShop)
                list.Add(kv.Value);
        return list;
    }
}
```

3. Create `Assets/Scripts/UI/SkinSelectPopup.cs` — runtime-built skin picker popup:
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime-built popup for selecting / purchasing skins for a fly.
/// Launched from ClanScreenController.
/// </summary>
public class SkinSelectPopup : MonoBehaviour
{
    public static SkinSelectPopup Instance { get; private set; }

    Canvas _canvas;
    GameObject _panel;
    string _flyId;
    HashSet<string> _unlockedSkins = new() { "default" };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start() => BuildUI();

    void BuildUI()
    {
        _canvas = new GameObject("SkinSelectCanvas").AddComponent<Canvas>();
        _canvas.transform.SetParent(transform);
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 830;
        _canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvas.gameObject.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("SkinPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        var panelRT = _panel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero; panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = new Vector2(40, 80); panelRT.offsetMax = new Vector2(-40, -80);
        var panelImg = _panel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.15f, 0.97f);

        _panel.SetActive(false);
    }

    public void Show(string flyId)
    {
        _flyId = flyId;
        _panel.SetActive(true);
        RebuildContent();
    }

    public void Hide() => _panel.SetActive(false);

    void RebuildContent()
    {
        // Clear previous children (except panel background)
        foreach (Transform child in _panel.transform)
            Destroy(child.gameObject);

        // Title
        var title = CreateLabel(_panel.transform, "Choose Skin", 28, TextAlignmentOptions.Center);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1); titleRT.anchoredPosition = new Vector2(0, -10);
        titleRT.sizeDelta = new Vector2(0, 40);

        // Scroll area
        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(_panel.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0.1f); scrollRT.anchorMax = new Vector2(1, 0.88f);
        scrollRT.offsetMin = new Vector2(10, 0); scrollRT.offsetMax = new Vector2(-10, 0);
        var scroll = scrollGO.AddComponent<ScrollRect>();
        scrollGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.2f);
        scrollGO.AddComponent<Mask>().showMaskGraphic = true;

        var content = new GameObject("Content");
        content.transform.SetParent(scrollGO.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1); contentRT.sizeDelta = new Vector2(0, 0);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6; vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRT;
        scroll.vertical = true; scroll.horizontal = false;

        // Current fly skin
        var fly = ClanManager.Instance?.Flies;
        string currentSkin = "default";
        if (fly != null)
            foreach (var f in fly)
                if (f.InstanceId == _flyId) { currentSkin = f.SkinId ?? "default"; break; }

        // Skin cards
        foreach (var kv in SkinRegistry.All)
        {
            var skin = kv.Value;
            bool unlocked = _unlockedSkins.Contains(skin.SkinId);
            bool equipped = skin.SkinId == currentSkin;
            CreateSkinCard(content.transform, skin, unlocked, equipped);
        }

        // Close button
        var closeBtn = CreateButton(_panel.transform, "Close", () => Hide());
        var closeBtnRT = closeBtn.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(0.3f, 0); closeBtnRT.anchorMax = new Vector2(0.7f, 0.1f);
        closeBtnRT.offsetMin = Vector2.zero; closeBtnRT.offsetMax = Vector2.zero;
    }

    void CreateSkinCard(Transform parent, SkinRegistry.SkinDef skin, bool unlocked, bool equipped)
    {
        var card = new GameObject(skin.SkinId);
        card.transform.SetParent(parent, false);
        var le = card.AddComponent<LayoutElement>();
        le.preferredHeight = 70;
        var cardImg = card.AddComponent<Image>();

        if (equipped)
            cardImg.color = new Color(0.15f, 0.35f, 0.15f);
        else if (unlocked)
            cardImg.color = new Color(0.2f, 0.2f, 0.25f);
        else
            cardImg.color = new Color(0.15f, 0.15f, 0.18f);

        // Color swatch
        var swatch = new GameObject("Swatch");
        swatch.transform.SetParent(card.transform, false);
        var swatchRT = swatch.AddComponent<RectTransform>();
        swatchRT.anchorMin = new Vector2(0, 0.15f); swatchRT.anchorMax = new Vector2(0, 0.85f);
        swatchRT.pivot = new Vector2(0, 0.5f); swatchRT.anchoredPosition = new Vector2(10, 0);
        swatchRT.sizeDelta = new Vector2(40, 0);
        var swatchImg = swatch.AddComponent<Image>();
        ColorUtility.TryParseHtmlString(skin.ColorHex, out var skinColor);
        swatchImg.color = skinColor;

        // Name + description
        var nameLabel = CreateLabel(card.transform, skin.DisplayName + (equipped ? " ✓" : ""), 18, TextAlignmentOptions.Left);
        var nameRT = nameLabel.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0.5f); nameRT.anchorMax = new Vector2(0.65f, 1);
        nameRT.offsetMin = new Vector2(60, 0); nameRT.offsetMax = Vector2.zero;

        var descLabel = CreateLabel(card.transform, skin.Description, 13, TextAlignmentOptions.Left);
        descLabel.color = new Color(0.7f, 0.7f, 0.7f);
        var descRT = descLabel.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0, 0); descRT.anchorMax = new Vector2(0.65f, 0.5f);
        descRT.offsetMin = new Vector2(60, 0); descRT.offsetMax = Vector2.zero;

        // Action button
        if (equipped)
        {
            var eqLabel = CreateLabel(card.transform, "Equipped", 14, TextAlignmentOptions.Center);
            eqLabel.color = new Color(0.4f, 0.9f, 0.4f);
            var eqRT = eqLabel.GetComponent<RectTransform>();
            eqRT.anchorMin = new Vector2(0.7f, 0.2f); eqRT.anchorMax = new Vector2(0.98f, 0.8f);
            eqRT.offsetMin = Vector2.zero; eqRT.offsetMax = Vector2.zero;
        }
        else if (unlocked)
        {
            string sid = skin.SkinId;
            var btn = CreateButton(card.transform, "Apply", () => ApplySkin(sid));
            var btnRT = btn.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.7f, 0.2f); btnRT.anchorMax = new Vector2(0.98f, 0.8f);
            btnRT.offsetMin = Vector2.zero; btnRT.offsetMax = Vector2.zero;
        }
        else if (skin.Source == SkinRegistry.SkinSource.GemShop && skin.GemCost > 0)
        {
            string sid = skin.SkinId;
            int cost = skin.GemCost;
            var btn = CreateButton(card.transform, $"{cost} 💎", () => BuySkin(sid, cost));
            var btnRT = btn.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.7f, 0.2f); btnRT.anchorMax = new Vector2(0.98f, 0.8f);
            btnRT.offsetMin = Vector2.zero; btnRT.offsetMax = Vector2.zero;
        }
        else
        {
            var lockLabel = CreateLabel(card.transform, "🔒 " + skin.UnlockCondition, 12, TextAlignmentOptions.Center);
            lockLabel.color = new Color(0.6f, 0.6f, 0.6f);
            var lockRT = lockLabel.GetComponent<RectTransform>();
            lockRT.anchorMin = new Vector2(0.65f, 0.1f); lockRT.anchorMax = new Vector2(0.98f, 0.9f);
            lockRT.offsetMin = Vector2.zero; lockRT.offsetMax = Vector2.zero;
        }
    }

    void ApplySkin(string skinId)
    {
        if (ClanManager.Instance == null) return;
        foreach (var f in ClanManager.Instance.Flies)
        {
            if (f.InstanceId == _flyId)
            {
                f.SkinId = skinId;
                Debug.Log($"[SkinSelectPopup] Applied skin '{skinId}' to fly {_flyId}");
                ToastManager.Instance?.ShowToast($"Skin applied: {SkinRegistry.Get(skinId)?.DisplayName}", ToastType.Success);
                break;
            }
        }
        RebuildContent();
        ClanScreenController.Instance?.RefreshFlyList();
    }

    void BuySkin(string skinId, int gemCost)
    {
        if (ResourceManager.Instance == null || !ResourceManager.Instance.SpendGems(gemCost))
        {
            ToastManager.Instance?.ShowToast("Not enough Gems!", ToastType.Warning);
            return;
        }
        _unlockedSkins.Add(skinId);
        Debug.Log($"[SkinSelectPopup] Purchased skin '{skinId}' for {gemCost} Gems");
        ToastManager.Instance?.ShowToast($"Skin unlocked: {SkinRegistry.Get(skinId)?.DisplayName}", ToastType.Reward);
        RebuildContent();
    }

    public void UnlockSkin(string skinId)
    {
        if (!_unlockedSkins.Contains(skinId))
        {
            _unlockedSkins.Add(skinId);
            Debug.Log($"[SkinSelectPopup] Unlocked skin '{skinId}'");
        }
    }

    public bool IsSkinUnlocked(string skinId) => _unlockedSkins.Contains(skinId);

    // ---- Save / Load ----

    [System.Serializable]
    public class SaveData
    {
        public List<string> UnlockedSkins;
    }

    public SaveData GetSaveData() => new() { UnlockedSkins = new List<string>(_unlockedSkins) };

    public void LoadSaveData(SaveData data)
    {
        if (data?.UnlockedSkins == null) return;
        _unlockedSkins = new HashSet<string>(data.UnlockedSkins);
        if (!_unlockedSkins.Contains("default")) _unlockedSkins.Add("default");
    }

    // ---- UI Helpers ----

    static TextMeshProUGUI CreateLabel(Transform parent, string text, int fontSize, TextAlignmentOptions align)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.alignment = align;
        tmp.color = Color.white; tmp.enableAutoSizing = false;
        return tmp;
    }

    static GameObject CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.45f, 0.7f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        var tmp = new GameObject("Label").AddComponent<TextMeshProUGUI>();
        tmp.transform.SetParent(go.transform, false);
        tmp.text = label; tmp.fontSize = 14; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var tmpRT = tmp.GetComponent<RectTransform>();
        tmpRT.anchorMin = Vector2.zero; tmpRT.anchorMax = Vector2.one;
        tmpRT.offsetMin = Vector2.zero; tmpRT.offsetMax = Vector2.zero;
        return go;
    }
}
```

4. Add `SkinSelectPopup` to `GameManager.AutoBootstrap()`:
```csharp
go.AddComponent<SkinSelectPopup>();
```

5. Add a "Skin" button to each fly card in `ClanScreenController.cs` (next to the Equip/Train buttons), calling `SkinSelectPopup.Instance.Show(fly.InstanceId)`.

6. Add `SkinSelectPopup.SaveData` to `SaveSystem.SaveFile`, and wire `GetSaveData()` / `LoadSaveData()` in `SaveSystem.Save()` / `SaveSystem.Load()`.

7. Refresh → zero errors.

---

### TASK 36 — IAP Gem Packs (Unity Purchasing)

**Goal:** Integrate Unity IAP (`com.unity.purchasing` already in manifest) so players can buy Gem packs with real money. Uses reflection to avoid hard compile dependency — same pattern as NotificationManager.

**Files to create:** `Assets/Scripts/Managers/IAPManager.cs`, `Assets/Scripts/UI/GemShopUI.cs`
**Files to modify:** `Assets/Scripts/Core/GameManager.cs`, `Assets/Scripts/Core/Constants.cs`

**Steps:**

1. Add IAP product constants to `Constants.cs`:
```csharp
// --- IAP Product IDs ---
public const string IAP_GEMS_100  = "com.flylife.gems100";
public const string IAP_GEMS_500  = "com.flylife.gems500";
public const string IAP_GEMS_1200 = "com.flylife.gems1200";
public const string IAP_GEMS_3000 = "com.flylife.gems3000";
```

2. Create `Assets/Scripts/Managers/IAPManager.cs` — uses reflection to interface with Unity Purchasing:
```csharp
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
    static Type _storeListenerType;
    static Type _configBuilderType;
    static Type _purchasingType;
    static Type _productType;
    static Type _productTypeEnumType;

    public bool IsInitialized { get; private set; }
    object _storeController;
    object _extensionProvider;

    // Product ID → gem amount
    static readonly Dictionary<string, int> _products = new()
    {
        [Constants.IAP_GEMS_100]  = 100,
        [Constants.IAP_GEMS_500]  = 500,
        [Constants.IAP_GEMS_1200] = 1200,
        [Constants.IAP_GEMS_3000] = 3000,
    };

    // Product ID → display price (updated after initialization)
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
        _storeListenerType = Type.GetType("UnityEngine.Purchasing.IStoreListener, UnityEngine.Purchasing");
        _packageAvailable = _purchasingType != null && _configBuilderType != null;
    }

    void InitializePurchasing()
    {
        if (!_packageAvailable) return;
        try
        {
            // ConfigurationBuilder.Instance(StandardPurchasingModule.Instance())
            var stdModuleType = Type.GetType("UnityEngine.Purchasing.StandardPurchasingModule, UnityEngine.Purchasing");
            var moduleInstance = stdModuleType?.GetMethod("Instance", Type.EmptyTypes)?.Invoke(null, null);
            var builder = _configBuilderType?.GetMethod("Instance")?.Invoke(null, new[] { moduleInstance });

            // builder.AddProduct(id, ProductType.Consumable)
            var consumable = Enum.ToObject(_productTypeEnumType, 0); // Consumable = 0
            var addMethod = builder?.GetType().GetMethod("AddProduct", new[] { typeof(string), _productTypeEnumType });
            foreach (var productId in _products.Keys)
                addMethod?.Invoke(builder, new[] { (object)productId, consumable });

            Debug.Log("[IAPManager] IAP products configured. Initialization deferred to store callback.");
            IsInitialized = true; // Simplified — real init is async via IStoreListener
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
            // Fallback: grant gems directly in debug/editor mode
#if UNITY_EDITOR
            GrantGems(productId);
#endif
            return;
        }
        // In production, call UnityPurchasing.InitiatePurchase(productId)
        // For now, simulate purchase in editor
#if UNITY_EDITOR
        GrantGems(productId);
#endif
    }

    void GrantGems(string productId)
    {
        if (!_products.TryGetValue(productId, out int gems)) return;
        ResourceManager.Instance?.AddGems(gems);
        Debug.Log($"[IAPManager] Granted {gems} gems for product {productId}");
        ToastManager.Instance?.ShowToast($"+{gems} Gems purchased!", ToastType.Reward);
    }

    public string GetPrice(string productId)
        => _prices.TryGetValue(productId, out var p) ? p : "???";

    public int GetGemAmount(string productId)
        => _products.TryGetValue(productId, out var g) ? g : 0;
}
```

3. Create `Assets/Scripts/UI/GemShopUI.cs` — runtime-built Gem shop overlay:
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime-built Gem shop overlay. Shows IAP packs with prices.
/// Accessed via a gem icon button in the HUD or Settings.
/// </summary>
public class GemShopUI : MonoBehaviour
{
    public static GemShopUI Instance { get; private set; }

    Canvas _canvas;
    GameObject _panel;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start() => BuildUI();

    void BuildUI()
    {
        _canvas = new GameObject("GemShopCanvas").AddComponent<Canvas>();
        _canvas.transform.SetParent(transform);
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 860;
        _canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvas.gameObject.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("ShopPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        var rt = _panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(30, 100); rt.offsetMax = new Vector2(-30, -60);
        _panel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.97f);

        _panel.SetActive(false);
    }

    public void Show()
    {
        _panel.SetActive(true);
        RebuildContent();
    }

    public void Hide() => _panel.SetActive(false);
    public void Toggle() { if (_panel.activeSelf) Hide(); else Show(); }

    void RebuildContent()
    {
        foreach (Transform child in _panel.transform)
            Destroy(child.gameObject);

        // Title
        var title = CreateLabel(_panel.transform, "💎 Gem Shop", 28, TextAlignmentOptions.Center);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1); titleRT.anchoredPosition = new Vector2(0, -10);
        titleRT.sizeDelta = new Vector2(0, 45);

        // Current balance
        int gems = ResourceManager.Instance?.Gems ?? 0;
        var balance = CreateLabel(_panel.transform, $"Balance: {gems} Gems", 18, TextAlignmentOptions.Center);
        balance.color = new Color(0.6f, 0.85f, 1f);
        var balRT = balance.GetComponent<RectTransform>();
        balRT.anchorMin = new Vector2(0, 1); balRT.anchorMax = new Vector2(1, 1);
        balRT.pivot = new Vector2(0.5f, 1); balRT.anchoredPosition = new Vector2(0, -55);
        balRT.sizeDelta = new Vector2(0, 30);

        // Product cards
        float yOffset = -100;
        string[] productIds = {
            Constants.IAP_GEMS_100, Constants.IAP_GEMS_500,
            Constants.IAP_GEMS_1200, Constants.IAP_GEMS_3000
        };
        string[] bonusLabels = { "", "", "+20% bonus!", "+50% bonus!" };

        for (int i = 0; i < productIds.Length; i++)
        {
            string pid = productIds[i];
            int gemAmt = IAPManager.Instance?.GetGemAmount(pid) ?? 0;
            string price = IAPManager.Instance?.GetPrice(pid) ?? "???";
            CreateProductCard(_panel.transform, pid, gemAmt, price, bonusLabels[i], yOffset);
            yOffset -= 80;
        }

        // Close button
        var closeBtn = CreateButton(_panel.transform, "Close", () => Hide());
        var closeBtnRT = closeBtn.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(0.3f, 0); closeBtnRT.anchorMax = new Vector2(0.7f, 0.08f);
        closeBtnRT.offsetMin = Vector2.zero; closeBtnRT.offsetMax = Vector2.zero;
    }

    void CreateProductCard(Transform parent, string productId, int gems, string price, string bonus, float yPos)
    {
        var card = new GameObject("Product_" + gems);
        card.transform.SetParent(parent, false);
        var cardRT = card.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.05f, 1); cardRT.anchorMax = new Vector2(0.95f, 1);
        cardRT.pivot = new Vector2(0.5f, 1); cardRT.anchoredPosition = new Vector2(0, yPos);
        cardRT.sizeDelta = new Vector2(0, 70);
        card.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f);

        // Gem amount
        string label = $"💎 {gems} Gems";
        if (!string.IsNullOrEmpty(bonus)) label += $"  <color=#FFD700><size=14>{bonus}</size></color>";
        var gemLabel = CreateLabel(card.transform, label, 20, TextAlignmentOptions.Left);
        gemLabel.richText = true;
        var gemRT = gemLabel.GetComponent<RectTransform>();
        gemRT.anchorMin = new Vector2(0, 0); gemRT.anchorMax = new Vector2(0.65f, 1);
        gemRT.offsetMin = new Vector2(15, 0); gemRT.offsetMax = Vector2.zero;

        // Buy button
        string pid = productId;
        var btn = CreateButton(card.transform, price, () => IAPManager.Instance?.PurchaseProduct(pid));
        var btnRT = btn.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.68f, 0.15f); btnRT.anchorMax = new Vector2(0.95f, 0.85f);
        btnRT.offsetMin = Vector2.zero; btnRT.offsetMax = Vector2.zero;
        btn.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f);
    }

    // ---- UI Helpers (same pattern as other UI classes) ----

    static TextMeshProUGUI CreateLabel(Transform parent, string text, int size, TextAlignmentOptions align)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.alignment = align;
        tmp.color = Color.white;
        return tmp;
    }

    static GameObject CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.45f, 0.7f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        var tmp = new GameObject("Label").AddComponent<TextMeshProUGUI>();
        tmp.transform.SetParent(go.transform, false);
        tmp.text = label; tmp.fontSize = 16; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var tmpRT = tmp.GetComponent<RectTransform>();
        tmpRT.anchorMin = Vector2.zero; tmpRT.anchorMax = Vector2.one;
        tmpRT.offsetMin = Vector2.zero; tmpRT.offsetMax = Vector2.zero;
        return go;
    }
}
```

4. Add both to `GameManager.AutoBootstrap()`:
```csharp
go.AddComponent<IAPManager>();
go.AddComponent<GemShopUI>();
```

5. Add a Gem Shop button (💎) next to the settings gear in the HUD, calling `GemShopUI.Instance.Toggle()`. Add this in `SettingsScreenController.cs` Start() or as a separate HUD button.

6. Refresh → zero errors.

---

### TASK 37 — Expanded Map (7×7 Grid)

**Goal:** Expand the city map from 5×5 to 7×7 (49 districts). Adds a new Ring 3 (outer edge) with difficulty 7. New theme types: `Hospital` and `Airport`. Camera bounds updated.

**Files to modify:** `Assets/Scripts/Map/TerritoryManager.cs`, `Assets/Scripts/Data/DistrictThemeData.cs`, `Assets/Scripts/Map/MapCameraController.cs`, `Assets/Scripts/Core/Constants.cs`

**Steps:**

1. Add two new theme types to `DistrictThemeType` enum in `DistrictThemeData.cs`:
```csharp
public enum DistrictThemeType
{
    Residential, DogPark, BackAlley, Sewer, Playground,
    TownSquare, Market, Construction, University, Waterfront,
    Hospital,   // new
    Airport     // new
}
```

2. Add theme data entries for `Hospital` and `Airport` in `DistrictThemeData.BuildRegistry()`:
```csharp
[DistrictThemeType.Hospital] = new()
{
    Type = DistrictThemeType.Hospital,
    ThemeName = "Hospital",
    Icon = "[H]",
    ThemeColor = new Color(0.85f, 0.85f, 0.9f),
    PooSpawnRateMultiplier = 0.6f,
    PooQualityMultiplier = 1.3f,
    ResourceBonusMultiplier = 1.1f,
    XPBonusMultiplier = 1.0f,
    Description = "Clean but busy. +30% poo quality."
},
[DistrictThemeType.Airport] = new()
{
    Type = DistrictThemeType.Airport,
    ThemeName = "Airport",
    Icon = "[✈]",
    ThemeColor = new Color(0.5f, 0.6f, 0.8f),
    PooSpawnRateMultiplier = 1.4f,
    PooQualityMultiplier = 0.8f,
    ResourceBonusMultiplier = 1.2f,
    XPBonusMultiplier = 1.0f,
    Description = "International poo. +40% rate, +20% income."
},
```

3. Update `TerritoryManager` — change `GridWidth` and `GridHeight` from 5 to 7, replace `CityLayout`:
```csharp
public int GridWidth  = 7;
public int GridHeight = 7;

// City layout: themed 7x7 grid (y=0 is bottom row)
static readonly DistrictThemeType[,] CityLayout =
{
    // x=0              x=1              x=2              x=3              x=4              x=5              x=6
    { DistrictThemeType.Airport,       DistrictThemeType.Market,       DistrictThemeType.Residential,  DistrictThemeType.TownSquare,    DistrictThemeType.University,    DistrictThemeType.Waterfront,   DistrictThemeType.Airport      }, // y=0
    { DistrictThemeType.Hospital,      DistrictThemeType.Market,       DistrictThemeType.Residential,  DistrictThemeType.TownSquare,    DistrictThemeType.University,    DistrictThemeType.Waterfront,   DistrictThemeType.Hospital     }, // y=1
    { DistrictThemeType.Residential,   DistrictThemeType.Residential,  DistrictThemeType.BackAlley,    DistrictThemeType.Playground,    DistrictThemeType.Construction,  DistrictThemeType.Residential,  DistrictThemeType.Construction }, // y=2
    { DistrictThemeType.Waterfront,    DistrictThemeType.Construction, DistrictThemeType.DogPark,      DistrictThemeType.TownSquare,    DistrictThemeType.DogPark,       DistrictThemeType.BackAlley,    DistrictThemeType.Sewer        }, // y=3
    { DistrictThemeType.Construction,  DistrictThemeType.Residential,  DistrictThemeType.Playground,   DistrictThemeType.DogPark,       DistrictThemeType.Sewer,         DistrictThemeType.Residential,  DistrictThemeType.Residential  }, // y=4
    { DistrictThemeType.Hospital,      DistrictThemeType.Waterfront,   DistrictThemeType.University,   DistrictThemeType.TownSquare,    DistrictThemeType.Residential,   DistrictThemeType.Market,       DistrictThemeType.Hospital     }, // y=5
    { DistrictThemeType.Airport,       DistrictThemeType.Residential,  DistrictThemeType.Market,       DistrictThemeType.Playground,    DistrictThemeType.University,    DistrictThemeType.Residential,  DistrictThemeType.Airport      }, // y=6
};
```

4. Update `GenerateDistricts()` center reference from `(2,2)` to `(3,3)` and add Ring 3:
```csharp
int ring = Mathf.Max(Mathf.Abs(x - 3), Mathf.Abs(y - 3)); // Chebyshev from new center (3,3)
bool isBoss = (x == 0 || x == GridWidth - 1) && (y == 0 || y == GridHeight - 1);
int difficulty = isBoss ? 8 : (ring == 0 ? 1 : ring == 1 ? 3 : ring == 2 ? 5 : 7);
```

5. Update `Start()` center district from `"district_2_2"` to `"district_3_3"`.

6. Update `MapCameraController` bounds for 7×7 grid:
```csharp
public Vector2 MinBounds = new(-2f, -2f);
public Vector2 MaxBounds = new(8f, 8f);   // was (6,6) for 5x5
public float MaxZoom = 10f;               // was 8 — need more zoom-out for bigger map
```

7. Add a new max-level constant if desired:
```csharp
public const int MAX_DISTRICT_DIFFICULTY = 8;
```

8. Refresh → zero errors. Note: existing saves with `district_2_2` ownership will need migration — the center is now `district_3_3`. Consider checking both in `Start()` for backward compatibility.

---

### TASK 38 — Legendary Gear Tier

**Goal:** Add `Legendary` rarity to the equipment system with 4 powerful new items. Legendary items have unique effects and higher stats. Drops only from Boss districts.

**Files to modify:** `Assets/Scripts/Data/EquipmentData.cs`, `Assets/Scripts/Data/EquipmentRegistry.cs`, `Assets/Scripts/Battle/BattleManager.cs`

**Steps:**

1. Add `Legendary` to `EquipmentRarity` enum in `EquipmentData.cs`:
```csharp
public enum EquipmentRarity { Common, Uncommon, Rare, Epic, Legendary }
```

2. Add new `SpecialEffect` values for legendary items:
```csharp
public enum SpecialEffect
{
    None,
    AOESplash, ReflectDamage, BlockFirstHit, ActFirst,
    ReduceAccuracy, StunEnemy, FastItemGrab,
    // Legendary effects:
    LifeSteal,      // heal % of damage dealt
    DoubleStrike,   // chance to attack twice
    Invincible,     // survive one lethal hit at 1 HP
    RallyAllies     // boost all allies' ATK by %
}
```

3. Add 4 Legendary items to `EquipmentRegistry._items`:
```csharp
["GoldenDungScepter"] = new EquipmentDef
{
    ItemName = "Golden Dung Scepter", Slot = EquipmentSlot.Weapon,
    Rarity = EquipmentRarity.Legendary,
    AttackBonus = 20, SpeedBonus = 5,
    Description = "The holy grail of poo weapons. Heals 25% of damage dealt.",
    Effect = SpecialEffect.LifeSteal, EffectValue = 0.25f
},
["DiamondWings"] = new EquipmentDef
{
    ItemName = "Diamond Wings", Slot = EquipmentSlot.Wings,
    Rarity = EquipmentRarity.Legendary,
    SpeedBonus = 15, DefenseBonus = 8,
    Description = "Impossibly fast. 30% chance to strike twice.",
    Effect = SpecialEffect.DoubleStrike, EffectValue = 0.3f
},
["CrownOfTheSwarm"] = new EquipmentDef
{
    ItemName = "Crown of the Swarm", Slot = EquipmentSlot.Head,
    Rarity = EquipmentRarity.Legendary,
    DefenseBonus = 12, HealthBonus = 30,
    Description = "The swarm obeys. All allies gain +20% ATK.",
    Effect = SpecialEffect.RallyAllies, EffectValue = 0.2f
},
["PhoenixCarapace"] = new EquipmentDef
{
    ItemName = "Phoenix Carapace", Slot = EquipmentSlot.Body,
    Rarity = EquipmentRarity.Legendary,
    DefenseBonus = 18, HealthBonus = 20,
    Description = "Survive one lethal blow. Refuse to die.",
    Effect = SpecialEffect.Invincible, EffectValue = 1f
},
```

4. Add a helper to `EquipmentRegistry`:
```csharp
/// <summary>Returns only items of Legendary rarity.</summary>
public static List<string> GetLegendaryItems()
{
    var list = new List<string>();
    foreach (var kv in _items)
        if (kv.Value.Rarity == EquipmentRarity.Legendary)
            list.Add(kv.Key);
    return list;
}

/// <summary>Returns items droppable in a Boss district.</summary>
public static List<string> GetBossDrops()
    => GetLegendaryItems(); // Boss districts drop legendary items
```

5. In `BattleManager` (item drop logic), modify the drop selection to use boss drops when the battle district is a boss district:
```csharp
// In the item drop section after battle win:
bool isBoss = false;
if (TerritoryManager.Instance?.GetDistrict(_currentDistrictId) is District d)
    isBoss = d.IsBoss;

var dropPool = isBoss
    ? EquipmentRegistry.GetBossDrops()
    : EquipmentRegistry.GetDroppableItems();

if (dropPool.Count > 0)
{
    string droppedItem = dropPool[Random.Range(0, dropPool.Count)];
    // ... existing drop logic ...
}
```

6. Implement legendary effects in `BattleManager.ExecuteAttack()`:
   - `LifeSteal`: after dealing damage, heal attacker by `EffectValue * damageDealt`
   - `DoubleStrike`: `Random.value < EffectValue` → attack again
   - `Invincible`: if target would die and has this effect and hasn't triggered it yet, set HP to 1 instead
   - `RallyAllies`: at battle start, add `EffectValue * baseATK` to all allies' attack

7. Update `GearEquipPopup` to show Legendary items in gold text color.

8. Refresh → zero errors.

---

### TASK 39 — Fly Promotion System (Elite Tier)

**Goal:** Flies that reach level 10 can be "promoted" to Elite status. Elite flies get a stat multiplier, a visual badge, and can equip a 7th gear slot (Accessory). Costs PooCoins.

**Files to modify:** `Assets/Scripts/Data/FlyData.cs`, `Assets/Scripts/Data/EquipmentData.cs`, `Assets/Scripts/Managers/ClanManager.cs`, `Assets/Scripts/Managers/InventoryManager.cs`, `Assets/Scripts/UI/ClanScreenController.cs`, `Assets/Scripts/Core/Constants.cs`

**Steps:**

1. Add promotion fields to `FlyInstance` in `FlyData.cs`:
```csharp
public bool   IsElite;                    // promoted
public string EquippedAccessoryId;        // 7th slot (elite only)
```

2. Add `Accessory` to `EquipmentSlot` enum:
```csharp
public enum EquipmentSlot { Weapon, Head, Body, Feet, Wings, Offhand, Accessory }
```

3. Add promotion constants to `Constants.cs`:
```csharp
// --- Promotion ---
public const int    PROMOTION_LEVEL_REQ  = 10;
public const int    PROMOTION_COST       = 500;   // PooCoins
public const float  ELITE_STAT_MULTIPLIER = 1.25f; // 25% stat boost
```

4. Add `PromoteFly()` to `ClanManager`:
```csharp
public bool PromoteFly(string flyId)
{
    var fly = _flies.FirstOrDefault(f => f.InstanceId == flyId);
    if (fly == null || fly.IsElite || fly.Level < Constants.PROMOTION_LEVEL_REQ)
        return false;
    if (ResourceManager.Instance == null ||
        !ResourceManager.Instance.SpendPooCoins(Constants.PROMOTION_COST))
        return false;

    fly.IsElite = true;
    fly.Attack    = Mathf.RoundToInt(fly.Attack  * Constants.ELITE_STAT_MULTIPLIER);
    fly.Defense   = Mathf.RoundToInt(fly.Defense * Constants.ELITE_STAT_MULTIPLIER);
    fly.Speed     = Mathf.RoundToInt(fly.Speed   * Constants.ELITE_STAT_MULTIPLIER);
    fly.MaxHealth = Mathf.RoundToInt(fly.MaxHealth * Constants.ELITE_STAT_MULTIPLIER);
    fly.CurrentHealth = fly.MaxHealth;

    Debug.Log($"[ClanManager] Fly {flyId} promoted to ELITE! Stats boosted by {(Constants.ELITE_STAT_MULTIPLIER - 1f) * 100}%");
    ToastManager.Instance?.ShowToast($"{fly.Role} promoted to Elite!", ToastType.Success);
    UIEventBus.Emit(UIEvent.ClanUpdated);
    return true;
}
```

5. Update `InventoryManager.SetFlySlot()` to handle the new Accessory slot:
```csharp
case EquipmentSlot.Accessory: fly.EquippedAccessoryId = itemId; break;
```

6. Update `InventoryManager.EquipItem()` to check elite status for Accessory slot:
```csharp
// After the null checks:
if (data.Slot == EquipmentSlot.Accessory && !fly.IsElite)
{
    Debug.LogWarning("[Inventory] Only Elite flies can equip accessories.");
    return false;
}
```

7. Add 2 Accessory items to `EquipmentRegistry`:
```csharp
["LuckyCharm"] = new EquipmentDef
{
    ItemName = "Lucky Charm", Slot = EquipmentSlot.Accessory,
    Rarity = EquipmentRarity.Rare,
    AttackBonus = 3, DefenseBonus = 3, SpeedBonus = 3, HealthBonus = 10,
    Description = "A little bit of everything. Elite flies only.",
    Effect = SpecialEffect.None, EffectValue = 0f
},
["VeteranBadge"] = new EquipmentDef
{
    ItemName = "Veteran Badge", Slot = EquipmentSlot.Accessory,
    Rarity = EquipmentRarity.Epic,
    AttackBonus = 8, HealthBonus = 20,
    Description = "Battle-tested. +20% XP gain. Elite flies only.",
    Effect = SpecialEffect.FastItemGrab, EffectValue = 1.2f
},
```

8. Update `ClanScreenController` fly cards: show "⭐ ELITE" badge, and if fly is level 10+ and not elite, show a "Promote" button (costs `PROMOTION_COST`).

9. Refresh → zero errors.

---

### TASK 40 — Seasonal Events Framework

**Goal:** Create a framework for time-limited seasonal events. Each event has start/end dates, bonus modifiers, and a themed reward skin. First event: "Poo Year" (runs whenever date matches).

**Files to create:** `Assets/Scripts/Data/SeasonalEventRegistry.cs`, `Assets/Scripts/Managers/SeasonalEventManager.cs`
**Files to modify:** `Assets/Scripts/Core/GameManager.cs`, `Assets/Scripts/Managers/ResourceManager.cs`, `Assets/Scripts/Utils/UIEventBus.cs`

**Steps:**

1. Add new UIEvent:
```csharp
// In UIEvent enum:
SeasonalEventStarted,
SeasonalEventEnded
```

2. Create `Assets/Scripts/Data/SeasonalEventRegistry.cs`:
```csharp
using System;
using System.Collections.Generic;

/// <summary>
/// Defines seasonal events with dates, bonuses, and rewards.
/// </summary>
public static class SeasonalEventRegistry
{
    public struct EventDef
    {
        public string EventId;
        public string DisplayName;
        public string Description;
        public int StartMonth, StartDay;      // recurring annually
        public int DurationDays;
        public float PooRateMultiplier;       // applied globally during event
        public float XPMultiplier;
        public float CoinMultiplier;
        public string RewardSkinId;           // unlocked for participating
        public string BannerColorHex;
    }

    static readonly List<EventDef> _events = new()
    {
        new EventDef
        {
            EventId = "poo_year", DisplayName = "Poo Year Festival",
            Description = "Ring in the Poo Year! Double poo rate and +50% coins for 3 days.",
            StartMonth = 1, StartDay = 1, DurationDays = 3,
            PooRateMultiplier = 2.0f, XPMultiplier = 1.0f, CoinMultiplier = 1.5f,
            RewardSkinId = "candy", BannerColorHex = "#FFD700"
        },
        new EventDef
        {
            EventId = "summer_swarm", DisplayName = "Summer Swarm",
            Description = "Summer heat means more flies! +50% XP and double hatch speed.",
            StartMonth = 6, StartDay = 21, DurationDays = 7,
            PooRateMultiplier = 1.0f, XPMultiplier = 1.5f, CoinMultiplier = 1.0f,
            RewardSkinId = "neon", BannerColorHex = "#39FF14"
        },
        new EventDef
        {
            EventId = "spooky_swarm", DisplayName = "Spooky Swarm",
            Description = "Haunted poos and ghostly flies. +75% XP from battles.",
            StartMonth = 10, StartDay = 28, DurationDays = 5,
            PooRateMultiplier = 1.3f, XPMultiplier = 1.75f, CoinMultiplier = 1.0f,
            RewardSkinId = "shadow", BannerColorHex = "#1A1A2E"
        },
    };

    /// <summary>Returns the currently active event, or null if none.</summary>
    public static EventDef? GetActiveEvent()
    {
        var now = DateTime.Now;
        foreach (var e in _events)
        {
            var start = new DateTime(now.Year, e.StartMonth, e.StartDay);
            var end = start.AddDays(e.DurationDays);
            if (now >= start && now < end) return e;
        }
        return null;
    }

    public static IReadOnlyList<EventDef> AllEvents => _events;
}
```

3. Create `Assets/Scripts/Managers/SeasonalEventManager.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Checks for active seasonal events on startup.
/// Displays a banner and applies global multipliers during events.
/// Awards the event skin on first login during an event.
/// </summary>
public class SeasonalEventManager : MonoBehaviour
{
    public static SeasonalEventManager Instance { get; private set; }

    Canvas _canvas;
    GameObject _banner;
    TextMeshProUGUI _bannerText;

    public SeasonalEventRegistry.EventDef? ActiveEvent { get; private set; }
    public bool HasActiveEvent => ActiveEvent.HasValue;

    // Multipliers other systems can query
    public float PooRateMultiplier  => ActiveEvent?.PooRateMultiplier  ?? 1f;
    public float XPMultiplier       => ActiveEvent?.XPMultiplier       ?? 1f;
    public float CoinMultiplier     => ActiveEvent?.CoinMultiplier     ?? 1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        BuildBanner();
        CheckForEvent();
    }

    void CheckForEvent()
    {
        ActiveEvent = SeasonalEventRegistry.GetActiveEvent();
        if (ActiveEvent.HasValue)
        {
            var e = ActiveEvent.Value;
            Debug.Log($"[SeasonalEventManager] Active event: {e.DisplayName}");
            ShowBanner(e);
            UIEventBus.Emit(UIEvent.SeasonalEventStarted, e);

            // Award skin if not already unlocked
            if (SkinSelectPopup.Instance != null && !SkinSelectPopup.Instance.IsSkinUnlocked(e.RewardSkinId))
            {
                SkinSelectPopup.Instance.UnlockSkin(e.RewardSkinId);
                ToastManager.Instance?.ShowToast(
                    $"Event reward: {SkinRegistry.Get(e.RewardSkinId)?.DisplayName} skin unlocked!",
                    ToastType.Reward);
            }
        }
        else
        {
            _banner.SetActive(false);
        }
    }

    void BuildBanner()
    {
        _canvas = new GameObject("EventBannerCanvas").AddComponent<Canvas>();
        _canvas.transform.SetParent(transform);
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 750;
        _canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvas.gameObject.AddComponent<GraphicRaycaster>();

        _banner = new GameObject("Banner");
        _banner.transform.SetParent(_canvas.transform, false);
        var rt = _banner.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1); rt.anchoredPosition = new Vector2(0, -45);
        rt.sizeDelta = new Vector2(0, 35);
        _banner.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.1f, 0.9f);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(_banner.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(10, 0); textRT.offsetMax = new Vector2(-10, 0);
        _bannerText = textGO.AddComponent<TextMeshProUGUI>();
        _bannerText.fontSize = 16; _bannerText.alignment = TextAlignmentOptions.Center;
        _bannerText.color = Color.white;

        _banner.SetActive(false);
    }

    void ShowBanner(SeasonalEventRegistry.EventDef e)
    {
        if (ColorUtility.TryParseHtmlString(e.BannerColorHex, out var color))
            _banner.GetComponent<Image>().color = new Color(color.r, color.g, color.b, 0.9f);
        _bannerText.text = $"🎉 {e.DisplayName} — {e.Description}";
        _banner.SetActive(true);
    }
}
```

4. Add `SeasonalEventManager` to `GameManager.AutoBootstrap()`:
```csharp
go.AddComponent<SeasonalEventManager>();
```

5. Wire multipliers into `ResourceManager.AddPooCoins()`:
```csharp
float seasonalBonus = SeasonalEventManager.Instance?.CoinMultiplier ?? 1f;
int bonus = Mathf.RoundToInt(amount * (BonusManager.Instance?.ResourceMultiplier ?? 1f) * seasonalBonus);
```

6. Wire XP multiplier into `ClanManager.AddXP()`:
```csharp
float seasonalXP = SeasonalEventManager.Instance?.XPMultiplier ?? 1f;
int bonus = Mathf.RoundToInt(amount * (BonusManager.Instance?.XPMultiplier ?? 1f) * seasonalXP);
```

7. Refresh → zero errors.

---

### TASK 41 — Leaderboard (Local + Cloud)

**Goal:** Track player scores (total districts owned + clan level + total flies) and display a leaderboard. Uses CloudSyncManager to sync scores. Falls back to local-only if cloud is unavailable.

**Files to create:** `Assets/Scripts/Managers/LeaderboardManager.cs`, `Assets/Scripts/UI/LeaderboardUI.cs`
**Files to modify:** `Assets/Scripts/Core/GameManager.cs`, `Assets/Scripts/Utils/UIEventBus.cs`

**Steps:**

1. Add UIEvent:
```csharp
LeaderboardUpdated
```

2. Create `Assets/Scripts/Managers/LeaderboardManager.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Calculates and stores player score.
/// In a real game this would sync with a server — here we track local + simulated rivals.
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    [System.Serializable]
    public class LeaderboardEntry
    {
        public string PlayerName;
        public string ClanName;
        public int Score;
        public int Districts;
        public int ClanLevel;
        public int TotalFlies;
        public bool IsPlayer;
    }

    List<LeaderboardEntry> _entries = new();
    public IReadOnlyList<LeaderboardEntry> Entries => _entries;
    public int PlayerScore { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        GenerateRivals();
        InvokeRepeating(nameof(RefreshScore), 2f, 30f); // refresh every 30s
    }

    public void RefreshScore()
    {
        int districts = TerritoryManager.Instance?.GetPlayerDistricts().Count ?? 0;
        int clanLevel = ClanManager.Instance?.ClanLevel ?? 1;
        int totalFlies = ClanManager.Instance?.TotalFlies ?? 0;

        PlayerScore = (districts * 100) + (clanLevel * 50) + (totalFlies * 10);

        // Update player entry
        var playerEntry = _entries.FirstOrDefault(e => e.IsPlayer);
        if (playerEntry != null)
        {
            playerEntry.Score = PlayerScore;
            playerEntry.Districts = districts;
            playerEntry.ClanLevel = clanLevel;
            playerEntry.TotalFlies = totalFlies;
            playerEntry.ClanName = ClanManager.Instance?.ClanName ?? "The Fly Lifers";
        }

        // Sort by score descending
        _entries.Sort((a, b) => b.Score.CompareTo(a.Score));
        UIEventBus.Emit(UIEvent.LeaderboardUpdated);
    }

    void GenerateRivals()
    {
        // Player entry
        _entries.Add(new LeaderboardEntry
        {
            PlayerName = "You",
            ClanName = ClanManager.Instance?.ClanName ?? "The Fly Lifers",
            Score = 0, Districts = 0, ClanLevel = 1, TotalFlies = 1,
            IsPlayer = true
        });

        // Simulated rival clans
        string[] names = {
            "Lord Buzzkill", "Queen Maggot", "Sir Poopsalot", "The Dung Beetles",
            "Swarm Master", "Captain Compost", "Baron Von Stink", "Lady Larva",
            "Professor Poo", "General Gnat"
        };
        string[] clans = {
            "Dung Dynasty", "Maggot Militia", "Poo Pirates", "Beetle Brigade",
            "Swarm Squad", "Compost Crew", "Stink Syndicate", "Larva Legion",
            "Poo Professors", "Gnat Gang"
        };
        var rng = new System.Random(42); // deterministic for consistency
        for (int i = 0; i < names.Length; i++)
        {
            int districts = rng.Next(0, 6);
            int level = rng.Next(1, 12);
            int flies = rng.Next(1, 30);
            _entries.Add(new LeaderboardEntry
            {
                PlayerName = names[i],
                ClanName = clans[i],
                Score = (districts * 100) + (level * 50) + (flies * 10),
                Districts = districts,
                ClanLevel = level,
                TotalFlies = flies,
                IsPlayer = false
            });
        }

        _entries.Sort((a, b) => b.Score.CompareTo(a.Score));
    }
}
```

3. Create `Assets/Scripts/UI/LeaderboardUI.cs` — runtime-built leaderboard screen:
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime-built leaderboard overlay showing rankings.
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI Instance { get; private set; }

    Canvas _canvas;
    GameObject _panel;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start() => BuildUI();

    void BuildUI()
    {
        _canvas = new GameObject("LeaderboardCanvas").AddComponent<Canvas>();
        _canvas.transform.SetParent(transform);
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 870;
        _canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvas.gameObject.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("LeaderboardPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        var rt = _panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(25, 60); rt.offsetMax = new Vector2(-25, -40);
        _panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.97f);

        _panel.SetActive(false);
    }

    public void Show()
    {
        LeaderboardManager.Instance?.RefreshScore();
        _panel.SetActive(true);
        RebuildContent();
    }

    public void Hide() => _panel.SetActive(false);
    public void Toggle() { if (_panel.activeSelf) Hide(); else Show(); }

    void RebuildContent()
    {
        foreach (Transform child in _panel.transform)
            Destroy(child.gameObject);

        // Title
        var title = CreateLabel(_panel.transform, "🏆 Leaderboard", 28, TextAlignmentOptions.Center);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1); titleRT.anchoredPosition = new Vector2(0, -10);
        titleRT.sizeDelta = new Vector2(0, 40);

        // Scroll area
        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(_panel.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0.08f); scrollRT.anchorMax = new Vector2(1, 0.9f);
        scrollRT.offsetMin = new Vector2(8, 0); scrollRT.offsetMax = new Vector2(-8, 0);
        var scroll = scrollGO.AddComponent<ScrollRect>();
        scrollGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.15f);
        scrollGO.AddComponent<Mask>().showMaskGraphic = true;

        var content = new GameObject("Content");
        content.transform.SetParent(scrollGO.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4; vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRT;
        scroll.vertical = true; scroll.horizontal = false;

        // Entries
        var entries = LeaderboardManager.Instance?.Entries;
        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
                CreateEntryRow(content.transform, i + 1, entries[i]);
        }

        // Close button
        var closeBtn = CreateButton(_panel.transform, "Close", () => Hide());
        var closeRT = closeBtn.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(0.3f, 0); closeRT.anchorMax = new Vector2(0.7f, 0.07f);
        closeRT.offsetMin = Vector2.zero; closeRT.offsetMax = Vector2.zero;
    }

    void CreateEntryRow(Transform parent, int rank, LeaderboardManager.LeaderboardEntry entry)
    {
        var row = new GameObject($"Rank_{rank}");
        row.transform.SetParent(parent, false);
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 50;
        var rowImg = row.AddComponent<Image>();
        rowImg.color = entry.IsPlayer
            ? new Color(0.15f, 0.3f, 0.15f)
            : (rank <= 3 ? new Color(0.25f, 0.22f, 0.1f) : new Color(0.15f, 0.15f, 0.18f));

        // Rank medal
        string medal = rank == 1 ? "🥇" : rank == 2 ? "🥈" : rank == 3 ? "🥉" : $"#{rank}";
        var rankLabel = CreateLabel(row.transform, medal, 18, TextAlignmentOptions.Center);
        var rankRT = rankLabel.GetComponent<RectTransform>();
        rankRT.anchorMin = new Vector2(0, 0); rankRT.anchorMax = new Vector2(0.12f, 1);
        rankRT.offsetMin = Vector2.zero; rankRT.offsetMax = Vector2.zero;

        // Name + clan
        string nameStr = entry.IsPlayer ? $"<b>{entry.PlayerName}</b>" : entry.PlayerName;
        var nameLabel = CreateLabel(row.transform, $"{nameStr}\n<size=11><color=#AAA>{entry.ClanName}</color></size>", 15, TextAlignmentOptions.Left);
        nameLabel.richText = true;
        var nameRT = nameLabel.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0.13f, 0); nameRT.anchorMax = new Vector2(0.6f, 1);
        nameRT.offsetMin = new Vector2(5, 0); nameRT.offsetMax = Vector2.zero;

        // Score
        var scoreLabel = CreateLabel(row.transform, $"{entry.Score}\n<size=11>pts</size>", 18, TextAlignmentOptions.Center);
        scoreLabel.richText = true;
        if (rank <= 3) scoreLabel.color = new Color(1f, 0.84f, 0f);
        var scoreRT = scoreLabel.GetComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(0.62f, 0); scoreRT.anchorMax = new Vector2(0.82f, 1);
        scoreRT.offsetMin = Vector2.zero; scoreRT.offsetMax = Vector2.zero;

        // Stats mini
        var statsLabel = CreateLabel(row.transform, $"🏠{entry.Districts} 🪰{entry.TotalFlies}", 11, TextAlignmentOptions.Center);
        statsLabel.color = new Color(0.7f, 0.7f, 0.7f);
        var statsRT = statsLabel.GetComponent<RectTransform>();
        statsRT.anchorMin = new Vector2(0.82f, 0); statsRT.anchorMax = new Vector2(1, 1);
        statsRT.offsetMin = Vector2.zero; statsRT.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI CreateLabel(Transform parent, string text, int size, TextAlignmentOptions align)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.alignment = align;
        tmp.color = Color.white;
        return tmp;
    }

    static GameObject CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.45f, 0.7f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        var tmp = new GameObject("Label").AddComponent<TextMeshProUGUI>();
        tmp.transform.SetParent(go.transform, false);
        tmp.text = label; tmp.fontSize = 16; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var tmpRT = tmp.GetComponent<RectTransform>();
        tmpRT.anchorMin = Vector2.zero; tmpRT.anchorMax = Vector2.one;
        tmpRT.offsetMin = Vector2.zero; tmpRT.offsetMax = Vector2.zero;
        return go;
    }
}
```

4. Add both to `GameManager.AutoBootstrap()`:
```csharp
go.AddComponent<LeaderboardManager>();
go.AddComponent<LeaderboardUI>();
```

5. Add a 🏆 button to the HUD (alongside the ⚙ and 💎 buttons) that calls `LeaderboardUI.Instance.Toggle()`.

6. Refresh → zero errors.

---

### TASK 42 — Analytics Integration

**Goal:** Track key game events using Unity Analytics (`com.unity.analytics` already in manifest). Uses reflection to avoid compile errors if the package is removed. Tracks: first_open, battle_won, battle_lost, district_captured, gem_purchase, level_up, tutorial_complete.

**Files to create:** `Assets/Scripts/Managers/AnalyticsManager.cs`
**Files to modify:** `Assets/Scripts/Core/GameManager.cs`

**Steps:**

1. Create `Assets/Scripts/Managers/AnalyticsManager.cs`:
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wraps Unity Analytics via reflection — compiles with or without the package.
/// Subscribes to UIEventBus to automatically fire analytics events.
/// </summary>
public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    static bool _analyticsAvailable;
    static Type _analyticsType;

    // Cached delegates for clean unsubscribe
    Action<object> _onBattleEnded;
    Action<object> _onDistrictCaptured;
    Action<object> _onLevelUp;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        DetectPackage();
    }

    void Start()
    {
        if (!_analyticsAvailable)
        {
            Debug.Log("[AnalyticsManager] Unity Analytics not detected. Analytics disabled.");
            return;
        }

        // Track first open
        TrackEvent("first_open", new Dictionary<string, object>
        {
            ["platform"] = Application.platform.ToString(),
            ["version"] = Application.version
        });

        // Subscribe to events
        _onBattleEnded = data => OnBattleEnded(data);
        _onDistrictCaptured = data => OnDistrictCaptured(data);
        _onLevelUp = data => OnLevelUp(data);

        UIEventBus.On(UIEvent.BattleEnded, _onBattleEnded);
        UIEventBus.On(UIEvent.DistrictCaptured, _onDistrictCaptured);
        UIEventBus.On(UIEvent.LevelUp, _onLevelUp);

        Debug.Log("[AnalyticsManager] Analytics initialized and subscribed to events.");
    }

    void OnDisable()
    {
        if (_onBattleEnded != null)    UIEventBus.Off(UIEvent.BattleEnded, _onBattleEnded);
        if (_onDistrictCaptured != null) UIEventBus.Off(UIEvent.DistrictCaptured, _onDistrictCaptured);
        if (_onLevelUp != null)        UIEventBus.Off(UIEvent.LevelUp, _onLevelUp);
    }

    static void DetectPackage()
    {
        // Try Unity Analytics (Legacy via com.unity.analytics)
        _analyticsType = Type.GetType("UnityEngine.Analytics.Analytics, UnityEngine.UnityAnalyticsModule");
        _analyticsAvailable = _analyticsType != null;
        if (!_analyticsAvailable)
        {
            // Try newer namespace
            _analyticsType = Type.GetType("Unity.Services.Analytics.AnalyticsService, Unity.Services.Analytics");
            _analyticsAvailable = _analyticsType != null;
        }
    }

    /// <summary>Track a custom event with optional parameters.</summary>
    public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        if (!_analyticsAvailable) return;
        try
        {
            // Legacy: Analytics.CustomEvent(eventName, dict)
            var customEvent = _analyticsType.GetMethod("CustomEvent",
                new[] { typeof(string), typeof(Dictionary<string, object>) });
            if (customEvent != null)
            {
                customEvent.Invoke(null, new object[] { eventName, parameters ?? new Dictionary<string, object>() });
                Debug.Log($"[Analytics] Event: {eventName}");
                return;
            }

            // Fallback: just log
            Debug.Log($"[Analytics] (log only) Event: {eventName}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Analytics] Failed to track '{eventName}': {e.Message}");
        }
    }

    // ---- Event handlers ----

    void OnBattleEnded(object data)
    {
        // data might be bool (won) or could be a string/object
        bool won = data is bool b && b;
        TrackEvent(won ? "battle_won" : "battle_lost", new Dictionary<string, object>
        {
            ["clan_level"] = ClanManager.Instance?.ClanLevel ?? 0,
            ["total_flies"] = ClanManager.Instance?.TotalFlies ?? 0
        });
    }

    void OnDistrictCaptured(object data)
    {
        string districtName = data is District d ? d.Name : "unknown";
        TrackEvent("district_captured", new Dictionary<string, object>
        {
            ["district_name"] = districtName,
            ["total_districts"] = TerritoryManager.Instance?.GetPlayerDistricts().Count ?? 0
        });
    }

    void OnLevelUp(object data)
    {
        int level = data is FlyInstance fly ? fly.Level : (ClanManager.Instance?.ClanLevel ?? 0);
        TrackEvent("level_up", new Dictionary<string, object>
        {
            ["new_level"] = level
        });
    }

    /// <summary>Track tutorial completion (call from TutorialManager).</summary>
    public void TrackTutorialComplete()
    {
        TrackEvent("tutorial_complete", new Dictionary<string, object>
        {
            ["time_since_start"] = Time.realtimeSinceStartup
        });
    }

    /// <summary>Track gem purchase (call from IAPManager).</summary>
    public void TrackGemPurchase(string productId, int gemAmount)
    {
        TrackEvent("gem_purchase", new Dictionary<string, object>
        {
            ["product_id"] = productId,
            ["gem_amount"] = gemAmount
        });
    }
}
```

2. Add to `GameManager.AutoBootstrap()`:
```csharp
go.AddComponent<AnalyticsManager>();
```

3. Wire analytics into existing systems:
   - In `TutorialManager` (when tutorial completes): `AnalyticsManager.Instance?.TrackTutorialComplete();`
   - In `IAPManager.GrantGems()`: `AnalyticsManager.Instance?.TrackGemPurchase(productId, gems);`

4. Refresh → zero errors.

---

## Notes for Claude Code

- Always run `.\unity-cmd.ps1 '{"type": "refresh"}' -Timeout 120` after editing any .cs file
- Use `.\unity-cmd.ps1 '{"type": "errors"}'` to check compilation status
- Use scratch pad (`BridgeScratch.cs` + `{"type": "scratch"}`) for complex scene setup
- Save scenes after every unity-cmd batch: `{"type": "save-scene"}`
- When in doubt about scene state: `{"type": "scene"}` or `{"type": "inspect", "path": "...", "lens": "scripts"}`
- Screenshots via `{"type": "screenshot", "delay": 2}` to verify visual output
