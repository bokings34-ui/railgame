using System;
using UnityEngine;
using UnityEngine.UI;

namespace Railgame.Hansol.ShoulderView
{
    public sealed class ShoulderShopPanel : MonoBehaviour
    {
        [Serializable]
        public sealed class OfferView
        {
            public Text title;
            public Text description;
            public Text tier;
            public Text stat;
            public Text cost;
            public Button buyButton;
        }

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text boltText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Button closeButton;
        [SerializeField] private OfferView[] offerViews;

        [SerializeField] private ShoulderShopEconomy economy;
        [SerializeField] private ShoulderShopOffer[] offers;
        [SerializeField] private ShoulderCameraRig cameraRig;
        [SerializeField] private ShoulderLocomotionController locomotion;
        private bool runtimeBound;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public ShoulderShopOffer[] Offers => offers;

        public void Initialize(GameObject root, Text bolts, Text feedback, Button close, OfferView[] views,
            ShoulderShopEconomy shopEconomy, ShoulderShopOffer[] shopOffers, ShoulderCameraRig camera,
            ShoulderLocomotionController playerLocomotion)
        {
            panelRoot = root;
            boltText = bolts;
            feedbackText = feedback;
            closeButton = close;
            offerViews = views;
            economy = shopEconomy;
            offers = shopOffers;
            cameraRig = camera;
            locomotion = playerLocomotion;

            BindRuntime();
            Refresh();
            panelRoot.SetActive(false);
        }

        private void Awake()
        {
            BindRuntime();
            Refresh();
        }

        private void OnDestroy()
        {
            if (runtimeBound && economy != null)
                economy.BoltsChanged -= OnBoltsChanged;
        }

        private void BindRuntime()
        {
            if (runtimeBound)
                return;
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }
            if (offerViews != null)
            {
                for (int index = 0; index < offerViews.Length; index++)
                {
                    int captured = index;
                    offerViews[index].buyButton.onClick.RemoveAllListeners();
                    offerViews[index].buyButton.onClick.AddListener(() => TryPurchase(captured));
                }
            }
            if (economy != null)
                economy.BoltsChanged += OnBoltsChanged;
            runtimeBound = true;
        }

        public void Open()
        {
            if (panelRoot == null)
                return;
            panelRoot.SetActive(true);
            if (cameraRig != null) cameraRig.SetMouseInputEnabled(false);
            if (locomotion != null) locomotion.SetKeyboardInputEnabled(false);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (feedbackText != null) feedbackText.text = "SELECT AN UPGRADE FOR THE NEXT LEG";
            Refresh();
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (cameraRig != null) cameraRig.SetMouseInputEnabled(true);
            if (locomotion != null) locomotion.SetKeyboardInputEnabled(true);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public bool TryPurchase(int index)
        {
            if (offers == null || index < 0 || index >= offers.Length)
                return false;
            ShoulderShopOffer offer = offers[index];
            bool bought = offer.TryUpgrade(economy);
            if (feedbackText != null)
                feedbackText.text = bought
                    ? $"{offer.Title} UPGRADED TO TIER {offer.Tier}"
                    : offer.IsMaxed ? "UPGRADE ALREADY MAXED" : "NOT ENOUGH BOLTS";
            Refresh();
            return bought;
        }

        public void Refresh()
        {
            if (boltText != null && economy != null)
                boltText.text = $"BOLTS   {economy.Bolts:00}";
            if (offers == null || offerViews == null)
                return;
            for (int index = 0; index < Mathf.Min(offers.Length, offerViews.Length); index++)
            {
                ShoulderShopOffer offer = offers[index];
                OfferView view = offerViews[index];
                view.title.text = offer.Title;
                view.description.text = offer.Description;
                view.tier.text = $"TIER {offer.Tier} / {offer.MaximumTier}";
                view.stat.text = $"{offer.StatName}\n{offer.CurrentValue:0.#}  >  {offer.NextValue:0.#}";
                view.cost.text = offer.IsMaxed ? "MAX" : $"BUY   {offer.CurrentCost} BOLTS";
                view.buyButton.interactable = !offer.IsMaxed && economy != null && economy.Bolts >= offer.CurrentCost;
            }
        }

        private void OnBoltsChanged(int value) => Refresh();
    }
}
