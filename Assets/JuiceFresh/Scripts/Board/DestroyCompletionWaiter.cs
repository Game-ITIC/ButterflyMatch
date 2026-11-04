using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace JuiceFresh.Board
{
    /// <summary>
    /// Waits until the destroy animations of a cascade pass have finished. A piece whose animation event
    /// never fires is finished by force after <see cref="MaxWaitMs"/>, so the board cannot lock up.
    /// </summary>
    public class DestroyCompletionWaiter
    {
        private const int MaxWaitMs = 2000;
        private const int PollMs = 100;

        public async UniTask WaitAsync(CancellationToken cancellationToken)
        {
            int waitedMs = 0;
            while (!AllFinished())
            {
                if (waitedMs >= MaxWaitMs)
                {
                    ForceFinishStuck();
                    break;
                }

                await UniTask.Delay(PollMs, cancellationToken: cancellationToken);
                waitedMs += PollMs;
            }
        }

        private static bool AllFinished()
        {
            var items = Item.Active;
            for (int i = 0; i < items.Count; i++)
            {
                Item item = items[i];
                if (item == null || (item.destroying && !item.animationFinished))
                    return false;
            }

            return true;
        }

        private static void ForceFinishStuck()
        {
            // SetAnimationDestroyingFinished only sets two flags, so the live list is safe here.
            var items = Item.Active;
            for (int i = 0; i < items.Count; i++)
            {
                Item item = items[i];
                if (item != null && item.destroying && !item.animationFinished)
                    item.SetAnimationDestroyingFinished();
            }
        }
    }
}
