using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using JuiceFresh;
using JuiceFresh.Board;

public enum ItemsTypes
{
    NONE = 0,
    VERTICAL_STRIPPED,
    HORIZONTAL_STRIPPED,
    PACKAGE,
    CHOCOBOMB,
    INGREDIENT,
    BOMB
}

/// <summary>
/// A piece on the board. Keeps the piece state and the entry points that animation events, BroadcastMessage
/// and the game call by name; the work is done by plain collaborators in JuiceFresh.Board
/// (fall mover, bomb, sprites, effects, special explosions).
/// </summary>
public class Item : MonoBehaviour
{
    #region Serialized Fields
    [Header("Item Visuals")]
    public Sprite[] items;
    public Sprite[] itemsAnimation;
    public List<StripedItem> stripedItems = new List<StripedItem>();
    public Sprite[] packageItems;
    public Sprite[] ChocoBombItems;
    public Sprite[] bombItems;
    public SpriteRenderer sprRenderer;

    [Header("Effects")]
    public GameObject appearingEffect;
    public GameObject timerTextPrefab;
    public GameObject bombSelection;
    public GameObject bombSelection1;
    public GameObject wickBurningPrefab;

    [Header("Debug")]
    public ItemsTypes debugType = ItemsTypes.NONE;
    public int COLORView;
    #endregion

    #region Private Fields
    private ItemTweenAnimator _animation;
    private ItemFallMover _fall;
    private ItemBomb _bomb;
    private ItemSpriteView _sprites;
    private ItemEffects _effects;
    private ItemSpecialDestroyer _specials;
    private int COLOR;
    private const float DefaultBoardItemScale = 0.9f;
    private const float MinimumValidBoardItemScale = 0.5f;

    // Set when a striped blast already fired this striped piece across, so it doesn't fire twice.
    internal bool extraChecked;
    // The striped piece has blasted its line(s); its own death must not blast again. The late second blast
    // (after the destroy animation) hit pieces that had already refilled the line and left holes.
    internal bool stripeFired;
    internal ItemTweenAnimator Animation => _animation;
    #endregion

    #region Properties
    public Square square { get; set; }

    public bool dragThis { get; set; }
    public Vector3 mousePos { get; set; }
    public Vector3 deltaPos { get; set; }
    public Vector3 switchDirection { get; set; }

    public bool falling { get; set; }

    public ItemsTypes nextType
    {
        get { return NextType; }
        set { NextType = value; }
    }
    private ItemsTypes NextType = ItemsTypes.NONE;

    public ItemsTypes currentType
    {
        get { return CurrentType; }
        set { CurrentType = value; }
    }
    private ItemsTypes CurrentType = ItemsTypes.NONE;

    public int color
    {
        get { return COLOR; }
        set { COLOR = value; }
    }

    public bool destroying { get; set; }
    public bool appeared { get; set; }
    public bool animationFinished { get; set; }
    public bool justCreatedItem { get; set; }
    public int bombTimer { get; set; }
    public bool awaken { get; set; }
    public Item item { get; private set; }
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _animation = new ItemTweenAnimator(gameObject, sprRenderer.transform);
        _fall = new ItemFallMover(this);
        _bomb = new ItemBomb(this);
        _sprites = new ItemSpriteView(this);
        _effects = new ItemEffects(this);
        _specials = new ItemSpecialDestroyer(this);
    }

    void Start()
    {
        InitializeItem();
    }

    void Update()
    {
        COLORView = color;
        CheckDebugTypeChange();
        _bomb.RefreshTimerText();
    }

    void OnDestroy()
    {
        _animation?.Dispose();
    }
    #endregion

    #region Initialization
    private void InitializeItem()
    {
        item = this;
        falling = true;
        GenColor();

        if (nextType != ItemsTypes.NONE)
        {
            InitializeWithNextType();
        }

        EnsureValidBoardScale();

        if (currentType == ItemsTypes.INGREDIENT)
        {
            CreateIngredientArrow();
        }
    }

    private void InitializeWithNextType()
    {
        // A piece marked as a bomb before its Start() becomes the bomb right here, so it needs the bomb
        // setup too: without it the bomb had no timer (0) and blew up on the next move.
        if (nextType == ItemsTypes.BOMB)
        {
            sprRenderer.sprite = bombItems[color];
            _bomb.Setup();
        }

        debugType = nextType;
        currentType = nextType;
        nextType = ItemsTypes.NONE;
        transform.position = square.transform.position;
        falling = false;
    }

    private void CreateIngredientArrow()
    {
        GameObject obj = Instantiate(Resources.Load("Prefabs/arrow_ingredients")) as GameObject;
        obj.transform.SetParent(transform);
        obj.transform.localPosition = new Vector3(0.66f, -0.53f, 0);
        obj.transform.localScale = Vector3.one * 0.6f;
    }
    #endregion

    #region Item Color Management
    /// <summary>
    /// Gives the piece a colour (avoiding exceptColor), may turn it into an ingredient on collect levels,
    /// and starts its fall to the square.
    /// </summary>
    /// <param name="exceptColor">Color to exclude</param>
    /// <param name="onlyNONEType">Whether to only generate color for NONE type items</param>
    public void GenColor(int exceptColor = -1, bool onlyNONEType = false)
    {
        int randColor = ItemColorPicker.Pick(LevelManager.Instance, square.col, square.row, exceptColor);

        LevelManager.THIS.lastRandColor = randColor;
        _sprites.SetColorSprite(randColor);
        _sprites.ApplyPendingTypeSprite();

        int ingredient = LevelManager.THIS.target == Target.COLLECT ? RollIngredient(onlyNONEType) : -1;

        StartCoroutine(_fall.FallTo(square, true));
        if (ingredient >= 0)
        {
            color = 1000 + ingredient;
            currentType = ItemsTypes.INGREDIENT;
            _sprites.SetIngredientSprite(ingredient);
        }
        else
        {
            color = Array.IndexOf(items, sprRenderer.sprite);
        }
    }

    /// <summary>Index of the ingredient this new piece becomes, or -1. One roll per ingredient type.</summary>
    private int RollIngredient(bool onlyNONEType)
    {
        LevelManager lm = LevelManager.THIS;
        for (int i = 0; i < lm.NumIngredients; i++)
        {
            if (UnityEngine.Random.Range(0, 10 + (10 * lm.GetIngredients().Count)) == 0
                && square.row + 1 < lm.maxRows
                && !onlyNONEType
                && lm.ingrTarget[i].count > 0
                && lm.GetIngredients(i).Count < lm.ingrTarget[i].count)
            {
                return i;
            }
        }

        return -1;
    }

    public void SetColor(int col)
    {
        color = col;
        sprRenderer.sprite = items[color];
    }
    #endregion

    #region Item State Management
    public void AwakeItem()
    {
        awaken = true;
        _animation.PlaySelected();

        if (currentType != ItemsTypes.BOMB)
            _sprites.SetAwake();
        else
            SetLight();
    }

    public void SleepItem()
    {
        awaken = false;
        bool hasAmbientAnimation = appeared && currentType != ItemsTypes.BOMB;
        bool strongAmbientAnimation = currentType == ItemsTypes.PACKAGE;
        _animation.PlayNeutral(hasAmbientAnimation, strongAmbientAnimation);

        if (currentType != ItemsTypes.BOMB)
            _sprites.SetNeutral();

        _bomb.HideLight();
    }

    public void SetAppeared()
    {
        appeared = true;
        ItemsTypes animatedType = nextType != ItemsTypes.NONE ? nextType : currentType;
        _animation.PlayAmbient(animatedType == ItemsTypes.PACKAGE || animatedType == ItemsTypes.BOMB);
    }
    #endregion

    #region Item Type Changing
    private void CheckDebugTypeChange()
    {
        if (currentType != debugType && currentType != ItemsTypes.INGREDIENT && LevelManager.THIS.gameStatus == GameState.Playing)
        {
            Debug.Log("debug type " + debugType);
            Debug.Log("current type " + currentType);
            nextType = debugType;
            ChangeType();
        }
    }

    public void CheckChocoBomb(Item item1, Item item2)
    {
        _specials.CheckChocoBomb(item1, item2);
    }

    public void ChangeType()
    {
        if (this != null)
            StartCoroutine(ChangeTypeCor());
    }

    IEnumerator ChangeTypeCor()
    {
        // A special item is created from an item that has just finished selection,
        // destruction and combo feedback. Any interrupted scale animation must not
        // become the new permanent scale of the booster.
        if(nextType == ItemsTypes.NONE)
            yield break;

        appeared = false;
        EnsureValidBoardScale();
        PlayTypeChangeEffect();

        // The appear tween can be killed (piece selected, board shaken); don't wait for it forever,
        // or the special piece (e.g. a level's bomb) never shows up.
        float appearDeadline = Time.time + 1.5f;
        while (!appeared && Time.time < appearDeadline)
            yield return new WaitForFixedUpdate();
        appeared = true;

        EnsureValidBoardScale();

        if (nextType == ItemsTypes.NONE)
            yield break;

        _sprites.ApplyChangedTypeSprite(nextType);
        if (nextType == ItemsTypes.BOMB)
            _bomb.Setup();

        debugType = nextType;
        currentType = nextType;
        nextType = ItemsTypes.NONE;
    }

    private void PlayTypeChangeEffect()
    {
        if (nextType == ItemsTypes.HORIZONTAL_STRIPPED || nextType == ItemsTypes.VERTICAL_STRIPPED)
        {
            _effects.PlayStripeAppear(nextType);
            SoundBase.Instance.PlaySound(SoundBase.Instance.appearStipedColorBomb);
        }
        else if (nextType == ItemsTypes.PACKAGE || nextType == ItemsTypes.CHOCOBOMB)
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.appearStipedColorBomb);

            if (nextType == ItemsTypes.CHOCOBOMB)
                color = 555;
        }
        else if (nextType == ItemsTypes.BOMB)
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.appearStipedColorBomb);

            EnsureValidBoardScale();
        }

        _animation.PlayAppear(SetAppeared);
    }

    private void EnsureValidBoardScale()
    {
        Vector3 scale = transform.localScale;
        if (Mathf.Abs(scale.x) < MinimumValidBoardItemScale ||
            Mathf.Abs(scale.y) < MinimumValidBoardItemScale)
        {
            scale = new Vector3(DefaultBoardItemScale, DefaultBoardItemScale, 1f);
            transform.localScale = scale;
        }
    }
    #endregion

    #region Bomb
    // Called by GameField.BroadcastMessage("BombTick") once per move.
    public void BombTick()
    {
        _bomb.Tick();
    }

    // Callback of the bomb explosion animation.
    public void BombFailedAnimCallBack()
    {
        LevelManager.Instance.gameStatus = GameState.GameOver;
    }

    public void SetLight()
    {
        _bomb.ShowLight();
    }
    #endregion

    #region Item Movement
    public void CheckNeedToFall(Square _square)
    {
        _fall.MoveLinkTo(_square);
    }

    public void StartFalling()
    {
        if (!falling)
            StartCoroutine(_fall.FallTo(square, true));
    }

    public bool GetNearEmptySquares()
    {
        return _fall.SlideIntoNearHole();
    }
    #endregion

    #region Item Destruction
    // Animation event / destroy tween callback.
    public void SetAnimationDestroyingFinished()
    {
        LevelManager.THIS.itemsHided = true;
        animationFinished = true;
    }

    public void DestroyItem(bool showScore = false, string anim_name = "", bool explEffect = false, bool directly = false)
    {
        if (destroying)
            return;

        if (this == null)
            return;

        _animation.StopAll(resetVisual: true);
        animationFinished = false;
        destroying = true;
        square.item = null;

        if (LevelManager.THIS != null && LevelManager.THIS.gameStatus == GameState.Playing)
            Models.LevelSessionStats.ItemsCollected++;

        if (this == null)
            return;

        StartCoroutine(DestroyCor(showScore, anim_name, explEffect, directly));
    }

    public void SetHighlight(ItemsTypes thisType)
    {
        List<Item> itemsList = new List<Item>();

        if (thisType == ItemsTypes.HORIZONTAL_STRIPPED)
            itemsList = LevelManager.THIS.GetRow(square.row);
        else if (thisType == ItemsTypes.VERTICAL_STRIPPED)
            itemsList = LevelManager.THIS.GetColumn(square.col);

        foreach (Item itemSelected in itemsList)
        {
            if (itemSelected != null)
            {
                if ((itemSelected.currentType == ItemsTypes.HORIZONTAL_STRIPPED ||
                     itemSelected.currentType == ItemsTypes.VERTICAL_STRIPPED) &&
                    LevelManager.THIS.highlightedItems.IndexOf(itemSelected) < 0 &&
                    itemSelected != this)
                {
                    LevelManager.THIS.highlightedItems.Add(itemSelected);
                    ItemsTypes reverseType = ItemsTypes.HORIZONTAL_STRIPPED;

                    if (thisType == ItemsTypes.HORIZONTAL_STRIPPED)
                        reverseType = ItemsTypes.VERTICAL_STRIPPED;

                    itemSelected.SetHighlight(reverseType);
                }

                itemSelected.square.SetActiveCage(true, true);
                itemSelected.square.HighLight(true);
            }
        }
    }

    IEnumerator DestroyCor(bool showScore = false, string anim_name = "", bool explEffect = false, bool directly = false)
    {
        _animation.PlayNeutral(ambient: false);

        if (currentType == ItemsTypes.PACKAGE)
        {
            yield return new WaitForSeconds(0.1f);

            GameObject partcl = Instantiate(Resources.Load("Prefabs/Effects/Firework"), transform.position, Quaternion.identity) as GameObject;
            partcl.GetComponent<ParticleSystem>().startColor = LevelManager.THIS.scoresColors[color];
            Destroy(partcl, 1f);
        }

        bool hasDestroyAnimation = currentType != ItemsTypes.INGREDIENT && currentType != ItemsTypes.CHOCOBOMB;
        if (hasDestroyAnimation)
        {
            // A caged piece survives until the cage is broken.
            if (square.type == SquareTypes.WIREBLOCK && square.cageHP > 0)
            {
                destroying = false;
                square.item = this;
                yield break;
            }

            _animation.PlayDestroy(SetAnimationDestroyingFinished);
            _effects.PlayPop();
        }

        _effects.ReleaseTimeBonus();

        if (showScore)
            LevelManager.THIS.PopupScore(LevelManager.THIS.scoreForItem, transform.position, color);

        LevelManager.THIS.CheckCollectedTarget(gameObject);

        float destroyWaitTimeout = 1.5f;
        float destroyWaitElapsed = 0f;
        while (hasDestroyAnimation && !animationFinished)
        {
            destroyWaitElapsed += Time.fixedDeltaTime;
            if (destroyWaitElapsed >= destroyWaitTimeout)
            {
                animationFinished = true;
                break;
            }

            yield return new WaitForFixedUpdate();
        }

        square.DestroyBlock();

        // Special pieces destroyed directly (not by their own chain) explode here.
        if (directly && !stripeFired)
        {
            if (currentType == ItemsTypes.HORIZONTAL_STRIPPED)
                DestroyHorizontal();
            else if (currentType == ItemsTypes.VERTICAL_STRIPPED)
                DestroyVertical();
        }

        if (currentType == ItemsTypes.PACKAGE)
            DestroyPackage();
        else if (currentType == ItemsTypes.CHOCOBOMB && LevelManager.THIS.gameStatus == GameState.PreWinAnimations)
            CheckChocoBomb(this, LevelManager.THIS.GetRandomItems(1)[0]);

        // The chain asked for a special piece on this square.
        if (nextType != ItemsTypes.NONE)
        {
            Item i = square.GenItem();
            i.nextType = nextType;
            i.SetColor(color);
            i.ChangeType();
        }

        if (destroying)
        {
            Destroy(gameObject);
        }
    }

    public void DestroyHorizontal(bool boost = false)
    {
        StartCoroutine(_specials.DestroyStriped(boost, true));
    }

    public void DestroyVertical(bool boost = false)
    {
        StartCoroutine(_specials.DestroyStriped(boost, false));
    }

    public void DestroyPackage()
    {
        _specials.DestroyPackage();
    }

    public void DestroyColor(int p)
    {
        _specials.DestroyColor(p);
    }

    public void SmoothDestroy()
    {
        square.item = null;
        _animation.PlayDisappear(() => Destroy(gameObject));
    }

    public void PlayHintAnimation()
    {
        _animation.PlayHint();
    }

    public void PlayAppearAnimation()
    {
        _animation.PlayAppear(completed: null);
    }

    public void StopVisualAnimations()
    {
        // Destroy tweens must not be killed mid-flight: ProcessBoard freezes the board
        // right after striped boosters start DestroyItem, and Kill(complete:false) leaves
        // destroying=true / animationFinished=false forever, so gravity never resumes.
        if (destroying)
        {
            return;
        }

        _animation.StopAll(resetVisual: true);
    }
    #endregion

    #region Utility Methods
    public bool IsExtraItem()
    {
        return currentType == ItemsTypes.HORIZONTAL_STRIPPED || currentType == ItemsTypes.VERTICAL_STRIPPED;
    }
    #endregion
}

[System.Serializable]
public class StripedItem
{
    public Sprite horizontal;
    public Sprite vertical;
}
