# Latin America region (islands 13-15): source and provenance

Three low-poly islands built from the approved concepts in `ArtSource/RegionConcepts/LatinAmerica/`.
All modelling, preview renders, baking, `.blend` saves and FBX export were done in the live Blender
application through the `blender` MCP server (2026-11-04/05), with the shared North/Europe/Africa kits.

| Island (IslandSO id) | Source | Unity content | Stars |
|---|---|---|---|
| Mexico / Chichen Itza (`mexico`) | `scripts/island_chichen.py` -> `ChichenItza.blend` | `Assets/Content/Models/Regions/LatinAmerica/ChichenItza/` | 54 |
| Peru / Machu Picchu (`peru`) | `scripts/island_machupicchu.py` -> `MachuPicchu.blend` | `.../LatinAmerica/MachuPicchu/` | 56 |
| Brazil / Rio de Janeiro (`brazil`) | `scripts/island_rio.py` -> `RioDeJaneiro.blend` | `.../LatinAmerica/RioDeJaneiro/` | 58 |

## Rebuild

1. In the live Blender (MCP), run per island:
   `JOB = {'island': 'island_rio', 'bake': True, 'preview': True}` then exec `ArtSource/LatinAmerica/scripts/run.py`.
   Kits: `North/scripts/nordic_lib.py` + `nordic_pipe.py`, `Europe/scripts/euro_lib.py`, `Africa/scripts/africa_lib.py`.
2. Output: FBX pair + baked 2048 BaseMaps into the Unity folder, `<Id>.blend` + `<Id>_preview.png` here.
3. Unity: `Tools/Regions/Build Latin America Region Islands` (`Assets/Scripts/Utils/Editor/LatinAmericaRegionBuilder.cs`),
   then `RegionContentSetup.SetupLatinAmerica()` writes `Configs/Regions/LatinAmericaRegion.asset` and region slot 4
   in `MainMenu.unity`. Run the Africa setup first so the region order stays Asia, North, Europe, Africa, Latin America.

Build parts are named `S01_*`..`S10_*` (animation step = slot order, linear prerequisite chain).
Menu background: `ArtSource/Backgrounds/scripts/menu_bg.py` with `JOB = 'latam'` -> `Assets/Content/UI/Background/LatinAmericaMenuBackground.png`.
In-Unity validation renders (finished state + all 11 build states): `unity_validation/`.
Icons are rendered from the real prefabs; the concept icons in `Assets/Content/UI/RegionConcepts/LatinAmerica` are unused candidates.
