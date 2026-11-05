using System.Reflection;
using UnityEngine;
public static class SimShot
{
    public const string Dir = "C:/Users/mumid/AppData/Local/Temp/claude/D--GitHub-JigsawMatch2/095daf06-f579-4ff2-8d31-023d7708dfba/scratchpad/";
    public static string Take(string name)
    {
        foreach (var w in Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>())
        {
            if (!w.GetType().FullName.Contains("SimulatorWindow")) continue;
            var f = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.PlayModeView").GetField("m_TargetTexture", BindingFlags.Instance | BindingFlags.NonPublic);
            var rt = f.GetValue(w) as RenderTexture;
            if (rt == null) return "no rt";
            var prev = RenderTexture.active;
            var tmp = RenderTexture.GetTemporary(rt.width, rt.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(rt, tmp);
            RenderTexture.active = tmp;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(tmp);
            // flip if needed (RT from editor is often upside-down)
            int sw = rt.width / 2, sh = rt.height / 2;
            var outT = new Texture2D(sw, sh, TextureFormat.RGB24, false);
            var px = tex.GetPixels();
            var o = new Color[sw * sh];
            for (int y = 0; y < sh; y++) for (int x = 0; x < sw; x++) o[y * sw + x] = px[(rt.height - 1 - y * 2) * rt.width + x * 2];
            outT.SetPixels(o); outT.Apply();
            System.IO.File.WriteAllBytes(Dir + name + ".png", outT.EncodeToPNG());
            Object.DestroyImmediate(tex); Object.DestroyImmediate(outT);
            return "ok " + rt.width + "x" + rt.height;
        }
        return "no sim";
    }
}
