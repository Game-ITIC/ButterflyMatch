# Region Island Concept Art Direction

## Project fit

- Purpose: candidate island-card art for the six post-Europe islands described in `Docs/GDD.md`.
- Runtime slot: `IslandSO.icon`, displayed by `RegionCardView` with preserved aspect ratio.
- Existing target: North and Europe `*_Icon.png` assets under `Assets/Content/Models/Regions`.
- Status: concept art only; no `RegionSO`, `IslandSO`, progression, prefab, or scene wiring is changed.

## Technical frame

- Source: square RGBA PNG generated at the tool's native resolution.
- Unity candidate: 512 x 512 RGBA PNG.
- Background: genuinely transparent; no backdrop, frame, label, UI, or external cast shadow.
- Composition: centered floating-island diorama; content should fit approximately x=8-92%, y=8-93%.
- Camera: orthographic-like three-quarter view, about 30-35 degrees elevation, front and right faces visible.
- Mobile read: one unmistakable landmark is the primary silhouette; supporting props stay chunky and subordinate.

## Visual system

- Shape language: compact rounded island base, chunky low-poly architecture, small bevels, simplified foliage.
- Edge treatment: clean 3D silhouettes without ink outlines; no painterly halo around transparency.
- Materials: matte hand-painted stone, plaster, wood, foliage, water, and rock with restrained texture variation.
- Lighting: soft warm key from upper left, cool ambient fill, readable contact shadows contained on the island.
- Detail: concentrated around the landmark and front approach; avoid tiny clutter that disappears in a 100 px UI card.
- Palette: saturated but natural regional colors, medium-high value contrast, dark underside framing the bright top.
- Exclude: text, logos, watermarks, UI, people, duplicate landmarks, cropped island edges, photorealism, toy-plastic gloss.

## Candidate chapters

1. Africa: Egypt / Giza, Morocco / Marrakech, South Africa / Cape Town.
2. Latin America: Mexico / Chichen Itza, Peru / Machu Picchu, Brazil / Rio de Janeiro.

These locations were selected because each has a distinct landmark silhouette and together cover desert, medina, coast, jungle, mountain, and tropical city palettes without duplicating the existing Asia, North, and Europe chapters.
