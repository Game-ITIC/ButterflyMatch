using UnityEngine;
using System.Collections;
using TMPro;
using UI;
using GameLocalization;

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
                SetTargetText(L10n.Format("keep_play_moves", LevelManager.THIS.ExtraFailedMoves));
            else
                SetTargetText(L10n.Format("keep_play_secs", LevelManager.THIS.ExtraFailedSecs));

        }

        if (name == "BestScore")
        {
            SetTargetText(L10n.Format("best_score", PlayerPrefs.GetInt("Score" + PlayerPrefs.GetInt("OpenLevel"))));
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
            DisplayText = "" + (LevelManager.THIS.CurrencyService != null ? (int)(LevelManager.THIS.CurrencyService.GetCurrency(Systems.CurrencySystem.CurrencyType.Diamond)?.Value ?? 0) : 0);
        }
        if (name == "Level")
        {
            SetTargetText(L10n.Format("level_n", PlayerPrefs.GetInt("OpenLevel")));
        }
        if (punchOnChange)
            PunchIfChanged();

        if (name == "TargetDescription1")
        {
            var descriptions = LevelManager.THIS.targetDiscriptions;
            if (LevelManager.THIS.target == Target.SCORE)
                SetTargetText(L10n.Format("target_stars", (int)LevelManager.THIS.starsTargetCount));
            else if (LevelManager.THIS.target == Target.BLOCKS)
                SetTargetText(L10n.Get("target_blocks", descriptions[1]));
            else if (LevelManager.THIS.target == Target.COLLECT)
                SetTargetText(L10n.Get("target_collect", descriptions[2]));
            else if (LevelManager.THIS.target == Target.ITEMS)
                SetTargetText(L10n.Get("target_items", descriptions[3]));
            else if (LevelManager.THIS.target == Target.CAGES)
                SetTargetText(L10n.Get("target_cages", descriptions[4]));
            else if (LevelManager.THIS.target == Target.BOMBS)
                SetTargetText(L10n.Get("target_bombs", descriptions[5]));
        }


    }

    // Assign only when changed: avoids re-layout of the TMP mesh every frame.
    void SetTargetText(string value)
    {
        if (!string.Equals(DisplayText, value))
            DisplayText = value;
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
