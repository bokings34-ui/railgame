using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

namespace Railgame.Map
{
    public sealed class DirtBlock : MonoBehaviour
    {
        private readonly List<NavMeshLink> jumpLinks = new();
        private ProceduralMapGenerator owner;
        private Vector2Int cell;

        public Vector2Int Cell => cell;
        public IReadOnlyList<NavMeshLink> JumpLinks => jumpLinks;

        public void Initialize(ProceduralMapGenerator mapOwner, Vector2Int mapCell)
        {
            owner = mapOwner;
            cell = mapCell;
            jumpLinks.Clear();
        }

        public void AddJumpLink(NavMeshLink link)
        {
            if (link != null)
                jumpLinks.Add(link);
        }

        public bool Mine()
        {
            return owner != null && owner.TryMineDirt(cell);
        }

        public void DisableForMining()
        {
            foreach (NavMeshLink link in jumpLinks)
                if (link != null)
                    link.activated = false;

            foreach (Collider item in GetComponentsInChildren<Collider>(true))
                item.enabled = false;
        }
    }
}
