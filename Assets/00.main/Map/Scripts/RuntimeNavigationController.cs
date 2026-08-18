using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Railgame.Map
{
    public sealed class RuntimeNavigationController : MonoBehaviour
    {
        [SerializeField] private NavMeshSurface surface;
        [SerializeField] private Transform enemyRoot;

        private readonly List<Vector3> removedSupports = new();
        private Coroutine updateRoutine;
        private bool updateRequested;

        public NavMeshSurface Surface => surface;
        public bool IsUpdating => updateRoutine != null;
        public int CompletedUpdateCount { get; private set; }

        public void BuildInitialNavMesh()
        {
            if (surface == null)
                throw new MissingReferenceException("RuntimeNavigationController requires NavMeshSurface.");
            surface.BuildNavMesh();
            foreach (NavMeshLink link in surface.GetComponentsInChildren<NavMeshLink>(true))
                link.UpdateLink();
        }

        public void RequestUpdate(Vector3 removedSupportWorldPosition)
        {
            removedSupports.Add(removedSupportWorldPosition);
            updateRequested = true;
            if (updateRoutine == null && isActiveAndEnabled)
                updateRoutine = StartCoroutine(UpdateNavMesh());
        }

        private IEnumerator UpdateNavMesh()
        {
            yield return null;

            while (updateRequested)
            {
                updateRequested = false;
                if (surface.navMeshData == null)
                    surface.BuildNavMesh();
                else
                    yield return surface.UpdateNavMesh(surface.navMeshData);

                CompletedUpdateCount++;
                RecoverAndRepathAgents();
                removedSupports.Clear();
            }

            updateRoutine = null;
        }

        private void RecoverAndRepathAgents()
        {
            Transform searchRoot = enemyRoot != null ? enemyRoot : transform;
            foreach (NavMeshAgent agent in searchRoot.GetComponentsInChildren<NavMeshAgent>(true))
            {
                if (!agent.isActiveAndEnabled)
                    continue;

                Vector3 destination = agent.hasPath ? agent.destination : agent.transform.position;
                if (!agent.isOnNavMesh && NavMesh.SamplePosition(agent.transform.position + Vector3.down, out NavMeshHit hit, 1.5f, agent.areaMask))
                    agent.Warp(hit.position);

                if (agent.isOnNavMesh && agent.hasPath)
                    agent.SetDestination(destination);
            }
        }
    }
}
