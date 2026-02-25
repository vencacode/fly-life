# Fly Life — Unity Setup Guide

## Step 1: Create the Unity Project

1. Open Unity Hub
2. Click **New Project**
3. Select **2D (URP)** template — NOT "2D Core", use the URP one
4. Name it: `FlyLife`
5. Location: wherever you keep projects
6. Unity version: **6.3 LTS**
7. Click Create

---

## Step 2: Connect the GitHub Repo

1. Once the project opens, close Unity
2. Open terminal / Git Bash in the project folder
3. Run:
   ```
   git init
   git remote add origin https://github.com/vencacode/fly-life.git
   git pull origin main
   ```
4. Copy all files from this repo into your Unity project root (overwrite when asked)
5. Reopen Unity — it will reimport assets

---

## Step 3: Project Settings (do these once)

### Player Settings (Edit > Project Settings > Player)
- **Company Name:** vencacode
- **Product Name:** Fly Life
- **Bundle Identifier:** com.vencacode.flylife
- **Version:** 0.1.0

### iOS
- Target minimum iOS version: **15.0**
- Architecture: ARM64

### Android
- Minimum API Level: **Android 8.0 (API 26)**
- Target API Level: **Android 14 (API 34)**
- Scripting Backend: **IL2CPP**
- Target Architectures: ✅ ARMv7, ✅ ARM64

### Quality Settings (Edit > Project Settings > Quality)
- Delete all quality levels except "Medium" and "High"
- Medium: for low-end Android devices
- High: for iOS and high-end Android

### Graphics (Edit > Project Settings > Graphics)
- Scriptable Render Pipeline Settings: URP Asset (auto-set by template)

---

## Step 4: Install Required Packages (Window > Package Manager)

Install these via **Unity Registry**:
- ✅ **2D Sprite** (built-in, verify it's enabled)
- ✅ **2D Animation** (for Spine-like skeletal animation)
- ✅ **Unity UI** (built-in)
- ✅ **Unity IAP** (search: "In App Purchasing")
- ✅ **Advertisement (LevelPlay)** — in Services menu
- ✅ **TextMeshPro** — install and import TMP Essentials when prompted

---

## Step 5: Create the Scenes

Create these scenes in **Assets/Scenes/**:

1. **Bootstrap.unity** — empty scene, just has the GameManager prefab
2. **MainMap.unity** — main gameplay scene
3. **Battle.unity** — battle minigame scene

Set Build Settings (File > Build Settings):
- Add scenes in this order: Bootstrap (index 0), MainMap (index 1), Battle (index 2)

---

## Step 6: Create the GameManager Prefab

1. In the Bootstrap scene, create an empty GameObject named **GameManager**
2. Add these components:
   - `GameManager.cs`
   - `FlyManager.cs`
   - `ClanManager.cs`
   - `PooManager.cs`
   - `TerritoryManager.cs`
   - `ResourceManager.cs`
   - `BattleManager.cs`
   - `AudioManager.cs`
3. Add an AudioSource child object for music, name it "MusicSource"
4. Wire the AudioSource reference in AudioManager
5. Drag the GameObject to **Assets/Prefabs/** to make it a prefab
6. This prefab will persist across all scenes (DontDestroyOnLoad)

---

## Step 7: Create ScriptableObject Assets

### Poo Types (Assets/ScriptableObjects/Poos/)
Right-click > Create > FlyLife > Poo Type, create one for each:
- DogPoo, BirdPoo, HorsePoo, CatPoo, Compost, BabyDiaper, HumanPoo
- Fill values from the presets in `PooTypeSO.cs` comments

### Equipment (Assets/ScriptableObjects/Equipment/)
Right-click > Create > FlyLife > Equipment Item, create:
- PooThrower, NailHelmet, BottleCapShield, SugarCubeBoots
- RottedCheeseArmor, ToothpickLance, GumWings, AntennaRadar
- Fill values from presets in `EquipmentItemSO.cs` comments

---

## Step 8: Verify Everything Compiles

1. Open Unity and wait for compilation
2. Check the Console (Window > General > Console)
3. There should be **zero errors** at this point
4. If you see errors about missing namespaces, install the missing packages from Step 4

---

## You're Ready for Phase 2 Development!

Tell me when this is done and I'll generate the Map system and UI scripts next.
