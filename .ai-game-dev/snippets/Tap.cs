
using UnityEngine; using UnityEngine.EventSystems; using System.Collections.Generic;
public static class Tap {
  // x,y in screenshot pixels (top-left origin, 1080x2160)
  public static string At(float x, float y) {
    var es = EventSystem.current; if (es == null) return "no EventSystem";
    var pos = new Vector2(x * Screen.width / 1080f, Screen.height - y * Screen.height / 2160f);
    var ped = new PointerEventData(es) { position = pos, pressPosition = pos, button = PointerEventData.InputButton.Left };
    var hits = new List<RaycastResult>(); es.RaycastAll(ped, hits);
    if (hits.Count == 0) return "no hit at " + pos;
    var go = hits[0].gameObject; ped.pointerCurrentRaycast = hits[0]; ped.pointerPressRaycast = hits[0];
    var down = ExecuteEvents.ExecuteHierarchy(go, ped, ExecuteEvents.pointerDownHandler);
    ped.pointerPress = down; ped.rawPointerPress = go;
    var up = ExecuteEvents.ExecuteHierarchy(go, ped, ExecuteEvents.pointerUpHandler);
    var click = ExecuteEvents.GetEventHandler<IPointerClickHandler>(go);
    if (click != null) ExecuteEvents.Execute(click, ped, ExecuteEvents.pointerClickHandler);
    return "hit " + go.name + " click=" + (click ? click.name : "none") + " screen=" + Screen.width + "x" + Screen.height;
  }
}