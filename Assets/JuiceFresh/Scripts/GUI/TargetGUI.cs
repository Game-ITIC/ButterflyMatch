using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UI;

public class TargetGUI : MonoBehaviour {
    public Sprite star;
    public Sprite bomb;
    public GameObject bigBack;
    public GameObject small;
    public GameObject check;
    public GameObject text;
    public GameObject textForMenu;
    public Image image;

    public void SetSprite(Sprite sprite) {
        image.sprite = sprite;
    }

    void OnEnable() {
        Reset();
    }

    public void Reset() {
        text.SetActive(true);
        check.SetActive(false);

    }

    public void Done() {
        bool wasDone = check.activeSelf;
        text.SetActive(false);
        check.SetActive(true);

        if (!wasDone) {
            UIFeel.PopIn(check.transform, 0f, 0f, 0.4f);
            UIFeel.Punch(transform, 0.15f, 0.35f);
        }
    }

    public void SetBack(bool show) {
        if (show) {
            bigBack.SetActive(true);
            small.SetActive(false);
            text.SetActive(false);
            textForMenu.SetActive(true);
        }
        else {
            bigBack.SetActive(false);
            small.SetActive(false);
            text.SetActive(true);
            textForMenu.SetActive(false);
        }
    }
}
