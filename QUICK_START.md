# Quick Start Guide - What Changed

## For Programmers

### Key Script Changes
1. **VipMarkerUI.cs**: Added progress ring & color-coded states
2. **GameManager.cs**: Rebalanced economy (hyperloop now 8000 EUR)
3. **HudController.cs**: Added passive income popups every 10 EUR
4. **UpgradeButtonController.cs**: Blocks upgrades during travel
5. **TrainVipHandler.cs**: Spawns particle FX on VIP events
6. **VipSpawnManager.cs**: VIP rewards scale with route distance

### Build Status
✅ Compiles successfully (0 errors, 1 deprecation warning)

## For Artists

### Scene Hierarchy Changes
All objects renamed for clarity:
- Stations: `Station_Brussels`, `Station_Paris`, etc.
- UI: `MoneyCounter`, `PassengerCounter`, `Canvas_HUD`
- World: `Grass_Layer1-5`, `Tube_Main`, `Rails_Layer`

See `Assets/Scenes/HIERARCHY_GUIDE.md` for full reference.

### Assets Needed
1. **NPC Icons** (2 sprites):
   - Male VIP icon (32×32px)
   - Female VIP icon (32×32px)
   - Assign to `VipMarkerUI` prefab

2. **Particle Textures** (optional):
   - Sparkle/star for pickup
   - Coin/money for delivery
   - Smoke/puff for expiry

### Prefabs Created
- `Assets/Prefabs/VipMarkerUI.prefab` - Configure sprites here
- `Assets/Prefabs/FX/` - Particle effects (need textures)

## For Testers

### What to Test
1. **Mobile Scaling**: Everything readable/touchable on 2340×1080?
2. **Economy**: Can you afford hyperloop? Progression feel good?
3. **Visual Feedback**: See money popups? VIP colors clear?
4. **UX**: Upgrade blocked during travel? VIP markers informative?

### Known Issues
- Parallax tiling needs adjustment (in Editor)
- Godot HyperloopScene not yet ported (future task)
- Particle effects need proper textures/animations

## Quick Numbers

### Pricing
- Starting money: 100 EUR
- Passive income: 12.5 EUR/s (25 passengers × 0.5)
- After hyperloop: 50 EUR/s (2.5x multiplier)
- Hyperloop cost: 8000 EUR (~10 minutes to afford)
- Brussels: 1500 EUR
- Hamburg: 2000 EUR
- Paris: 3500 EUR
- Hannover: 4500 EUR
- Berlin: 6000 EUR

### VIP Rewards
- Base: 250 EUR
- +100 EUR per station distance
- Short routes: 250-350 EUR
- Long routes: 650-750 EUR

## Canvas Settings (Unity)
- Resolution: 2340×1080
- Scale Mode: Scale With Screen Size
- Match: 0.5
- Camera Orthographic Size: 5

## What Works
✓ Mobile UI scaling
✓ Touch-friendly buttons
✓ Economy progression
✓ Money feedback
✓ VIP color coding
✓ Upgrade blocking
✓ Particle FX system

## What Needs Work
- Parallax layer tiling (manual config)
- Particle effect visuals (need textures)
- Godot scene port (separate task)
- NPC sprite assignment (artist)
