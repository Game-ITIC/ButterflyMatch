using System.Collections.Generic;

// Owns pending reservations and applies counters when a flight finishes.
public sealed class CollectedTargetProgressTracker
{
    readonly LevelManager _levelManager;
    readonly Dictionary<int, int> _pendingByTarget = new();

    public CollectedTargetProgressTracker(LevelManager levelManager)
    {
        _levelManager = levelManager;
    }

    public bool HasAvailableCount(int counterIndex)
    {
        if (_levelManager == null || counterIndex < 0 || counterIndex >= _levelManager.ingrTarget.Count)
        {
            return false;
        }

        _pendingByTarget.TryGetValue(counterIndex, out int pending);
        return _levelManager.ingrTarget[counterIndex].count - pending > 0;
    }

    // Defused bombs still flying to the counter (not yet in TargetBombs).
    public int PendingBombs { get; private set; }

    public void Reserve(CollectedTargetFlyRequest request)
    {
        if (request.IncrementBombCounterOnArrival)
        {
            PendingBombs++;
        }

        Reserve(request.CounterIndex);
    }

    public void Reserve(int counterIndex)
    {
        _pendingByTarget.TryGetValue(counterIndex, out int pending);
        _pendingByTarget[counterIndex] = pending + 1;
    }

    public void Complete(CollectedTargetFlyRequest request, bool arrived)
    {
        Release(request.CounterIndex);
        if (request.IncrementBombCounterOnArrival && PendingBombs > 0)
        {
            PendingBombs--;
        }

        // The piece is already gone from the board, so it counts even if its flight was cut short;
        // dropping it lost goal progress for good (a defused bomb was neither counted nor respawned).
        if (_levelManager == null)
        {
            return;
        }

        DecrementTargetCounter(request.CounterIndex);

        if (request.IncrementBombCounterOnArrival)
        {
            _levelManager.TargetBombs++;
        }
    }

    void DecrementTargetCounter(int counterIndex)
    {
        if (counterIndex >= 0 && counterIndex < _levelManager.ingrTarget.Count &&
            _levelManager.ingrTarget[counterIndex].count > 0)
        {
            _levelManager.ingrTarget[counterIndex].count--;
        }
    }

    void Release(int counterIndex)
    {
        if (!_pendingByTarget.TryGetValue(counterIndex, out int pending))
        {
            return;
        }

        if (pending <= 1)
        {
            _pendingByTarget.Remove(counterIndex);
        }
        else
        {
            _pendingByTarget[counterIndex] = pending - 1;
        }
    }
}
