# Claude Code task: build and integrate six new 3D islands

Work directly in `D:\GitHub\JigsawMatch2`. The user wants the six approved island concepts turned into real, buildable 3D islands and integrated into the existing Unity game.

## Mandatory first step: study the real project

Do not assume the architecture. Before editing anything:

1. Read the root `AGENTS.md` completely and follow it throughout the task.
2. Read `README.md`, `Docs/GDD.md` sections 1, 5 and the completion checklist, and `Docs/MainMenuBuildAnimations.md`.
3. Query graphify before architectural decisions. Start with:
   - `graphify explain "RegionSO"`
   - `graphify explain "IslandSO"`
   - `graphify explain "IslandBuildService"`
   - `graphify explain "RegionService"`
   - relevant `graphify path` queries.
   The current graph is under `Assets/Scripts/graphify-out`; run graphify from `Assets/Scripts` if needed.
4. Inspect the current dirty worktree with `git status --short`. Existing edits belong to the user. Do not revert, reformat, overwrite, or clean unrelated changes. Do not commit unless explicitly asked.
5. Use Unity MCP for Unity truth: editor state, project info, assets, prefabs, ScriptableObjects, scenes, screenshots and validation. Do not trust generated `.csproj` files or external `dotnet` builds.
6. Inspect at least one full North island and one full Europe island, including:
   - Blender/Python source in `ArtSource/North/scripts` and `ArtSource/Europe/scripts`;
   - FBX, materials, textures, icon and prefab under `Assets/Content/Models/Regions`;
   - `BuildingsAnimationConfig`, AnimatorController and AnimationClip;
   - corresponding `IslandSO`, its ten `BuildingSlot` records and the BuildingSO assets;
   - `RegionSO`, menu background, localization and the serialized region array in the active menu lifetime scope.
7. Capture a baseline of current Unity compilation errors. There are known unrelated dirty-worktree errors around `Item.Animation` and `Item.extraChecked`; do not repair unrelated gameplay code unless it blocks your work and the user explicitly authorizes it. Your change must add no new errors.

## Mandatory Blender MCP workflow

All 3D authoring must be performed in the live Blender application through the configured `blender` MCP server. This is a hard user requirement.

- At the beginning, verify that the `blender` MCP server is connected and that you can inspect the current Blender scene.
- Use Blender MCP calls for scene reset/setup, Python execution inside Blender, object/material/collection inspection, camera framing, rendered previews, `.blend` saves, baking and FBX export.
- You may study and reuse the existing deterministic Python generators, but execute the authoring code through Blender MCP. Do not use `blender -b`, an unrelated CLI renderer, or manually fabricated FBX files as the primary production path.
- After each island milestone, inspect the live Blender scene through MCP and render at least one preview before exporting.
- Save a real `.blend` source file for every island or a clearly documented region source file containing all three islands with separate collections. The saved Blender source must reproduce the exported result.
- If Blender MCP is disconnected or unhealthy, do not silently fall back to another modeling route. Diagnose the connection, retry safely, and report the exact blocker if it cannot be restored.

## Approved concept references

Art direction and exact prompts:

- `ArtSource/RegionConcepts/art-direction-brief.md`
- `ArtSource/RegionConcepts/asset-manifest.json`
- `ArtSource/RegionConcepts/prompts.md`
- `ArtSource/RegionConcepts/region_concepts_contact_sheet.png`

Unity-ready 512x512 icon candidates already exist under:

- `Assets/Content/UI/RegionConcepts/Africa`
- `Assets/Content/UI/RegionConcepts/LatinAmerica`

Build these two complete regions, exactly three islands per region:

### Africa

1. Egypt / Giza — Great Pyramid, Great Sphinx, small oasis.
2. Morocco / Marrakech — Koutoubia-inspired minaret, medina gate, tiled fountain.
3. South Africa / Cape Town — Table Mountain, Cape Dutch waterfront, small harbor.

### Latin America

4. Mexico / Chichen Itza — El Castillo, stelae, agave, cenote.
5. Peru / Machu Picchu — terraces, stone citadel, Sun Temple, Huayna Picchu silhouette.
6. Brazil / Rio de Janeiro — Christ the Redeemer, Sugarloaf, curved bay, colorful hillside houses.

Do not introduce additional islands inside either region. A region is exactly three islands.

## Production goal

Create real low-poly mobile 3D content that follows the existing North/Europe pipeline and can be selected, viewed and built in the main menu. Concept PNGs are references, not meshes.

For each of the six islands produce through Blender MCP:

- reproducible Blender/Python source under `ArtSource`, following the current deterministic generator pattern where practical;
- a coherent island base and landmark/building mesh groups;
- FBX export(s), baked base-map texture(s), Unity materials and a prefab in the established region folder conventions;
- a complete build sequence of exactly ten meaningful construction steps;
- `BuildingsAnimationConfig`, AnimatorController and AnimationClip wired like the existing working islands;
- a valid `IslandSO` with a stable lowercase ID, display/localization key, icon, prefab, `isComingSoon = false`, and ten ordered slots;
- BuildingSO/config assets required by those slots, named consistently and without generated filenames;
- mobile-safe import settings, no unnecessary Read/Write, no accidental mipmaps on UI icons, sensible texture sizes and compression;
- a representative finished-state preview render and an in-Unity screenshot.

Do not fake the result with six flat quads or a single combined mesh that cannot support construction. Each landmark must have a readable silhouette at the existing menu camera distance, and each of the ten build steps must reveal a meaningful visible part.

## Progression and data requirements

The GDD defines islands 10-15 as six islands across two regions, approximately 24 levels per island, exactly ten build slots each, with total star costs:

1. Giza: 48 stars
2. Marrakech: 50 stars
3. Cape Town: 52 stars
4. Chichen Itza: 54 stars
5. Machu Picchu: 56 stars
6. Rio de Janeiro: 58 stars

Distribute each total over ten slots using costs from 4 to 7 stars, with sensible prerequisites and a clear build order. Preserve strict sequential island unlocking through the existing `IslandBuildService`; do not create a second progression system.

Create and integrate:

- `AfricaRegion.asset` with the three Africa islands in the order above;
- `LatinAmericaRegion.asset` with the three Latin America islands in the order above;
- stable region IDs, localization keys, icons and menu backgrounds;
- the region array in progression order: Asia, North, Europe, Africa, Latin America;
- localization entries through the existing source/table workflow;
- any editor setup/build scripts needed to regenerate the new content deterministically, following `NorthRegionBuilder` and `EuropeRegionBuilder` rather than hand-editing fragile GUID references.

The current code uses `RegionSO.MenuBackground` and `IslandSO.Icon`. Produce or render a coherent menu background for each new region from the completed visual direction. Do not leave required serialized fields null.

## Execution strategy

1. Build Giza first as the gold-standard vertical slice in the live Blender scene through Blender MCP.
2. Integrate Giza end to end and validate it in Unity: import, prefab, animation, `IslandSO`, one region asset, selection, ten build states and screenshots.
3. Only after Giza works, reuse the proven tooling for the other five islands. Keep camera, scale, light direction, materials, texture density, pivots and animation conventions consistent.
4. Finish both regions. Do not stop after writing a plan, producing only concepts, or finishing only Giza.
5. If an external blocker truly prevents completion, exhaust safe in-scope alternatives, leave the workspace buildable, document the exact blocker and continue every independent part that remains possible.

## Validation and acceptance criteria

- Unity MCP AssetDatabase refresh after every external asset/script batch.
- Graphify update after any change under `Assets/Scripts`: `graphify update Assets/Scripts` or the project-approved equivalent.
- No new Unity compilation errors relative to the captured baseline.
- All six prefabs load through `RegionService` and expose a valid `BuildingsAnimationConfig`.
- All six icons render correctly in `RegionCardView` at mobile scale.
- All six islands have ten visible build steps, correct totals, prerequisites and deterministic object references.
- The two `RegionSO` assets contain exactly three islands each and are serialized into the menu lifetime scope after Europe.
- Region background switching works through the existing `RegionBackgroundPresenter`.
- Verify locked/unlocked/finished selection behavior without rewriting the progression architecture.
- Capture final screenshots/contact sheets for all six islands and both region menu backgrounds.
- Update relevant project documentation and provenance records.
- At the end, run a scoped `git diff`/status review and report every created or modified path, validations performed, known pre-existing errors and any remaining risk.

Use the smallest compatible changes and existing legacy patterns. Consistency and working serialized wiring are more important than architectural cleanup.
