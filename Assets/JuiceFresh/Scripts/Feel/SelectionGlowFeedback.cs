using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JuiceFresh.Feel
{
    /// <summary>
    /// The soft aura copied from a piece's sprite while it is in the chain. On release the aura blooms
    /// (successful move) or shrinks away (invalid move).
    /// </summary>
    public sealed class SelectionGlowFeedback
    {
        private const string AuraName = "Feel_SelectionGlow";

        private readonly MonoBehaviour _host;
        private readonly IGameFeelTuning _tuning;
        private readonly Dictionary<int, GlowState> _active = new Dictionary<int, GlowState>();
        private readonly List<GameObject> _releasing = new List<GameObject>();
        private readonly List<GlowState> _releaseBuffer = new List<GlowState>();

        public SelectionGlowFeedback(MonoBehaviour host, IGameFeelTuning tuning)
        {
            _host = host;
            _tuning = tuning;
        }

        public void Add(Transform target, int chainCount)
        {
            if (target == null)
                return;

            int id = target.GetInstanceID();
            if (_active.ContainsKey(id))
                return;

            Item item = target.GetComponent<Item>();
            SpriteRenderer source = item != null && item.sprRenderer != null
                ? item.sprRenderer
                : target.GetComponentInChildren<SpriteRenderer>();
            if (source == null || source.sprite == null)
                return;

            GameObject auraObject = new GameObject(AuraName);
            auraObject.layer = source.gameObject.layer;
            auraObject.transform.SetParent(source.transform.parent, false);
            auraObject.transform.localPosition = source.transform.localPosition;
            auraObject.transform.localRotation = source.transform.localRotation;
            auraObject.transform.localScale = source.transform.localScale * _tuning.SelectionGlowScale;

            SpriteRenderer aura = auraObject.AddComponent<SpriteRenderer>();
            aura.sprite = source.sprite;
            aura.sharedMaterial = source.sharedMaterial;
            aura.sortingLayerID = source.sortingLayerID;
            aura.sortingOrder = source.sortingOrder + 2;
            aura.flipX = source.flipX;
            aura.flipY = source.flipY;

            Color glowColor = GlowColor(chainCount);
            aura.color = glowColor;

            var state = new GlowState(target, source, auraObject, aura, source.transform.localScale,
                auraObject.transform.localRotation, glowColor);
            state.Coroutine = _host.StartCoroutine(BreatheRoutine(state, chainCount));
            _active[id] = state;
        }

        public void Remove(Transform target, bool successfulRelease)
        {
            if (target == null)
                return;

            int id = target.GetInstanceID();
            if (!_active.TryGetValue(id, out GlowState state))
                return;

            _active.Remove(id);
            Release(state, successfulRelease);
        }

        public void ReleaseAll(bool successfulRelease)
        {
            if (_active.Count == 0)
                return;

            _releaseBuffer.Clear();
            _releaseBuffer.AddRange(_active.Values);
            _active.Clear();

            foreach (GlowState state in _releaseBuffer)
                Release(state, successfulRelease);

            _releaseBuffer.Clear();
        }

        /// <summary>Removes every aura at once, without animation.</summary>
        public void DestroyAll()
        {
            foreach (GlowState glow in _active.Values)
            {
                if (glow.AuraObject != null)
                    Object.Destroy(glow.AuraObject);
            }

            _active.Clear();

            for (int i = _releasing.Count - 1; i >= 0; i--)
            {
                if (_releasing[i] != null)
                    Object.Destroy(_releasing[i]);
            }

            _releasing.Clear();
        }

        private Color GlowColor(int chainCount)
        {
            float chainEnergy = Mathf.InverseLerp(1f, 10f, chainCount);
            Color start = _tuning.SelectionGlowColorStart;
            start.a = _tuning.SelectionGlowAlpha;
            Color end = _tuning.SelectionGlowColorEnd;
            end.a = _tuning.SelectionGlowAlpha + 0.08f;
            return Color.Lerp(start, end, chainEnergy);
        }

        private void Release(GlowState state, bool successfulRelease)
        {
            if (state.Coroutine != null)
                _host.StopCoroutine(state.Coroutine);

            _host.StartCoroutine(ReleaseRoutine(state, successfulRelease));
        }

        private IEnumerator BreatheRoutine(GlowState state, int chainCount)
        {
            // Offset by chain position so neighbouring auras don't breathe in sync.
            float elapsed = chainCount * 0.17f;

            while (state.Target != null && state.Source != null && state.AuraObject != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float phase = elapsed * _tuning.SelectionGlowWaveSpeed;
                float wave;

                switch (_tuning.SelectionAuraStyle)
                {
                    case SelectionAuraStyle.Still:
                        wave = 0.35f;
                        break;
                    case SelectionAuraStyle.Heartbeat:
                        float heartbeat = Mathf.Max(0f, Mathf.Sin(phase));
                        wave = heartbeat * heartbeat * heartbeat;
                        break;
                    case SelectionAuraStyle.SlowSpin:
                        wave = Mathf.Sin(phase * 0.45f) * 0.5f + 0.5f;
                        state.AuraObject.transform.localRotation =
                            state.BaseRotation * Quaternion.Euler(0f, 0f, elapsed * 18f);
                        break;
                    default: // Breathe
                        wave = Mathf.Sin(phase) * 0.5f + 0.5f;
                        break;
                }

                float scale = _tuning.SelectionGlowScale + wave * _tuning.SelectionGlowBreathing;
                state.AuraObject.transform.localScale = state.RestScale * scale;
                state.Aura.sprite = state.Source.sprite;

                Color color = state.GlowColor;
                color.a *= 0.78f + wave * 0.22f;
                state.Aura.color = color;
                yield return null;
            }
        }

        private IEnumerator ReleaseRoutine(GlowState state, bool successfulRelease)
        {
            if (state.AuraObject == null || state.Aura == null)
                yield break;

            state.AuraObject.transform.SetParent(null, true);
            _releasing.Add(state.AuraObject);

            Vector3 startScale = state.AuraObject.transform.localScale;
            Vector3 startPosition = state.AuraObject.transform.position;
            float startAlpha = state.Aura.color.a;

            ReleaseShape(successfulRelease, startScale, out Vector3 endScale, out float verticalTravel, out float duration);
            duration *= _tuning.MotionDurationScale;

            float elapsed = 0f;
            while (elapsed < duration && state.AuraObject != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                state.AuraObject.transform.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
                state.AuraObject.transform.position = startPosition + Vector3.up * (verticalTravel * eased);

                Color color = state.Aura.color;
                color.a = Mathf.Lerp(startAlpha, 0f, t * t);
                state.Aura.color = color;
                yield return null;
            }

            _releasing.Remove(state.AuraObject);
            if (state.AuraObject != null)
                Object.Destroy(state.AuraObject);
        }

        private void ReleaseShape(bool successfulRelease, Vector3 startScale,
            out Vector3 endScale, out float verticalTravel, out float duration)
        {
            verticalTravel = 0f;
            duration = 0.18f;

            if (!successfulRelease)
            {
                endScale = startScale * 0.82f;
                return;
            }

            switch (_tuning.GlowReleaseStyle)
            {
                case GlowReleaseStyle.Fade:
                    endScale = startScale * 0.96f;
                    duration = 0.24f;
                    break;
                case GlowReleaseStyle.FloatAway:
                    endScale = startScale * 1.08f;
                    verticalTravel = 0.38f;
                    duration = 0.42f;
                    break;
                case GlowReleaseStyle.Snap:
                    endScale = startScale * 0.12f;
                    verticalTravel = -0.06f;
                    duration = 0.14f;
                    break;
                default: // Bloom
                    endScale = startScale * _tuning.GlowReleasePopScale;
                    verticalTravel = 0.12f;
                    duration = 0.3f;
                    break;
            }
        }

        private sealed class GlowState
        {
            public GlowState(Transform target, SpriteRenderer source, GameObject auraObject, SpriteRenderer aura,
                Vector3 restScale, Quaternion baseRotation, Color glowColor)
            {
                Target = target;
                Source = source;
                AuraObject = auraObject;
                Aura = aura;
                RestScale = restScale;
                BaseRotation = baseRotation;
                GlowColor = glowColor;
            }

            public Transform Target { get; }
            public SpriteRenderer Source { get; }
            public GameObject AuraObject { get; }
            public SpriteRenderer Aura { get; }
            public Vector3 RestScale { get; }
            public Quaternion BaseRotation { get; }
            public Color GlowColor { get; }
            public Coroutine Coroutine { get; set; }
        }
    }
}
