using UnityEngine;

namespace Railgame.Enemy
{
    public sealed class EnemySpawnMarker : MonoBehaviour
    {
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform entryPoint;
        [SerializeField] private int legIndex;
        [SerializeField] private bool leftSide;

        public Transform SpawnPoint => spawnPoint;
        public Transform EntryPoint => entryPoint;
        public int LegIndex => legIndex;
        public bool LeftSide => leftSide;

        public void Initialize(Transform spawn, Transform entry, int leg, bool isLeft)
        {
            spawnPoint = spawn;
            entryPoint = entry;
            legIndex = leg;
            leftSide = isLeft;
        }
    }
}
