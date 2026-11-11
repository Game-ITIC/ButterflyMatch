using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLocalization;
using JuiceFresh.States.Playing;
using Models;
using UnityEngine;

namespace GameTutorials
{
    /// <summary>
    /// Runs the in-level tutorials (Docs/GDD.md 12):
    /// level 1 - drag a chain, then the moves counter; level 2 - score and stars; first collect level - the goal;
    /// a booster's unlock level - tap the booster, then a piece. At most one tutorial per level start; each is
    /// shown until finished once. A level that ends mid-tutorial shows it again next time.
    /// </summary>
    public sealed class LevelTutorialDirector : MonoBehaviour
    {
        private const int ChainLength = 3;

        // The level start check of PlayingState runs 1 s in; the board must have its final colours by then.
        private const float SettleTime = 1.3f;

        // Booster tutorials only for players who reach the booster now, not for old saves far past it.
        private const int BoosterLevelWindow = 3;

        private const int LastChainLevel = 3;
        private const int LastGoalsLevel = 6;

        private static readonly BoostType[] BoosterOrder =
            { BoostType.Shovel, BoostType.Energy, BoostType.Bomb, BoostType.ExtraMoves };

        [SerializeField] private TutorialOverlay _overlay;
        [SerializeField] private RectTransform _movesCard;
        [SerializeField] private RectTransform _scoreCard;
        [SerializeField] private RectTransform _goalsCard;

        private readonly BoardDimmer _dimmer = new BoardDimmer();
        private readonly UiRaise _raise = new UiRaise();
        private readonly List<Item> _chain = new List<Item>();
        private readonly List<Vector3> _path = new List<Vector3>();

        private bool _tapped;

        private static LevelManager Level => LevelManager.THIS;

        private void Start()
        {
            _overlay.Tapped += OnTapped;
            RunAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void OnDestroy()
        {
            _overlay.Tapped -= OnTapped;

            // The scene is closing: the raised card and the tinted pieces go with it, only the static gate stays.
            TutorialGate.Clear();
        }

        private void OnTapped() => _tapped = true;

        private async UniTaskVoid RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await WaitForLevelStart(token);

                try
                {
                    await RunFor(Level.currentLevel, token);
                }
                catch (LevelEndedException)
                {
                    // Not marked as done: it comes back on the next try.
                }
                finally
                {
                    Cleanup();
                }

                await UniTask.WaitUntil(() => Level == null || IsEnded(Level.gameStatus), cancellationToken: token);
            }
        }

        private UniTask RunFor(int level, CancellationToken token)
        {
            if (level <= LastChainLevel && !TutorialProgress.IsDone(TutorialProgress.Chain))
                return ChainTutorial(token);

            if (level <= LastChainLevel && !TutorialProgress.IsDone(TutorialProgress.Score) && level >= 2)
                return CardTutorial(TutorialProgress.Score, _scoreCard, L10n.Get("tut_score"), token);

            if (level <= LastGoalsLevel && Level.target != Target.SCORE && !TutorialProgress.IsDone(TutorialProgress.Goals))
                return CardTutorial(TutorialProgress.Goals, _goalsCard, L10n.Get("tut_goals"), token);

            foreach (BoostType type in BoosterOrder)
            {
                int unlockLevel = BoosterUnlocks.UnlockLevel(type);
                if (level >= unlockLevel && level <= unlockLevel + BoosterLevelWindow
                    && !TutorialProgress.IsDone(TutorialProgress.Booster(type)))
                {
                    return BoosterTutorial(type, token);
                }
            }

            return UniTask.CompletedTask;
        }

        #region Tutorials

        private async UniTask ChainTutorial(CancellationToken token)
        {
            while (true)
            {
                await WaitBoardIdle(token);
                if (!FindChain())
                    return;

                int move = Level.moveID;
                TutorialGate.IsShowing = true;
                TutorialGate.RestrictTo(_chain);
                _dimmer.Dim(Level.GameField, _chain);
                _overlay.ShowMessage(L10n.Get("tut_chain"), TutorialOverlay.Place.Top, false);
                _overlay.Swipe(_path);

                // A board rebuild (no more matches) replaces the pieces: the chain is found again.
                await WaitStep(() => Level.moveID != move || !ChainIsAlive(), token);
                bool played = Level.moveID != move;
                Cleanup();

                if (played)
                    break;
            }

            TutorialProgress.MarkDone(TutorialProgress.Chain);

            if (!TutorialProgress.IsDone(TutorialProgress.Moves))
                await CardTutorial(TutorialProgress.Moves, _movesCard, L10n.Get("tut_moves"), token);
        }

        // Dims the screen, lifts one top bar card and waits for a tap anywhere.
        private async UniTask CardTutorial(string id, RectTransform card, string text, CancellationToken token)
        {
            if (card == null || !card.gameObject.activeInHierarchy)
                return;

            await WaitBoardIdle(token);

            TutorialGate.IsShowing = true;
            TutorialGate.BlocksBoard = true;
            _overlay.ShowDim(true);
            _raise.Raise(card, _overlay.SortingLayerId, _overlay.RaisedOrder);
            _overlay.ShowMessage(text, TutorialOverlay.Place.Top, true);
            _tapped = false;

            await WaitStep(() => _tapped, token);
            Cleanup();
            await ReleaseOfTheTap(token);
            TutorialProgress.MarkDone(id);
        }

        private async UniTask BoosterTutorial(BoostType type, CancellationToken token)
        {
            BoostIcon icon = FindBoosterIcon(type);
            BoosterModel model = Level.BoostersProvider.GetBoosterModel(type);
            if (icon == null || model == null || model.Count.Value <= 0)
                return;

            string name = L10n.Get("boost_" + type.ToString().ToLowerInvariant(), type.ToString());
            string description = L10n.Get("boost_desc_" + type.ToString().ToLowerInvariant(), string.Empty);
            int before = model.Count.Value;

            while (true)
            {
                // Step 1: the booster button is the only thing that can be tapped.
                await WaitBoardIdle(token);
                TutorialGate.IsShowing = true;
                TutorialGate.BlocksBoard = true;
                _overlay.ShowDim(true);
                _raise.Raise((RectTransform)icon.transform, _overlay.SortingLayerId, _overlay.RaisedOrder);
                _overlay.ShowMessage(L10n.Format("tut_booster", name, description), TutorialOverlay.Place.Bottom, false);
                _overlay.TapAt((RectTransform)icon.transform);

                await WaitStep(() => Level.activatedBoost == icon || model.Count.Value < before, token);
                Cleanup();
                await ReleaseOfTheTap(token);

                // +5 moves works at once; the board boosters wait for a piece.
                if (model.Count.Value < before)
                    break;

                // Step 2: tap a piece with the armed booster. Cancelling it goes back to step 1.
                Item piece = PieceNearCenter();
                TutorialGate.IsShowing = true;
                _overlay.ShowMessage(L10n.Get("tut_booster_target"), TutorialOverlay.Place.Bottom, false);
                if (piece != null)
                    _overlay.TapAt(piece.transform.position);

                await WaitStep(() => model.Count.Value < before || Level.activatedBoost != icon, token);
                Cleanup();

                if (model.Count.Value < before)
                    break;
            }

            TutorialProgress.MarkDone(TutorialProgress.Booster(type));
        }

        #endregion

        #region Waiting

        private sealed class LevelEndedException : Exception
        {
        }

        private static bool IsEnded(GameState state) =>
            state == GameState.Map || state == GameState.PrepareGame || state == GameState.PrepareBoosts
            || state == GameState.Win || state == GameState.PreWinAnimations || state == GameState.GameOver
            || state == GameState.ToMap || state == GameState.Highscore;

        // The raw activatedBoost field: the ActivatedBoost property returns the empty icon when nothing is armed.
        private static bool IsBoardIdle =>
            Level.gameStatus == GameState.Playing && !Level.DragBlocked && Level.destroyAnyway.Count == 0
            && Level.activatedBoost == null && !Input.GetMouseButton(0);

        private static async UniTask WaitForLevelStart(CancellationToken token)
        {
            await UniTask.WaitUntil(() => Level != null && Level.gameStatus == GameState.Playing, cancellationToken: token);
            await UniTask.Delay(TimeSpan.FromSeconds(SettleTime), cancellationToken: token);
        }

        private async UniTask WaitBoardIdle(CancellationToken token)
        {
            while (!IsBoardIdle)
            {
                ThrowIfEnded();
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        // Waits for the step to be done. Popups and pause hide the layer; the end of the level stops the tutorial.
        private async UniTask WaitStep(Func<bool> done, CancellationToken token)
        {
            while (true)
            {
                ThrowIfEnded();
                _overlay.SetVisible(Level.gameStatus == GameState.Playing);
                if (done())
                    break;

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            _overlay.SetVisible(true);
        }

        // The tap that closed a step must not reach the board as a touch.
        private static async UniTask ReleaseOfTheTap(CancellationToken token)
        {
            TutorialGate.BlocksBoard = true;
            await UniTask.WaitUntil(() => !Input.GetMouseButton(0), cancellationToken: token);
            await UniTask.Yield(PlayerLoopTiming.Update, token);
            TutorialGate.BlocksBoard = false;
        }

        private static void ThrowIfEnded()
        {
            if (Level == null || IsEnded(Level.gameStatus))
                throw new LevelEndedException();
        }

        #endregion

        #region Board

        private bool FindChain()
        {
            _chain.Clear();
            _path.Clear();

            var found = new List<Item>();
            if (!new PossibleMovesDetector(Level).TryFindChain(found))
                return false;

            for (int i = 0; i < found.Count && i < ChainLength; i++)
            {
                _chain.Add(found[i]);
                _path.Add(found[i].transform.position);
            }

            return true;
        }

        private bool ChainIsAlive()
        {
            foreach (Item item in _chain)
            {
                if (item == null || item.destroying)
                    return false;
            }

            return true;
        }

        private static BoostIcon FindBoosterIcon(BoostType type)
        {
            foreach (BoostIcon icon in Level.InGameBoosts)
            {
                if (icon != null && icon.type == type && icon.gameObject.activeInHierarchy)
                    return icon;
            }

            return null;
        }

        // A plain piece close to the middle of the board, where the booster effect is easy to see.
        private static Item PieceNearCenter()
        {
            var pieces = new List<Item>();
            Vector3 center = Vector3.zero;
            foreach (Transform child in Level.GameField)
            {
                Item item = child.GetComponent<Item>();
                if (item == null || item.destroying || item.currentType != ItemsTypes.NONE)
                    continue;

                pieces.Add(item);
                center += item.transform.position;
            }

            if (pieces.Count == 0)
                return null;

            center /= pieces.Count;
            Item best = null;
            float bestDistance = float.MaxValue;
            foreach (Item item in pieces)
            {
                float distance = (item.transform.position - center).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = item;
                }
            }

            return best;
        }

        #endregion

        private void Cleanup()
        {
            _dimmer.Restore();
            _raise.Undo();
            if (_overlay != null)
            {
                _overlay.Hide();
                _overlay.SetVisible(true);
            }

            TutorialGate.Clear();
        }
    }
}
