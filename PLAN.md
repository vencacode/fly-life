# Fly Life — Game Plan

## Vision

A casual RPG for Android where you command a clan of flies taking over a city, one poo at a time.
**Core session (5 min):** Open app → check map → pick a district → fight → level up → close app.

## Core Loop

```
Open app
  → Poos have spawned on map (passive, time-based)
  → Tap a district
  → If unowned: Fight enemy flies to claim it
  → If owned: Collect poo resources, send scouts
  → Win battle → XP → flies level up → stronger flies
  → Stronger flies → unlock harder districts → better gear
  → Gear → win harder battles → more territory
Close app (progress saved)
```

## Player Progression

Flies level up from battles. Higher level flies unlock harder outer-ring districts.
Gear drops from battles — equip to individual flies to improve stats.
District ownership gives passive poo resource income.

## Map Structure

5×5 grid = 25 districts, arranged by difficulty:
- **Ring 0** (center, district_2_2): Tutorial district, very easy
- **Ring 1** (8 surrounding): Easy, level 1-3 flies needed
- **Ring 2** (outer edge, 16): Hard, level 4-6 flies needed

Enemy clans hold Ring 1 and Ring 2 districts at game start.
Player starts at center, expands outward.

## Economy (simplified — 2 currencies)

| Currency | Earned by | Spent on |
|---|---|---|
| Poo Coins | Owning districts, winning battles | Gear upgrades, fly training |
| Gems | Daily login, achievements, boss kills | Boosters, cosmetics, skins |

*TrashResources removed from v1 (too complex for casual players).*

## Battle System

Quick auto-battle, result in ~20 seconds:
- Player selects up to 5 Soldier flies
- Enemy has 5 flies scaled to district difficulty
- Rounds resolve automatically, damage shown as numbers
- Win: district captured, XP awarded, possible gear drop
- Lose: district stays enemy, flies recover to full HP after battle (no permadeath)

Fly roles in battle:
- **Scout**: bonus first-strike chance
- **Soldier**: highest attack/defense stats
- **Worker**: heals allies between rounds

## Monetization

**Deferred to post-playtest.** First playable build ships with no ads and no IAP.
- Gems earned from gameplay only (daily login, achievements, boss kills)
- All Gem-gated content remains functional, just no way to buy Gems yet
- AdMob and Unity IAP stubs exist in code, disabled at runtime via feature flags

*When ready to monetize:*
- Rewarded ads: double XP from next battle, or instant poo coin pack
- IAP: Gem packs (small/medium/large)
- No pay-to-win

## What's Cut from v1

- Egg hatching system (idle mechanic, wrong genre fit — defer to v2)
- Fly lifespan/death (frustrating for casual players — removed)
- TrashResources currency (simplify to Poo Coins + Gems)
- Poo ownership threshold claims (60%/40% logic — replaced by direct battle claims)

---

## Development Phases

### Phase 2 — Playable Core Loop ✅
Goal: one complete session works end-to-end.

1. Bootstrap → MainMap scene flow (GameManager DontDestroyOnLoad)
2. District difficulty + lock system (ring-based, level gating)
3. Battle scene — quick auto-battle, result screen
4. Fly XP + level up after battle
5. Currency simplification (remove TrashResources, wire Poo Coins)
6. Save/load end-to-end

### Phase 3 — RPG Depth ✅
Goal: sessions feel rewarding, player keeps coming back.

7. Gear drop system (1 item drops on battle win, equip to fly)
8. Gear stats applied in battle (AttackBonus, DefenseBonus etc.)
9. Enemy clan AI (districts fight back, re-capture over time)
10. PooData ScriptableObjects (7 real poo types with stats)
11. FlyRoleData ScriptableObjects (Scout/Soldier/Worker with real stats)
12. Poo resource income from owned districts (passive, per minute)
13. ResourceHUD wired to real runtime values

### Phase 4 — Retention & Monetization ✅
Goal: daily active use, first revenue.

14. Daily poo spawns + bonus resource on first open of day
15. Rewarded ad integration (AdMob) — double XP booster
16. Clan screen UI (view flies, their levels, gear, reassign roles)
17. Level-up visual feedback (flash, stat screen)
18. Boss districts (special tiles, unique gear, harder fights)

### Phase 5 — Polish & Ship ✅
Goal: shippable Android build.

19. Sprite art — district tiles (3 themes: slum/midtown/uptown)
20. Sprite art — fly characters (3 roles, visible in battle)
21. Poo marker sprites (replace [POO] text placeholder)
22. TMP emoji atlas (for 💩 emoji in UI)
23. Battle animations (hit flash, death animation)
24. Sound effects (battle hits, level up, district capture)
25. Android build configuration + Google Play submission

### Phase 6 — Game Feel & Onboarding ✅
Goal: new players understand the game in 60 seconds, sessions feel tight.

26. Tutorial flow (guided first battle at center district)
27. Toast notification system (district lost, gear drop, level up — in-game popups)
28. Settings screen (SFX/music volume, reset progress, credits)
29. Battle fly selection screen (pick which 5 flies to send before fight)
30. Gear equip UI in Clan Screen (drag-drop or tap-to-equip)
31. PooCoins spend: fly training (pay coins to boost XP)
32. PooCoins spend: gear upgrade (pay coins to +1 gear stats)
33. Map zoom & pan (pinch-zoom, drag to scroll on larger maps)
34. Push notifications (district under attack, daily login reminder)

### Phase 7 — v2 Content ✅
Goal: second content drop.

35. Cosmetic fly skins (3 skins per role, purchasable with Gems)
36. ~~IAP: Gem packs~~ → deferred, Gems from gameplay only for now
37. Expanded map: 7×7 grid (49 districts, Ring 3 = level 7-9)
38. New gear tier: Legendary items (boss-only drops, unique abilities)
39. Fly promotion system (max-level flies evolve to elite variant)
40. Seasonal events (limited-time districts with unique rewards)
41. Leaderboard (total districts owned — local only until Play Games wired)
42. Analytics integration (stub — real Firebase deferred)

### Phase 8 — Meta-Progression & Engagement ✅
Goal: long-term retention loops.

43. Achievement system
44. Daily missions
45. Fly breeding/fusion
46. Offline progress
47. Collection codex
48. Boss rush mode
49. Stat respec
50. Rate app prompt

### Phase 9 — First Playable *(current)*
Goal: a real person can install the APK, play for 30 minutes, and have fun. No crashes, no placeholder text, no broken flows.

51. Disable ads & IAP at runtime (feature flags, hide "Watch Ad" buttons, Gems from gameplay only)
52. Gem economy rebalance (add Gem rewards to: boss kills, achievements, daily missions)
53. Stub removal audit (find all reflection-based stubs, replace with graceful no-ops or real logic)
54. Scene flow audit (every scene transition works, no dead ends, back buttons all function)
55. Save system stress test (corrupt save recovery, fresh install flow, upgrade path)
56. UI/UX pass (no overlapping text, all buttons reachable, touch targets ≥ 48dp, safe areas)
57. Balance tuning pass (difficulty curve, economy rates, battle pacing — all in one Constants sweep)
58. Placeholder art audit (find all [POO], grey squares, missing sprites — list for replacement)
59. Error handling (null checks on all manager singletons, graceful fallbacks, no silent failures)
60. Android device testing (build signed APK, test on 2+ real devices, fix crashes)

### Phase 10 — Art, Audio & Store (future)
Goal: production-quality assets, store submission.

61. Commission/create real sprite art (flies, poos, tiles, UI)
62. Music tracks (menu loop, battle loop, victory jingle)
63. Real SFX files (replace placeholder clips)
64. App icon + store assets (feature graphic, screenshots)
65. Google Play Console listing + closed testing
66. AdMob + Unity IAP real SDK integration (when ready to monetize)
67. Firebase Analytics real integration
68. Privacy policy + age rating
