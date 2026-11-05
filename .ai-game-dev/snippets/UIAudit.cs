using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public static class UIAudit
{
    static string Path(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }
    static Rect ScreenRect(RectTransform rt)
    {
        var c = new Vector3[4]; rt.GetWorldCorners(c);
        var canvas = rt.GetComponentInParent<Canvas>(); Camera cam = null;
        if (canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = canvas.rootCanvas.worldCamera;
        var a = RectTransformUtility.WorldToScreenPoint(cam, c[0]); var b = RectTransformUtility.WorldToScreenPoint(cam, c[2]);
        // top-left origin, in 642x1389 screenshot pixels
        float k = 0.5f;
        return new Rect(a.x * k, (Screen.height - b.y) * k, (b.x - a.x) * k, (b.y - a.y) * k);
    }
    public static string Buttons()
    {
        var sb = new StringBuilder();
        foreach (var b in Object.FindObjectsByType<Selectable>(FindObjectsSortMode.None))
        {
            if (!b.isActiveAndEnabled) continue;
            var r = ScreenRect((RectTransform)b.transform);
            sb.AppendLine($"{Path(b.transform)} [{b.GetType().Name}] x={r.x:0} y={r.y:0} w={r.width:0} h={r.height:0} inter={b.interactable}");
        }
        return sb.ToString();
    }
    public static string Canvases()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"screen {Screen.width}x{Screen.height} safe={Screen.safeArea}");
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!c.isRootCanvas || !c.isActiveAndEnabled) continue;
            var cs = c.GetComponent<CanvasScaler>();
            sb.AppendLine($"{Path(c.transform)} mode={c.renderMode} sort={c.sortingOrder} scaler={(cs ? cs.uiScaleMode + " ref=" + cs.referenceResolution + " match=" + cs.matchWidthOrHeight : "none")} scale={c.scaleFactor}");
        }
        return sb.ToString();
    }
    public static string Texts(string rootFilter = null)
    {
        var sb = new StringBuilder();
        foreach (var t in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
        {
            if (!t.isActiveAndEnabled) continue;
            var p = Path(t.transform); if (rootFilter != null && !p.Contains(rootFilter)) continue;
            var r = ScreenRect(t.rectTransform);
            sb.AppendLine($"{p} '{t.text.Replace("\n", "\n")}' font={t.font?.name} size={t.fontSize:0}{(t.enableAutoSizing ? "(auto " + t.fontSizeMin + "-" + t.fontSizeMax + ")" : "")} trunc={t.isTextTruncated} ovf={t.overflowMode} x={r.x:0} y={r.y:0} w={r.width:0} h={r.height:0}");
        }
        return sb.ToString();
    }
    public static string Click(string nameOrPathPart)
    {
        foreach (var b in Object.FindObjectsByType<Selectable>(FindObjectsSortMode.None))
        {
            if (!b.isActiveAndEnabled) continue;
            var p = Path(b.transform); if (!p.EndsWith(nameOrPathPart) && b.name != nameOrPathPart) continue;
            var ped = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(b.gameObject, ped, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(b.gameObject, ped, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(b.gameObject, ped, ExecuteEvents.pointerClickHandler);
            return "clicked " + p;
        }
        return "not found " + nameOrPathPart;
    }
}
