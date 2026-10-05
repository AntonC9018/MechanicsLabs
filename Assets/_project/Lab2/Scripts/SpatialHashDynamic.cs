using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class SpatialHashDynamic
    {
        private float _cellSize;
        private HashSet<Vector2Int> _table = new();

        public SpatialHashDynamic(float cellSize) => _cellSize = cellSize;
        public void Clear() => _table.Clear();
        public void Insert(Vector2Int pos) => _table.Add(pos);
        public bool Has(Vector2Int pos) => _table.Contains(pos);
    }
}