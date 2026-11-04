using UnityEngine;

namespace JuiceFresh.States
{
    /// <summary>
    /// Game states that never got their own state class: they only toggle time or open a legacy menu.
    /// </summary>
    public static class StatelessGameStates
    {
        private const string GlobalCanvasName = "CanvasGlobal";

        public static void Enter(GameState state)
        {
            switch (state)
            {
                case GameState.PreFailed:
                    ShowGlobalMenu("PreFailed");
                    break;

                case GameState.Pause:
                    Time.timeScale = 0;
                    break;

                case GameState.ToMap:
                    MusicBase.Instance.GetComponent<AudioSource>().Stop();
                    SoundBase.Instance.PlaySound(SoundBase.Instance.gameOver[0]);
                    ShowGlobalMenu("MenuFailed");
                    break;
            }
        }

        private static void ShowGlobalMenu(string menuName)
        {
            GameObject canvas = GameObject.Find(GlobalCanvasName);
            Transform menu = canvas != null ? canvas.transform.Find(menuName) : null;

            if (menu == null)
            {
                Debug.LogWarning($"[GameState] {GlobalCanvasName}/{menuName} not found.");
                return;
            }

            menu.gameObject.SetActive(true);
        }
    }
}
