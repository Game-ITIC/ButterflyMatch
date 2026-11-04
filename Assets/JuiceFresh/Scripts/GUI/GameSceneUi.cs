using UnityEngine;

/// <summary>
/// The game scene's legacy UI roots, found by name in one place instead of in every state and popup.
/// CanvasGlobal holds the popups (MenuComplete, MenuFailed, PreFailed, MenuPause, BoostShop, BoostInfo, GemsShop);
/// Level/Canvas holds the in-board banners (NoMoreMatches, PreCompleteBanner).
/// Lookups happen when a state or button needs them, never per frame.
/// </summary>
public static class GameSceneUi
{
    public const string GlobalCanvasPath = "CanvasGlobal";
    public const string LevelCanvasPath = "Level/Canvas";

    public const string MenuComplete = "MenuComplete";
    public const string MenuFailed = "MenuFailed";
    public const string MenuPause = "MenuPause";
    public const string PreFailed = "PreFailed";
    public const string BoostShop = "BoostShop";
    public const string BoostInfo = "BoostInfo";
    public const string GemsShop = "GemsShop";

    public const string NoMoreMatches = "NoMoreMatches";
    public const string PreCompleteBanner = "PreCompleteBanner";

    /// <summary>The popup canvas, or null when the game scene isn't loaded.</summary>
    public static Transform GlobalCanvas => FindRoot(GlobalCanvasPath);

    /// <summary>The board's own canvas, or null outside a level.</summary>
    public static Transform LevelCanvas => FindRoot(LevelCanvasPath);

    /// <summary>A direct child of CanvasGlobal (a popup), or null.</summary>
    public static Transform FindInGlobalCanvas(string childName) => FindChild(GlobalCanvas, childName);

    /// <summary>A direct child of Level/Canvas (a banner), or null.</summary>
    public static Transform FindInLevelCanvas(string childName) => FindChild(LevelCanvas, childName);

    private static Transform FindRoot(string path)
    {
        GameObject root = GameObject.Find(path);
        return root != null ? root.transform : null;
    }

    private static Transform FindChild(Transform root, string childName)
    {
        return root != null ? root.Find(childName) : null;
    }
}
