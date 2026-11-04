using UnityEngine;

namespace JuiceFresh.States
{
    /// <summary>
    /// Game states that never got their own state class: they only toggle time or open a legacy menu.
    /// </summary>
    public static class StatelessGameStates
    {
        public static void Enter(GameState state)
        {
            switch (state)
            {
                case GameState.PreFailed:
                    ShowGlobalMenu(GameSceneUi.PreFailed);
                    break;

                case GameState.Pause:
                    Time.timeScale = 0;
                    break;

                case GameState.ToMap:
                    MusicBase.Instance.GetComponent<AudioSource>().Stop();
                    SoundBase.Instance.PlaySound(SoundBase.Instance.gameOver[0]);
                    ShowGlobalMenu(GameSceneUi.MenuFailed);
                    break;
            }
        }

        private static void ShowGlobalMenu(string menuName)
        {
            Transform menu = GameSceneUi.FindInGlobalCanvas(menuName);

            if (menu == null)
            {
                Debug.LogWarning($"[GameState] {GameSceneUi.GlobalCanvasPath}/{menuName} not found.");
                return;
            }

            menu.gameObject.SetActive(true);
        }
    }
}
