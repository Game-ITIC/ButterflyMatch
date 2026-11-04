using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

namespace JuiceFresh.Feel
{
    /// <summary>
    /// Camera shakes (Feel MMF_Player) for a released match, a big combo / booster and a win.
    /// </summary>
    public sealed class CameraShakeFeedback
    {
        private readonly Transform _playersParent;

        private MMF_Player _match;
        private MMF_Player _combo;
        private MMF_Player _win;

        public CameraShakeFeedback(Transform playersParent)
        {
            _playersParent = playersParent;
        }

        /// <summary>Adds the Feel shaker components the shake feedback needs to the camera.</summary>
        public static void PrepareCamera(Camera camera)
        {
            MMCameraShaker shaker = camera.GetComponent<MMCameraShaker>();
            if (shaker == null)
            {
                if (camera.GetComponent<MMWiggle>() == null)
                    camera.gameObject.AddComponent<MMWiggle>();

                shaker = camera.gameObject.AddComponent<MMCameraShaker>();
            }

            shaker.CooldownBetweenShakes = 0f;
        }

        /// <summary>Feel's time manager (freeze frames) must exist once per game.</summary>
        public static void EnsureTimeManager()
        {
            if (MMTimeManager.Instance != null)
                return;

            GameObject timeManager = new GameObject("MMTimeManager");
            timeManager.AddComponent<MMTimeManager>();
            Object.DontDestroyOnLoad(timeManager);
        }

        /// <summary>(Re)creates the shake players with the given amplitudes.</summary>
        public void Build(float matchAmplitude, float comboAmplitude, float winAmplitude, float frequency)
        {
            if (!Application.isPlaying)
                return;

            DestroyPlayers();

            _match = CreatePlayer("Feel_Match", 0.2f, matchAmplitude, frequency);
            _combo = CreatePlayer("Feel_Combo", 0.26f, comboAmplitude, frequency * 0.85f);
            _win = CreatePlayer("Feel_Win", 0.38f, winAmplitude, frequency * 0.7f);
        }

        public void PlayMatch(Vector3 position, float intensity) => _match?.PlayFeedbacks(position, intensity);

        public void PlayCombo(Vector3 position, float intensity) => _combo?.PlayFeedbacks(position, intensity);

        public void PlayWin(float intensity) => _win?.PlayFeedbacks(Vector3.zero, intensity);

        public void DestroyPlayers()
        {
            DestroyPlayer(ref _match);
            DestroyPlayer(ref _combo);
            DestroyPlayer(ref _win);
        }

        private MMF_Player CreatePlayer(string playerName, float duration, float amplitude, float frequency)
        {
            GameObject playerObject = new GameObject(playerName);
            playerObject.transform.SetParent(_playersParent);

            MMF_Player player = playerObject.AddComponent<MMF_Player>();
            MMF_CameraShake shake = (MMF_CameraShake)player.AddFeedback(typeof(MMF_CameraShake));
            shake.CameraShakeProperties = new MMCameraShakeProperties(duration, amplitude, frequency);
            player.Initialization();
            return player;
        }

        private static void DestroyPlayer(ref MMF_Player player)
        {
            if (player == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(player.gameObject);
            else
                Object.DestroyImmediate(player.gameObject);

            player = null;
        }
    }
}
