# Known Bugs / Unfinished

> Always current. Nothing in this project has been tested in Unity yet unless marked "owner-tested".

## Unfinished
- Art pipeline (step 2): owner-tested — compiles, placeholders generate, owner art overrides placeholder, `_Nf` sheets animate. Not yet tested: deleting/renaming art while Unity is open.
- `SpriteGalleryTest` is a throwaway test tool. With 192 terrain tiles it now shows a very big grid — expected.
- Terrain tiles exist but nothing draws them yet (map = step 3). In the gallery they appear as separate squares, not joined up, so seams can't be judged there.
- Borders between different terrain types (snow next to ice) will be hard edges until a blending method is decided.
- Registry `origin` field (owner / generated / placeholder) is new and not yet tested in Unity.
- Working-state glow (2D light) not built yet — comes with the building system.
- Old placeholders removed from the manifest are reported in the Console but not deleted automatically.

## Bugs
- None recorded yet (nothing to test).

## Warnings
- GitHub repo New_Gen may still be **public** — confirm it's private.
- Extra repo `New_Generation` was created by Unity Hub; unused. Owner can delete it.
