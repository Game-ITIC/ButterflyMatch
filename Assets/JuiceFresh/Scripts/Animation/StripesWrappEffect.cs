using UnityEngine;
using System.Collections;

public class StripesWrappEffect : MonoBehaviour
{
    public GameObject topArrow;
    public GameObject bottomArrow;
    public GameObject bombSelection1;
    public SpriteRenderer itemSprite;
    // The arrows breathe inside the cell.
    float offset = 0.06f;
    // Use this for initialization
    void Start()
    {
        iTween.MoveTo(topArrow, iTween.Hash("position", topArrow.transform.localPosition + Vector3.up * offset, "time", 1f, "loopType", iTween.LoopType.pingPong, "easetype", iTween.EaseType.linear, "islocal", true));
        iTween.MoveTo(bottomArrow, iTween.Hash("position", bottomArrow.transform.localPosition + Vector3.up * -offset, "time", 1f, "loopType", iTween.LoopType.pingPong, "easetype", iTween.EaseType.linear, "islocal", true));
        GameObject light = Instantiate(bombSelection1) as GameObject;
        light.transform.SetParent(transform);
        light.name = "BombSelection";
        light.transform.localPosition = Vector3.zero;
        light.transform.localScale = Vector3.one * 1.7f;
        // Above the piece: under it the wings hide them.
        topArrow.GetComponent<SpriteRenderer>().sortingOrder = itemSprite.sortingOrder + 1;
        bottomArrow.GetComponent<SpriteRenderer>().sortingOrder = itemSprite.sortingOrder + 1;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
