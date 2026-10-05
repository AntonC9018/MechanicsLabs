using UnityEngine;

namespace Core
{
    public class GridCell : MonoBehaviour
    {
        [SerializeField] private Color _highlightColor = Color.cyan;
        [SerializeField] private Color _allowedMoveColor = new Color(0, 1, 0, 0.5f);

        private SpriteRenderer _renderer;
        private Color _originalColor;
        private bool _isAllowedMove;

        private void Awake() => Initialize();

        private void Initialize()
        {
            if (_renderer != null) return;
            _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer != null) _originalColor = _renderer.color;
        }

        public void SetAllowed(bool allowed)
        {
            _isAllowedMove = allowed;
            RefreshVisuals(false);
        }

        private void OnMouseEnter() => RefreshVisuals(true);
        private void OnMouseExit() => RefreshVisuals(false);

        private void RefreshVisuals(bool isHovering)
        {
            Initialize();
            if (_renderer == null) return;
            if (isHovering) _renderer.color = _highlightColor;
            else _renderer.color = _isAllowedMove ? _allowedMoveColor : _originalColor;
        }

        public void UpdateBaseColor(Color newColor)
        {
            _originalColor = newColor;
            RefreshVisuals(false);
        }
    }
}