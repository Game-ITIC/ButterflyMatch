using System;
using Cysharp.Threading.Tasks;
using Models;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using System.Threading;

namespace Services
{
    public class RegionUpgradeService : IDisposable
    {
        private PlayableGraph playableGraph;
        private AnimationPlayableOutput playableOutput;
        private AnimationClipPlayable clipPlayable;

        private RegionModel _regionModel;
        public Animator animator;
        public AnimationClip animationClip;

        private CancellationTokenSource _cancellationTokenSource;

        public void Initialize(RegionModel regionModel)
        {
            _regionModel = regionModel;

            if (regionModel?.RegionService?.ActiveRegion == null)
            {
                Debug.LogWarning("[RegionUpgradeService] Cannot initialize: ActiveRegion is null!");
                return;
            }

            if (playableGraph.IsValid())
            {
                playableGraph.Destroy();
            }

            var activeRegion = regionModel.RegionService.ActiveRegion;
            animator = activeRegion.animator;
            if (animator == null)
            {
                animator = activeRegion.GetComponent<Animator>() ??
                           activeRegion.GetComponentInChildren<Animator>(true);
                activeRegion.animator = animator;
            }

            animationClip = activeRegion.animationClip;

            if (animator == null || animationClip == null)
            {
                Debug.LogWarning($"[RegionUpgradeService] Missing animator ({animator}) or animationClip ({animationClip}) on active region {activeRegion.gameObject.name}!");
                return;
            }

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            playableGraph = PlayableGraph.Create("RegionUpgradeGraph");
            playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);

            clipPlayable = AnimationClipPlayable.Create(playableGraph, animationClip);
            clipPlayable.SetSpeed(0);
            playableOutput.SetSourcePlayable(clipPlayable);

            if (animator != null) animator.speed = 1f;
            playableGraph.Play();
            Debug.Log($"[RegionUpgradeService] ✅ Initialized PlayableGraph for {activeRegion.gameObject.name} (Animator: {animator.gameObject.name}, Clip: {animationClip.name})");
        }

        public bool IsPlaying { get; private set; }

        public bool IsReady => playableGraph.IsValid() && clipPlayable.IsValid() && animationClip != null;

        public void SetWriting(bool writing)
        {
            if (animator != null)
            {
                animator.keepAnimatorStateOnDisable = true;
                animator.enabled = true;
            }

            if (!playableOutput.IsOutputValid())
            {
                return;
            }

            playableOutput.SetWeight(writing ? 1f : 0f);
        }

        public void HoldPose()
        {
            SetWriting(false);
            if (animator == null)
            {
                return;
            }

            animator.keepAnimatorStateOnDisable = true;
            animator.enabled = false;
        }

        public void CancelPlayback()
        {
            _cancellationTokenSource?.Cancel();
        }

        [Button]
        public void JumpToFrame(int frame)
        {
            if (!playableGraph.IsValid() || !clipPlayable.IsValid() || animationClip == null)
            {
                Debug.LogWarning($"[RegionUpgradeService] Cannot JumpToFrame({frame}): PlayableGraph or AnimationClip is invalid.");
                return;
            }

            _cancellationTokenSource?.Cancel();
            SeekFrame(frame);
            Debug.Log($"[RegionUpgradeService] ⏩ Jumped to frame {frame} ({FrameToTime(frame):F2}s) [Clip: {animationClip.name}]");
        }

        public void SeekFrame(int frame)
        {
            if (!IsReady)
            {
                return;
            }

            clipPlayable.SetTime(FrameToTime(frame));
            clipPlayable.SetSpeed(0);
            playableGraph.Evaluate();
        }

        [Button]
        public async UniTask PlayToFrame(int targetFrame, Action afterEvaluate = null)
        {
            if (!playableGraph.IsValid() || !clipPlayable.IsValid() || animationClip == null)
            {
                Debug.LogWarning($"[RegionUpgradeService] Cannot PlayToFrame({targetFrame}): PlayableGraph or AnimationClip is invalid.");
                return;
            }

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            float frameRate = animationClip.frameRate;
            double targetTime = targetFrame / frameRate;
            double currentTime = clipPlayable.GetTime();
            int currentFrame = Mathf.RoundToInt((float)(currentTime * frameRate));

            Debug.Log($"[RegionUpgradeService] ▶️ START animation: frame {currentFrame} ({currentTime:F2}s) ➔ frame {targetFrame} ({targetTime:F2}s) [Clip: {animationClip.name}, FPS: {frameRate}]");

            IsPlaying = true;
            try
            {
                if (animator != null) animator.speed = 1f;
                clipPlayable.SetSpeed(1);
                await StopAtFrame(targetTime, token, afterEvaluate);

                if (!playableGraph.IsValid() || !clipPlayable.IsValid()) return;

                double finalTime = clipPlayable.GetTime();
                int finalFrame = Mathf.RoundToInt((float)(finalTime * frameRate));
                Debug.Log($"[RegionUpgradeService] ⏹️ END animation: reached frame {finalFrame} ({finalTime:F2}s, target was {targetFrame})");
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[RegionUpgradeService] ⏹️ Animation cancelled at target frame {targetFrame}");
            }
            finally
            {
                IsPlaying = false;
            }
        }

        private double FrameToTime(int frame)
        {
            return frame / animationClip.frameRate;
        }

        private async UniTask StopAtFrame(double targetTime, CancellationToken cancellationToken, Action afterEvaluate)
        {
            var timing = afterEvaluate == null ? PlayerLoopTiming.Update : PlayerLoopTiming.PostLateUpdate;
            while (playableGraph.IsValid() && clipPlayable.IsValid() && clipPlayable.GetTime() < targetTime)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(timing, cancellationToken);
                if (!playableGraph.IsValid() || !clipPlayable.IsValid())
                {
                    return;
                }

                afterEvaluate?.Invoke();
            }

            if (playableGraph.IsValid() && clipPlayable.IsValid())
            {
                clipPlayable.SetTime(targetTime);
                clipPlayable.SetSpeed(0);
                playableGraph.Evaluate();
                afterEvaluate?.Invoke();
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            if (playableGraph.IsValid())
            {
                playableGraph.Destroy();
            }
        }
    }
}