using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Small helpers for editing UI prefabs from script-execute (include: ["UiEdit.cs"]).
public static class UiEdit
{
    public const string Root = "Assets/Content/Prefabs/UI/";

    // Opens a prefab, runs the edit, saves it. Nested prefab edits become overrides unless applied.
    public static void Prefab(string relativePath, Action<Transform> edit)
    {
        var path = Root + relativePath + ".prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    public static Transform Get(Transform root, string path)
    {
        var t = string.IsNullOrEmpty(path) ? root : root.Find(path);
        if (t == null) throw new Exception($"'{path}' not found under '{root.name}'");
        return t;
    }

    public static RectTransform Rect(Transform root, string path) => (RectTransform)Get(root, path);

    // Anchors both corners to one point; pos and size are then plain offsets from it.
    public static RectTransform Place(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
    {
        r.anchorMin = anchor;
        r.anchorMax = anchor;
        if (pivot.HasValue) r.pivot = pivot.Value;
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        return r;
    }

    public static RectTransform Stretch(RectTransform r, float left = 0, float right = 0, float top = 0, float bottom = 0)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(left, bottom);
        r.offsetMax = new Vector2(-right, -top);
        return r;
    }

    // Auto size between min and max, staying on one line unless wrap is asked for.
    public static TMP_Text Font(TMP_Text text, float max, float min = 0, bool wrap = false)
    {
        if (min > 0)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
        }
        else
        {
            text.enableAutoSizing = false;
        }

        text.fontSize = max;
        text.enableWordWrapping = wrap;
        return text;
    }

    public static TMP_Text Font(Transform root, string path, float max, float min = 0, bool wrap = false) =>
        Font(Get(root, path).GetComponent<TMP_Text>(), max, min, wrap);

    public static RectTransform NewChild(Transform parent, string name, int siblingIndex = -1)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        if (siblingIndex >= 0) go.transform.SetSiblingIndex(siblingIndex);
        return (RectTransform)go.transform;
    }

    // Adds an invisible tap zone centred on the button.
    public static void HitArea(Transform button, float size)
    {
        var existing = button.Find("Hit Area");
        var rect = existing != null ? (RectTransform)existing : NewChild(button, "Hit Area");
        if (rect.GetComponent<UI.UIHitArea>() == null) rect.gameObject.AddComponent<UI.UIHitArea>();
        Place(rect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size), new Vector2(0.5f, 0.5f));
    }

    // Moves a uniform scale off the object into real sizes, so the inspector shows what the player sees.
    // The object's own rect stays as it is when it stretches with its parent (popup roots).
    public static void BakeScale(RectTransform r)
    {
        var s = r.localScale.x;
        if (s <= 0f || Mathf.Approximately(s, 1f))
        {
            return;
        }

        r.localScale = Vector3.one;
        if (r.anchorMin == r.anchorMax)
        {
            r.sizeDelta *= s;
        }

        ScaleContent(r, s);
        foreach (Transform child in r)
        {
            ScaleSubtree((RectTransform)child, s);
        }
    }

    private static void ScaleSubtree(RectTransform r, float s)
    {
        r.anchoredPosition *= s;
        r.sizeDelta *= s;
        ScaleContent(r, s);
        foreach (Transform child in r)
        {
            ScaleSubtree((RectTransform)child, s);
        }
    }

    private static void ScaleContent(RectTransform r, float s)
    {
        foreach (var text in r.GetComponents<TMP_Text>())
        {
            if (text.fontSize > 0f) text.fontSize *= s;
            text.fontSizeMin *= s;
            text.fontSizeMax *= s;
            text.margin *= s;
        }

        // A sliced frame draws its border in canvas units, so it has to be told to keep its look.
        foreach (var image in r.GetComponents<Image>())
        {
            if (image.type == Image.Type.Sliced || image.type == Image.Type.Tiled) image.pixelsPerUnitMultiplier /= s;
        }

        foreach (var shadow in r.GetComponents<Shadow>())
        {
            shadow.effectDistance *= s;
        }

        foreach (var layout in r.GetComponents<LayoutGroup>())
        {
            layout.padding = new RectOffset(
                Mathf.RoundToInt(layout.padding.left * s), Mathf.RoundToInt(layout.padding.right * s),
                Mathf.RoundToInt(layout.padding.top * s), Mathf.RoundToInt(layout.padding.bottom * s));
            if (layout is HorizontalOrVerticalLayoutGroup line) line.spacing *= s;
            if (layout is GridLayoutGroup grid)
            {
                grid.cellSize *= s;
                grid.spacing *= s;
            }
        }

        foreach (var element in r.GetComponents<LayoutElement>())
        {
            if (element.minWidth > 0f) element.minWidth *= s;
            if (element.minHeight > 0f) element.minHeight *= s;
            if (element.preferredWidth > 0f) element.preferredWidth *= s;
            if (element.preferredHeight > 0f) element.preferredHeight *= s;
        }
    }

    public static void SetField(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(target);
        var property = so.FindProperty(field);
        if (property == null) throw new Exception($"{target.GetType().Name}.{field} not found");
        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
