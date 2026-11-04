using System;
using UnityEngine;

namespace UI
{
    public sealed class ComboTextFeedbackView : MonoBehaviour
    {
        [Serializable]
        private struct ComboFeedbackEntry
        {
            [Min(1)] public int minimumCombo;
            public string fallbackName;
            public GameObject target;
        }

        [SerializeField] private Transform searchRoot;
        [SerializeField] private ComboFeedbackEntry[] feedbacks =
        {
            new ComboFeedbackEntry { minimumCombo = 6, fallbackName = "gratzWord1" },
            new ComboFeedbackEntry { minimumCombo = 9, fallbackName = "gratzWord2" },
            new ComboFeedbackEntry { minimumCombo = 12, fallbackName = "gratzWord3" }
        };

        private static ComboTextFeedbackView instance;

        private void Awake()
        {
            instance = this;
            ResolveFeedbackReferences();
        }

        private void OnDestroy()
        {
            if(instance == this)
            {
                instance = null;
            }
        }

        public static void ShowForCombo(int combo)
        {
            if(instance == null)
            {
                return;
            }

            instance.Show(combo);
        }

        public void Show(int combo)
        {
            ResolveFeedbackReferences();

            GameObject feedback = FindBestFeedback(combo);
            if(feedback == null)
            {
                return;
            }

            if(feedback.activeInHierarchy && feedback.TryGetComponent(out UITransientTweenAnimation animation))
            {
                animation.Play();
                return;
            }

            feedback.SetActive(true);
        }

        private GameObject FindBestFeedback(int combo)
        {
            GameObject best = null;
            int bestMinimumCombo = int.MinValue;

            foreach(ComboFeedbackEntry feedback in feedbacks)
            {
                if(feedback.target == null || combo < feedback.minimumCombo || feedback.minimumCombo <= bestMinimumCombo)
                {
                    continue;
                }

                best = feedback.target;
                bestMinimumCombo = feedback.minimumCombo;
            }

            return best;
        }

        private void ResolveFeedbackReferences()
        {
            if(feedbacks == null || feedbacks.Length == 0)
            {
                return;
            }

            Transform root = ResolveSearchRoot();

            for(int i = 0; i < feedbacks.Length; i++)
            {
                ComboFeedbackEntry feedback = feedbacks[i];
                if(feedback.target == null && !string.IsNullOrEmpty(feedback.fallbackName))
                {
                    Transform target = FindChildRecursive(root, feedback.fallbackName);
                    feedback.target = target != null ? target.gameObject : null;
                    feedbacks[i] = feedback;
                }
            }
        }

        private Transform ResolveSearchRoot()
        {
            if(searchRoot != null)
            {
                return searchRoot;
            }

            Transform canvas = GameSceneUi.LevelCanvas;
            if(canvas != null)
            {
                return canvas;
            }

            return transform;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if(root == null)
            {
                return null;
            }

            foreach(Transform child in root)
            {
                if(child.name == childName)
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, childName);
                if(nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
