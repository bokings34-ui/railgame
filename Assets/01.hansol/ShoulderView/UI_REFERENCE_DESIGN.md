# Shoulder View UI Reference Design

## Goal

Create a readable, controller-friendly UI for a bright 3D shoulder-view railway game.
The visual shell uses the licensed Tiny Swords UI pack, while the information
architecture is derived from the supplied references. External screenshots and
reference-game assets are not copied into the project.

## Reference roles

| Reference | Keep | Do not copy |
| --- | --- | --- |
| `#DRIVE` | Minimal gameplay HUD, edge anchoring, strong selected state, large action target | Italic racing typography, orange-heavy identity, mobile layout |
| `51 Worldwide Games` | Calm full-screen dimmer, short contextual guidance, simple modal hierarchy | Globe presentation and Nintendo-specific visual identity |
| `Adorable Home` | Category rail + product grid, clear item/cost cards, friendly material treatment | Hand-drawn home theme, heart currency, mobile-only density |
| `Zenless Zone Zero` Fairy Library | Numbered sections, icon + label pairing, black panel with bright focus color, dense data grouping | Blue/yellow brand identity, industrial decals, exact compositions |
| Tiny Swords UI | Wood, parchment, ribbons, button states, bars, utility icons | Medieval combat imagery and human avatars |

## Chosen concept: Railway Field Workshop

The UI should feel like an improvised station workbench placed over a modern,
high-contrast information system. Wood and parchment establish a warm co-op
crafting tone; dark navy panels and precise labels keep shoulder-view gameplay
readable.

### Visual layers

1. **World layer** remains visible whenever the player can still move.
2. **HUD layer** uses small edge-anchored modules and never covers the aiming area.
3. **Context layer** shows interaction prompts and short purchase feedback.
4. **Menu layer** dims and slightly softens the world, then presents the workshop.
5. **Critical modal layer** is reserved for destructive or irreversible choices.

## Design tokens

| Token | Suggested value | Use |
| --- | --- | --- |
| Ink | `#182231` | Primary text, outlines, modal background |
| Paper | `#F3E5BC` | Card and tooltip surfaces |
| Workshop wood | Tiny Swords WoodTable | Main shop frame and header rail |
| Focus blue | Tiny Swords blue button/ribbon | Selection, confirm, neutral action |
| Signal yellow | `#F3C94B` | Current cost, new item, active category |
| Danger red | Tiny Swords red button | Close, unavailable, destructive action |
| Success mint | `#7AD9A3` | Purchased state and positive stat delta |
| Dimmer | `#09111CCC` | World separation behind full menus |

Color must not be the only state cue. Selected entries also gain a 4 px cream
outline, a small pointer, and a 2-4 px upward/forward offset.

## Typography

- Use one readable Korean-capable sans-serif family for all production text.
- Headings: bold, 28-40 px at 1920x1080.
- Card titles: bold, 24-28 px.
- Body and descriptions: 18-22 px with a maximum of three lines.
- Controller hints and metadata: 16-18 px.
- Avoid the racing italics from `#DRIVE` and the decorative handwritten face from
  `Adorable Home`; both reduce Korean readability and do not match the railway UI.

## Screen architecture

### Gameplay HUD

```text
┌ STATION 04 ─ NEXT LEG ──────────────── BOLTS 07 ┐
│                                                  │
│                 world / crosshair                │
│                                                  │
│ [objective/status]          [tool or carried item]│
└─────────────── [E] OPEN WORKSHOP ────────────────┘
```

- Borrow `#DRIVE`'s edge anchoring, not its full-width lower dashboard.
- Keep the center 45% of the screen clear for shoulder aiming.
- Interaction prompt uses a small blue ribbon over a dark backing.

### Station shop

```text
┌ BACK ── STATION 04 / WORKSHOP ───────── BOLTS 07 ┐
│ [01 UPGRADES] [02 SUPPLIES] [03 TOOLS]            │
│                                                   │
│  ┌ CRAFT DRIVE ┐ ┌ CARGO RACK ┐ ┌ COOLANT LOOP ┐ │
│  │ icon / tier │ │ icon / tier │ │ icon / tier │ │
│  │ short value │ │ short value │ │ short value │ │
│  │ +stat        │ │ +stat        │ │ +stat        │ │
│  │ [BUY 03]     │ │ [BUY 04]     │ │ [BUY 05]     │ │
│  └──────────────┘ └──────────────┘ └──────────────┘ │
│          purchase feedback / controller hints       │
└─────────────────────────────────────────────────────┘
```

- Use the `Adorable Home` category + card model with only three visible cards.
- Use ZZZ-style two-digit section numbers as navigation aids, not decoration.
- Each card presents title, tier, one-sentence effect, stat delta, then cost.
- Blue is available, yellow is focused, mint is purchased, red is unavailable.
- The selected card grows by roughly 3% and reveals the full action hint.

### Camera options

- Use `#DRIVE`'s two-column scan pattern: option name left, current value right.
- Use Tiny Swords bar pieces for sensitivity and field of view sliders.
- Separate gameplay settings from destructive reset actions.
- The focused row receives a blue ribbon marker and cream outline.

### Tutorials and confirmation

- Follow `51 Worldwide Games`: dim/blur the world, one clear heading, one short
  explanation, one primary action.
- Tutorial callouts should point to a world object and disappear after the first
  successful interaction.
- Purchase confirmation is immediate for cheap, repeatable upgrades. Reserve a
  confirmation modal for unique or irreversible purchases only.

## Tiny Swords asset mapping

| UI role | Asset |
| --- | --- |
| Shop outer frame | `Wood Table/WoodTable.png` |
| Standard card | `Papers/RegularPaper.png` |
| Focused/rare card | `Papers/SpecialPaper.png` |
| Section/header | Blue or yellow slice from `Ribbons/SmallRibbons.png` |
| Buy/confirm | `Buttons/BigBlueButton_*` |
| Back/close/unavailable | `Buttons/SmallRed*` or `BigRedButton_*` |
| Slider | `Bars/BigBar_Base.png` + `BigBar_Fill.png` |
| Craft upgrade | `Icons/Icon_01.png` hammer |
| Settings | `Icons/Icon_10.png` gear |
| Help | `Icons/Icon_11.png` information |
| Sound | `Icons/Icon_12.png` note |

Do not use the swords, meat, shield, human avatars, or faction banners in the
railway interface.

## Interaction states

Every actionable element must define these states:

| State | Visual and behavior |
| --- | --- |
| Default | Normal sprite, no motion |
| Hover/focus | Cream outline, yellow marker, subtle scale-up |
| Pressed | Provided pressed sprite and 3-5 px downward offset |
| Disabled | Desaturated surface, lock or reason text, no hidden action |
| Purchased | Mint stamp/label and immediate updated stat |
| Error | Red edge flash plus a short reason such as `BOLTS 02 NEEDED` |

## Responsive rules

- Reference canvas: 1920x1080, match value `0.5`.
- Validate at 1920x1080 and 1280x720.
- Minimum controller target: 64x64 px at reference resolution.
- Keep essential HUD inside a 5% safe margin.
- At narrower layouts, cards remain three columns until card width would fall
  below 300 px; then switch to one focused card with left/right navigation.

## Technical implementation boundary

- All changes remain under `Assets/01.hansol/ShoulderView`.
- Existing shop economy, purchase rules, camera, and interaction code remain
  unchanged.
- Introduce a skin/theme data object so sprites and colors are not embedded in
  shop logic.
- Import only the Tiny Swords files used by the mapping above.
- Configure sprite sheets on the 64 px grid and verify sliced/tiled rendering.
- Do not commit Game UI Database or ZZZ screenshots.
- Before committing Tiny Swords source PNGs to a public repository, confirm that
  repository distribution is compatible with the pack's no-redistribution rule.

## Evidence checklist

- Gameplay HUD with the workshop closed.
- Interaction prompt while aiming at the station terminal.
- Shop opened with the first card focused.
- Pressed purchase button.
- Successful purchase and changed stat/tier.
- Insufficient bolts and max-tier disabled states.
- Options panel with slider focus.
- Spring and summer gameplay backgrounds.
- 1920x1080 and 1280x720 screenshots.
- PlayMode tests for open/close, purchase, affordability, and focus state.

## Reference links

- https://www.gameuidatabase.com/gameData.php?id=1030 (`#DRIVE`)
- https://www.gameuidatabase.com/gameData.php?id=186 (`51 Worldwide Games`)
- https://www.gameuidatabase.com/gameData.php?id=775 (`Adorable Home`)
- https://zenless.hoyoverse.com/ko-kr/news/113767 (`Fairy Library: Menu Interface`)
- https://pixelfrog-assets.itch.io/tiny-swords (Tiny Swords license and contents)
