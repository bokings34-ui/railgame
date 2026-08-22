# Railway Workshop Original UI Atlas

`RailwayWorkshopAtlas.png` is an original generated support asset created for
this repository. It does not contain screenshots or copied assets from the UI
references, Tiny Swords, or another game.

## Generation method

- Tool: built-in Codex image generation
- Mode: new image generation, no input/reference images
- Intended use: Unity UI sprite atlas
- Background: transparent RGBA
- Source size: 1774 x 887

## Final prompt

```text
Use case: ui-mockup
Asset type: original 2D game UI texture atlas for a Unity shoulder-view railway workshop interface
Primary request: create a clean production-ready sprite sheet containing eight separate UI pieces arranged in a strict 4 by 2 grid with wide transparent gutters: (1) wide rustic railway workbench header frame, (2) warm parchment upgrade card frame, (3) dark navy recessed stat panel, (4) teal confirm button, (5) red close button, (6) golden numbered badge, (7) compact dark interaction prompt frame, (8) small steel bolt currency icon
Style/medium: original hand-painted low-poly/cartoon game UI, warm cooperative railway workshop, crisp dark navy outlines, slightly chunky readable forms
Composition/framing: each piece fully isolated inside an equal rectangular cell, front-facing orthographic, centered, no overlap, generous transparent padding, consistent outline weight
Color palette: dark navy, warm wood brown, parchment cream, teal blue, signal yellow, muted red, mint highlights
Materials/textures: subtle wood grain, paper fibers, painted metal, no photographic texture
Constraints: genuinely transparent background; no words, letters, numbers, logos, watermark, characters, weapons, medieval heraldry, or recognizable branding; every item must be usable independently as a Unity UI sprite; original design, not a copy of any reference game or asset pack
```

## Unity slices

The editor setup in `ShoulderUiAtlasSetup.cs` uses Unity 6's Sprite Data
Provider API and creates these named sub-sprites:

- `WorkshopHeader`
- `UpgradeCard`
- `WorkshopPanel`
- `PrimaryButton`
- `DangerButton`
- `FocusBadge`
- `InteractionPrompt`
- `BoltCurrency`

Panel and button sprites have 9-slice borders. The badge and bolt use simple
aspect-preserving rendering.

## Replacement

This atlas is optional. Delete its sprite assignments from a
`ShoulderUiTheme` to use solid-color fallback rendering, or assign a purchased
Tiny Swords theme without changing shop, camera, or economy code.
