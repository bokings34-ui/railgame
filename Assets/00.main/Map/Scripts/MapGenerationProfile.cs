using UnityEngine;

namespace Railgame.Map
{
    [CreateAssetMenu(menuName = "Railgame/Map Generation Profile", fileName = "MapGenerationProfile")]
    public sealed class MapGenerationProfile : ScriptableObject
    {
        [Header("Core map prefabs")]
        [SerializeField] private GameObject groundChunkPrefab;
        [SerializeField] private GameObject groundCellPrefab;
        [SerializeField] private GameObject dirtPrefab;
        [SerializeField] private GameObject waterPrefab;
        [SerializeField] private GameObject boundaryPrefab;
        [SerializeField] private GameObject backgroundMountainPrefab;

        [Header("Season materials")]
        [SerializeField] private Material groundMaterial;
        [SerializeField] private Material dirtMaterial;
        [SerializeField] private Material waterMaterial;
        [SerializeField] private Material leavesMaterial;

        [Header("Team prefab slots")]
        [Tooltip("2x2 footprint, south-west bottom pivot")]
        [SerializeField] private GameObject treePrefab;
        [Tooltip("1x1 footprint, south-west bottom pivot")]
        [SerializeField] private GameObject ironPrefab;
        [SerializeField] private GameObject railPrefab;
        [SerializeField] private GameObject enemyPrefab;

        [Header("Per 32m leg")]
        [Min(1)] [SerializeField] private int treeCount = 12;
        [Min(1)] [SerializeField] private int ironCount = 12;
        [Min(6)] [Tooltip("Minimum count. Generator rounds up to complete 2x3 patches.")]
        [SerializeField] private int waterCellCount = 12;
        [Range(0f, 1f)] [SerializeField] private float hillResourceChance = 0.3f;

        [Header("Season terrain")]
        [Range(2, 3)] [SerializeField] private int riverWidth = 2;
        [Min(2)] [SerializeField] private int riverBendMin = 3;
        [Min(2)] [SerializeField] private int riverBendMax = 5;
        [Range(3, 5)] [SerializeField] private int fordWidth = 5;
        [Min(4)] [SerializeField] private int dirtBaseCount = 8;
        [Min(1)] [SerializeField] private int dirtIncreasePerLeg = 2;
        [Range(0f, 1f)] [SerializeField] private float resourceSideBias = 0.25f;

        public GameObject GroundChunkPrefab => groundChunkPrefab;
        public GameObject GroundCellPrefab => groundCellPrefab;
        public GameObject DirtPrefab => dirtPrefab;
        public GameObject WaterPrefab => waterPrefab;
        public GameObject BoundaryPrefab => boundaryPrefab;
        public GameObject BackgroundMountainPrefab => backgroundMountainPrefab;
        public Material GroundMaterial => groundMaterial;
        public Material DirtMaterial => dirtMaterial;
        public Material WaterMaterial => waterMaterial;
        public Material LeavesMaterial => leavesMaterial;
        public GameObject TreePrefab => treePrefab;
        public GameObject IronPrefab => ironPrefab;
        public GameObject RailPrefab => railPrefab;
        public GameObject EnemyPrefab => enemyPrefab;
        public int TreeCount => treeCount;
        public int IronCount => ironCount;
        public int WaterCellCount => waterCellCount;
        public float HillResourceChance => hillResourceChance;
        public int RiverWidth => riverWidth;
        public int RiverBendMin => riverBendMin;
        public int RiverBendMax => riverBendMax;
        public int FordWidth => fordWidth;
        public int DirtBaseCount => dirtBaseCount;
        public int DirtIncreasePerLeg => dirtIncreasePerLeg;
        public float ResourceSideBias => resourceSideBias;
    }
}
