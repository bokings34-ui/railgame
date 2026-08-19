using System.Collections;
using System.Linq;
using Railgame.Map;
using UnityEngine;
using UnityEngine.AI;

namespace Railgame.Enemy
{
    public sealed class DummyEnemySpawner : MonoBehaviour
    {
        [SerializeField] private RuntimeNavigationController navigation;
        [SerializeField] private Transform player;
        [SerializeField] private DummyEnemyChaser dummyEnemyPrefab;
        [SerializeField] private bool spawnOnStart = true;

        public DummyEnemyChaser SpawnedEnemy { get; private set; }

        public void Configure(RuntimeNavigationController navigationController, Transform playerTarget,
            DummyEnemyChaser prefab)
        {
            navigation = navigationController;
            player = playerTarget;
            dummyEnemyPrefab = prefab;
        }

        private IEnumerator Start()
        {
            if (!spawnOnStart)
                yield break;

            while (navigation != null && navigation.Surface.navMeshData == null)
                yield return null;

            SpawnFirstDummy();
        }

        public bool SpawnFirstDummy()
        {
            if (SpawnedEnemy != null || navigation == null || player == null || dummyEnemyPrefab == null)
                return false;

            EnemySpawnMarker marker = navigation.GetComponentsInChildren<EnemySpawnMarker>(true)
                .OrderBy(item => item.LegIndex)
                .ThenByDescending(item => item.LeftSide)
                .FirstOrDefault();
            if (marker == null || marker.SpawnPoint == null || marker.EntryPoint == null ||
                !NavMesh.SamplePosition(marker.SpawnPoint.position, out NavMeshHit spawnHit, 1f, NavMesh.AllAreas))
                return false;

            SpawnedEnemy = Instantiate(dummyEnemyPrefab, spawnHit.position, Quaternion.identity, transform);
            SpawnedEnemy.name = "DummyEnemy_PlayerChaser";
            SpawnedEnemy.Initialize(marker.EntryPoint.position, player);
            return true;
        }
    }
}
