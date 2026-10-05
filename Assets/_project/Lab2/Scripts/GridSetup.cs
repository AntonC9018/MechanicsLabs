using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class GridParams
    {
        public float CellSize = 1f;
        public Vector2 GridToWorld(Vector2Int p) => new Vector2(p.x * CellSize, -p.y * CellSize);
        public Vector2Int WorldToGrid(Vector2 w) => new Vector2Int(Mathf.RoundToInt(w.x / CellSize), Mathf.RoundToInt(-w.y / CellSize));
    }

    [ExecuteAlways]
    public sealed class GridSetup : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GridParams _grid = new();
        [SerializeField] private GameObject _cellPrefab = null!;
        [SerializeField] private GameObject _playerPrefab = null!;
        [SerializeField] private GameObject _rockPrefab = null!;
        [SerializeField] private GameObject _wallPrefab = null!;
        [SerializeField] private Camera _camera = null!;
        
        [Header("Generation Settings")]
        [SerializeField] private Sprite[] _cellSprites = null!;
        [SerializeField, UnityEngine.Range(0, 1)] private float _obstacleDensity = 0.15f;
        [SerializeField] private float _noiseScale = 0.1f;
        [SerializeField] private Vector2 _noiseOffset = new Vector2(1000, 1000);

        [Header("Infinite Logic")]
        [SerializeField] private int _viewRadius = 10;
        [SerializeField] private int _cleanupBuffer = 5;
        [SerializeField] private int _maxMoveDist = 3;
        [SerializeField] private bool _allowDiagonal = true;

        [Header("Obstacle Settings")]
        [SerializeField] private bool _enableObstacleMovement = true;

        private GameObject _player;
        private Vector2Int _playerPos;

        private SpatialHashStatic _staticHash;
        private SpatialHashDynamic _dynamicHash;

        private Dictionary<Vector2Int, GridCell> _activeCells = new();
        private Dictionary<Vector2Int, Obstacle> _staticObstacles = new();
        private Dictionary<Vector2Int, Obstacle> _dynamicObstacles = new();

        private HashSet<Vector2Int> _destroyedCoords = new HashSet<Vector2Int>();
        private HashSet<Vector2Int> _generatedCoords = new HashSet<Vector2Int>();

        private void OnEnable()
        {
            _staticHash = new SpatialHashStatic(_grid.CellSize);
            _dynamicHash = new SpatialHashDynamic(_grid.CellSize);
            if (Application.isPlaying) InitGame();
        }

        private void InitGame()
        {
            if (_playerPrefab == null) return;
            ClearGridData();

            _player = Instantiate(_playerPrefab, Vector3.back, Quaternion.identity);
            _playerPos = Vector2Int.zero;

            if (_camera != null && _camera.TryGetComponent<CameraFollow>(out var f))
                f.SetTarget(_player.transform);

            UpdateGrid();
        }

        private void Update()
        {
            if (!Application.isPlaying || _player == null) return;

            Vector2Int currentCenter = _grid.WorldToGrid(_player.transform.position);
            if (currentCenter != _playerPos)
            {
                _playerPos = currentCenter;
                UpdateGrid();
            }

            if (Input.GetMouseButtonDown(0)) HandleInput();
        }

        private void UpdateGrid()
        {
            Vector2Int center = _playerPos;

            var activeKeys = _activeCells.Keys.ToList();
            foreach (var p in activeKeys)
            {
                if (Vector2Int.Distance(p, center) > _viewRadius + _cleanupBuffer)
                {
                    if (_activeCells[p] != null) Destroy(_activeCells[p].gameObject);
                    _activeCells.Remove(p);

                    if (_staticObstacles.TryGetValue(p, out var s) && s != null) s.gameObject.SetActive(false);
                    if (_dynamicObstacles.TryGetValue(p, out var d) && d != null) d.gameObject.SetActive(false);
                }
            }

            for (int x = center.x - _viewRadius; x <= center.x + _viewRadius; x++)
            {
                for (int y = center.y - _viewRadius; y <= center.y + _viewRadius; y++)
                {
                    Vector2Int p = new Vector2Int(x, y);
                    if (!_activeCells.ContainsKey(p)) RenderCoordinate(p);
                }
            }

            RebuildHashes();
            SyncHighlights();
        }

        private void RenderCoordinate(Vector2Int p)
        {
            GameObject tileObj = Instantiate(_cellPrefab, _grid.GridToWorld(p), Quaternion.identity, transform);
            GridCell cellLogic = tileObj.GetComponent<GridCell>();
            _activeCells[p] = cellLogic;

            float n = Mathf.PerlinNoise(p.x * _noiseScale + _noiseOffset.x, p.y * _noiseScale + _noiseOffset.y);

            if (_cellSprites != null && _cellSprites.Length > 0)
            {
                var sr = tileObj.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    int index = Mathf.Clamp(Mathf.FloorToInt(n * _cellSprites.Length), 0, _cellSprites.Length - 1);
                    sr.sprite = _cellSprites[index];
                    if (cellLogic != null) cellLogic.UpdateBaseColor(sr.color);
                }
            }

            if (_staticObstacles.TryGetValue(p, out var sObs) && sObs != null)
            {
                sObs.gameObject.SetActive(true);
                return;
            }

            if (_dynamicObstacles.TryGetValue(p, out var dObs) && dObs != null)
            {
                dObs.gameObject.SetActive(true);
                return;
            }

            if (_generatedCoords.Contains(p)) return;

            _generatedCoords.Add(p);
            if (_destroyedCoords.Contains(p)) return;

            if (n < _obstacleDensity) SpawnNewObstacle(p, n);
        }

        private void SpawnNewObstacle(Vector2Int p, float nValue)
        {
            bool destructible = nValue < _obstacleDensity * 0.6f;
            GameObject prefab = destructible ? _rockPrefab : _wallPrefab;
            if (prefab == null) return;

            GameObject obj = Instantiate(prefab, (Vector3)_grid.GridToWorld(p) + Vector3.forward * -0.1f, Quaternion.identity, transform);
            Obstacle obs = obj.GetComponent<Obstacle>() ?? obj.AddComponent<Obstacle>();
            obs.IsDestructible = destructible;
            obs.GridPos = p;

            if (destructible) _dynamicObstacles[p] = obs;
            else _staticObstacles[p] = obs;
        }

        private void HandleInput()
        {
            if (_camera == null) return;
            RaycastHit2D hit = Physics2D.GetRayIntersection(_camera.ScreenPointToRay(Input.mousePosition));
            if (!hit.collider) return;

            Vector2Int target = _grid.WorldToGrid(hit.point);

            if (IsValidMove(target))
            {
                _player.transform.position = (Vector3)_grid.GridToWorld(target) + Vector3.back;
                ProcessTurn();
            }
            else if (_dynamicObstacles.TryGetValue(target, out var obs) && obs != null)
            {
                if (Vector2Int.Distance(_playerPos, target) < 1.5f)
                {
                    _dynamicObstacles.Remove(target);
                    _destroyedCoords.Add(target);
                    obs.DestroyObstacle();
                    ProcessTurn();
                }
            }
        }

        private void ProcessTurn()
        {
            if (_enableObstacleMovement)
            {
                Vector2Int[] dirs = {
                    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                    new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,1), new Vector2Int(-1,-1)
                };

                var obstacles = _dynamicObstacles.ToArray();

                foreach (var (position, obs) in obstacles)
                {
                    Assert.IsNotNull(obs);

                    int dirIndex = UnityEngine.Random.Range(0, dirs.Length);
                    Vector2Int move = dirs[dirIndex];
                    Vector2Int newPos = obs.GridPos + move;

                    if (IsCellFree(newPos))
                    {
                        _dynamicObstacles.Remove(position);
                        obs.GridPos = newPos;
                        obs.transform.position = (Vector3)_grid.GridToWorld(newPos) + Vector3.forward * -0.1f;
                        _dynamicObstacles.Add(newPos, obs);
                    }
                }
            }

            RebuildHashes();
            SyncHighlights();
        }

        private bool IsCellFree(Vector2Int p)
        {
            if (_staticHash.Has(p)) return false;
            if (_dynamicObstacles.ContainsKey(p)) return false;
            if (p == _playerPos) return false;
            return true;
        }

        private void RebuildHashes()
        {
            _staticHash.Clear();
            _dynamicHash.Clear();

            foreach (var kvp in _staticObstacles)
                if (kvp.Value != null && kvp.Value.gameObject.activeSelf)
                    _staticHash.Insert(kvp.Key);

            foreach (var kvp in _dynamicObstacles)
                if (kvp.Value != null && kvp.Value.gameObject.activeSelf)
                    _dynamicHash.Insert(kvp.Key);
        }

        private bool IsValidMove(Vector2Int t)
        {
            if (_staticHash.Has(t) || _dynamicHash.Has(t)) return false;

            int dx = Mathf.Abs(t.x - _playerPos.x);
            int dy = Mathf.Abs(t.y - _playerPos.y);

            if ((dx == 0 && dy == 0) || dx > _maxMoveDist || dy > _maxMoveDist) return false;
            if (!_allowDiagonal && dx != 0 && dy != 0) return false;

            return true;
        }

        private void SyncHighlights()
        {
            foreach (var kvp in _activeCells)
                if (kvp.Value != null)
                    kvp.Value.SetAllowed(IsValidMove(kvp.Key));
        }

        private void ClearGridData()
        {
            _activeCells.Clear();
            _staticObstacles.Clear();
            _dynamicObstacles.Clear();
        }

        [Button]
        private void ClearGrid()
        {
            var children = transform.Cast<Transform>().ToList();
            foreach (var child in children) DestroyImmediate(child.gameObject);
            ClearGridData();
        }
    }
}