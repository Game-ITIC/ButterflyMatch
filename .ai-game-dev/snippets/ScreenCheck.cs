using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>Play-mode helper: switch the Game View to a phone size, fake its safe area and restart a level.</summary>
public static class ScreenCheck
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static string Run(int w, int h, int safeTop, int safeBottom, int level)
    {
        string r = SetScreen(w, h, safeTop, safeBottom);
        Cheat("GoToLevel", level);
        return r;
    }

    public static string SetScreen(int w, int h, int safeTop, int safeBottom)
    {
        int idx = EnsureSize(w, h);
        var asm = typeof(UnityEditor.Editor).Assembly;
        var gvType = asm.GetType("UnityEditor.GameView");
        foreach (var gv in Resources.FindObjectsOfTypeAll(gvType))
            gvType.GetMethod("SizeSelectionCallback", Any).Invoke(gv, new object[] { idx, null });

        UI.SafeAreaFitter.EditorSafeAreaOverride = new Rect(0, (float)safeBottom / h, 1, (float)(h - safeTop - safeBottom) / h);
        return "size idx " + idx + " " + w + "x" + h;
    }

    /// <summary>Calls a CheatLevels method: GoToLevel(n), GoToMenu(null), WinLevel(3), LoseLevel().</summary>
    public static void Cheat(string method, params object[] args)
    {
        var t = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Utils.DebugTools.CheatLevels")).First(x => x != null);
        var panelT = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Utils.DebugTools.CheatPanel")).First(x => x != null);
        var panel = UnityEngine.Object.FindObjectOfType(panelT, true);
        var inst = Activator.CreateInstance(t, Any, null, new object[] { panel }, null);
        t.GetMethod(method).Invoke(inst, args);
    }

    public static int EnsureSize(int w, int h)
    {
        var asm = typeof(UnityEditor.Editor).Assembly;
        var sizesType = asm.GetType("UnityEditor.GameViewSizes");
        var inst = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        var group = sizesType.GetMethod("GetGroup").Invoke(inst, new object[] { (int)UnityEditor.GameViewSizeGroupType.Android });
        var gvsType = asm.GetType("UnityEditor.GameViewSize");
        int total = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        for (int i = 0; i < total; i++)
        {
            var s = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
            if ((int)gvsType.GetProperty("width").GetValue(s) == w && (int)gvsType.GetProperty("height").GetValue(s) == h) return i;
        }
        var ctor = gvsType.GetConstructor(new[] { asm.GetType("UnityEditor.GameViewSizeType"), typeof(int), typeof(int), typeof(string) });
        var ns = ctor.Invoke(new object[] { Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), w, h, "check " + w + "x" + h });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { ns });
        return total;
    }

    /// <summary>Report of where the HUD ends up on screen, in pixels from the bottom.</summary>
    public static string Report()
    {
        var canvas = GameObject.Find("Level").transform.Find("Canvas");
        var cam = canvas.GetComponent<Canvas>().worldCamera;
        string R(string path)
        {
            var rt = canvas.Find(path) as RectTransform;
            if (rt == null) return path + ": none";
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            var a = RectTransformUtility.WorldToScreenPoint(cam, c[0]);
            var b = RectTransformUtility.WorldToScreenPoint(cam, c[2]);
            return path + ": x " + (int)a.x + ".." + (int)b.x + " y " + (int)a.y + ".." + (int)b.y;
        }
        return "screen " + Screen.width + "x" + Screen.height + " ortho " + Camera.main.orthographicSize.ToString("0.00") + "\n"
               + R("Panel/TopBarPanel") + "\n" + R("BottomPanel/Panel") + "\n" + R("Panel/TopBarPanel/Pause");
    }
}
