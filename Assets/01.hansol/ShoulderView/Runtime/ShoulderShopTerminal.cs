using UnityEngine;

namespace Railgame.Hansol.ShoulderView
{
    public sealed class ShoulderShopTerminal : MonoBehaviour, IShoulderInteractable
    {
        [SerializeField] private ShoulderShopPanel shopPanel;

        public string InteractionPrompt => "OPEN STATION SHOP";
        public bool CanInteract => shopPanel != null && !shopPanel.IsOpen;

        public void Initialize(ShoulderShopPanel panel)
        {
            shopPanel = panel;
        }

        public void Interact(ShoulderInteractor interactor)
        {
            if (CanInteract)
                shopPanel.Open();
        }
    }
}
