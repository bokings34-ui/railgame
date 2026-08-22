using System;
using UnityEngine;

namespace Railgame.Hansol.ShoulderView
{
    public sealed class ShoulderShopEconomy : MonoBehaviour
    {
        [SerializeField, Min(0)] private int bolts = 7;

        public int Bolts => bolts;
        public event Action<int> BoltsChanged;

        public void Initialize(int startingBolts)
        {
            bolts = Mathf.Max(0, startingBolts);
            BoltsChanged?.Invoke(bolts);
        }

        public bool TrySpend(int cost)
        {
            if (cost < 0 || bolts < cost)
                return false;
            bolts -= cost;
            BoltsChanged?.Invoke(bolts);
            return true;
        }

        public void AddBolts(int amount)
        {
            if (amount <= 0)
                return;
            bolts += amount;
            BoltsChanged?.Invoke(bolts);
        }
    }
}
