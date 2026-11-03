using UI;
using UnityEngine;
using System;

[RequireComponent(typeof(UIFillBar))]
public class ProgressBarScript : MonoBehaviour
{
    public static ProgressBarScript Instance { get; private set; }

    public GameObject[] stars;
    public Vector3 pivot;
    public Vector3 angles;

    public int DisplayedStars { get; private set; }
    public event Action<int> DisplayedStarsChanged;

    UIFillBar _fillBar;
    UIFillBarMilestones _milestones;
    RectTransform _barTrack;

    void OnEnable()
    {
        Instance = this;
        _fillBar = GetComponent<UIFillBar>();
        if (_fillBar == null)
        {
            _fillBar = gameObject.AddComponent<UIFillBar>();
        }

        _milestones = GetComponent<UIFillBarMilestones>();
        if (_milestones == null)
        {
            _milestones = gameObject.AddComponent<UIFillBarMilestones>();
        }

        _milestones.MilestoneRevealed -= HandleMilestoneRevealed;
        _milestones.MilestoneRevealed += HandleMilestoneRevealed;

        _barTrack = transform.parent as RectTransform;
        ConfigureForLevel();
    }

    void OnDisable()
    {
        if (_milestones != null)
        {
            _milestones.MilestoneRevealed -= HandleMilestoneRevealed;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetProgress(float currentValue, float maxValue)
    {
        _fillBar.SetFill(currentValue, maxValue);
    }

    public void UpdateDisplay(float normalizedProgress)
    {
        _fillBar.SetNormalizedFill(normalizedProgress);
    }

    public void AddValue(float delta)
    {
        _fillBar.SetNormalizedFill(_fillBar.NormalizedFill + delta);
    }

    public bool IsFull()
    {
        if (_fillBar.NormalizedFill >= 1f)
        {
            ResetBar();
            return true;
        }

        return false;
    }

    public void ResetBar()
    {
        _fillBar.SetNormalizedFill(0f, animated: false);
        SetDisplayedStars(0);
        _milestones?.ResetMilestones();
    }

    public void ConfigureForLevel()
    {
        ResetBar();
        PrepareStars();
    }

    void HandleMilestoneRevealed(int milestoneIndex, UIFillMilestone _)
    {
        SetDisplayedStars(milestoneIndex + 1);

        if (SoundBase.Instance != null)
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.getStarIngr);
        }
    }

    void SetDisplayedStars(int value)
    {
        int clampedValue = Mathf.Clamp(value, 0, 3);
        if (DisplayedStars == clampedValue)
        {
            return;
        }

        DisplayedStars = clampedValue;
        DisplayedStarsChanged?.Invoke(DisplayedStars);
    }

    void PrepareStars()
    {
        if (LevelManager.THIS == null || stars == null || stars.Length < 3)
        {
            return;
        }

        // The bar sits in a layout-driven HUD card: settle the layout so the track width is final.
        Canvas.ForceUpdateCanvases();
        float width = _barTrack != null ? _barTrack.rect.width : 0f;
        int maxScore = LevelManager.Instance.star3;
        if (width <= 0f || maxScore <= 0)
        {
            return;
        }

        float halfWidth = width * 0.5f;
        PositionStar(stars[0], LevelManager.Instance.star1, width, halfWidth, maxScore);
        PositionStar(stars[1], LevelManager.Instance.star2, width, halfWidth, maxScore);
        PositionStar(stars[2], maxScore, width, halfWidth, maxScore);

        _milestones.Setup(_fillBar, _barTrack, new[]
        {
            CreateStarMilestone(LevelManager.Instance.star1, maxScore, stars[0]),
            CreateStarMilestone(LevelManager.Instance.star2, maxScore, stars[1]),
            CreateStarMilestone(maxScore, maxScore, stars[2])
        });
    }

    static UIFillMilestone CreateStarMilestone(int score, int maxScore, GameObject star)
    {
        return new UIFillMilestone
        {
            threshold = maxScore > 0 ? Mathf.Clamp01((float)score / maxScore) : 0f,
            target = star,
            revealMode = MilestoneRevealMode.ActivateFirstChild
        };
    }

    static void PositionStar(GameObject star, int starScore, float width, float halfWidth, int maxScore)
    {
        if (star == null)
        {
            return;
        }

        float x = starScore * width / maxScore - halfWidth;
        var position = star.transform.localPosition;
        star.transform.localPosition = new Vector3(x, position.y, position.z);
    }

    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivotPoint, Vector3 rotationAngles)
    {
        Vector3 dir = point - pivotPoint;
        dir = Quaternion.Euler(rotationAngles) * dir;
        return dir + pivotPoint;
    }
}
