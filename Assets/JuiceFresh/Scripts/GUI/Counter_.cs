using UnityEngine;
using System.Collections;
using TMPro;
using UI;

[RequireComponent(typeof(TMP_Text))]
public class Counter_ : MonoBehaviour
{
    public int ingrTrackNumber;
    private TMP_Text text;
    private float lastTime;
    bool alert;
    public int totalCount;
    TargetGUI parentGUI;
    private bool punchOnChange;
    private string lastPunchText;

    private string DisplayText
    {
        get => text.text;
        set => text.text = value;
    }

    private Transform TextTransform => text.transform;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        punchOnChange = name == "Limit" || name == "Score" || name == "TargetBlocks" || name == "TargetCages"
                        || name == "TargetBombs" || name == "CountIngr";
    }

    // Use this for initialization
    void Start()
    {
        if (transform.parent.GetComponent<TargetGUI>() != null)
            parentGUI = transform.parent.GetComponent<TargetGUI>();
    }

    void OnEnable()
    {
        lastTime = 0;
        lastPunchText = null;
        UpdateText();
        alert = false;
        if (name == "TargetScore" && LevelManager.THIS != null)
        {
            if (LevelManager.THIS.target == Target.SCORE)
                DisplayText = "" + LevelManager.THIS.GetScoresOfTargetStars();
        }

    }

    void UpdateText()
    {
        if (name == "StarsT")
        {
            DisplayText = "" + (LevelManager.THIS.stars);
        }
        // Добавьте другие проверки для обновления текста в зависимости от имени компонента, если нужно
    }

    // Update is called once per frame
    void Update()
    {
        if (name == "Score")
        {
            DisplayText = "" + LevelManager.Score;
        }
        if (name == "LabelKeepPlay")
        {
            if (LevelManager.THIS.limitType == LIMIT.MOVES)
                DisplayText = "GET + " + LevelManager.THIS.ExtraFailedMoves + " moves";
            else
                DisplayText = "GET + " + LevelManager.THIS.ExtraFailedSecs + " secs";

        }

        if (name == "BestScore")
        {
            DisplayText = "Best score:" + PlayerPrefs.GetInt("Score" + PlayerPrefs.GetInt("OpenLevel"));
        }

        if (name == "Limit")
        {
            if (LevelManager.Instance.limitType == LIMIT.MOVES)
            {
                DisplayText = "" + LevelManager.THIS.Limit;
                //txt.transform.GetComponent<RectTransform>().anchoredPosition = new Vector3(-460, -42, 0);
                //txt.transform.localScale = Vector3.one;
                if (LevelManager.THIS.Limit <= 5)
                {
                    //txt.color = Color.red;
                    //txt.GetComponent<Outline>().effectColor = new Color(214 / 255, 0, 196 / 255);
                    if (!alert)
                    {
                        alert = true;
                        SoundBase.Instance.PlaySound(SoundBase.Instance.alert);
                    }

                }
                else
                {
                    alert = false;
                    //txt.color = new Color(214f / 255f, 0, 196f / 255f);
                    //txt.GetComponent<Outline>().effectColor = new Color(148f / 255f, 61f / 255f, 95f / 255f);
                }

            }
            else
            {
                int minutes = Mathf.FloorToInt(LevelManager.THIS.Limit / 60F);
                int seconds = Mathf.FloorToInt(LevelManager.THIS.Limit - minutes * 60);
                DisplayText = "" + string.Format("{0:00}:{1:00}", minutes, seconds);
                TextTransform.localScale = Vector3.one * 0.35f;
                //txt.transform.GetComponent<RectTransform>().anchoredPosition = new Vector3(-445, -42, 0);
                if (LevelManager.THIS.Limit <= 30 && LevelManager.THIS.gameStatus == GameState.Playing)
                {
                    //txt.color = Color.red;
                    //txt.GetComponent<Outline>().effectColor = Color.white;
                    if (lastTime + 30f < Time.time)
                    {
                        lastTime = Time.time;
                        SoundBase.Instance.PlaySound(SoundBase.Instance.timeOut);
                    }

                }
                else
                {
                    //txt.color = new Color(214f / 255f, 0, 196f / 255f);
                    //txt.GetComponent<Outline>().effectColor = new Color(148f / 255f, 61f / 255f, 95f / 255f);
                }

            }
        }
        if (name == "TargetBlocks")
        {
            DisplayText = "" + (totalCount - LevelManager.THIS.targetBlocks) + "/" + totalCount;
            if (LevelManager.THIS.targetBlocks == 0)
                parentGUI.Done();
        }
        if (name == "TargetCages")
        {
            DisplayText = "" + (totalCount - LevelManager.THIS.TargetCages) + "/" + totalCount;
            if (LevelManager.THIS.TargetCages == 0)
                parentGUI.Done();
        }
        if (name == "TargetBombs")
        {
            DisplayText = "" + (LevelManager.THIS.TargetBombs) + "/" + totalCount;
            if (LevelManager.THIS.TargetBombs >= totalCount)
                parentGUI.Done();
        }
        if (name == "CountIngr")
        {
            DisplayText = "" + (totalCount - LevelManager.THIS.ingrTarget[ingrTrackNumber].count) + "/" + totalCount;
            if (LevelManager.THIS.ingrTarget[ingrTrackNumber].count == 0)
                parentGUI.Done();
        }
        if (name == "CountIngrForMenu")
        {
            DisplayText = "" + totalCount;
            if (LevelManager.THIS.target == Target.BOMBS)
            {
                DisplayText = "" + LevelManager.THIS.bombsCollect; //1.4.5
            }
        }

        // if (name == "TargetIngr2") {
        // 	txt.text = "" + LevelManager.THIS.ingrCountTarget [1];
        // }
        if (name == "Lifes")
        {
            // txt.text = "" + InitScript.Instance.GetLife();
        }

        if (name == "Gems")
        {
            DisplayText = "" + (LevelManager.THIS.CurrencyService != null ? (int)(LevelManager.THIS.CurrencyService.GetCurrency(Systems.CurrencySystem.CurrencyType.Cash)?.Value ?? 0) : 0);
        }
        if (name == "Level")
        {
            DisplayText = "LEVEL " + PlayerPrefs.GetInt("OpenLevel");
        }
        if (punchOnChange)
            PunchIfChanged();

        if (name == "TargetDescription1")
        {
            if (LevelManager.THIS.target == Target.SCORE)
                DisplayText = LevelManager.THIS.targetDiscriptions[6].Replace("%n", "" + LevelManager.THIS.GetScoresOfTargetStars()).Replace("%s", "" + (int)LevelManager.THIS.starsTargetCount);
            else if (LevelManager.THIS.target == Target.BLOCKS)
                DisplayText = LevelManager.THIS.targetDiscriptions[1];
            else if (LevelManager.THIS.target == Target.COLLECT)
                DisplayText = LevelManager.THIS.targetDiscriptions[2];
            else if (LevelManager.THIS.target == Target.ITEMS)
                DisplayText = LevelManager.THIS.targetDiscriptions[3];
            else if (LevelManager.THIS.target == Target.CAGES)
                DisplayText = LevelManager.THIS.targetDiscriptions[4];
            else if (LevelManager.THIS.target == Target.BOMBS)
                DisplayText = LevelManager.THIS.targetDiscriptions[5];
        }


    }

    // Small bump whenever a gameplay counter changes so moves/score/targets read as alive.
    void PunchIfChanged()
    {
        string current = DisplayText;
        if (lastPunchText != null && !string.Equals(current, lastPunchText))
        {
            if (name != "Limit")
            {
                UIFeel.Punch(text, name == "Score" ? 0.08f : 0.18f);
            }
            else if (LevelManager.THIS.limitType == LIMIT.MOVES)
            {
                // Time limit text has its scale forced every frame, so only the moves counter punches.
                UIFeel.Punch(text, LevelManager.THIS.Limit <= 5 ? 0.28f : 0.15f);
            }
        }

        lastPunchText = current;
    }
}
