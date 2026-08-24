# Shoulder View UI Modular Architecture

## Scope boundary

Everything described here lives below `Assets/01.hansol/ShoulderView` and can be
removed without changing a teammate-owned scene or prefab. Integration with the
team project should be additive: one UI root prefab, one interaction terminal,
and optional theme assets.

## Design requirements

- The shop rules must work without any particular visual skin.
- A visual skin must be replaceable without changing purchase or camera code.
- Screens must be addable or removable independently.
- Generated images, third-party sprites, and plain-color fallback graphics must
  all use the same binding API.
- A merge conflict in a shared scene must not be required to test this feature.
- The entire feature must have a documented uninstall path.

## Dependency direction

```text
ShoulderCameraRig ─┐
Locomotion         ├── ShoulderShopPanel (interaction/presentation state)
ShopEconomy/Offer ─┘                 │
                                     v
                           Unity UI view references
                                     │
                                     v
ShoulderUiTheme -> ShoulderUiThemeController -> ShoulderUiSkinElement
      ^                                      (visuals only)
      │
fallback colors / original sprites / licensed sprites
```

The arrows only point downward. Shop and camera classes never reference a
specific texture, sprite-sheet path, or reference game.

## Runtime modules

### Core gameplay

- `ShoulderCameraRig`
- `ShoulderLocomotionController`
- `ShoulderInteractor`
- `ShoulderShopEconomy`
- `ShoulderShopOffer`

These modules contain no theme data.

### Screen presenters

- `ShoulderShopPanel`: shop open/close, purchasing, and view refresh.
- `ShoulderViewOptionsPanel`: camera preference binding.
- Future screens should follow the same pattern and own only their local view
  references.

### Theme layer

- `ShoulderUiTheme`: colors and optional sprites.
- `ShoulderUiSkinElement`: a local role such as card, button, or accent text.
- `ShoulderUiThemeController`: applies or replaces a theme across one UI root.

If a sprite is missing, the theme layer deliberately falls back to a solid
color. This keeps scenes runnable while third-party or generated assets are
removed.

## UI role contract

The role enum is the stable boundary between layout and art:

- `CanvasDimmer`
- `HudBar`
- `Panel`
- `Card`
- `Inset`
- `Header`
- `PrimaryButton`
- `DangerButton`
- `Prompt`
- `CurrencyIcon`
- `FocusBadge`
- `Divider`
- `PrimaryText`
- `LightText`
- `SecondaryText`
- `AccentText`
- `PositiveText`

New art packs implement these roles instead of forcing a screen rewrite.

## Screen lifecycle

1. The demo builder or a prefab creates the layout and assigns semantic roles.
2. `ShoulderUiThemeController` finds skin elements within its own root.
3. The selected `ShoulderUiTheme` applies colors, sprites, sliced image modes,
   and selectable-state colors.
4. Screen presenters bind interaction callbacks.
5. A theme can be swapped and reapplied at runtime without rebinding gameplay.

## Extension points

### Add a new shop category

Add offer data and a category presenter. Do not add cases to the theme layer.

### Add a generated image

Import it below `ShoulderView/UI/Original`, assign it to a theme role, and keep
the fallback color. Removing the image restores the fallback rather than
breaking the screen.

### Add a 3D workshop terminal

Create it below `ShoulderView/Demo` or an isolated prefab folder. It only needs
`ShoulderShopTerminal` and a reference to `ShoulderShopPanel`.

### Replace Tiny Swords

Create a different `ShoulderUiTheme` asset and reassign the UI root. No screen
or economy code changes should be required.

## Merge strategy

- Deliver scripts and theme assets first.
- Deliver the standalone demo scene and screenshots second.
- Integrate into a team scene only after team branches are merged.
- Prefer an additive prefab or additive test scene over editing a shared scene.
- Never modify another contributor's owned asset folder for this feature.

## Removal procedure

1. Remove the UI root prefab or demo scene reference.
2. Remove the station terminal component/object if it was added.
3. Delete `Assets/01.hansol/ShoulderView`.
4. No project setting, shared scene, economy system, or teammate prefab should
   require repair.

## Validation gates

- Runtime theme swap does not change shop state.
- Missing sprites render valid fallback UI.
- Disabled, focused, pressed, success, and error states remain distinguishable.
- UI works at 1920x1080 and 1280x720.
- Shop open/close correctly owns and releases camera/player input.
- Standalone demo and tests pass without teammate content.
