# Project Plan

## Overview
**Project:** Poo Fly Territory Game (working title)
**Goal:** A mobile Unity game where players join clans, explore a real-world map divided into districts, spot poo locations, deploy flies, claim territory, and battle enemy clans for district control. Success = playable MVP with map, territory claiming, and basic combat.
**Tech Stack:** Unity (C#), TextMeshPro, Unity UI, likely Firebase or similar backend for clan/multiplayer data

## Architecture
```
Assets/
├── Scripts/
│   ├── UI/
│   │   └── DistrictInfoPanel.cs    — Bottom panel for district tap interaction
│   ├── Map/
│   │   ├── MapManager.cs           — Renders map tiles, refreshes display
│   │   ├── TerritoryManager.cs     — Claims/transfers districts, ownership logic
│   │   └── District.cs             — Data model: Id, Name, OwnerId, PooIds
│   ├── Clan/
│   │   └── ClanManager.cs          — Singleton, holds ClanId, clan membership
│   ├── Battle/
│   │   └── (TODO) BattleManager.cs — Battle scene logic
│   └── Core/
│       └── (TODO) GameManager.cs   — App lifecycle, scene transitions
├── Scenes/
│   ├── Map                         — Main map scene
│   └── Battle                      — (TODO) Battle scene
└── Prefabs/
    └── UI/
        └── DistrictInfoPanel       — Panel prefab
```

## Constraints
- [x] Unity 2022 LTS or newer
- [x] Mobile-first (touch input, portrait orientation)
- [x] Singleton pattern for managers (ClanManager, TerritoryManager, MapManager)
- [x] Null-safe access to singletons (use `?.` operator throughout)
- [x] All UI text uses TextMeshPro, not legacy Text
- [x] No new dependencies without justification
- [x] Keep scripts single-responsibility — one class per file

## Milestones

### M1: Core Data & Map Foundation — Week 1
- Goal: District data model, map rendering, tap-to-select district
- Done when: Player can see map tiles, tap a district, see DistrictInfoPanel with correct info

### M2: Territory & Claiming — Week 2
- Goal: Claim unclaimed districts, ownership persists, visual feedback on map
- Done when: Player can claim neutral districts, map colors update, panel shows correct owner state

### M3: Clan System — Week 3
- Goal: ClanManager with clan creation/joining, ClanId flows into territory ownership
- Done when: Multiple clans can exist, districts show correct clan ownership

### M4: Combat — Week 4
- Goal: Attack enemy districts, basic battle scene, win/lose territory transfer
- Done when: Player can attack enemy district, resolve battle, territory changes hands

### M5: Fly Deployment & Bonuses — Week 5
- Goal: Send flies to owned districts, bonus system (+10% hatch speed, resource bonuses)
- Done when: Deployed flies visible, bonuses applied to owned districts

### M6: Polish & Backend — Week 6+
- Goal: Firebase integration, persistent data, multiplayer clan wars
- Done when: Data persists across sessions, multiple players see same map state

## Decisions Log
| Date | Decision | Reason |
|------|----------|--------|
| 2026-02-27 | Singleton pattern for managers | Simplifies cross-script access in Unity, standard for game managers |
| 2026-02-27 | TextMeshPro for all UI text | Better rendering, emoji support, standard in modern Unity |
| 2026-02-27 | Null-conditional `?.` on all singleton access | Prevents null ref crashes during scene transitions and testing |
| 2026-02-27 | District ownership uses string OwnerId (clan ID) | Flexible for multiplayer — empty string = unclaimed, any string = owning clan |
| 2026-02-27 | Three district states: player/enemy/neutral | Drives all UI and action logic in DistrictInfoPanel |
