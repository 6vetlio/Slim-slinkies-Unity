# Slim Slinkies — Unity (Hyperloop + VIP)

Unity **6** project (`6000.0.62f1`). Godot-style **Hyperloop parallax** scene merged with an existing **map, stations, VIP, and economy** loop from `SimpleNaturePack_Demo`.

## Read this first (handoff)

Full onboarding, scene comparison, hierarchy diagram, script map, dependencies, and **agent rules** (including runtime-spawn exceptions) live **outside** this repo:

On disk, the handoff is the **sibling** file: `../Slim-slinkies-HANDOFF.md` (same parent folder as `Slim-slinkies-Unity/`). Example: `Documents/GitHub/Slim-slinkies-HANDOFF.md` next to `Documents/GitHub/Slim-slinkies-Unity/`.

That file is **not inside the git repo** by default; zip or commit it next to the project for submission, or copy its contents into a repo-local `HANDOFF.md` if the course requires a single upload.

## Main scenes

| Scene | Path |
|--------|------|
| **Hyperloop (generated)** | `Assets/Scenes/HyperloopDemo.unity` |
| **Reference gameplay + nature** | `Assets/Scenes/SimpleNaturePack_Demo.unity` |

## Regenerate HyperloopDemo

Unity menu: **Tools → Generate Hyperloop Scene (Clean)**  
Editor script: `Assets/Editor/HyperloopSceneGenerator.cs`

Do not hand-edit the huge `HyperloopDemo.unity` YAML as your primary workflow — change the generator (and prefabs), then regenerate.

## Artist / hierarchy notes

`Assets/Scenes/README_HIERARCHY.md` — station/route/VIP notes for `SimpleNaturePack_Demo`; see pointer at top for canonical handoff location.

## Prefabs / extraction

**Tools → Extract Map Prefab from Original Scene** — `Assets/Editor/CreateMapPrefab.cs`  
Output: `Assets/Prefabs/` (`CanvasMap`, `MapSystems`, etc.)
