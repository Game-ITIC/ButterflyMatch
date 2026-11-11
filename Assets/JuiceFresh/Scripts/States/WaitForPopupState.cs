using UnityEngine;
using UI;

namespace JuiceFresh.States
{
    public class WaitForPopupState : GameStateBase
    {
        private ILevelManagerActions _levelManagerActions;
        public WaitForPopupState(LevelManager levelManager) : base(levelManager)
        {
            _levelManagerActions = levelManager;
        }

        public override void EnterState()
        {
            InitLevel();

            LevelManager.TriggerOnLevelLoaded();

            float defaultScale = levelManager.GetComponent<Camera>().orthographicSize;
            ScaleCamera(defaultScale);

            // The screen size at level start can be stale (Simulator, window switch, rotation): refit when it changes.
            if (!levelManager.TryGetComponent(out ScreenSizeRefit refit))
                refit = levelManager.gameObject.AddComponent<ScreenSizeRefit>();
            refit.Track(() => ScaleCamera(defaultScale));
        }

        public override void UpdateState()
        {
            // No specific update logic for this state
        }

        public override void ExitState()
        {
            // Nothing specific to clean up
        }

        private void InitLevel()
        {
            _levelManagerActions.GenerateLevel();
            _levelManagerActions.GenerateNewItems(false);
            levelManager.nextExtraItems = 0;
            levelManager.bombTimers.Clear();

            InitTargets();

            levelManager.GameField.gameObject.SetActive(true);
            PlayGameFieldIntro();
        }

        private void PlayGameFieldIntro()
        {
            GameFieldTweenAnimation animation = levelManager.GameField.GetComponent<GameFieldTweenAnimation>();
            if(animation == null)
            {
                levelManager.GameField.localPosition = levelManager.GameFieldTargetLocalPosition;
                GameFeelManager.Instance?.RefreshBoardCanonicalTransform();
                _levelManagerActions.GenerateOutline();
                levelManager.gameStatus = GameState.PrepareBoosts;
                return;
            }

            animation.Play(levelManager.GameFieldTargetLocalPosition,
                () =>
                {
                    // A board shake still running from the previous level may have moved the board during the intro.
                    levelManager.GameField.localPosition = levelManager.GameFieldTargetLocalPosition;
                    GameFeelManager.Instance?.RefreshBoardCanonicalTransform();
                    _levelManagerActions.GenerateOutline();
                    levelManager.gameStatus = GameState.PrepareBoosts;
                });
        }

        private void InitTargets()
        {
            SetActiveIfAssigned(levelManager.blocksObject, false);
            SetActiveIfAssigned(levelManager.ingrObject, false);
            SetActiveIfAssigned(levelManager.scoreTargetObject, false);
            SetActiveIfAssigned(levelManager.cageTargetObject, false);
            SetActiveIfAssigned(levelManager.bombTargetObject, false);

            foreach (GameObject item in levelManager.listIngredientsGUIObjects)
            {
                Object.Destroy(item);
            }

            levelManager.listIngredientsGUIObjects.Clear();

            SetupTargetUI();
        }

        private void SetupTargetUI()
        {
            if (levelManager.target != Target.COLLECT && levelManager.target != Target.ITEMS)
            {
                SetActiveIfAssigned(levelManager.ingrObject, false);
            }
            else if (levelManager.target == Target.COLLECT)
            {
                SetActiveIfAssigned(levelManager.blocksObject, false);
                CreateCollectableTargetIfAssigned();
            }
            else if (levelManager.target == Target.ITEMS)
            {
                SetActiveIfAssigned(levelManager.blocksObject, false);
                CreateCollectableTargetIfAssigned();
            }

            if (levelManager.targetBlocks > 0 && levelManager.target == Target.BLOCKS)
            {
                SetActiveIfAssigned(levelManager.blocksObject, true);
                SetTargetCount(levelManager.blocksObject, levelManager.targetBlocks);
            }
            else if (levelManager.target == Target.CAGES)
            {
                SetActiveIfAssigned(levelManager.cageTargetObject, true);
                SetTargetCount(levelManager.cageTargetObject, levelManager.TargetCages);
            }
            else if (levelManager.target == Target.BOMBS)
            {
                levelManager.StartCoroutine(_levelManagerActions.InitBombs());
                SetActiveIfAssigned(levelManager.bombTargetObject, true);
                SetTargetCount(levelManager.bombTargetObject, levelManager.bombsCollect);
            }
            else if (levelManager.target == Target.SCORE)
            {
                SetActiveIfAssigned(levelManager.ingrObject, false);
                SetActiveIfAssigned(levelManager.blocksObject, false);
                SetActiveIfAssigned(levelManager.scoreTargetObject, true);
                SetActiveIfAssigned(levelManager.cageTargetObject, false);
                SetActiveIfAssigned(levelManager.bombTargetObject, false);
            }
        }

        private void CreateCollectableTargetIfAssigned()
        {
            if (levelManager.ingrObject != null)
            {
                _levelManagerActions.CreateCollectableTarget(levelManager.ingrObject, levelManager.target, false);
            }
        }

        private static void SetTargetCount(GameObject target, int count)
        {
            if (target == null)
            {
                return;
            }

            TargetGUI targetGui = target.GetComponent<TargetGUI>();
            if (targetGui == null || targetGui.text == null)
            {
                return;
            }

            Counter_ counter = targetGui.text.GetComponent<Counter_>();
            if (counter != null)
            {
                counter.totalCount = count;
            }
        }

        private static void SetActiveIfAssigned(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private void ScaleCamera(float defaultScale)
        {
            // Get the width of the playfield
            Square leftSquare = _levelManagerActions.GetSquare(0, 0);
            Square rightSquare = _levelManagerActions.GetSquare(levelManager.maxCols - 1, 0);
        
            if (leftSquare == null || rightSquare == null)
                return;
            
            float leftX = leftSquare.transform.position.x;
            float rightX = rightSquare.transform.position.x;

            // Include half-cell padding so edge cells are not clipped.
            float cellWidth = Mathf.Max(0.001f, levelManager.squareWidth);
            float gridWorldWidth = Mathf.Abs(rightX - leftX) + cellWidth;

            // Side margin for swipes and the board frame (world units). It has to be added to the width:
            // adding it to the orthographic (vertical) size left wide boards almost touching the screen edges.
            float sideMargin = Mathf.Max(0.9f, cellWidth * 0.6f);
            // The camera spans the full screen, so the visible width is 2 * ortho * aspect (the camera's own aspect:
            // Screen.width/height can still report another window while the level loads in the Editor).
            float screenAspect = 1f / Mathf.Max(0.01f, levelManager.GetComponent<Camera>().aspect);
            float requiredOrtho = (gridWorldWidth + 2f * sideMargin) * screenAspect * 0.5f;

            // Tall boards must still fit between the top bar and the booster row.
            Square bottomSquare = _levelManagerActions.GetSquare(0, levelManager.maxRows - 1);
            if (bottomSquare != null)
            {
                float cellHeight = Mathf.Max(0.001f, levelManager.squareHeight);
                if (TryGetHudBand(out float bandBottom, out float bandTop))
                {
                    // Measured band: works on any screen shape (tablets and foldables give the HUD a bigger share).
                    float firstRowY = TargetWorldY(leftSquare.transform);
                    float lastRowY = TargetWorldY(bottomSquare.transform);
                    float boardTop = Mathf.Max(firstRowY, lastRowY) + cellHeight * (0.5f + HudGapInCells);
                    float boardBottom = Mathf.Min(firstRowY, lastRowY) - cellHeight * (0.5f + HudGapInCells);
                    float cameraY = levelManager.transform.position.y;
                    if (bandTop > 0.5f)
                        requiredOrtho = Mathf.Max(requiredOrtho, (boardTop - cameraY) / (2f * (bandTop - 0.5f)));
                    if (bandBottom < 0.5f)
                        requiredOrtho = Mathf.Max(requiredOrtho, (cameraY - boardBottom) / (2f * (0.5f - bandBottom)));
                }
                else
                {
                    float gridWorldHeight = Mathf.Abs(leftSquare.transform.position.y - bottomSquare.transform.position.y) + cellHeight;
                    float safeShare = Mathf.Clamp01(Screen.safeArea.height / Mathf.Max(1f, Screen.height));
                    requiredOrtho = Mathf.Max(requiredOrtho, gridWorldHeight / (0.6f * Mathf.Max(0.5f, safeShare)) * 0.5f);
                }
            }

            // Set the camera size, clamped between the default and calculated values
            levelManager.GetComponent<Camera>().orthographicSize = Mathf.Max(defaultScale, requiredOrtho);
        }

        /// <summary>Free space between the board and the HUD, in cells.</summary>
        private const float HudGapInCells = 0.25f;

        /// <summary>
        /// Where the board may go, as screen fractions from the bottom: above the booster row, below the top bar.
        /// The level canvas is Screen Space - Camera, so these pixels don't depend on the camera size.
        /// </summary>
        private bool TryGetHudBand(out float bottom, out float top)
        {
            bottom = 0f;
            top = 1f;

            Transform canvasTransform = levelManager.Level != null ? levelManager.Level.transform.Find("Canvas") : null;
            Canvas canvas = canvasTransform != null ? canvasTransform.GetComponent<Canvas>() : null;
            var topBar = canvasTransform != null ? canvasTransform.Find("Panel/TopBarPanel") as RectTransform : null;
            if (canvas == null || topBar == null || Screen.height <= 0)
                return false;

            Canvas.ForceUpdateCanvases();
            Camera cam = canvas.worldCamera;
            var corners = new Vector3[4];

            topBar.GetWorldCorners(corners);
            top = RectTransformUtility.WorldToScreenPoint(cam, corners[0]).y / Screen.height;

            // Booster icons share one row; hidden ones (locked boosters) still mark it, so the board doesn't jump at unlock.
            foreach (BoostIcon icon in levelManager.InGameBoosts)
            {
                if (icon == null)
                    continue;

                ((RectTransform)icon.transform).GetWorldCorners(corners);
                bottom = Mathf.Max(bottom, RectTransformUtility.WorldToScreenPoint(cam, corners[1]).y / Screen.height);
            }

            return top > bottom;
        }

        /// <summary>A square's world Y once the board's intro tween has finished.</summary>
        private float TargetWorldY(Transform square)
        {
            Transform field = levelManager.GameField;
            Vector3 target = field.parent != null
                ? field.parent.TransformPoint(levelManager.GameFieldTargetLocalPosition)
                : levelManager.GameFieldTargetLocalPosition;
            return square.position.y - field.position.y + target.y;
        }
    }
}
