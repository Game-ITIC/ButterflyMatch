using System;
using DG.Tweening;
using UnityEngine;

// Owns every transform tween used by an Item. Gameplay code only chooses a visual state.
public sealed class ItemTweenAnimator : IDisposable
{
    const float AppearDuration = 0.3f;
    const float DestroyDuration = 0.25f;
    const float DisappearDuration = 0.32f;

    readonly GameObject _owner;
    readonly Transform _visual;
    readonly Vector3 _restPosition;
    readonly Vector3 _restScale;

    Tween _actionTween;
    Tween _idleTween;
    bool _ambientEnabled;
    bool _strongAmbient;

    public ItemTweenAnimator(GameObject owner, Transform visual)
    {
        _owner = owner;
        _visual = visual;
        _restPosition = visual.localPosition;
        _restScale = visual.localScale;
    }

    public void PlayAppear(Action completed)
    {
        StopAll(resetVisual: true);
        _visual.localScale = Vector3.Scale(_restScale, Vector3.one * 0.25f);

        _actionTween = _visual
            .DOScale(_restScale, AppearDuration)
            .SetEase(Ease.OutCubic)
            .SetTarget(_owner)
            .OnComplete(() =>
            {
                _actionTween = null;
                completed?.Invoke();
            });
    }

    public void PlaySelected()
    {
        _ambientEnabled = false;
        KillTrackedTween(ref _idleTween);
        KillTrackedTween(ref _actionTween);
        ResetVisual();

        var sequence = DOTween.Sequence().SetTarget(_owner);
        // The peak has to stay inside the cell.
        sequence.Append(_visual.DOScale(Scaled(1.18f, 1.12f), 0.4f).SetEase(Ease.OutCubic));
        sequence.Append(_visual.DOScale(Scaled(0.96f, 1.02f), 0.55f).SetEase(Ease.InOutSine));
        sequence.Append(_visual.DOScale(_restScale, 0.38f).SetEase(Ease.OutCubic));
        sequence.SetLoops(-1, LoopType.Restart);
        _idleTween = sequence;
    }

    public void PlayNeutral(bool ambient, bool strongAmbient = false)
    {
        if (ambient)
        {
            PlayAmbient(strongAmbient);
            return;
        }

        StopAll(resetVisual: true);
    }

    public void PlayAmbient(bool strong = false)
    {
        _ambientEnabled = true;
        _strongAmbient = strong;
        KillTrackedTween(ref _idleTween);
        KillTrackedTween(ref _actionTween);
        ResetVisual();

        var sequence = DOTween.Sequence().SetTarget(_owner);
        if (strong)
        {
            sequence.Append(_visual.DOScale(Scaled(1.15f, 1.15f), 0.2f).SetEase(Ease.OutSine));
            sequence.Append(_visual.DOScale(_restScale, 0.2f).SetEase(Ease.InSine));
            sequence.Append(_visual.DOScale(Scaled(1.15f, 1.15f), 0.17f).SetEase(Ease.OutSine));
            sequence.Append(_visual.DOScale(_restScale, 0.18f).SetEase(Ease.InSine));
            sequence.AppendInterval(0.75f);
        }
        else
        {
            sequence.Append(_visual.DOScale(Scaled(0.95f, 1.05f), 0.9f).SetEase(Ease.InOutSine));
            sequence.Append(_visual.DOScale(_restScale, 0.9f).SetEase(Ease.InOutSine));
        }

        sequence.SetLoops(-1, LoopType.Restart);
        _idleTween = sequence;
    }

    public void PlayHint()
    {
        bool resumeAmbient = _ambientEnabled;
        bool strongAmbient = _strongAmbient;
        KillTrackedTween(ref _idleTween);
        KillTrackedTween(ref _actionTween);
        ResetVisual();

        var sequence = DOTween.Sequence().SetTarget(_owner);
        sequence.Append(_visual.DOScale(Scaled(1.15f, 1.15f), 0.3f).SetEase(Ease.OutSine));
        sequence.Append(_visual.DOScale(_restScale, 0.3f).SetEase(Ease.InSine));
        sequence.Append(_visual.DOScale(Scaled(1.15f, 1.15f), 0.3f).SetEase(Ease.OutSine));
        sequence.Append(_visual.DOScale(_restScale, 0.3f).SetEase(Ease.InSine));
        sequence.OnComplete(() =>
        {
            _actionTween = null;
            if (resumeAmbient)
            {
                PlayAmbient(strongAmbient);
            }
        });
        _actionTween = sequence;
    }

    public void PlayLanding()
    {
        bool resumeAmbient = _ambientEnabled;
        bool strongAmbient = _strongAmbient;
        KillTrackedTween(ref _idleTween);
        KillTrackedTween(ref _actionTween);
        ResetVisual();

        var sequence = DOTween.Sequence().SetTarget(_owner);
        sequence.Join(_visual.DOLocalMoveY(_restPosition.y - 0.06f, 0.08f).SetEase(Ease.OutCubic));
        sequence.Join(_visual.DOScale(Scaled(1.1f, 0.9f), 0.08f).SetEase(Ease.OutCubic));
        sequence.Append(_visual.DOLocalMove(_restPosition, 0.18f).SetEase(Ease.OutBack, 1.05f));
        sequence.Join(_visual.DOScale(_restScale, 0.18f).SetEase(Ease.OutBack, 1.05f));
        sequence.OnComplete(() =>
        {
            _actionTween = null;
            if (resumeAmbient)
            {
                PlayAmbient(strongAmbient);
            }
        });
        _actionTween = sequence;
    }

    public void PlayDestroy(Action completed)
    {
        _ambientEnabled = false;
        StopAll(resetVisual: true);
        _actionTween = _visual
            .DOScale(Vector3.zero, DestroyDuration)
            .SetEase(Ease.InCubic)
            .SetTarget(_owner)
            .OnComplete(() =>
            {
                _actionTween = null;
                completed?.Invoke();
            });
    }

    public void PlayDisappear(Action completed)
    {
        _ambientEnabled = false;
        StopAll(resetVisual: true);
        _actionTween = _visual
            .DOScale(Vector3.zero, DisappearDuration)
            .SetEase(Ease.InCubic)
            .SetTarget(_owner)
            .OnComplete(() =>
            {
                _actionTween = null;
                completed?.Invoke();
            });
    }

    public void StopAll(bool resetVisual)
    {
        _ambientEnabled = false;
        KillTrackedTween(ref _idleTween);
        KillTrackedTween(ref _actionTween);

        if (resetVisual)
        {
            ResetVisual();
        }
    }

    public void Dispose()
    {
        StopAll(resetVisual: false);
    }

    Vector3 Scaled(float x, float y)
    {
        return new Vector3(_restScale.x * x, _restScale.y * y, _restScale.z);
    }

    void ResetVisual()
    {
        _visual.localPosition = _restPosition;
        _visual.localScale = _restScale;
        _visual.localRotation = Quaternion.identity;
    }

    static void KillTrackedTween(ref Tween tween)
    {
        if (tween != null && tween.IsActive())
        {
            tween.Kill(complete: false);
        }

        tween = null;
    }
}
