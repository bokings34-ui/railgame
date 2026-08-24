using UnityEngine;

namespace Railgame.Player
{
    public sealed class RailgameCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(8f, 11f, -9f);
        [SerializeField] private float gameplayOrthographicSize = 7f;

        private void Awake()
        {
            if (TryGetComponent(out Camera view))
                view.orthographicSize = gameplayOrthographicSize;
        }

        public void SetTarget(Transform value)
        {
            target = value;
        }

        private void LateUpdate()
        {
            if (target == null)
                return;
            transform.position = target.position + offset;
            transform.LookAt(target.position + Vector3.up * 0.8f);
        }
    }
}
