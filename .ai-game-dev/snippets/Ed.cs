using System.Text;
using UnityEngine;
using UnityEngine.UI;
public static class Ed
{
    public static Transform Find(string path)
    {
        var parts = path.Split('/');
        foreach (var g in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (g.transform.parent != null || !g.scene.IsValid()) continue;
            if (g.name != parts[0]) continue;
            var t = g.transform;
            for (int i = 1; i < parts.Length && t != null; i++) t = t.Find(parts[i]);
            if (t != null) return t;
        }
        return null;
    }
    public static string Dump(Transform t, int depth = 2, int d = 0)
    {
        var sb = new StringBuilder();
        if (t == null) return "null";
        var rt = t as RectTransform;
        sb.Append(new string(' ', d * 2)).Append(t.name).Append(t.gameObject.activeSelf ? "" : " (off)");
        if (rt) sb.Append($" a[{rt.anchorMin.x:0.##},{rt.anchorMin.y:0.##}-{rt.anchorMax.x:0.##},{rt.anchorMax.y:0.##}] piv[{rt.pivot.x:0.##},{rt.pivot.y:0.##}] pos({rt.anchoredPosition.x:0.#},{rt.anchoredPosition.y:0.#}) size({rt.sizeDelta.x:0.#},{rt.sizeDelta.y:0.#}) sc{t.localScale.x:0.##}");
        foreach (var c in t.GetComponents<Component>())
        {
            if (c == null) { sb.Append(" <missing>"); continue; }
            if (c is Transform || c is CanvasRenderer) continue;
            var n = c.GetType().Name;
            if (c is TMPro.TMP_Text tx) n += $"('{tx.text.Replace("\n", "\n")}' {tx.font?.name} {tx.fontSize}{(tx.enableAutoSizing ? $" auto{tx.fontSizeMin}-{tx.fontSizeMax}" : "")} col#{ColorUtility.ToHtmlStringRGBA(tx.color)} al={tx.alignment} wrap={tx.enableWordWrapping} ovf={tx.overflowMode})";
            else if (c is Image im) n += $"({(im.sprite ? im.sprite.name : "none")} #{ColorUtility.ToHtmlStringRGBA(im.color)} rc={im.raycastTarget} {im.type})";
            else if (c is LayoutGroup lg) n += $"(pad {lg.padding.left},{lg.padding.right},{lg.padding.top},{lg.padding.bottom} {(lg is HorizontalOrVerticalLayoutGroup h ? "sp=" + h.spacing + " ctrl=" + h.childControlWidth + "/" + h.childControlHeight + " exp=" + h.childForceExpandWidth + "/" + h.childForceExpandHeight : "")}{(lg is GridLayoutGroup gl ? "cell=" + gl.cellSize + " sp=" + gl.spacing + " c=" + gl.constraint + gl.constraintCount : "")} {lg.childAlignment})";
            else if (c is LayoutElement le) n += $"(min{le.minWidth}/{le.minHeight} pref{le.preferredWidth}/{le.preferredHeight} ign={le.ignoreLayout})";
            else if (c is ContentSizeFitter f) n += $"({f.horizontalFit}/{f.verticalFit})";
            sb.Append(" +").Append(n);
        }
        sb.AppendLine();
        if (d < depth) for (int i = 0; i < t.childCount; i++) sb.Append(Dump(t.GetChild(i), depth, d + 1));
        return sb.ToString();
    }
    public static string Dump(string path, int depth = 2) => Dump(Find(path), depth);
    public static string DumpPrefab(string assetPath, int depth = 3)
    {
        var root = UnityEditor.PrefabUtility.LoadPrefabContents(assetPath);
        try { return Dump(root.transform, depth); } finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
    }
}
