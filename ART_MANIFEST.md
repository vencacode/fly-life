# Art Asset Manifest — Fly Life

> Generated 2026-03-02. Lists every placeholder or missing visual/audio asset.
> Use this to commission or create art for the final build.

---

## Current State

The game uses **procedural runtime-generated visuals** and **TextMeshPro text** for nearly all graphics. There are only 2 actual sprite files in the project (`WhiteSquare.png` used for UI panels and `EmojiOne.png` bundled with TMP). All district tiles, poo markers, and fly representations are either procedural textures or colored text.

---

## Sprites Needed

### District Tiles (12 themes)

Each theme needs a unique tile sprite. Currently all tiles use `TileArtGenerator.cs` which produces 64x64 procedural textures (3 ring variants only).

| Asset | Theme | Current State | Suggested Size | Notes |
|---|---|---|---|---|
| tile_dogpark.png | Dog Park | Green + dot pattern | 128x128 | Grass, dog silhouettes |
| tile_backalley.png | Back Alley | Grey + grid pattern | 128x128 | Dumpsters, shadows |
| tile_sewer.png | Sewer | Dark + diagonal stripes | 128x128 | Grates, green glow |
| tile_playground.png | Playground | Green + dot pattern | 128x128 | Swings, sandbox |
| tile_townsquare.png | Town Square | Brown/grey + grid | 128x128 | Fountain, benches |
| tile_residential.png | Residential | Blue-grey + grid | 128x128 | Houses, fences |
| tile_market.png | Market | Orange + grid | 128x128 | Stalls, crates |
| tile_construction.png | Construction | Brown + stripes | 128x128 | Cranes, barriers |
| tile_university.png | University | Purple + grid | 128x128 | Books, columns |
| tile_waterfront.png | Waterfront | Teal + dot pattern | 128x128 | Waves, docks |
| tile_hospital.png | Hospital | White/light + grid | 128x128 | Cross symbol, clean |
| tile_airport.png | Airport | Blue-grey + stripes | 128x128 | Runway lines, planes |
| tile_boss.png | Boss overlay | N/A | 128x128 | Skull or danger overlay for corner tiles |

### Poo Types (7)

Currently all poos use `PooSpriteGenerator.cs` which draws a single 32x32 brown blob with eyes. Each type needs a unique sprite.

| Asset | Poo Type | Current State | Size | Notes |
|---|---|---|---|---|
| poo_dog.png | Dog | Brown blob (procedural) | 32x32 | Classic swirl shape |
| poo_bird.png | Bird | Brown blob (procedural) | 32x32 | White splat |
| poo_horse.png | Horse | Brown blob (procedural) | 32x32 | Large, round |
| poo_human.png | Human | Brown blob (procedural) | 32x32 | ... creative discretion |
| poo_cat.png | Cat | Brown blob (procedural) | 32x32 | Buried/sandy look |
| poo_compost.png | Compost | Brown blob (procedural) | 32x32 | Green-brown, leafy |
| poo_babydiaper.png | Baby Diaper | Brown blob (procedural) | 32x32 | Diaper wrapped |

### Fly Characters (3 roles x variants)

Flies are currently represented as colored text labels. No fly sprites exist at all.

| Asset | Role | Current State | Size | Notes |
|---|---|---|---|---|
| fly_scout_idle.png | Scout | Text label only | 64x64 | Thin, antennae, fast look |
| fly_soldier_idle.png | Soldier | Text label only | 64x64 | Bulky, armored, tough |
| fly_worker_idle.png | Worker | Text label only | 64x64 | Round, carrying pose |
| fly_scout_battle.png | Scout (battle) | Text label only | 64x64 | Attack stance |
| fly_soldier_battle.png | Soldier (battle) | Text label only | 64x64 | Attack stance |
| fly_worker_battle.png | Worker (battle) | Text label only | 64x64 | Attack stance |
| fly_elite_overlay.png | Elite badge | Gold text "ELITE" | 32x32 | Gold star/crown overlay |

### Equipment Icons (10 items)

Equipment is text-only in UI. Each item needs an icon for inventory/equip screens.

| Asset | Item | Slot | Rarity | Size |
|---|---|---|---|---|
| equip_poothrower.png | Poo Thrower | Weapon | Uncommon | 48x48 |
| equip_nailhelmet.png | Nail Helmet | Head | Uncommon | 48x48 |
| equip_bottlecapshield.png | Bottle Cap Shield | Offhand | Rare | 48x48 |
| equip_sugarcubeboots.png | Sugar Cube Boots | Feet | Uncommon | 48x48 |
| equip_goldendungscepter.png | Golden Dung Scepter | Weapon | Legendary | 48x48 |
| equip_diamondwings.png | Diamond Wings | Wings | Legendary | 48x48 |
| equip_crownoftheswarm.png | Crown of the Swarm | Head | Legendary | 48x48 |
| equip_phoenixcarapace.png | Phoenix Carapace | Body | Legendary | 48x48 |
| equip_luckycharm.png | Lucky Charm | Accessory | Rare | 48x48 |
| equip_veteranbadge.png | Veteran Badge | Accessory | Epic | 48x48 |

### Skin Color Swatches (8 skins)

Skins are color hex tints on the fly sprite. Once fly sprites exist, skins will be color variants. No additional sprite files needed unless unique skin art is desired.

| Skin | Color | Source | Notes |
|---|---|---|---|
| Classic Fly | #888888 | Default | Base sprite |
| Golden Fly | #FFD700 | Gem Shop (100g) | Gold tint |
| Neon Buzz | #39FF14 | Gem Shop (75g) | Green glow |
| Royal Purple | #7B2FBE | Gem Shop (150g) | Purple tint |
| Crimson Warrior | #DC143C | Battle reward | Red tint |
| Frost Wing | #87CEEB | Level reward | Ice blue tint |
| Shadow Fly | #1A1A2E | Gem Shop (200g) | Near-black |
| Candy Coat | #FF69B4 | Seasonal | Pink tint |

### UI Elements

All UI is built with Unity's default `Image` component (solid colors) and TextMeshPro text. No custom UI sprites exist.

| Asset | Current State | Size | Notes |
|---|---|---|---|
| icon_poocoin.png | Text only | 32x32 | Brown coin with poo swirl |
| icon_gem.png | Text only | 32x32 | Blue/purple gem |
| icon_xp.png | Text only | 32x32 | Star or shield |
| btn_attack.png | Solid color rect | 9-slice | Red themed button |
| btn_claim.png | Solid color rect | 9-slice | Green themed button |
| btn_train.png | Solid color rect | 9-slice | Yellow themed button |
| btn_equip.png | Solid color rect | 9-slice | Blue themed button |
| panel_card.png | Solid color rect | 9-slice | Dark panel for fly cards |
| panel_popup.png | Solid color rect | 9-slice | Modal popup background |
| icon_scout.png | Text "S" | 24x24 | Role icon |
| icon_soldier.png | Text "So" | 24x24 | Role icon |
| icon_worker.png | Text "W" | 24x24 | Role icon |
| app_icon.png | None | 512x512 | Play Store / app icon |
| splash_screen.png | None | 1080x1920 | Unity splash screen |

### Booster Icons (4 boosters)

| Asset | Booster | Current State | Size |
|---|---|---|---|
| booster_pooradar.png | Poo Radar | Text only | 48x48 |
| booster_hatchspeed.png | Hatch Speed | Text only | 48x48 |
| booster_fertility.png | Fertility Spike | Text only | 48x48 |
| booster_shield.png | Shield | Text only | 48x48 |

---

## Audio Needed

### Music (3 tracks)

`AudioManager.cs` defines 3 music tracks but no clips are assigned.

| Asset | Track | Current State | Duration | Notes |
|---|---|---|---|---|
| music_menu.ogg | Menu | None (silent) | 30-60s loop | Chill, quirky |
| music_citymap.ogg | City Map | None (silent) | 30-60s loop | Exploration, upbeat |
| music_battle.ogg | Battle | None (silent) | 30-60s loop | Intense, fast |

### Sound Effects (12 SFX)

`AudioManager.cs` defines 12 SFX enum values. No clips assigned.

| Asset | SFX | Current State | Duration | Notes |
|---|---|---|---|---|
| sfx_buttonclick.ogg | ButtonClick | Silent | <0.5s | UI tap |
| sfx_poofound.ogg | PooFound | Silent | <1s | Squish/splat |
| sfx_egglaid.ogg | EggLaid | Silent | <1s | Pop/plop |
| sfx_egghatched.ogg | EggHatched | Silent | <1s | Crack/chirp |
| sfx_battlehit.ogg | BattleHit | Silent | <0.5s | Thwack |
| sfx_battlewin.ogg | BattleWin | Silent | 1-2s | Fanfare |
| sfx_battleloss.ogg | BattleLoss | Silent | 1-2s | Sad trombone |
| sfx_districtcaptured.ogg | DistrictCaptured | Silent | 1-2s | Triumphant |
| sfx_districtlost.ogg | DistrictLost | Silent | 1s | Warning/alarm |
| sfx_boosteractivated.ogg | BoosterActivated | Silent | <1s | Power-up |
| sfx_coincollected.ogg | CoinCollected | Silent | <0.5s | Cha-ching |
| sfx_levelup.ogg | LevelUp | Silent | 1-2s | Fanfare/chime |

---

## Summary

| Category | Count | Status |
|---|---|---|
| District tile sprites | 13 | All procedural — need real art |
| Poo type sprites | 7 | All procedural (single blob) — need unique art |
| Fly character sprites | 7 | None exist — text labels only |
| Equipment icons | 10 | None exist — text only |
| UI elements | 14 | Solid color rects — need styled art |
| Booster icons | 4 | None exist — text only |
| Music tracks | 3 | None assigned |
| Sound effects | 12 | None assigned |
| **Total assets needed** | **70** | |

### Priority for First Playable

1. **P0 (must have):** App icon, fly sprites (3 idle), poo sprites (7), district tiles (13)
2. **P1 (should have):** Music tracks (3), key SFX (battle hit, win, loss, button click)
3. **P2 (nice to have):** Equipment icons, UI button sprites, booster icons, remaining SFX
4. **P3 (post-launch):** Unique skin sprites, battle animations, splash screen
