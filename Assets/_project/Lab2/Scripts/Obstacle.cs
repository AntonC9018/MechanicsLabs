using UnityEngine;

namespace Core
{
    public class Obstacle : MonoBehaviour
    {
        public bool IsDestructible;
        public Vector2Int GridPos;
        [SerializeField] private GameObject _deathParticles;
        [SerializeField] private AudioClip _deathSound;

        public void DestroyObstacle()
        {
            if (!IsDestructible) return;
            if (_deathSound != null)
            {
                AudioSource.PlayClipAtPoint(_deathSound, transform.position);
            }

            if (_deathParticles != null)
            {
                Instantiate(_deathParticles, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}