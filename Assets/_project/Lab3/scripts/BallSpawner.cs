using UnityEngine;

namespace Mechanics
{
    public sealed class  BallSpawner : MonoBehaviour
    {
        public static BallSpawner Instance { get; private set; }
        [SerializeField] public Transform _ballPrefab = null!;

        private void Awake() => Instance = this;

        public Transform SpawnBall()
        { 
            var ret = Instantiate(_ballPrefab);
            return ret;
        }
    }
}
