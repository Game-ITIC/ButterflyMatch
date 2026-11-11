using UnityEngine;
using UnityEngine.UI;

namespace GameTutorials
{
    /// <summary>
    /// Lifts one UI element above the tutorial dim with its own sorting canvas, so it stays bright and tappable.
    /// Undo removes what was added and puts back what was already there.
    /// </summary>
    public sealed class UiRaise
    {
        private RectTransform _target;
        private Canvas _canvas;
        private GraphicRaycaster _raycaster;
        private bool _addedCanvas;
        private bool _addedRaycaster;
        private bool _hadOverride;
        private int _oldLayer;
        private int _oldOrder;

        public void Raise(RectTransform target, int sortingLayerId, int sortingOrder)
        {
            Undo();
            if (target == null)
                return;

            _target = target;
            _canvas = target.GetComponent<Canvas>();
            _addedCanvas = _canvas == null;
            if (_addedCanvas)
                _canvas = target.gameObject.AddComponent<Canvas>();

            _hadOverride = _canvas.overrideSorting;
            _oldLayer = _canvas.sortingLayerID;
            _oldOrder = _canvas.sortingOrder;

            _canvas.overrideSorting = true;
            _canvas.sortingLayerID = sortingLayerId;
            _canvas.sortingOrder = sortingOrder;

            _raycaster = target.GetComponent<GraphicRaycaster>();
            _addedRaycaster = _raycaster == null;
            if (_addedRaycaster)
                _raycaster = target.gameObject.AddComponent<GraphicRaycaster>();
        }

        public void Undo()
        {
            // Nothing raised, or the element was destroyed together with what was added to it.
            if (_target == null)
                return;

            // The raycaster depends on the canvas, so it goes first and right away.
            if (_addedRaycaster && _raycaster != null)
                Object.DestroyImmediate(_raycaster);

            if (_canvas != null)
            {
                if (_addedCanvas)
                {
                    Object.DestroyImmediate(_canvas);
                }
                else
                {
                    _canvas.overrideSorting = _hadOverride;
                    _canvas.sortingLayerID = _oldLayer;
                    _canvas.sortingOrder = _oldOrder;
                }
            }

            _target = null;
            _canvas = null;
            _raycaster = null;
        }
    }
}
