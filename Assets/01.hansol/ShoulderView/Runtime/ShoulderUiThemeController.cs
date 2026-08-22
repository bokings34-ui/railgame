using UnityEngine;

namespace Railgame.Hansol.ShoulderView
{
    [DisallowMultipleComponent]
    public sealed class ShoulderUiThemeController : MonoBehaviour
    {
        [SerializeField] private ShoulderUiTheme theme;

        public ShoulderUiTheme Theme => theme;

        public void Initialize(ShoulderUiTheme value)
        {
            theme = value;
            ApplyTheme();
        }

        public void SetTheme(ShoulderUiTheme value)
        {
            theme = value;
            ApplyTheme();
        }

        [ContextMenu("Apply UI Theme")]
        public void ApplyTheme()
        {
            if (theme == null)
                return;
            ShoulderUiSkinElement[] elements = GetComponentsInChildren<ShoulderUiSkinElement>(true);
            foreach (ShoulderUiSkinElement element in elements)
                element.Apply(theme);
        }

        private void Awake()
        {
            ApplyTheme();
        }
    }
}
