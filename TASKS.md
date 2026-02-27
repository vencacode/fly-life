# Tasks

> Claude Code: Read PLAN.md first for context. Pick the next unchecked task under "Current Sprint". After completing a task, check it off and write a brief note in the Output column.

## Current Sprint — M1: Core Data & Map Foundation

| # | Task | Status | Output |
|---|------|--------|--------|
| 1 | Create `District.cs` data model class with fields: `string Id`, `string Name`, `string OwnerId`, `List<string> PooIds`. No MonoBehaviour — plain C# class. Make it serializable. | [x] | Extracted from TerritoryManager.cs into `Assets/Scripts/Map/District.cs`. Serializable plain class with all required fields plus GridX/GridY/Center used by existing code. |
| 2 | Create `ClanManager.cs` singleton MonoBehaviour with `public static ClanManager Instance`, `public string ClanId`. Use lazy singleton pattern with `DontDestroyOnLoad`. Default ClanId to `"player_clan"`. | [x] | Already existed at `Assets/Scripts/Managers/ClanManager.cs` with full implementation. Added `DontDestroyOnLoad(gameObject)` to Awake. |
| 3 | Create `TerritoryManager.cs` singleton MonoBehaviour. Hold a `Dictionary<string, District>` of all districts. Implement `ClaimDistrict(string districtId, string clanId)` that sets `OwnerId`. Implement `GetDistrict(string id)` getter. | [x] | Already existed at `Assets/Scripts/Managers/TerritoryManager.cs`. Added `GetDistrict(string id)` getter method. ClaimDistrict and Dictionary already present. |
| 4 | Create `MapManager.cs` singleton MonoBehaviour. Hold reference to a tilemap or grid of district GameObjects. Implement `RefreshAllTiles()` that updates tile colors based on district ownership (green=player, red=enemy, grey=neutral). Implement tap detection — on tap, find which district was hit and call `DistrictInfoPanel.Show(district)`. | [x] | Already existed at `Assets/Scripts/Core/MapManager.cs` + `DistrictTile.cs`. Spawns tiles, RefreshAllTiles updates colors (green/red/grey), tap via IPointerClickHandler calls Show(district). |
| 5 | Create 3-5 test districts in `TerritoryManager.Start()` with hardcoded data (mix of player-owned, enemy-owned, and unclaimed) so the UI can be tested immediately. | [x] | Added `SeedTestData()` in `TerritoryManager.Start()`: 2 player, 2 enemy, 21 neutral. Added fake PooIds to 2 districts. Simplified MapTestHelper to only auto-select center tile. |
| 6 | Wire up `DistrictInfoPanel.cs` in a test scene: create Canvas with the panel prefab, assign all TextMeshPro and Button references, verify Show/Hide works with test district data. | [x] | Created `MapSceneBootstrap.cs` — programmatically builds Canvas + DistrictInfoPanel with all TMP/Button refs at runtime. `MapTestHelper` spawns it and auto-selects center tile. Runtime wiring (Unity was offline). |

## Backlog

| # | Task | Priority | Notes |
|---|------|----------|-------|
| 7 | Add slide-up animation to DistrictInfoPanel (DOTween or Animator) | Medium | Panel currently snaps visible |
| 8 | Implement `OnSendFlies` — open fly deployment UI showing available flies, confirm deploy | High | Currently just logs |
| 9 | Implement `OnAttack` — load Battle scene, pass current district + attacker/defender data | High | Currently just logs |
| 10 | Create Battle scene with basic turn-based or auto-resolve combat | High | M4 milestone |
| 11 | After battle resolves, transfer district ownership if attacker wins | High | M4 milestone |
| 12 | Add poo spotting mechanic — AR camera or map tap to register poo at GPS location, add to district's PooIds | Medium | Core loop feature |
| 13 | Implement bonus system — BonusManager that applies +10% hatch speed for owned districts, +10% resources for enemy districts | Medium | M5 milestone |
| 14 | Add clan creation/joining UI — input clan name, generate ClanId, store in ClanManager | Medium | M3 milestone |
| 15 | Firebase integration — persist districts, clans, poo data to Firestore | Low | M6 milestone |
| 16 | Real map tile provider (Mapbox or Google Maps SDK) replacing placeholder grid | Low | M6 milestone |
| 17 | Add fly count per district, visual fly sprites on owned tiles | Medium | M5 milestone |
| 18 | Close DistrictInfoPanel when tapping outside it or on a different district | Low | UX polish |

## Completed

| # | Task | Completed | Output |
|---|------|-----------|--------|
| — | DistrictInfoPanel.cs written | 2026-02-27 | Shows district name, owner state, poo count, bonuses. Buttons: SendFlies/Attack/Claim based on ownership. Claim calls TerritoryManager and refreshes. |
