using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using JuiceFresh.Board;
namespace JuiceFresh
{
    public enum FindSeparating
    {
        NONE = 0,
        HORIZONTAL,
        VERTICAL
    }

    public enum SquareTypes
    {
        NONE = 0,
        EMPTY,
        BLOCK,
        WIREBLOCK,
        SOLIDBLOCK,
        DOUBLESOLIDBLOCK,
        DOUBLEBLOCK,
        UNDESTROYABLE,
        THRIVING
    }

    /// <summary>
    /// Represents a square on the game grid
    /// Handles square behavior, matching logic, and obstacle interactions
    /// </summary>
    public class Square : MonoBehaviour
    {
        #region Properties and Fields
        [Header("References")]
        public Square square;
        public Item item;
        
        [Header("Position")]
        public int row;
        public int col;
        
        [Header("State")]
        public SquareTypes type;
        public int cageHP;
        public int cageHPPreview;
        
        [Header("Prefabs")]
        public GameObject boomPrefab;
        public GameObject icePrefab;
        
        [Header("Blocks")]
        public List<GameObject> block = new List<GameObject>();

        [Header("Layers")]
        [Tooltip("Cell mark of the chain and the move hint (under the piece).")]
        public GameObject chainMark;
        [Tooltip("Block only: the part of the jar drawn in front of the piece.")]
        public GameObject frontLayer;
        
        private bool hightlighted;
        private SpriteRenderer _spriteRenderer;

        // Highlight is cleared on every square at each chain step, so the renderer lookup is cached.
        private SpriteRenderer CellRenderer
        {
            get
            {
                if (_spriteRenderer == null)
                    _spriteRenderer = GetComponent<SpriteRenderer>();
                return _spriteRenderer;
            }
        }

        // Lazy: squares are also used as plain data holders on block prefabs, and some are set up inactive.
        private SquareCage _cage;
        private SquareBlockDestroyer _blockDestroyer;
        private SquareCage Cage => _cage ??= new SquareCage(this);
        private SquareBlockDestroyer BlockDestroyer => _blockDestroyer ??= new SquareBlockDestroyer(this);
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            square = this;
        }

        void LateUpdate()
        {
            cageHPPreview = cageHP;
        }
        #endregion

        #region Item Generation and Management
        /// <summary>
        /// Generates a new item on this square
        /// </summary>
        /// <param name="falling">Should the item be created with falling animation</param>
        /// <returns>The created item</returns>
        public Item GenItem(bool falling = true)
        {
            if (IsNone() && !CanGoInto())
                return null;
                
            GameObject itemObj = Instantiate(LevelManager.THIS.itemPrefab) as GameObject;
            itemObj.transform.localScale = Vector2.one * 0.9f;
            Item newItem = itemObj.GetComponent<Item>();
            newItem.square = this;

            itemObj.transform.SetParent(transform.parent);

            if (falling)
            {
                itemObj.transform.position = transform.position + Vector3.back * 0.2f + Vector3.up * 3f;
                newItem.justCreatedItem = true;
            }
            else
            {
                itemObj.transform.position = transform.position + Vector3.back * 0.2f;
            }

            this.item = newItem;
            return this.item;
        }
        #endregion

        #region Neighbor Access
        public Square GetNeighborLeft(bool safe = false)
        {
            if (col == 0 && !safe)
                return null;
            return LevelManager.THIS.GetSquare(col - 1, row, safe);
        }

        public Square GetNeighborRight(bool safe = false)
        {
            if (col >= LevelManager.THIS.maxCols && !safe)
                return null;
            return LevelManager.THIS.GetSquare(col + 1, row, safe);
        }

        public Square GetNeighborTop(bool safe = false)
        {
            if (row == 0 && !safe)
                return null;
            return LevelManager.THIS.GetSquare(col, row - 1, safe);
        }

        public Square GetNeighborBottom(bool safe = false)
        {
            if (row >= LevelManager.THIS.maxRows && !safe)
                return null;
            return LevelManager.THIS.GetSquare(col, row + 1, safe);
        }

        public List<Square> GetAllNeghbors()
        {
            List<Square> sqList = new List<Square>();
            
            Square nextSquare = GetNeighborBottom();
            if (nextSquare != null)
                sqList.Add(nextSquare);
                
            nextSquare = GetNeighborTop();
            if (nextSquare != null)
                sqList.Add(nextSquare);
                
            nextSquare = GetNeighborLeft();
            if (nextSquare != null)
                sqList.Add(nextSquare);
                
            nextSquare = GetNeighborRight();
            if (nextSquare != null)
                sqList.Add(nextSquare);
                
            return sqList;
        }
        #endregion

        #region Visual Effects
        public void HighLight(bool on, float col = 0.5f)
        {
            if (on)
            {
                CellRenderer.color = new Color(col, 0.65f, 0);
                if (!hightlighted)
                    StartCoroutine(AnimateHighlight(col));
            }
            else
            {
                // The old StopCoroutine(AnimateHighlight(col)) stopped nothing (a new enumerator) and only allocated.
                CellRenderer.color = new Color(1, 1, 1);
            }

            hightlighted = on;
            SetActiveCage(on);
        }

        IEnumerator AnimateHighlight(float col = 0.8f)
        {
            CellRenderer.color = new Color(col, 0.65f, 0);
            while (hightlighted)
            {
                yield return new WaitForSeconds(0.3f);
                CellRenderer.color = new Color(col, 0.65f, 0);
                yield return new WaitForSeconds(0.3f);
                CellRenderer.color = new Color(col - 0.1f, col - 0.1f, col - 0.1f);
            }
        }
        #endregion

        #region Layers
        public void SetChainMark(bool on)
        {
            if (chainMark != null && chainMark.activeSelf != on)
                chainMark.SetActive(on);
        }

        public void SetFrontLayer(bool on)
        {
            if (frontLayer != null)
                frontLayer.SetActive(on);
        }
        #endregion

        #region Item Movement
        /// <summary>
        /// Checks if this square's item can fall to a square below
        /// </summary>
        public void FallOut()
        {
            if (item != null && type != SquareTypes.WIREBLOCK)
            {
                Square nextSquare = GetNeighborBottom();
                if (nextSquare != null)
                {
                    if (nextSquare.IsNone())
                    {
                        nextSquare = FindBottomMostValidSquare();
                    }

                    if (nextSquare.CanFallInto() && nextSquare.item == null)
                    {
                        item.CheckNeedToFall(nextSquare);
                    }
                }
            }
        }

        private Square FindBottomMostValidSquare()
        {
            Square nextSquare = null;
            
            for (int i = row + 1; i < LevelManager.THIS.maxRows; i++)
            {
                if (LevelManager.THIS.GetSquare(col, i) != null)
                {
                    if (!LevelManager.THIS.GetSquare(col, i).IsNone())
                    {
                        nextSquare = LevelManager.THIS.GetSquare(col, i);
                        break;
                    }
                }
            }
            
            return nextSquare ?? GetNeighborBottom();
        }
        #endregion

        #region Square Type Checking
        public bool IsNone()
        {
            return type == SquareTypes.NONE;
        }

        public bool IsHaveDestroybleObstacle()
        {
            return type == SquareTypes.SOLIDBLOCK || type == SquareTypes.THRIVING;
        }

        public bool CanGoOut()
        {
            return type != SquareTypes.WIREBLOCK;
        }

        public bool CanGoInto()
        {
            return type != SquareTypes.SOLIDBLOCK && 
                  type != SquareTypes.UNDESTROYABLE && 
                  type != SquareTypes.NONE &&
                  type != SquareTypes.THRIVING;
        }

        public bool CanFallInto()
        {
            return type != SquareTypes.WIREBLOCK && 
                  type != SquareTypes.SOLIDBLOCK &&
                  type != SquareTypes.UNDESTROYABLE && 
                  type != SquareTypes.NONE && 
                  type != SquareTypes.THRIVING;
        }

        public bool IsHaveSolidAbove()
        {
            for (int i = row; i >= 0; i--)
            {
                SquareTypes squareType = LevelManager.THIS.GetSquare(col, i).type;
                if (squareType == SquareTypes.WIREBLOCK ||
                    squareType == SquareTypes.SOLIDBLOCK ||
                    squareType == SquareTypes.UNDESTROYABLE ||
                    squareType == SquareTypes.THRIVING)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsExtraItem()
        {
            if (item != null)
            {
                return item.IsExtraItem();
            }

            return false;
        }
        #endregion

        #region Block Destruction
        /// <summary>
        /// Destroys the block on this square
        /// </summary>
        public void DestroyBlock()
        {
            BlockDestroyer.DestroyBlock();
        }
        #endregion

        #region Cage Management
        public void SetCage(int cageHP_)
        {
            Cage.Set(cageHP_);
        }

        /// <summary>
        /// Activates or deactivates the cage on this square
        /// </summary>
        public void SetActiveCage(bool active, bool _justHighlighted = false)
        {
            Cage.SetActive(active);
        }

        /// <summary>
        /// Checks if the cage on this square will be destroyed with additional damage
        /// </summary>
        public bool IsCageGoingToBroke(int extraDamage = 0)
        {
            return Cage.IsGoingToBreak(extraDamage);
        }

        /// <summary>
        /// Checks if the cage will be destroyed after taking damage
        /// </summary>
        public bool CheckDamage(int damage)
        {
            return Cage.CheckDamage(damage);
        }
        #endregion
    }
}
