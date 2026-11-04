#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Services.DailyRewards;
using Services.Tasks;
using UnityEngine;

namespace Utils.DebugTools
{
    /// <summary>
    /// Progress resets. The full reset only marks a flag and quits; <see cref="ApplyPending"/> wipes PlayerPrefs on
    /// the next launch, because running services would write their in-memory values back on quit.
    /// </summary>
    internal sealed class CheatResets
    {
        private const string ResetPendingKey = "cheat-reset-pending";

        private readonly CheatLevels _levels;

        public CheatResets(CheatLevels levels)
        {
            _levels = levels;
        }

        public static void ApplyPending()
        {
            if(PlayerPrefs.GetInt(ResetPendingKey, 0) == 1)
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
            }
        }

        public void ResetDaily()
        {
            PlayerPrefs.DeleteKey(DailyRewardService.LastClaimDateKey);
            PlayerPrefs.Save();
            _levels.GoToMenu("Ежедневную награду можно забрать снова");
        }

        public void ResetTasks()
        {
            new TaskStorage().Clear();
            _levels.GoToMenu("Задания обновлены");
        }

        public static void ResetEverything()
        {
            PlayerPrefs.SetInt(ResetPendingKey, 1);
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
#endif
