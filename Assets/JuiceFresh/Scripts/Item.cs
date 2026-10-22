using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using JuiceFresh;
using TMPro;
using UnityEngine.UI;

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
/// Represents an individual game item on the game board.
/// Handles item behaviors like falling, matching, special effects, etc.
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
    private Sprite[] ingredientItems;
    private TMP_Text timerText;
    private GameObject _light;
    private ItemTweenAnimator _animation;
    private bool extraChecked;
    private int COLOR;
    private const float DefaultBoardItemScale = 0.9f;
    private const float MinimumValidBoardItemScale = 0.5f;
    #endregion

    #region Properties
    public Square square { get; set; }
    
    public bool dragThis { get; set; }
    public Vector3 mousePos { get; set; }
    public Vector3 deltaPos { get; set; }
    public Vector3 switchDirection { get; set; }
    
    private Square neighborSquare { get; set; }
    private Item switchItem { get; set; }
    
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
    }

    void Start()
    {
        InitializeItem();
    }

    void Update()
    {
        COLORView = color;
        CheckDebugTypeChange();
        UpdateBombTimer();
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
        ingredientItems = LevelManager.THIS.ingrediendSprites;
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
    /// Generates a color for the item, avoiding matches at creation time.
    /// </summary>
    /// <param name="exceptColor">Color to exclude</param>
    /// <param name="onlyNONEType">Whether to only generate color for NONE type items</param>
    public void GenColor(int exceptColor = -1, bool onlyNONEType = false)
    {
        int row = square.row;
        int col = square.col;

        List<int> remainColors = GetAvailableColors(row, col, exceptColor);
        SetItemColor(remainColors, exceptColor, onlyNONEType);
    }

    private List<int> GetAvailableColors(int row, int col, int exceptColor)
    {
        List<int> remainColors = new List<int>();
        
        for (int i = 0; i < LevelManager.Instance.colorLimit; i++)
        {
            bool canGen = true;
            
            // Check left neighbor
            if (col > 1)
            {
                Square neighbor = LevelManager.Instance.GetSquare(row, col - 1);
                if (neighbor != null && neighbor.item != null && neighbor.CanGoInto() && neighbor.item.color == i)
                    canGen = false;
            }
            
            // Check right neighbor
            if (col < LevelManager.Instance.maxCols - 1)
            {
                Square neighbor = LevelManager.Instance.GetSquare(row, col + 1);
                if (neighbor != null && neighbor.item != null && neighbor.CanGoOut() && neighbor.item.color == i)
                    canGen = false;
            }
            
            // Check bottom neighbor
            if (row < LevelManager.Instance.maxRows)
            {
                Square neighbor = LevelManager.Instance.GetSquare(row + 1, col);
                if (neighbor != null && neighbor.item != null && neighbor.CanGoOut() && neighbor.item.color == i)
                    canGen = false;
            }
            
            if (canGen && i != exceptColor)
            {
                remainColors.Add(i);
            }
        }
        
        return remainColors;
    }

    private void SetItemColor(List<int> remainColors, int exceptColor, bool onlyNONEType)
    {
        int randColor = UnityEngine.Random.Range(0, LevelManager.Instance.colorLimit);
        
        if (remainColors.Count > 0)
            randColor = remainColors[UnityEngine.Random.Range(0, remainColors.Count)];
        
        if (exceptColor == randColor)
            randColor = (randColor++) % items.Length;
            
        LevelManager.THIS.lastRandColor = randColor;
        sprRenderer.sprite = items[randColor];
        
        UpdateSpriteBasedOnType(randColor);
        
        if (LevelManager.THIS.target == Target.COLLECT)
        {
            TryCreateIngredient(onlyNONEType);
        }
        else
        {
            StartCoroutine(FallingCor(square, true));
            color = Array.IndexOf(items, sprRenderer.sprite);
        }
    }

    private void UpdateSpriteBasedOnType(int randColor)
    {
        if (nextType == ItemsTypes.HORIZONTAL_STRIPPED)
            sprRenderer.sprite = stripedItems[color].horizontal;
        else if (nextType == ItemsTypes.VERTICAL_STRIPPED)
            sprRenderer.sprite = stripedItems[color].vertical;
        else if (nextType == ItemsTypes.PACKAGE)
            sprRenderer.sprite = packageItems[color];
        else if (nextType == ItemsTypes.CHOCOBOMB)
            sprRenderer.sprite = ChocoBombItems[0];
    }

    private void TryCreateIngredient(bool onlyNONEType)
    {
        for (int i = 0; i < LevelManager.THIS.NumIngredients; i++)
        {
            if (UnityEngine.Random.Range(0, 10 + (10 * LevelManager.THIS.GetIngredients().Count)) == 0 
                && square.row + 1 < LevelManager.THIS.maxRows 
                && !onlyNONEType 
                && LevelManager.THIS.ingrTarget[i].count > 0)
            {
                if (LevelManager.THIS.GetIngredients(i).Count < LevelManager.THIS.ingrTarget[i].count)
                {
                    SetIngr(i);
                    break;
                }
                else
                {
                    StartCoroutine(FallingCor(square, true));
                    color = Array.IndexOf(items, sprRenderer.sprite);
                }
            }
            else
            {
                StartCoroutine(FallingCor(square, true));
                color = Array.IndexOf(items, sprRenderer.sprite);
            }
        }
    }

    void SetIngr(int i)
    {
        StartCoroutine(FallingCor(square, true));
        color = 1000 + (int)i;
        currentType = ItemsTypes.INGREDIENT;
        sprRenderer.sprite = LevelManager.THIS.ingrTarget[i].sprite;
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
            SetAwakeSprite();
        else if (currentType == ItemsTypes.BOMB)
            SetLight();
    }

    public void SleepItem()
    {
        awaken = false;
        bool hasAmbientAnimation = appeared && currentType != ItemsTypes.BOMB;
        bool strongAmbientAnimation = currentType == ItemsTypes.PACKAGE;
        _animation.PlayNeutral(hasAmbientAnimation, strongAmbientAnimation);

        if (currentType != ItemsTypes.BOMB)
            SetNeutralSprite();
            
        if (_light != null)
            _light.SetActive(false);
    }

    private void SetAwakeSprite()
    {
        switch (currentType)
        {
            case ItemsTypes.INGREDIENT:
                SetIngredientSprite();
                break;
            case ItemsTypes.PACKAGE:
                SetSpriteIfAvailable(packageItems, color);
                break;
            case ItemsTypes.CHOCOBOMB:
                SetSpriteIfAvailable(ChocoBombItems, 0);
                break;
            default:
                SetSpriteIfAvailable(itemsAnimation, color);
                break;
        }
    }

    private void SetNeutralSprite()
    {
        switch (currentType)
        {
            case ItemsTypes.INGREDIENT:
                SetIngredientSprite();
                break;
            case ItemsTypes.PACKAGE:
                SetSpriteIfAvailable(packageItems, color);
                break;
            case ItemsTypes.CHOCOBOMB:
                SetSpriteIfAvailable(ChocoBombItems, 0);
                break;
            default:
                SetSpriteIfAvailable(items, color);
                break;
        }
    }

    private void SetIngredientSprite()
    {
        int targetIndex = color - 1000;
        if (LevelManager.THIS == null || targetIndex < 0 || targetIndex >= LevelManager.THIS.ingrTarget.Count)
            return;

        sprRenderer.sprite = LevelManager.THIS.ingrTarget[targetIndex].sprite;
    }

    private void SetSpriteIfAvailable(Sprite[] sprites, int spriteIndex)
    {
        if (sprRenderer == null || sprites == null || spriteIndex < 0 || spriteIndex >= sprites.Length)
            return;

        sprRenderer.sprite = sprites[spriteIndex];
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

    private void UpdateBombTimer()
    {
        if (currentType == ItemsTypes.BOMB && timerText != null)
            timerText.text = "" + bombTimer;
    }

    public void CheckChocoBomb(Item item1, Item item2)
    {
        if (item1.currentType == ItemsTypes.INGREDIENT || item2.currentType == ItemsTypes.INGREDIENT)
            return;
            
        if (item1.currentType == ItemsTypes.CHOCOBOMB)
        {
            HandleChocoBombInteraction(item1, item2);
        }
    }

    private void HandleChocoBombInteraction(Item item1, Item item2)
    {
        if (item2.currentType == ItemsTypes.NONE)
            DestroyColor(item2.color);
        else if (item2.currentType == ItemsTypes.HORIZONTAL_STRIPPED || item2.currentType == ItemsTypes.VERTICAL_STRIPPED)
            LevelManager.THIS.SetTypeByColor(item2.color, ItemsTypes.HORIZONTAL_STRIPPED);
        else if (item2.currentType == ItemsTypes.PACKAGE)
            LevelManager.THIS.SetTypeByColor(item2.color, ItemsTypes.PACKAGE);
        else if (item2.currentType == ItemsTypes.CHOCOBOMB)
            LevelManager.THIS.DestroyDoubleBomb(square.col);

        item1.DestroyItem();
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
        ApplyVisualEffect();
        
        while (!appeared)
            yield return new WaitForFixedUpdate();

        EnsureValidBoardScale();

        if (nextType == ItemsTypes.NONE)
            yield break;
            
        SetSpriteForType();
        
        debugType = nextType;
        currentType = nextType;
        nextType = ItemsTypes.NONE;
    }

    private void ApplyVisualEffect()
    {
        if (nextType == ItemsTypes.HORIZONTAL_STRIPPED || nextType == ItemsTypes.VERTICAL_STRIPPED)
        {
            StripeEffect(nextType);
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

    private void SetSpriteForType()
    {
        if (nextType == ItemsTypes.PACKAGE)
            sprRenderer.sprite = packageItems[color];
        else if (nextType == ItemsTypes.CHOCOBOMB)
            sprRenderer.sprite = ChocoBombItems[0];
        else if (nextType == ItemsTypes.BOMB)
        {
            sprRenderer.sprite = bombItems[color];
            SetupBomb();
        }
    }

    void StripeEffect(ItemsTypes _itemType)
    {
        GameObject obj = Instantiate(Resources.Load("Prefabs/StripeEffect")) as GameObject;
        obj.transform.SetParent(transform.Find("Sprite"));
        obj.transform.localScale = Vector3.one;
        obj.GetComponent<StripesWrappEffect>().itemSprite = transform.Find("Sprite").GetComponent<SpriteRenderer>();
        obj.transform.localPosition = Vector3.zero;
        
        if (_itemType == ItemsTypes.HORIZONTAL_STRIPPED)
            obj.transform.eulerAngles = new Vector3(0, 0, 90);
            
        transform.Find("Sprite").GetComponent<SpriteRenderer>().sortingOrder = 3;

        GameObject effect = Instantiate(appearingEffect) as GameObject;
        effect.transform.position = transform.position;
        Destroy(effect, 2);
    }

    void SetupBomb()
    {
        _animation.PlayAmbient(strong: true);

        GameObject t = Instantiate(timerTextPrefab) as GameObject;
        t.transform.SetParent(transform);
        t.transform.localPosition = new Vector3(1.5f, -0.8f, 0);
        t.transform.localScale = Vector3.one;
        timerText = t.transform.GetChild(0).GetComponent<TMP_Text>();
        
        if (bombTimer <= 0)
            bombTimer = LevelManager.Instance.bombTimer;

        GameObject wickBurning = Instantiate(wickBurningPrefab) as GameObject;
        wickBurning.transform.SetParent(transform);
        wickBurning.transform.localPosition = new Vector3(0.69f, 0.85f, 0);
        wickBurning.transform.localScale = Vector3.one * 0.2f;
    }

    public void BombTick()
    {
        bombTimer--;
        if (bombTimer <= 0 && currentType == ItemsTypes.BOMB && LevelManager.Instance.gameStatus == GameState.Playing)
        {
            LevelManager.Instance.gameStatus = GameState.PreFailedBomb;
            AwakeItem();
            sprRenderer.sortingOrder = 4;
            timerText.enabled = false;
            square = null;
            AnimatorHelper.Instance.BombFailed(gameObject, transform.position, Vector3.zero, 20, BombFailedAnimCallBack);
        }
    }

    public void BombFailedAnimCallBack()
    {
        LevelManager.Instance.gameStatus = GameState.GameOver;
    }

    public void SetLight()
    {
        if (_light == null)
        {
            _light = Instantiate(bombSelection) as GameObject;
            _light.transform.SetParent(transform);
            _light.name = "BombSelection";
            _light.transform.localPosition = Vector3.zero;
            
            if (currentType == ItemsTypes.BOMB)
                _light.transform.localScale = Vector3.one * 3.6f;
            else
                _light.transform.localScale = Vector3.one * 2f;
        }
        _light.SetActive(true);
    }
    #endregion

    #region Item Movement
    public void CheckNeedToFall(Square _square)
    {
        _square.item = this;
        square.item = null;
        square = _square;
    }

    public void StartFalling()
    {
        if (!falling)
            StartCoroutine(FallingCor(square, true));
    }

    IEnumerator FallingCor(Square _square, bool animate)
    {
        falling = true;
        float startTime = Time.time;
        Vector3 startPos = transform.position;
        float speed = 10;
        
        if (LevelManager.THIS.gameStatus == GameState.PreWinAnimations)
            speed = 10;
            
        float distance = Vector3.Distance(startPos, _square.transform.position);
        float fracJourney = 0;
        
        if (distance > 0.5f)
        {
            while (fracJourney < 1)
            {
                speed += 0.2f;
                float distCovered = (Time.time - startTime) * speed;
                fracJourney = distCovered / distance;
                transform.position = Vector3.Lerp(startPos, _square.transform.position + Vector3.back * 0.2f, fracJourney);
                yield return new WaitForFixedUpdate();
                if (fracJourney > 0.3f)
                    falling = false;
            }
        }
        
        if (distance > 0.5f && animate)
        {
            _animation.PlayLanding();
            SoundBase.Instance.PlaySound(SoundBase.Instance.drop[UnityEngine.Random.Range(0, SoundBase.Instance.drop.Length)]);
        }
        
        falling = false;
        justCreatedItem = false;
    }

    public bool GetNearEmptySquares()
    {
        bool nearEmptySquareDetected = false;
        
        if (!square.CanGoOut())
            return false;
            
        // Check bottom-right
        if (square.row < LevelManager.Instance.maxRows - 1 && square.col < LevelManager.Instance.maxCols)
        {
            Square checkingSquare = LevelManager.Instance.GetSquare(square.col + 1, square.row + 1, true);
            
            if (checkingSquare.CanFallInto() && checkingSquare.item == null && !falling)
            {
                checkingSquare = LevelManager.Instance.GetSquare(square.col + 1, square.row + 1, true);
                
                if (checkingSquare.CanFallInto() && checkingSquare.item == null)
                {
                    square.item = null;
                    checkingSquare.item = this;
                    square = checkingSquare;
                    StartFalling();
                    nearEmptySquareDetected = true;
                }
            }
        }
        
        // Check bottom-left
        if (square.row < LevelManager.Instance.maxRows - 1 && square.col > 0)
        {
            Square checkingSquare = LevelManager.Instance.GetSquare(square.col - 1, square.row + 1, true);
            
            if (checkingSquare.CanFallInto() && checkingSquare.item == null && !falling)
            {
                checkingSquare = LevelManager.Instance.GetSquare(square.col - 1, square.row + 1, true);
                
                if (checkingSquare.CanFallInto() && checkingSquare.item == null)
                {
                    square.item = null;
                    checkingSquare.item = this;
                    square = checkingSquare;
                    StartFalling();
                    nearEmptySquareDetected = true;
                }
            }
        }
        
        return nearEmptySquareDetected;
    }
    #endregion

    #region Item Destruction
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
        
        if (currentType != ItemsTypes.INGREDIENT && currentType != ItemsTypes.CHOCOBOMB)
        {
            if (square.type == SquareTypes.WIREBLOCK)
            {
                if (square.cageHP > 0)
                {
                    destroying = false;
                    square.item = this;
                    yield break;
                }
            }

            PlayDestroyAnimation();
            CreateDestructionEffect();
        }

        // Handle 5-second bonus
        HandleTimeBonus();

        if (showScore)
            LevelManager.THIS.PopupScore(LevelManager.THIS.scoreForItem, transform.position, color);

        LevelManager.THIS.CheckCollectedTarget(gameObject);

        bool hasDestroyAnimation = currentType != ItemsTypes.INGREDIENT && currentType != ItemsTypes.CHOCOBOMB;
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
        
        // Handle special item explosions
        if (directly)
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

        // Create new item of special type if needed
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

    private void CreateDestructionEffect()
    {
        GameObject partcl = LevelManager.THIS.GetExplFromPool();
        if (partcl != null)
        {
            partcl.GetComponent<ItemAnimEvents>().item = this;
            partcl.transform.localScale = Vector3.one * 1f;
            partcl.transform.position = transform.position;
            
            GameObject psObj = Instantiate(partcl.GetComponent<ItemAnimEvents>().particals, partcl.transform.position, Quaternion.identity) as GameObject;
            int spr = GetSpriteIndexForParticles();

            var ps = psObj.GetComponent<ParticleSystem>();
            var ts = ps.textureSheetAnimation;
            ts.frameOverTime = new ParticleSystem.MinMaxCurve((float)spr / ts.numTilesX);

            psObj.GetComponent<ParticleSystem>().Play();
            Destroy(psObj, 2);
            
            partcl.transform.rotation = Quaternion.Euler(new Vector3(0, 0, UnityEngine.Random.Range(0f, 360f)));
            partcl.GetComponent<Animator>().SetInteger("color", color);
            SoundBase.Instance.PlaySoundsRandom(SoundBase.Instance.pops);
            GameFeelManager.Instance?.OnItemDestroyed(transform.position);
        }
    }

    private int GetSpriteIndexForParticles()
    {
        int spr = 0;
        switch (color)
        {
            case 0: spr = 3; break;
            case 1: spr = 2; break;
            case 2: spr = 4; break;
            case 3: spr = 1; break;
            case 4: spr = 0; break;
            case 5: spr = 5; break;
        }
        return spr;
    }

    private void HandleTimeBonus()
    {
        if (LevelManager.THIS.limitType == LIMIT.TIME && transform.Find("5sec") != null)
        {
            GameObject FiveSec = transform.Find("5sec").gameObject;
            FiveSec.transform.SetParent(null);
            #if UNITY_5
            FiveSec.GetComponent<Animation>().clip.legacy = true;
            #endif

            FiveSec.GetComponent<Animation>().Play("5secfly");
            Destroy(FiveSec, 1);
            
            if (LevelManager.THIS.gameStatus == GameState.Playing)
                LevelManager.THIS.Limit += 5;
        }
    }

    public void DestroyHorizontal(bool boost = false)
    {
        StartCoroutine(DestroyStrippedCor(boost, true));
    }

    public void DestroyVertical(bool boost = false)
    {
        StartCoroutine(DestroyStrippedCor(boost, false));
    }

    IEnumerator DestroyStrippedCor(bool boost, bool horrizontal)
    {
        SoundBase.Instance.PlayLimitSound(SoundBase.Instance.strippedExplosion);
        LevelManager.THIS.StrippedShow(gameObject, horrizontal);
        
        List<Square> itemsList = null;
        if (horrizontal)
            itemsList = LevelManager.THIS.GetRowSquare(square.row);
        else
            itemsList = LevelManager.THIS.GetColumnSquare(square.col);
            
        foreach (var _square in itemsList)
        {
            if (_square != null)
            {
                if (_square.item != this && _square.item != null)
                {
                    if (_square.item.currentType != ItemsTypes.CHOCOBOMB && 
                        _square.item.currentType != ItemsTypes.INGREDIENT && 
                        _square.square.CheckDamage(5) && 
                        !_square.item.extraChecked)
                    {
                        if (_square.item.currentType == ItemsTypes.HORIZONTAL_STRIPPED)
                        {
                            _square.item.extraChecked = true;
                            _square.item.DestroyVertical();
                        }
                        else if (_square.item.currentType == ItemsTypes.VERTICAL_STRIPPED)
                        {
                            _square.item.extraChecked = true;
                            _square.item.DestroyHorizontal();
                        }
                        else
                        {
                            _square.item.DestroyItem(true, "", false, true);
                        }
                    }
                }
                else if (_square.item == null && 
                        (_square.square.type == SquareTypes.BLOCK || 
                         _square.square.type == SquareTypes.SOLIDBLOCK || 
                         _square.square.type == SquareTypes.THRIVING))
                {
                    _square.square.DestroyBlock();
                }
            }
        }
        
        yield return new WaitForFixedUpdate();

        if (!boost)
            DestroyItem(true, "", false, true);
    }

    public void DestroyPackage()
    {
        SoundBase.Instance.PlaySound(SoundBase.Instance.destroyPackage);

        List<Item> itemsList = LevelManager.THIS.GetItemsAround(square);
        foreach (Item item in itemsList)
        {
            if (item != null)
            {
                if (item.currentType != ItemsTypes.CHOCOBOMB && item.currentType != ItemsTypes.INGREDIENT)
                    item.DestroyItem(true, "destroy_package");
            }
        }
        
        SoundBase.Instance.PlaySound(SoundBase.Instance.explosion);
        currentType = ItemsTypes.NONE;
        DestroyItem(true);
    }

    public void DestroyColor(int p)
    {
        SoundBase.Instance.PlaySound(SoundBase.Instance.colorBombExpl);

        GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
        foreach (GameObject item in items)
        {
            if (item.GetComponent<Item>().color == p)
                item.GetComponent<Item>().DestroyItem(true, "", true);
        }
    }

    void PlayDestroyAnimation()
    {
        _animation.PlayDestroy(SetAnimationDestroyingFinished);
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
