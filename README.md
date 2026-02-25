# 🪰 Fly Life

A casual/strategy mobile game where you build and command a clan of flies, claim poo territory across a city, battle rival clans, and evolve your swarm.

**Platform:** iOS + Android  
**Engine:** Unity 6.3 LTS  
**Language:** C#  
**Status:** 🚧 In Development

---

## Concept

Start with a single fly. Find your crew. Discover poos. Lay eggs. Grow your clan. Claim territory. Fight rival fly clans in turn-based battle minigames. Equip your soldiers with hilarious gear. Defend what's yours.

## Core Systems

- **Poo System** — Each poo has Fertility, Consistency, Smell and Leftovers % attributes
- **Clan System** — Three fly roles: Scouts, Soldiers, Workers
- **Territory System** — Claim city districts by controlling 60%+ of poos
- **Battle Minigame** — 5v5 turn-based slot battles with item drops
- **Equipment** — Funny gear: Poo Thrower, Nail Helmet, Bottle Cap Shield, and more
- **Boosters** — Watch ads or pay gems to activate temporary bonuses
- **Async PvP** — Rivals attack your territory while you're offline

## Monetization

Free to play. Boosters available via rewarded ads or IAP gems. No pay-to-win.

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/           # GameManager, GameState, Constants
│   ├── Managers/       # FlyManager, PooManager, ClanManager, TerritoryManager, ResourceManager
│   ├── Battle/         # BattleManager, SlotSystem, ItemDropSystem
│   ├── UI/             # All UI controllers
│   ├── Data/           # ScriptableObject definitions
│   └── Utils/          # Helpers, Extensions, AudioManager, SaveSystem
├── Scenes/             # MainMenu, CityMap, Battle, ClanScreen
├── Prefabs/            # Reusable GameObjects
├── Sprites/            # All 2D art organised by category
├── ScriptableObjects/  # Game data instances
└── Audio/              # SFX and Music
Docs/                   # GDD and design documents
```

## Development Phases

| Phase | Focus | Status |
|-------|-------|--------|
| 1 | Foundation & Repo Setup | ✅ Done |
| 2 | Core Mechanics Prototype | 🔄 Next |
| 3 | UI & Map System | ⏳ Pending |
| 4 | Art & Animation | ⏳ Pending |
| 5 | Monetization | ⏳ Pending |
| 6 | Sound & Juice | ⏳ Pending |
| 7 | Polish & Testing | ⏳ Pending |
| 8 | Launch Prep & Submission | ⏳ Pending |

---

## Setup

1. Clone the repo
2. Open in Unity 6.3 LTS (Unity Hub → Add project from disk)
3. Open `Assets/Scenes/MainMenu.unity`
4. Press Play

## Branch Strategy

- `main` — stable, tested builds only
- `develop` — active development
- `feature/[name]` — one branch per feature

## Contributing

Solo project by [@vencacode](https://github.com/vencacode)
