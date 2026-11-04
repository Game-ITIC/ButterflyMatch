using System.Collections;
using JuiceFresh.States;
using UnityEngine;

/// <summary>
/// No chain is left on the board: shows "No more matches", rebuilds the pieces and returns to Playing.
/// </summary>
public class RegenLevelState : GameStateBase
{
    private const float MessageSeconds = 1f;

    public RegenLevelState(LevelManager levelManager) : base(levelManager)
    {
    }

    public override void EnterState()
    {
        levelManager.StartCoroutine(RegenLevelProcess());
    }

    public override void UpdateState()
    {
    }

    public override void ExitState()
    {
        Transform noMoreMatchesMessage = GameSceneUi.FindInLevelCanvas(GameSceneUi.NoMoreMatches);
        if (noMoreMatchesMessage != null && noMoreMatchesMessage.gameObject.activeSelf)
            noMoreMatchesMessage.gameObject.SetActive(false);
    }

    private IEnumerator RegenLevelProcess()
    {
        yield return new WaitForSeconds(MessageSeconds);

        RegenerateBoard();

        // Entering Playing also tops the bombs back up (PlayingState.EnterState -> InitBombs).
        levelManager.gameStatus = GameState.Playing;
    }

    private void RegenerateBoard()
    {
        levelManager.itemsHided = false;
        levelManager.DragBlocked = true;

        levelManager.BoardItems.DestroyItems(withoutEffects: true);
        levelManager.GenerateNewItems(false);

        levelManager.DragBlocked = false;
        LevelManager.TriggerOnLevelLoaded();
    }
}
