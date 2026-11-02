using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MovesTime : MonoBehaviour
{
    public Sprite[] sprites;
    // Use this for initialization
    void OnEnable()
    {
        StartCoroutine(WaitForLoading());
    }

    IEnumerator WaitForLoading()
    {
        yield return LevelManager.THIS;
        var moves = LevelManager.THIS.limitType == LIMIT.MOVES;
        GetComponent<Image>().sprite = moves ? sprites[0] : sprites[1];
        if (label != null)
            label.text = moves ? GameLocalization.L10n.Get("moves", "MOVES") : GameLocalization.L10n.Get("time", "TIME");
    }

    // Localized replacement of the baked "MOVES"/"TIME" sprite text (optional).
    public TMPro.TMP_Text label;

    // Update is called once per frame
    void Update()
    {

    }
}
