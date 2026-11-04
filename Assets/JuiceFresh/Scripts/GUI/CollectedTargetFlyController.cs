using UnityEngine;

// Entry point: coordinates request resolution, progress updates and visual animation.
public sealed class CollectedTargetFlyController : MonoBehaviour
{
    [Header("Flight animation")]
    [SerializeField, Min(0.05f)] float minFlightDuration = 0.55f;
    [SerializeField, Min(0.05f)] float maxFlightDuration = 0.75f;
    [SerializeField, Min(0f)] float arcHeight = 1.5f;
    [SerializeField] float rotationSpeed = 720f;
    [SerializeField] string sortingLayerName = "UI";
    [SerializeField] int sortingOrder = 100;

    LevelManager _levelManager;
    CollectedTargetProgressTracker _progressTracker;
    CollectedTargetFlyRequestResolver _requestResolver;
    CollectedTargetFlightAnimator _flightAnimator;

    public bool IsFlying => _flightAnimator != null && _flightAnimator.IsFlying;
    public int PendingBombs => _progressTracker != null ? _progressTracker.PendingBombs : 0;

    public void Initialize(LevelManager levelManager)
    {
        EnsureFlightAnimator();
        _flightAnimator.CancelAll();

        _levelManager = levelManager;
        _progressTracker = new CollectedTargetProgressTracker(levelManager);
        _requestResolver = new CollectedTargetFlyRequestResolver(levelManager, _progressTracker);

        _flightAnimator.Configure(
            minFlightDuration,
            maxFlightDuration,
            arcHeight,
            rotationSpeed,
            sortingLayerName,
            sortingOrder);
    }

    public bool TryFly(GameObject collectedObject)
    {
        if (_requestResolver == null || collectedObject == null ||
            !_requestResolver.TryResolve(collectedObject, out CollectedTargetFlyRequest request))
        {
            return false;
        }

        _progressTracker.Reserve(request);
        _flightAnimator.Play(request, OnFlightFinished);
        return true;
    }

    void OnDisable()
    {
        if (_flightAnimator != null)
        {
            _flightAnimator.CancelAll();
        }
    }

    void EnsureFlightAnimator()
    {
        if (_flightAnimator == null)
        {
            _flightAnimator = GetComponent<CollectedTargetFlightAnimator>();
        }

        if (_flightAnimator == null)
        {
            _flightAnimator = gameObject.AddComponent<CollectedTargetFlightAnimator>();
        }
    }

    void OnFlightFinished(CollectedTargetFlyRequest request, bool arrived)
    {
        _progressTracker?.Complete(request, arrived);

        if (_levelManager != null && _levelManager.gameStatus == GameState.Playing)
        {
            _levelManager.CheckWinLose();
        }
    }
}
