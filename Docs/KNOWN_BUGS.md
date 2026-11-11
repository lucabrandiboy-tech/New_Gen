# Known Bugs / Unfinished

> Always current. **Nothing in this project has been run in Unity since the art-pipeline test on 2026-11-09.**

## Status of the big build (2026-11-11)
- **Simulation (`Assets/Scripts/Sim`)**: compiled and tested outside Unity — 209 checks pass (`dotnet run --project Tools/simtest`). It has never run inside Unity.
- **Unity layer (`Assets/Scripts/Game`, `Assets/Scripts/Editor/SceneSetup.cs`)**: written and syntax-checked only. **It has never been compiled by Unity.** Expect compile errors on first open; paste them back.

## Unfinished / not built (Milestone 1 scope)
- **No recipes** (owner decision): smelter/assembler/encoder can be placed and given power but cannot craft; hand crafting list is empty. Data cells can only be obtained with the dev console (`give cell_resource 100`).
- **Buildings cost nothing** to place until recipes exist.
- **Tech tree**: one placeholder tech (`first_contact`). The real tier × branch chart needs owner approval.
- **No ground items**: inventory overflow (e.g. removing a full chest with a full inventory) is lost.
- **No audio** at all.
- **Localisation**: English table only; most UI text is still hard-coded English.
- **Gore toggle**: nothing to toggle yet.
- **Difficulty presets**: stored in the save but change nothing yet.
- **Minimap / map / fog of war**: not built.
- **Stats graphs, tooltips, recipe browser, blueprints, logic network**: not built.
- **Dev console**: `reveal`, `setpop`, `event` are stubs.

## Likely problems in the untested Unity layer
- Input System: actions are built in code; if Unity complains about `InputSystemUIInputModule` or `PerformInteractiveRebinding`, that is where to look.
- Corner tube sprites: orientation rule is in ART_GUIDE §3; may be mirrored wrong until seen.
- Building sprites are anchored bottom-left; rotating 1×1 pieces (tubes/wires/arms) spin around the tile centre via a pivot child — may be off by half a tile.
- Player sprite smoothing can look floaty; camera follow speed may need tuning.
- UI is placeholder uGUI built in code; layout overlaps at small resolutions are likely.
- Terrain tilemaps redraw 2 chunks per frame: fast camera zooms may show empty areas for a moment.

## Design decisions I made without asking (please confirm or change)
- Power rules (wires+tubes conduct; buildings must touch a conductor).
- Pod reactor decay: 200 kW → 10% over 2 hours.
- Data-center heat/cooling numbers; research point rate.
- Player reach 6 tiles; hand mining 60 ticks; shooting numbers.
- Chunk unload rule.
- LMB on your own building (nothing selected) opens it instead of shooting it.

## Warnings
- GitHub repo New_Gen may still be **public** — confirm it's private.
- Extra repo `New_Generation` was created by Unity Hub; unused. Owner can delete it.
