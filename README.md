# RuneRealm

**OSRS skilling meets Skyrim's open world.**

RuneRealm is a Unity game that combines Old School RuneScape's complete skilling system with Skyrim's atmospheric open-world exploration. Chop trees in misty forests, mine ore in snow-capped mountains, fish in sun-drenched coastlines -- all with the exact XP mechanics you know from OSRS, wrapped in a Skyrim-style third-person experience.

## Requirements

- **Unity 6000.3.9f1** (Unity 6)
- Universal Render Pipeline (URP)
- TextMeshPro
- Input System package

## Quick Start

1. Clone the repository
2. Open in Unity 6000.3.9f1
3. Go to **RuneRealm > Setup Game Scene** in the menu bar to auto-create all manager objects
4. Go to **RuneRealm > Generate Terrain** to build the procedural world
5. Go to **RuneRealm > Create Default Item Database** to generate item ScriptableObjects
6. Press Play

## Controls

| Key | Action |
|-----|--------|
| WASD | Move |
| Mouse | Look / Camera orbit |
| Shift | Sprint |
| Space | Jump |
| E | Interact with resource / NPC |
| I / Tab | Inventory |
| K | Skill menu |
| J | Quest journal |
| Scroll | Zoom camera |
| Esc | Pause menu |

## Features

### OSRS Skilling System (22 Skills)

All 22 OSRS skills with the **exact XP formula** (`floor(sum(floor(x + 300 * 2^(x/7))) / 4)`), levels 1-99, and 200M XP cap.

**Gathering skills** with tick-based mechanics and success rolls:
- **Woodcutting** -- Chop trees from normal to magic, scaled by axe tier
- **Mining** -- Mine copper through runite, pickaxe-dependent success rates
- **Fishing** -- Net, rod, and harpoon fishing at spots throughout the world

**Production skills** with recipe systems:
- **Cooking** -- Cook raw food with burn chance that decreases with level
- **Smithing** -- Smelt ores to bars, forge bars into equipment at anvils
- **Crafting** -- Craft leather, gems, pottery, jewelry
- **Firemaking** -- Burn logs for cooking fires and warmth
- **Fletching, Herblore, Runecrafting, Farming, Construction** -- Framework ready

**Combat & Utility skills:**
- Attack, Strength, Defence, Hitpoints, Ranged, Magic, Prayer
- Agility, Thieving, Hunter

### Skyrim Open World

- **Procedural terrain** -- Layered Perlin noise with ridged mountains, rolling hills, and valleys
- **6 biomes** blending OSRS and Skyrim:
  - Lumbridge Meadows (starter zone, gentle fields)
  - Falador Highlands (ore-rich windswept hills)
  - Darkwood Forest (ancient trees, dense undergrowth)
  - Frostpeak Mountains (snow peaks, rare ores)
  - Karamja Coastline (tropical fishing paradise)
  - Morytania Swamp (dark, foggy marshland)
- **Dynamic weather** -- Clear, overcast, rain, heavy rain, storms, fog, snow with smooth transitions
- **Day/night cycle** -- 20-minute days with realistic sun movement, ambient lighting, fireflies at night, torchlight flicker
- **Zone discovery** -- Skyrim-style "Discovered: [Location]" notifications

### Skyrim-Style UI

- **Minimal HUD** -- Compass bar, fading stamina, contextual `[E]` interaction prompts
- **Skill menu** -- View all 22 skills with levels, XP, progress bars, descriptions
- **Inventory** -- 28-slot grid with item details, rarity colors, drag-and-drop
- **Level-up banners** -- "SKILL INCREASED: Woodcutting 15" with fade animations
- **XP notifications** -- "+25 Woodcutting XP" popups
- **Pause menu** -- Save/load/settings with volume sliders and fullscreen toggle
- **Quest journal** -- Track active and completed quests
- **Minimap** -- Rotating overhead camera with zone name display

### NPCs & Quests

- **NPC AI** -- NavMesh-driven wandering, patrolling, stationary, and follow behaviors
- **Dialogue** -- Typewriter text with branching choices, skill checks, quest requirements
- **Quest system** -- Multi-objective quests with OSRS difficulty tiers (Novice to Grandmaster)
- **NPC types** -- Villagers, shopkeepers, quest givers, bankers, skill masters, guards

### Technical

- **Save system** -- Full JSON serialization of skills, inventory, position, quests, world state
- **Event bus** -- Decoupled communication between all systems
- **Object pooling** -- Efficient spawning for particles and world objects
- **Custom shaders** -- Atmospheric fog with sun scattering, wind-animated grass, stylized water with waves and fresnel
- **Equipment system** -- 11 slots with stat aggregation and visual attachment

## Project Structure

```
Assets/
  Scripts/
    Core/           GameManager, EventManager, SaveSystem, GameBootstrapper
    Player/         PlayerController, ThirdPersonCamera, PlayerInteraction, EquipmentManager
    Skills/         SkillManager, SkillDefinitions, SkillingAction, [Woodcutting|Mining|...]Action, ResourceNode
    Inventory/      InventoryManager, ItemData (ScriptableObject)
    UI/             HUDManager, SkillMenuUI, InventoryUI, PauseMenuUI, MainMenuUI, MinimapUI, QuestJournalUI
    World/          TerrainGenerator, BiomeSystem, WeatherSystem, ZoneManager, DayNightAmbience, ResourceSpawner
    NPCs/           NPCController, DialogueManager, DialogueData, QuestManager
    Audio/          AudioManager
    Utils/          ObjectPool, TreeShakeEffect
  Shaders/          SkyrimFog, WindGrass, Water
  Editor/           RuneRealmEditorTools
  Prefabs/          Player, Resources, NPCs, UI, Environment
  Materials/
  Textures/
  Audio/            Music/, SFX/
```

## Extending the Game

### Adding a new resource node

1. Create a prefab with a `ResourceNode` component
2. Set the required skill, level, harvests before depletion, and respawn time
3. Create an `ItemData` ScriptableObject for the harvested item
4. Assign it to the node's `harvestedItem` field
5. Place in the world or add to `ResourceSpawner` configuration

### Adding a new quest

1. Create a `DialogueData` ScriptableObject with the quest dialogue tree
2. Create a `QuestDefinition` entry in the `QuestManager`
3. Set objectives (talk, collect, reach location, skill check)
4. Set rewards (XP, items)
5. Assign dialogue to the quest-giving NPC

### Adding a new item

Use **RuneRealm > Create Default Item Database** or manually create `ItemData` ScriptableObjects in `Assets/Resources/Items/`.

## License

This project is for educational and personal use. OSRS and Skyrim are trademarks of their respective owners (Jagex Ltd and Bethesda Softworks). This project is not affiliated with or endorsed by either company.
