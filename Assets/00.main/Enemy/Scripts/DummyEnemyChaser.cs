using UnityEngine;
using UnityEngine.AI;

namespace Railgame.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class DummyEnemyChaser : MonoBehaviour
    {
        public enum ChaseState
        {
            Inactive,
            Entering,
            Chasing
        }

        [SerializeField, Min(0.05f)] private float destinationRefreshSeconds = 0.2f;

        private NavMeshAgent agent;
        private Transform target;
        private Vector3 entryPosition;
        private float nextRefreshTime;

        public ChaseState State { get; private set; }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        public void Initialize(Vector3 entry, Transform chaseTarget)
        {
            target = chaseTarget;
            entryPosition = entry;
            State = ChaseState.Entering;
            agent.SetDestination(entryPosition);
        }

        private void Update()
        {
            if (State == ChaseState.Entering)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
                {
                    State = ChaseState.Chasing;
                    RefreshTarget();
                }

                return;
            }

            if (State == ChaseState.Chasing && Time.unscaledTime >= nextRefreshTime)
                RefreshTarget();
        }

        private void RefreshTarget()
        {
            nextRefreshTime = Time.unscaledTime + destinationRefreshSeconds;
            if (target != null && agent.isOnNavMesh)
                agent.SetDestination(target.position);
        }
    }
}
