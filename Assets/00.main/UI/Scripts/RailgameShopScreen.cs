using System;
using UnityEngine;
using UnityEngine.UI;

namespace Railgame.UI
{
    public sealed class RailgameShopScreen : MonoBehaviour
    {
        [SerializeField] private Text walletText;
        [SerializeField] private Button[] offerButtons;
        [SerializeField] private Text[] offerTexts;
        [SerializeField] private Button closeButton;
        [SerializeField] private int startingBolts = 6;
        [SerializeField] private int[] prices = { 2, 3, 4 };

        private int bolts;
        private bool initialized;
        private bool[] sold;

        public event Action Closed;
        public int Bolts => bolts;

        private void Awake()
        {
            bolts = startingBolts;
            sold = new bool[offerButtons?.Length ?? 0];
            for (int index = 0; index < sold.Length; index++)
            {
                int captured = index;
                offerButtons[index]?.onClick.AddListener(() => Buy(captured));
            }
            closeButton?.onClick.AddListener(Close);
            initialized = true;
            Refresh();
        }

        private void OnDestroy()
        {
            closeButton?.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            if (initialized) Refresh();
        }

        public void Close()
        {
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        public bool Buy(int index)
        {
            if (index < 0 || index >= sold.Length || sold[index])
                return false;
            int price = index < prices.Length ? prices[index] : int.MaxValue;
            if (bolts < price)
                return false;

            bolts -= price;
            sold[index] = true;
            Refresh();
            return true;
        }

        private void Refresh()
        {
            if (walletText != null) walletText.text = $"BOLTS  {bolts}";
            for (int index = 0; index < sold.Length; index++)
            {
                if (offerButtons[index] != null) offerButtons[index].interactable = !sold[index];
                if (offerTexts != null && index < offerTexts.Length && offerTexts[index] != null)
                    offerTexts[index].text = sold[index] ? "SOLD" : $"TEST ITEM {index + 1}\n{prices[index]} BOLTS";
            }
        }
    }
}
