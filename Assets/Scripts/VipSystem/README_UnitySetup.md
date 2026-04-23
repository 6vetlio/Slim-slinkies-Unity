# Unity VIP Passenger System Setup

This folder ports the Godot VIP passenger design into Unity scripts. The VIP system is intentionally simple: persistent game state lives in `GameManager`, stations are clickable, VIPs spawn as map markers, and `TrainVipHandler` handles pickup and delivery when the train reaches a station.

## 1. Create the GameManager

1. Add an empty GameObject named `GameManager`.
2. Add the `GameManager` component.
3. Set starting money, regular passengers, passive income, and optionally the starting station ID.

`GameManager` uses `DontDestroyOnLoad`, so waiting VIPs and onboard VIPs survive scene changes.

## 2. Set up stations

1. Add a `Station` component to each station GameObject.
2. Give every station a unique `Station Id`, such as `amsterdam`, `paris`, or `berlin`.
3. Set `Display Name` to the player-facing name.
4. Add a collider for world clicks, or put the station under a Unity UI canvas for pointer clicks.
5. Assign `Train Handler To Notify` if clicking the station should send the train there.

VIPs will not spawn at the current train station, and only one waiting VIP can spawn per station.

## 3. Add the train handler

1. Add `TrainVipHandler` to the train or an empty controller object.
2. Assign `Train Transform` to the visible train object.
3. Assign `Starting Station`.
4. If you are using the simple `TrainMover` stop-and-map flow, turn `Move Train Transform` off so only one script moves the train.

When the train arrives, delivery is checked first. If no delivery happens, pickup is checked next.

## 4. Make the train stop and open the map

The existing `TrainMover` now treats `Point B` as the station stop.

1. Put `Point B` where the visible train should stop.
2. Keep `Stop At Point B` enabled on `TrainMover`.
3. Create a UI panel named `MapRoot` and disable it in the scene.
4. Add `MapStopController` to an empty GameObject.
5. Assign `Train Mover`, `Train Vip Handler`, `Starting Station`, `Map Root`, and `Vip Spawn Manager`.

Flow:

1. Train moves to `Point B`.
2. Train stops.
3. `MapRoot` opens.
4. VIPs spawn on the map.
5. Player clicks a station.
6. Map closes.
7. Train departs, loops back to `Point B`, and arrives at the clicked station logically.
8. Pickup or delivery is processed.
9. Map opens again.

For a clean 2D rail, add `RailLine2D` to an empty GameObject and assign `Point A` and `Point B`. Set `Center`, `Length`, and `Angle Degrees`. For a perfectly horizontal rail, keep `Angle Degrees` at `0`.

## 5. Add VIP spawning

1. Add an empty GameObject named `VipSpawnManager`.
2. Add `VipSpawnManager`.
3. Assign all station objects to `Stations`, or leave it empty to auto-find stations in the scene.
4. Create a UI/world marker prefab with `VipMarkerUI` and assign it as `Marker Prefab`.
5. Set `First Spawn Delay` low for demos, such as `1.5`.
6. Keep `Min Spawn Interval` and `Max Spawn Interval` around `8` and `14` for a 5-minute demo.

Optional: create `VipProfile` ScriptableObjects from `Assets > Create > Hyperloop > VIP Profile` for names and sprites.

## 6. Create the map station buttons

For a quick demo map, make a few UI buttons under `MapRoot`.

1. Add a `Station` component to each button.
2. Set a unique `Station Id` and readable `Display Name`.
3. Leave `Train Handler To Notify` empty if `MapStopController` is handling map clicks.
4. Assign these same stations to `VipSpawnManager`.

The `Station` component works on UI buttons through pointer clicks, and on world objects through colliders.

## 7. Create the VIP marker prefab

The marker prefab should have:

- `VipMarkerUI`
- TextMeshPro fields for name, destination, and timer
- Optional portrait `Image`
- Optional button wired by `VipMarkerUI` to send the train to the VIP origin

If the marker is in world space, enable `Follow Station In World`. If it is in a screen-space UI, you can disable that and position it from your map UI.

## 8. Add the HUD

1. Add `HudController` to a UI object.
2. Assign TextMeshPro fields for money, passengers, income, and onboard VIP.
3. Optionally assign a TextMeshPro popup prefab for green/red VIP reward and penalty feedback.

Passive income updates money quietly. Only VIP rewards and penalties create popups.

## 9. Parallax controls

`UIParallax` now has extra inspector controls:

- `Camera To Follow`: optional camera override.
- `Parallax Strength X/Y/Z`: how much camera movement on each axis affects the layer.
- `Scroll Multiplier`: overall speed multiplier.
- `Wrap Horizontally`: enables looping.
- `Tile Width` and `Tile Spacing`: controls the repeat distance.
- `Build Copies On Start`: automatically creates repeated copies.
- `Tile To Copy`: the tile image/panel to duplicate. If empty, the object with `UIParallax` is used.
- `Copies Left` and `Copies Right`: how many repeated tiles to place on each side.

You can also right-click the component menu and use `Build Tile Copies` or `Destroy Generated Copies`.
