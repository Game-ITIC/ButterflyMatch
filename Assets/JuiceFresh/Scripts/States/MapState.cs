using System;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceFresh.States
{
    public class MapState : GameStateBase
    {
        public MapState(LevelManager levelManager) : base(levelManager)
        {
        }

        public override void EnterState()
        {
            if (PlayerPrefs.GetInt("OpenLevelTest") <= 0)
            {
                PlayMapMusic();
                EnableMap(true);

                LevelManager.TriggerOnMapState();
            }
            else
            {
                levelManager.gameStatus = GameState.PrepareGame;
                PlayerPrefs.SetInt("OpenLevelTest", 0);
                PlayerPrefs.Save();
            }
        }

        public override void UpdateState()
        {
            // No specific update logic for map state
        }

        public override void ExitState()
        {
            EnableMap(false);
        }

        private void PlayMapMusic()
        {
            var audioSource = MusicBase.Instance.GetComponent<AudioSource>();
            audioSource.Stop();
            audioSource.loop = true;
            audioSource.clip = MusicBase.Instance.music[0];
            Services.Settings.SettingsPreferences.ApplyAudio();
            if (Services.Settings.SettingsPreferences.MusicEnabled)
            {
                audioSource.Play();
            }
        }

        public void EnableMap(bool enable)
        {
            float aspect = (float)Screen.height / (float)Screen.width;
            aspect = (float)Math.Round(aspect, 2);

            if (enable)
            {
                SetMapCamera(aspect);
            }
            else
            {
                InitScript.DateOfExit = DateTime.Now.ToString();
                SetupGameCamera();
                SetGameCamera(aspect);
            }

            levelManager.gameCamera.GetComponent<MapCamera>().enabled = enable;

            HandleLevelsMapActivation(enable);

            if (enable)
            {
                levelManager.GameField.gameObject.SetActive(false);
            }

            if (!enable)
            {
                levelManager.gameCamera.transform.position = new Vector3(0, 0, -10);
            }

            ClearGameField();
        }

        private void SetMapCamera(float aspect)
        {
            Camera gameCamera = levelManager.gameCamera;
            gameCamera.orthographicSize = 10.25f;

            // Adjust camera orthographic size based on aspect ratio
            if (Mathf.Abs(aspect - 1.6f) < 0.02f)
                gameCamera.orthographicSize = 12.2f; //16:10
            else if (Mathf.Abs(aspect - 1.78f) < 0.02f)
                gameCamera.orthographicSize = 13.6f; //16:9
            else if (Mathf.Abs(aspect - 1.5f) < 0.02f)
                gameCamera.orthographicSize = 11.2f; //3:2
            else if (Mathf.Abs(aspect - 1.33f) < 0.02f)
                gameCamera.orthographicSize = 10.25f; //4:3
            else if (Mathf.Abs(aspect - 1.67f) < 0.02f)
                gameCamera.orthographicSize = 12.5f; //5:3
            else if (Mathf.Abs(aspect - 2.06f) < 0.02f)
                gameCamera.orthographicSize = 15.75f; //2960:1440 S8
            else if (Mathf.Abs(aspect - 2.17f) < 0.03f)
                gameCamera.orthographicSize = 16.5f;
            else if (Mathf.Abs(aspect - 2.16f) < 0.03f)
                gameCamera.orthographicSize = 16.5f; //iphone x

            gameCamera.GetComponent<MapCamera>().SetPosition(new Vector2(0, gameCamera.transform.position.y));
        }

        private void SetGameCamera(float aspect)
        {
            Camera gameCamera = levelManager.gameCamera;
            if (Mathf.Abs(aspect - 2.06f) < 0.02f)
                gameCamera.orthographicSize = 11.5f; //2960:1440 S8
            else if (Mathf.Abs(aspect - 2.17f) < 0.03f)
                gameCamera.orthographicSize = 12.26f; //iphone x
            else if (Mathf.Abs(aspect - 2.16f) < 0.03f)
                gameCamera.orthographicSize = 12.26f; //iphone x

            GameObject.Find("CanvasGlobal").GetComponent<GraphicRaycaster>().enabled = false;
            GameObject.Find("CanvasGlobal").GetComponent<GraphicRaycaster>().enabled = true;

            var canvas = levelManager.Level.transform.Find("Canvas");
            canvas.GetComponent<GraphicRaycaster>().enabled = false;
            canvas.GetComponent<GraphicRaycaster>().enabled = true;

            Transform panelTransform = canvas.transform.Find("Panel/Panel");
            if (panelTransform == null)
            {
                panelTransform = canvas.transform.Find("Panel");
            }

            if (Mathf.Abs(aspect - 2.17f) < 0.03f && panelTransform != null &&
                panelTransform.TryGetComponent(out RectTransform panel))
            {
                // This offset is intended for tall mobile screens (e.g. iPhone X-like aspect ratios).
                // Applying it on desktop can push the whole HUD (including TopBarPanel) out of place.
                if (Application.isMobilePlatform)
                {
                    panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, 935) + Vector2.down * 100;
                }
            }
        }

        private void SetupGameCamera()
        {
            Camera gameCamera = levelManager.gameCamera;
            float aspect = (float)Screen.height / (float)Screen.width;
            gameCamera.orthographicSize = 10.05f;
            aspect = (float)Math.Round(aspect, 2);
            gameCamera.GetComponent<MapCamera>().SetPosition(new Vector2(0, gameCamera.transform.position.y));
        }

        private void HandleLevelsMapActivation(bool enable)
        {
            GameObject levelsMap = levelManager.LevelsMap;
            GameObject level = levelManager.Level;

            levelsMap.SetActive(!enable); // This seems counterintuitive but matches original code
            levelsMap.SetActive(enable);
            levelsMap.GetComponent<LevelsMap>().Reset();

            foreach (Transform tr in levelsMap.transform)
            {
                if (tr.name != "AvatarManager" && tr.name != "Character")
                    tr.gameObject.SetActive(enable);
                if (tr.name == "Character")
                {
                    tr.GetComponent<SpriteRenderer>().enabled = enable;
                    tr.transform.GetChild(0).gameObject.SetActive(enable);
                }
            }

            level.SetActive(!enable);
        }

        private void ClearGameField()
        {
            foreach (Transform item in levelManager.GameField)
            {
                UnityEngine.Object.Destroy(item.gameObject);
            }
        }
    }
}
