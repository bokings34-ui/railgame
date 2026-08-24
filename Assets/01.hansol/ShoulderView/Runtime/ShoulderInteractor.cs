using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Railgame.Hansol.ShoulderView
{
    public sealed class ShoulderInteractor : MonoBehaviour
    {
        [SerializeField] private Camera interactionCamera;
        [SerializeField, Min(0.5f)] private float interactionRange = 6f;
        [SerializeField, Min(0f)] private float castRadius = 0.18f;
        [SerializeField] private LayerMask interactionMask = ~0;
        [SerializeField] private Text promptText;

        public IShoulderInteractable CurrentTarget { get; private set; }
        public event Action<IShoulderInteractable> TargetChanged;

        private void Update()
        {
            ScanForTarget();
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                TryInteract();
        }

        public void Initialize(Camera camera, Text prompt, float range = 6f)
        {
            interactionCamera = camera;
            promptText = prompt;
            interactionRange = Mathf.Max(0.5f, range);
            castRadius = 0.45f;
            RefreshPrompt();
        }

        public IShoulderInteractable ScanForTarget()
        {
            IShoulderInteractable next = null;
            if (interactionCamera != null)
            {
                Ray ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
                RaycastHit[] hits = Physics.SphereCastAll(ray, castRadius, interactionRange, interactionMask,
                    QueryTriggerInteraction.Collide);
                foreach (RaycastHit hit in hits.OrderBy(value => value.distance))
                {
                    if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                        continue;
                    IShoulderInteractable candidate = FindInteractable(hit.collider);
                    if (candidate != null && candidate.CanInteract)
                    {
                        next = candidate;
                        break;
                    }
                }
            }

            if (!ReferenceEquals(next, CurrentTarget))
            {
                CurrentTarget = next;
                TargetChanged?.Invoke(CurrentTarget);
            }
            RefreshPrompt();
            return CurrentTarget;
        }

        public bool TryInteract()
        {
            if (CurrentTarget == null || !CurrentTarget.CanInteract)
                ScanForTarget();
            if (CurrentTarget == null || !CurrentTarget.CanInteract)
                return false;
            CurrentTarget.Interact(this);
            RefreshPrompt();
            return true;
        }

        private static IShoulderInteractable FindInteractable(Collider collider)
        {
            MonoBehaviour[] behaviours = collider.GetComponentsInParent<MonoBehaviour>(true);
            return behaviours.OfType<IShoulderInteractable>().FirstOrDefault();
        }

        private void RefreshPrompt()
        {
            if (promptText == null)
                return;
            bool visible = CurrentTarget != null && CurrentTarget.CanInteract;
            promptText.gameObject.SetActive(visible);
            promptText.text = visible ? $"E   {CurrentTarget.InteractionPrompt}" : string.Empty;
        }
    }
}
