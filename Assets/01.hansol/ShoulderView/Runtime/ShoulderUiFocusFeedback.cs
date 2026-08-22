using UnityEngine;
using UnityEngine.EventSystems;

namespace Railgame.Hansol.ShoulderView
{
    [DisallowMultipleComponent]
    public sealed class ShoulderUiFocusFeedback : MonoBehaviour, ISelectHandler, IDeselectHandler,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField, Range(1f, 1.08f)] private float focusedScale = 1.03f;
        [SerializeField, Range(0.9f, 1f)] private float pressedScale = 0.97f;

        private RectTransform rect;
        private bool focused;
        private bool pressed;

        private void Awake()
        {
            rect = transform as RectTransform;
        }

        public void OnSelect(BaseEventData eventData) => SetFocused(true);
        public void OnDeselect(BaseEventData eventData) => SetFocused(false);
        public void OnPointerEnter(PointerEventData eventData) => SetFocused(true);
        public void OnPointerExit(PointerEventData eventData) => SetFocused(false);

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
            RefreshScale();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
            RefreshScale();
        }

        private void OnDisable()
        {
            focused = false;
            pressed = false;
            RefreshScale();
        }

        private void SetFocused(bool value)
        {
            focused = value;
            RefreshScale();
        }

        private void RefreshScale()
        {
            if (rect == null)
                rect = transform as RectTransform;
            if (rect == null)
                return;
            float scale = pressed ? pressedScale : focused ? focusedScale : 1f;
            rect.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
