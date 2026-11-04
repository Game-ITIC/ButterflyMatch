#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Utils.DebugTools
{
    /// <summary>The round CHEAT button: opens the panel on tap, can be dragged, and remembers its spot.</summary>
    internal sealed class CheatFloatingButton
    {
        private const string PosXKey = "cheat-panel-pos-x";
        private const string PosYKey = "cheat-panel-pos-y";

        private readonly RectTransform _safeRoot;

        public CheatFloatingButton(CheatUi ui, RectTransform safeRoot, Action open)
        {
            _safeRoot = safeRoot;

            Rect = CheatUi.NewRect("Floating Button", safeRoot);
            Rect.anchorMin = Rect.anchorMax = new Vector2(0.5f, 0.5f);
            Rect.sizeDelta = new Vector2(132f, 132f);

            var image = ui.AddImage(Rect, CheatUi.FloatingColor);
            image.pixelsPerUnitMultiplier = 0.5f; // 32 px corners at 2x: a full circle at this size.

            var label = CheatUi.AddText(Rect, "CHEAT", 30f, Color.white);
            label.fontStyle = FontStyles.Bold;

            var floating = Rect.gameObject.AddComponent<CheatFloatingButtonDrag>();
            floating.Clicked = open;
            floating.Dropped = SavePosition;
        }

        public RectTransform Rect { get; }

        public void RestorePosition()
        {
            var size = _safeRoot.rect.size;
            var position = new Vector2(
                (PlayerPrefs.GetFloat(PosXKey, 1f) - 0.5f) * size.x,
                (PlayerPrefs.GetFloat(PosYKey, 0.68f) - 0.5f) * size.y);
            Rect.DOKill();
            Rect.anchoredPosition = CheatUi.ClampToSafeArea(position, Rect, _safeRoot);
        }

        private void SavePosition(Vector2 position)
        {
            var size = _safeRoot.rect.size;
            if(size.x > 0f && size.y > 0f)
            {
                PlayerPrefs.SetFloat(PosXKey, position.x / size.x + 0.5f);
                PlayerPrefs.SetFloat(PosYKey, position.y / size.y + 0.5f);
                PlayerPrefs.Save();
            }
        }
    }

    /// <summary>Tap opens the panel; a drag moves the button, which then sticks to the nearest screen side.</summary>
    internal sealed class CheatFloatingButtonDrag : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float EdgeSnapSeconds = 0.15f;

        public Action Clicked;
        public Action<Vector2> Dropped;

        private RectTransform Rect => (RectTransform)transform;
        private RectTransform Parent => (RectTransform)transform.parent;

        public void OnPointerClick(PointerEventData eventData)
        {
            if(!eventData.dragging)
            {
                Clicked?.Invoke();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Rect.DOKill();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(Parent, eventData.position, null, out var local))
            {
                Rect.anchoredPosition = CheatUi.ClampToSafeArea(local, Rect, Parent);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            var position = Rect.anchoredPosition;
            var halfX = Mathf.Max(0f, (Parent.rect.width - Rect.rect.width) * 0.5f);
            position.x = position.x >= 0f ? halfX : -halfX;
            Rect.DOAnchorPos(position, EdgeSnapSeconds).SetUpdate(true).SetLink(gameObject);
            Dropped?.Invoke(position);
        }
    }
}
#endif
