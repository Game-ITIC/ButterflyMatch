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
        
        private bool hightlighted;

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
            itemObj.GetComponent<Item>().square = this;
            
            itemObj.transform.SetParent(transform.parent);
            
            if (falling)
            {
                itemObj.transform.position = transform.position + Vector3.back * 0.2f + Vector3.up * 3f;
                itemObj.GetComponent<Item>().justCreatedItem = true;
            }
            else
            {
                itemObj.transform.position = transform.position + Vector3.back * 0.2f;
            }

            this.item = itemObj.GetComponent<Item>();
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

        #region Match Finding
        Hashtable FindMoreMatches(int colorToMatch, Hashtable countedSquares, FindSeparating separating,
            Hashtable countedSquaresGlobal = null)
        {
            bool globalCounter = countedSquaresGlobal != null;
            
            if (!globalCounter)
            {
                countedSquaresGlobal = new Hashtable();
            }

            if (this.item == null || this.item.destroying)
                return countedSquares;
                
            if (this.item.color == colorToMatch && 
                !countedSquares.ContainsValue(this.item) &&
                this.item.currentType != ItemsTypes.INGREDIENT)
            {
                if (LevelManager.THIS.onlyFalling && this.item.justCreatedItem)
                    countedSquares.Add(countedSquares.Count - 1, this.item);
                else if (!LevelManager.THIS.onlyFalling)
                    countedSquares.Add(countedSquares.Count - 1, this.item);
                else
                    return countedSquares;

                CheckDirectionalMatches(colorToMatch, countedSquares, separating);
            }

            return countedSquares;
        }

        private void CheckDirectionalMatches(int colorToMatch, Hashtable countedSquares, FindSeparating separating)
        {
            if (separating == FindSeparating.HORIZONTAL)
            {
                Square leftNeighbor = GetNeighborLeft();
                if (leftNeighbor != null)
                    countedSquares = leftNeighbor.FindMoreMatches(colorToMatch, countedSquares, FindSeparating.HORIZONTAL);
                    
                Square rightNeighbor = GetNeighborRight();
                if (rightNeighbor != null)
                    countedSquares = rightNeighbor.FindMoreMatches(colorToMatch, countedSquares, FindSeparating.HORIZONTAL);
            }
            else if (separating == FindSeparating.VERTICAL)
            {
                Square topNeighbor = GetNeighborTop();
                if (topNeighbor != null)
                    countedSquares = topNeighbor.FindMoreMatches(colorToMatch, countedSquares, FindSeparating.VERTICAL);
                    
                Square bottomNeighbor = GetNeighborBottom();
                if (bottomNeighbor != null)
                    countedSquares = bottomNeighbor.FindMoreMatches(colorToMatch, countedSquares, FindSeparating.VERTICAL);
            }
        }

        /// <summary>
        /// Finds all matching items around this square
        /// </summary>
        public List<Item> FindMatchesAround(FindSeparating separating = FindSeparating.NONE, int minimumMatches = 3,
            Hashtable countedSquaresGlobal = null)
        {
            bool globalCounter = countedSquaresGlobal != null;
            List<Item> newList = new List<Item>();
            
            if (!globalCounter)
            {
                countedSquaresGlobal = new Hashtable();
            }

            Hashtable countedSquares = new Hashtable();
            countedSquares.Clear();
            
            if (this.item == null)
                return newList;
                
            // Check horizontal matches
            if (separating != FindSeparating.VERTICAL)
            {
                countedSquares = this.FindMoreMatches(this.item.color, countedSquares, FindSeparating.HORIZONTAL,
                    countedSquaresGlobal);
            }

            foreach (DictionaryEntry de in countedSquares)
            {
                LevelManager.THIS.countedSquares.Add(LevelManager.THIS.countedSquares.Count - 1, de.Value);
            }

            if (countedSquares.Count < minimumMatches)
                countedSquares.Clear();

            // Check vertical matches
            if (separating != FindSeparating.HORIZONTAL)
            {
                countedSquares = this.FindMoreMatches(this.item.color, countedSquares, FindSeparating.VERTICAL,
                    countedSquaresGlobal);
            }

            foreach (DictionaryEntry de in countedSquares)
            {
                LevelManager.THIS.countedSquares.Add(LevelManager.THIS.countedSquares.Count - 1, de.Value);
            }

            if (countedSquares.Count < minimumMatches)
                countedSquares.Clear();
                
            // Convert to list
            foreach (DictionaryEntry de in countedSquares)
            {
                newList.Add((Item)de.Value);
            }

            return newList;
        }
        #endregion

        #region Visual Effects
        public void HighLight(bool on, float col = 0.5f)
        {
            if (on)
            {
                GetComponent<SpriteRenderer>().color = new Color(col, 0.65f, 0);
                if (!hightlighted)
                    StartCoroutine(AnimateHighlight(col));
            }
            else
            {
                StopCoroutine(AnimateHighlight(col));
                GetComponent<SpriteRenderer>().color = new Color(1, 1, 1);
            }

            hightlighted = on;
            SetActiveCage(on);
        }

        IEnumerator AnimateHighlight(float col = 0.8f)
        {
            GetComponent<SpriteRenderer>().color = new Color(col, 0.65f, 0);
            while (hightlighted)
            {
                yield return new WaitForSeconds(0.3f);
                GetComponent<SpriteRenderer>().color = new Color(col, 0.65f, 0);
                yield return new WaitForSeconds(0.3f);
                GetComponent<SpriteRenderer>().color = new Color(col - 0.1f, col - 0.1f, col - 0.1f);
            }
        }
        #endregion

        #region Item Movement
        public void FallInto()
        {
            // Reserved for future functionality
        }

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

        public void AddDagame(int damage)
        {
            Cage.AddDamage(damage);
        }
        #endregion

        #region Special Blocks
        public void GenThriveBlock(Square newSquare)
        {
            // Reserved for future functionality
        }
        #endregion
    }
}
