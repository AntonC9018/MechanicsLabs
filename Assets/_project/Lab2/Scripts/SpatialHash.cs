using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class SpatialHash
    {
        private float _cellSize;
        private Dictionary<Vector2Int, GameObject> _table = new();

        public SpatialHash(float cellSize) => _cellSize = cellSize;
        public void Clear() => _table.Clear();
        public void Insert(Vector2Int gridPos, GameObject obj) => _table[gridPos] = obj;
        public GameObject GetAt(Vector2Int gridPos) => _table.TryGetValue(gridPos, out var obj) ? obj : null;
        public bool IsEmpty(Vector2Int gridPos) => !_table.ContainsKey(gridPos);
    }
}