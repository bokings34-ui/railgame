using UnityEngine;
using UnityEngine.UI;

namespace Railgame.Hansol.ShoulderView
{
    [DisallowMultipleComponent]
    public sealed class ShoulderUiSkinElement : MonoBehaviour
    {
        [SerializeField] private ShoulderUiRole role;
        [SerializeField] private Graphic graphic;
        [SerializeField] private Selectable selectable;

        public ShoulderUiRole Role => role;

        public void Initialize(ShoulderUiRole elementRole)
        {
            role = elementRole;
            ResolveReferences();
        }

        public void Apply(ShoulderUiTheme theme)
        {
            if (theme == null)
                return;
            ResolveReferences();
            if (graphic != null)
                graphic.color = theme.GetColor(role);

            if (graphic is Image image)
            {
                Sprite sprite = theme.GetSprite(role);
                if (theme.UsesReplaceableSprite(role))
                {
                    image.sprite = sprite;
                    image.type = sprite != null && sprite.border.sqrMagnitude > 0f
                        ? Image.Type.Sliced
                        : Image.Type.Simple;
                }
            }

            if (selectable != null)
            {
                selectable.transition = Selectable.Transition.ColorTint;
                selectable.colors = theme.CreateSelectableColors(role);
                if (selectable is Button button)
                {
                    Sprite pressed = theme.GetPressedSprite(role);
                    if (pressed != null)
                    {
                        SpriteState state = button.spriteState;
                        state.pressedSprite = pressed;
                        state.selectedSprite = pressed;
                        button.spriteState = state;
                    }
                }
            }
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (graphic == null)
                graphic = GetComponent<Graphic>();
            if (selectable == null)
                selectable = GetComponent<Selectable>();
        }
    }
}
