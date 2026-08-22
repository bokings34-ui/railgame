using UnityEngine;
using UnityEngine.UI;

namespace Railgame.Hansol.ShoulderView
{
    [CreateAssetMenu(fileName = "ShoulderUiTheme", menuName = "Railgame/Shoulder View/UI Theme")]
    public sealed class ShoulderUiTheme : ScriptableObject
    {
        [Header("Workshop palette")]
        [SerializeField] private Color canvasDimmer = new(0.035f, 0.055f, 0.09f, 0.88f);
        [SerializeField] private Color hudBar = new(0.075f, 0.105f, 0.14f, 0.96f);
        [SerializeField] private Color panel = new(0.20f, 0.14f, 0.085f, 0.98f);
        [SerializeField] private Color card = new(0.91f, 0.82f, 0.62f, 1f);
        [SerializeField] private Color inset = new(0.075f, 0.105f, 0.14f, 0.96f);
        [SerializeField] private Color header = new(0.15f, 0.58f, 0.66f, 1f);
        [SerializeField] private Color primaryButton = new(0.15f, 0.58f, 0.66f, 1f);
        [SerializeField] private Color dangerButton = new(0.72f, 0.25f, 0.24f, 1f);
        [SerializeField] private Color focus = new(0.95f, 0.76f, 0.24f, 1f);
        [SerializeField] private Color primaryText = new(0.09f, 0.13f, 0.18f, 1f);
        [SerializeField] private Color lightText = new(0.96f, 0.92f, 0.82f, 1f);
        [SerializeField] private Color secondaryText = new(0.61f, 0.68f, 0.72f, 1f);
        [SerializeField] private Color positiveText = new(0.48f, 0.86f, 0.64f, 1f);
        [SerializeField] private Color disabled = new(0.30f, 0.33f, 0.35f, 0.82f);

        [Header("Optional replaceable artwork")]
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Sprite cardSprite;
        [SerializeField] private Sprite focusedCardSprite;
        [SerializeField] private Sprite headerSprite;
        [SerializeField] private Sprite primaryButtonSprite;
        [SerializeField] private Sprite primaryButtonPressedSprite;
        [SerializeField] private Sprite dangerButtonSprite;
        [SerializeField] private Sprite dangerButtonPressedSprite;
        [SerializeField] private Sprite promptSprite;
        [SerializeField] private Sprite currencyIconSprite;

        public Color GetColor(ShoulderUiRole role)
        {
            return role switch
            {
                ShoulderUiRole.CanvasDimmer => canvasDimmer,
                ShoulderUiRole.HudBar => hudBar,
                ShoulderUiRole.Panel => panel,
                ShoulderUiRole.Card => card,
                ShoulderUiRole.Inset => inset,
                ShoulderUiRole.Header => header,
                ShoulderUiRole.PrimaryButton => primaryButton,
                ShoulderUiRole.DangerButton => dangerButton,
                ShoulderUiRole.Prompt => hudBar,
                ShoulderUiRole.CurrencyIcon => lightText,
                ShoulderUiRole.FocusBadge => focus,
                ShoulderUiRole.Divider => new Color(focus.r, focus.g, focus.b, 0.4f),
                ShoulderUiRole.PrimaryText => primaryText,
                ShoulderUiRole.LightText => lightText,
                ShoulderUiRole.SecondaryText => secondaryText,
                ShoulderUiRole.AccentText => focus,
                ShoulderUiRole.PositiveText => positiveText,
                _ => lightText
            };
        }

        public Color LightText => lightText;
        public Color DisabledColor => disabled;
        public Color FocusColor => focus;

        public Sprite GetSprite(ShoulderUiRole role)
        {
            return role switch
            {
                ShoulderUiRole.Panel => panelSprite,
                ShoulderUiRole.Card => cardSprite,
                ShoulderUiRole.FocusBadge => focusedCardSprite,
                ShoulderUiRole.Header => headerSprite,
                ShoulderUiRole.PrimaryButton => primaryButtonSprite,
                ShoulderUiRole.DangerButton => dangerButtonSprite,
                ShoulderUiRole.Prompt => promptSprite,
                ShoulderUiRole.CurrencyIcon => currencyIconSprite,
                _ => null
            };
        }

        public Sprite GetPressedSprite(ShoulderUiRole role)
        {
            return role switch
            {
                ShoulderUiRole.PrimaryButton => primaryButtonPressedSprite,
                ShoulderUiRole.DangerButton => dangerButtonPressedSprite,
                _ => null
            };
        }

        public bool UsesReplaceableSprite(ShoulderUiRole role)
        {
            return role is ShoulderUiRole.Panel or ShoulderUiRole.Card or ShoulderUiRole.FocusBadge
                or ShoulderUiRole.Header or ShoulderUiRole.PrimaryButton or ShoulderUiRole.DangerButton
                or ShoulderUiRole.Prompt or ShoulderUiRole.CurrencyIcon;
        }

        public void ConfigureArtwork(Sprite panelValue, Sprite cardValue, Sprite focusedCardValue,
            Sprite headerValue, Sprite primaryButtonValue, Sprite dangerButtonValue, Sprite promptValue,
            Sprite currencyIconValue)
        {
            panelSprite = panelValue;
            cardSprite = cardValue;
            focusedCardSprite = focusedCardValue;
            headerSprite = headerValue;
            primaryButtonSprite = primaryButtonValue;
            dangerButtonSprite = dangerButtonValue;
            promptSprite = promptValue;
            currencyIconSprite = currencyIconValue;
        }

        public void ConfigurePressedArtwork(Sprite primaryPressedValue, Sprite dangerPressedValue)
        {
            primaryButtonPressedSprite = primaryPressedValue;
            dangerButtonPressedSprite = dangerPressedValue;
        }

        public ColorBlock CreateSelectableColors(ShoulderUiRole role)
        {
            Color normal = GetColor(role);
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = normal;
            colors.highlightedColor = Color.Lerp(normal, focus, 0.38f);
            colors.selectedColor = Color.Lerp(normal, focus, 0.55f);
            colors.pressedColor = Color.Lerp(normal, Color.black, 0.18f);
            colors.disabledColor = disabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            return colors;
        }
    }
}
