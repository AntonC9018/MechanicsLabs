using UnityEngine;

namespace Core
{
    public class CameraFollow : MonoBehaviour
    {
        private Transform _target;
        [SerializeField] private float _smoothTime = 0.2f;
        private Vector3 _velocity = Vector3.zero;

        public void SetTarget(Transform target) => _target = target;

        private void LateUpdate()
        {
            if (_target == null) return;
            Vector3 targetPos = new Vector3(_target.position.x, _target.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, _smoothTime);
        }
    }
}