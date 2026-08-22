using System;
using UnityEngine;

namespace Railgame.Hansol.ShoulderView
{
    public sealed class ShoulderSeasonPreview : MonoBehaviour
    {
        public enum Season
        {
            Spring,
            Summer
        }

        [SerializeField] private Season defaultSeason = Season.Spring;
        [SerializeField] private Material ground;
        [SerializeField] private Material waterAndGuide;
        [SerializeField] private Material earth;
        [SerializeField] private Material leaves;

        public Season ActiveSeason { get; private set; }

        private void Awake()
        {
            Apply(ReadSeasonArgument(defaultSeason));
        }

        public void Initialize(Material groundMaterial, Material waterMaterial, Material earthMaterial,
            Material leafMaterial)
        {
            ground = groundMaterial;
            waterAndGuide = waterMaterial;
            earth = earthMaterial;
            leaves = leafMaterial;
        }

        public void Apply(Season season)
        {
            ActiveSeason = season;
            if (season == Season.Spring)
            {
                SetColor(ground, new Color(0.42f, 0.72f, 0.25f));
                SetColor(waterAndGuide, new Color(0.17f, 0.62f, 0.82f));
                SetColor(earth, new Color(0.50f, 0.30f, 0.16f));
                SetColor(leaves, new Color(0.22f, 0.55f, 0.18f));
            }
            else
            {
                SetColor(ground, new Color(0.24f, 0.53f, 0.17f));
                SetColor(waterAndGuide, new Color(0.10f, 0.42f, 0.64f));
                SetColor(earth, new Color(0.58f, 0.34f, 0.16f));
                SetColor(leaves, new Color(0.10f, 0.38f, 0.12f));
            }

            Debug.Log($"SHOULDER_VIEW_SEASON_APPLIED season={ActiveSeason}");
        }

        public static string ReadSeasonLabel()
        {
            return ReadSeasonArgument(Season.Spring).ToString();
        }

        private static Season ReadSeasonArgument(Season fallback)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (!string.Equals(arguments[index], "-evidence-season", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (Enum.TryParse(arguments[index + 1], true, out Season season))
                    return season;
            }

            return fallback;
        }

        private static void SetColor(Material material, Color color)
        {
            if (material != null)
                material.color = color;
        }
    }
}
