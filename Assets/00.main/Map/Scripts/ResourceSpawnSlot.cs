using UnityEngine;
using UnityEngine.AI;

namespace Railgame.Map
{
    public sealed class ResourceSpawnSlot : MonoBehaviour
    {
        [SerializeField] private Vector2Int anchorCell;
        [SerializeField] private Vector2Int footprint = Vector2Int.one;
        [SerializeField] private int surfaceHeight;

        private ProceduralMapGenerator owner;
        private NavMeshObstacle obstacle;

        public Vector2Int AnchorCell => anchorCell;
        public Vector2Int Footprint => footprint;
        public int SurfaceHeight => surfaceHeight;

        public void Initialize(ProceduralMapGenerator mapOwner, Vector2Int cell, Vector2Int size, int height)
        {
            owner = mapOwner;
            anchorCell = cell;
            footprint = size;
            surfaceHeight = height;
            obstacle = GetComponent<NavMeshObstacle>();
        }

        public void MarkHarvested()
        {
            if (obstacle != null)
                obstacle.enabled = false;
            owner?.NotifyResourceRemoved(anchorCell, footprint);
        }
    }
}
