# Africa region (islands 10-12): source and provenance

Three low-poly islands built from the approved concepts in `ArtSource/RegionConcepts/Africa/`.
All modelling, preview renders, baking, `.blend` saves and FBX export were done in the live Blender
application through the `blender` MCP server (2026-11-04/05), using the same deterministic generators
as North/Europe. No hand-made or CLI-rendered meshes.

| Island (IslandSO id) | Source | Unity content | Stars |
|---|---|---|---|
| Egypt / Giza (`egypt`) | `scripts/island_giza.py` -> `GizaPyramids.blend` | `Assets/Content/Models/Regions/Africa/GizaPyramids/` | 48 |
| Morocco / Marrakech (`morocco`) | `scripts/island_marrakech.py` -> `MarrakechMedina.blend` | `.../Africa/MarrakechMedina/` | 50 |
| South Africa / Cape Town (`southafrica`) | `scripts/island_capetown.py` -> `CapeTownHarbor.blend` | `.../Africa/CapeTownHarbor/` | 52 |

## Rebuild

1. In the live Blender (MCP), run per island:
   `JOB = {'island': 'island_giza', 'bake': True, 'preview': True}` then exec `ArtSource/Africa/scripts/run.py`.
   `bake: False` only writes a quick preview to `ArtSource/Africa/preview_nobake/` (scratch, not kept).
   Kits: `North/scripts/nordic_lib.py` + `nordic_pipe.py`, `Europe/scripts/euro_lib.py`, then `scripts/africa_lib.py`
   (warm palette, palms, pools, stalls; also used by Latin America).
2. Output: `<Id>_Island.fbx`, `<Id>_Buildings.fbx` and baked 2048 `*_BaseMap.png` into the Unity folder,
   `<Id>.blend` + `<Id>_preview.png` here.
3. Unity: `Tools/Regions/Build Africa Region Islands` (`Assets/Scripts/Utils/Editor/AfricaRegionBuilder.cs`) builds
   materials, prefab, `<Id>_Build.anim/.controller`, `BuildingsAnimationConfig` and `<Id>_Icon.png`, then
   `RegionContentSetup.SetupAfrica()` writes `Configs/Regions/AfricaRegion.asset`, the IslandSO/BuildingSO assets,
   GDD prices (`IslandPricing`) and region slot 3 in `MainMenu.unity`.

Build parts are named `S01_*`..`S10_*` (animation step = slot order, linear prerequisite chain).
Menu background: `ArtSource/Backgrounds/scripts/menu_bg.py` with `JOB = 'africa'` -> `Assets/Content/UI/Background/AfricaMenuBackground.png`.
In-Unity validation renders (finished state + all 11 build states): `unity_validation/`.
Icons are rendered from the real prefabs; the concept icons in `Assets/Content/UI/RegionConcepts/Africa` are unused candidates.
