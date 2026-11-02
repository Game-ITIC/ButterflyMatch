using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    /// <summary>Entry points for small UI feedback animations. All scale work goes through <see cref="UIScaleFeel"/>.</summary>
    public static class UIFeel
    {
        private const float DefaultStaggerStep = 0.04f;
        private const float DefaultStaggerMaxDelay = 0.24f;

        public static UIScaleFeel Scale(Component target)
        {
            if(target == null)
            {
                return null;
            }

            if(!target.TryGetComponent(out UIScaleFeel feel))
            {
                feel = target.gameObject.AddComponent<UIScaleFeel>();
            }

            return feel;
        }

        public static void Punch(Component target, float strength = 0.12f, float duration = 0.3f)
        {
            if(IsVisible(target))
            {
                Scale(target).Punch(strength, duration);
            }
        }

        public static void PopIn(Component target, float delay = 0f, float from = 0.85f, float duration = 0.32f)
        {
            if(IsVisible(target))
            {
                Scale(target).PopIn(delay, from, duration);
            }
        }

        public static void SetBreathing(Component target, bool enabled, float amount = 0.04f, float period = 1.2f)
        {
            if(target == null)
            {
                return;
            }

            if(!enabled && !target.TryGetComponent(out UIScaleFeel _))
            {
                return;
            }

            Scale(target).SetBreathing(enabled, amount, period);
        }

        /// <summary>Pops the active direct children of <paramref name="container"/> in one after another.</summary>
        public static void StaggerChildren(Transform container, float from = 0.85f)
        {
            if(container == null)
            {
                return;
            }

            var count = 0;
            for(var i = 0; i < container.childCount; i++)
            {
                if(container.GetChild(i).gameObject.activeSelf)
                {
                    count++;
                }
            }

            var step = StaggerStep(count);
            var index = 0;
            for(var i = 0; i < container.childCount; i++)
            {
                var child = container.GetChild(i);
                if(!child.gameObject.activeSelf)
                {
                    continue;
                }

                PopIn(child, index * step, from);
                index++;
            }
        }

        public static void Stagger<T>(IReadOnlyList<T> items, float from = 0.85f) where T : Component
        {
            if(items == null)
            {
                return;
            }

            var step = StaggerStep(items.Count);
            var index = 0;
            for(var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if(item == null || !item.gameObject.activeSelf)
                {
                    continue;
                }

                PopIn(item, index * step, from);
                index++;
            }
        }

        // Keep the whole cascade inside the panel open time so late items never sit visibly shrunk.
        private static float StaggerStep(int count)
        {
            return count > 1 ? Mathf.Min(DefaultStaggerStep, DefaultStaggerMaxDelay / (count - 1)) : 0f;
        }

        private static bool IsVisible(Component target)
        {
            return target != null && target.gameObject.activeInHierarchy;
        }
    }
}
