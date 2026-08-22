using System;
using UnityEngine;

namespace Railgame.Hansol.ShoulderView
{
    [Serializable]
    public sealed class ShoulderShopOffer
    {
        [SerializeField] private string title;
        [SerializeField, TextArea] private string description;
        [SerializeField] private string statName;
        [SerializeField] private float baseValue;
        [SerializeField] private float valuePerTier;
        [SerializeField, Min(0)] private int baseCost = 2;
        [SerializeField, Min(0)] private int tier;
        [SerializeField, Min(1)] private int maximumTier = 3;

        public string Title => title;
        public string Description => description;
        public string StatName => statName;
        public int Tier => tier;
        public int MaximumTier => maximumTier;
        public bool IsMaxed => tier >= maximumTier;
        public int CurrentCost => IsMaxed ? 0 : baseCost + tier;
        public float CurrentValue => baseValue + valuePerTier * tier;
        public float NextValue => baseValue + valuePerTier * Mathf.Min(tier + 1, maximumTier);

        public ShoulderShopOffer(string title, string description, string statName, float baseValue,
            float valuePerTier, int baseCost, int maximumTier = 3)
        {
            this.title = title;
            this.description = description;
            this.statName = statName;
            this.baseValue = baseValue;
            this.valuePerTier = valuePerTier;
            this.baseCost = Mathf.Max(0, baseCost);
            this.maximumTier = Mathf.Max(1, maximumTier);
        }

        public bool TryUpgrade(ShoulderShopEconomy economy)
        {
            if (economy == null || IsMaxed || !economy.TrySpend(CurrentCost))
                return false;
            tier++;
            return true;
        }
    }
}
