using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Press squash / release bounce for a button. A tap on a non-interactable button plays a small wiggle.
    /// Scale is driven through <see cref="UIScaleFeel"/> so it composes with breathing and punches.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIScaleFeel))]
    public sealed class UIButtonFeel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float LargeSurfaceAreaRatio = 0.3f;

        [SerializeField] private Selectable selectable;
        [SerializeField] private bool wiggleWhenDisabled = true;

        private UIScaleFeel scaleFeel;
        private bool pressed;
        private bool surfaceChecked;
        private bool isLargeSurface;

        private UIScaleFeel ScaleFeel
        {
            get
            {
                if(scaleFeel == null)
                {
                    scaleFeel = GetComponent<UIScaleFeel>();
                }

                return scaleFeel;
            }
        }

        private void Awake()
        {
            if(selectable == null)
            {
                selectable = GetComponent<Selectable>();
            }
        }

        private void OnDisable()
        {
            pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if(eventData.button != PointerEventData.InputButton.Left || IsLargeSurface())
            {
                return;
            }

            if(!IsInteractable())
            {
                if(wiggleWhenDisabled)
                {
                    ScaleFeel.Wiggle();
                }

                return;
            }

            pressed = true;
            ScaleFeel.Press();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ReleaseIfPressed();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ReleaseIfPressed();
        }

        /// <summary>Adds press feedback to every button under <paramref name="root"/> that does not animate itself.</summary>
        public static void InstallInChildren(Component root)
        {
            if(root == null)
            {
                return;
            }

            var buttons = root.GetComponentsInChildren<Button>(true);
            for(var i = 0; i < buttons.Length; i++)
            {
                Ensure(buttons[i]);
            }
        }

        public static UIButtonFeel Ensure(Selectable target)
        {
            if(target == null)
            {
                return null;
            }

            if(target.TryGetComponent(out UIButtonFeel feel))
            {
                return feel;
            }

            if(!CanInstall(target))
            {
                return null;
            }

            feel = target.gameObject.AddComponent<UIButtonFeel>();
            feel.selectable = target;
            return feel;
        }

        private static bool CanInstall(Selectable target)
        {
            // Legacy buttons that already animate their own transform keep their animation.
            return target.transition != Selectable.Transition.Animation
                   && !target.TryGetComponent(out Animator _)
                   && !target.TryGetComponent(out Animation _)
                   && !target.TryGetComponent(out UIScaleTweenAnimation _)
                   && !target.TryGetComponent(out UIPopupOpenTweenAnimation _)
                   && !target.TryGetComponent(out UITransientTweenAnimation _);
        }

        private void ReleaseIfPressed()
        {
            if(!pressed)
            {
                return;
            }

            pressed = false;
            ScaleFeel.Release();
        }

        private bool IsInteractable()
        {
            return selectable == null || (selectable.isActiveAndEnabled && selectable.IsInteractable());
        }

        // Full-screen tap-to-close blockers are buttons too; scaling them would reveal the screen edges.
        private bool IsLargeSurface()
        {
            if(surfaceChecked)
            {
                return isLargeSurface;
            }

            surfaceChecked = true;

            var rectTransform = transform as RectTransform;
            var canvas = GetComponentInParent<Canvas>();
            var canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            if(rectTransform == null || canvasRect == null)
            {
                return isLargeSurface;
            }

            var size = Vector2.Scale(rectTransform.rect.size, rectTransform.lossyScale);
            var canvasSize = Vector2.Scale(canvasRect.rect.size, canvasRect.lossyScale);
            var canvasArea = canvasSize.x * canvasSize.y;

            isLargeSurface = canvasArea > 0f && size.x * size.y / canvasArea > LargeSurfaceAreaRatio;
            return isLargeSurface;
        }
    }
}
