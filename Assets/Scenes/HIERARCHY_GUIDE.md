# Scene Hierarchy - Artist Guide

## Overview
The scene has been reorganized with clear naming conventions to make it easier to find and edit objects. This guide explains the current structure and naming patterns.

## Naming Conventions

### Brackets [NAME]
- `[BRACKETS]` = Organizational groups (empty GameObjects for structure)
- Used for major categories like [UI], [WORLD], [GAME LOGIC], etc.

### Underscores Name_Type
- `Descriptive_Type` = Functional objects (actual game elements)
- Examples: `Grass_Layer1`, `Station_Brussels`, `Canvas_HUD`

### [RUNTIME] Prefix
- Objects created at runtime by scripts
- Don't edit these in the editor
- Examples: TrainDot (created by MapVisualizer)

## Current Hierarchy Structure

### Root Level
```
SimpleNaturePack_Demo
├── Canvas (all UI)
├── [GAME_WORLD] (game logic and world objects)
└── Main Camera
```

### UI Elements (Canvas children)

**HUD Elements:**
- `Canvas_HUD` - Main HUD container
  - `MoneyCounter` - Displays player money
  - `PassengerCounter` - Shows passenger count
  - `IncomeDisplay` - Shows income rate
  - `VipOnboardInfo` - VIP information display

**Map/Stations (Canvas_Map):**
- `Canvas_Map` - Map popup container
  - `Station_Groningen` - Groningen station button
  - `Station_Amsterdam` - Amsterdam station button
  - `Station_Brussels` - Brussels station button
  - `Station_Hamburg` - Hamburg station button
  - `Station_Paris` - Paris station button
  - `Station_Hannover` - Hannover station button
  - `Station_Berlin` - Berlin station button
  - Route lines (Route_X_Y pattern)
  - `[RUNTIME] TrainDot` - Animated train position indicator

**VIP Markers:**
- `VipMarkers_Container` - Parent for all VIP markers
  - Individual VIP markers created at runtime

### World/Environment Elements

**Grass Layers** (parallax background):
- `Grass_Layer1` - Foreground grass
- `Grass_Layer2` - Mid-ground grass
- `Grass_Layer3` - Background grass
- `Grass_Layer4` - Far background grass
- `Grass_Layer5` - Furthest grass layer

**Tubes** (hyperloop infrastructure):
- `Tube_Main` - Primary tube
- `Tube_Left` - Left tube section
- `Tube_Right1` - Right tube section 1
- `Tube_Right2` - Right tube section 2
- `Tube_Left3` - Left tube section 3

**Other World Elements:**
- `Rails_Layer` - Train rails/tracks
- `Train_NormalTrain` - Normal train visual

### Game Logic

**Core Systems:**
- `GameManager` - Economy, VIPs, progression
- `TrainMover` (HYperloop movment.cs) - Train movement
- `TransportSwitcher` - Normal/hyperloop switching
- `VipSpawnManager` - VIP spawning logic
- `MapVisualizer` - Map display and train dot
- `HudController` - HUD updates
- `UpgradeButtonController` - Upgrade button logic

## Finding Assets to Replace

### NPC Sprites
Located in scripts under `VipMarkerUI` component:
- `maleNpcSprite` - Male VIP icon
- `femaleNpcSprite` - Female VIP icon

These are referenced in the `VipMarkerUI.prefab` (when created) and can be swapped in the Inspector.

### Train Sprites
- Normal train: `Train_NormalTrain` GameObject
- Hyperloop: Referenced in `TransportSwitcher` component on `GameManager`

### Environment Sprites
- Grass layers: Each `Grass_LayerX` GameObject has an Image component
- Tubes: Each `Tube_X` GameObject has an Image component
- Rails: `Rails_Layer` GameObject

## Quick Reference: Common Tasks

### Change a station position
1. Find `Station_CityName` under `Canvas_Map`
2. Adjust RectTransform position

### Add new grass layer
1. Duplicate existing `Grass_LayerX`
2. Rename following pattern: `Grass_Layer6`
3. Adjust parallax settings

### Replace UI icons
1. Find UI element by descriptive name (e.g., `MoneyCounter`)
2. Look for child Image components
3. Replace sprite in Inspector

### Debug VIP system
1. Check `VipMarkers_Container` for active markers
2. Look for `[RUNTIME]` objects - these are created by scripts
3. Check GameManager for VIP spawn settings

## Notes

- **Don't rename [RUNTIME] objects** - they're destroyed/recreated by code
- **Canvas scaling** is set in Canvas component (reference resolution: 2340x1080)
- **Parallax layers** use different scroll speeds - lower numbers = slower movement
- **Station unlock costs** are set on individual Station components

## Troubleshooting

**Can't find an object?**
- Use the search bar in Unity Hierarchy (top right)
- Try searching for keywords: "Layer", "Station", "Canvas", etc.

**Object doesn't appear in game?**
- Check if GameObject is Active (checkbox in Inspector)
- Check if it's under correct parent (Canvas for UI, world parent for 3D)
- Check Layer and Sorting Order for sprites

**Changed sprite but doesn't update?**
- Make sure you're editing the prefab, not a runtime instance
- Check if script is overriding the sprite at runtime
- Verify sprite is imported correctly (check Import Settings)
