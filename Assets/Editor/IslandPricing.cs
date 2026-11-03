using System.Collections.Generic;
using Configs;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Star prices of every building slot from Docs/GDD.md 5.2 (sum per island = levels per island x 2.2).
/// Applied by the region setup menus and by its own menu, so rebuilding islands never brings old prices back.
/// </summary>
public static class IslandPricing
{
    private static readonly Dictionary<string, int[]> ChainPrices = new()
    {
        // Japan: main chain 10, then lantern, then 3 trees = 30
        { "japan", new[] { 1, 1, 2, 2, 2, 3, 3, 3, 3, 4, 2, 1, 1, 2 } },
        // China: chain 3, trees 2 = 32
        { "china", new[] { 5, 6, 7, 7, 7 } },
        // Korea: chain 4, lantern, trees 2 = 34
        { "korea", new[] { 4, 5, 5, 6, 4, 5, 5 } },
        { "norway", new[] { 2, 3, 3, 3, 4, 4, 4, 4, 4, 5 } },   // 36
        { "lapland", new[] { 3, 3, 3, 4, 4, 4, 4, 4, 4, 5 } },  // 38
        { "iceland", new[] { 3, 3, 4, 4, 4, 4, 4, 4, 5, 5 } },  // 40
        // Europe is played in the level loop (91+): same rule, 15 levels x 2.2 + a little each island.
        { "france", new[] { 3, 3, 4, 4, 4, 4, 5, 5, 5, 5 } },   // 42
        { "greece", new[] { 3, 4, 4, 4, 4, 5, 5, 5, 5, 5 } },   // 44
        { "italy", new[] { 4, 4, 4, 4, 5, 5, 5, 5, 5, 5 } },    // 46
    };

    [MenuItem("Tools/JigsawMatch2/Apply GDD Island Prices")]
    public static void ApplyAll()
    {
        var changed = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:IslandSO"))
        {
            var island = AssetDatabase.LoadAssetAtPath<IslandSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (island != null && Apply(island))
            {
                changed++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[IslandPricing] Prices applied to {changed} islands.");
    }

    public static bool Apply(IslandSO island)
    {
        if (island == null || !ChainPrices.TryGetValue(island.IslandId, out var prices))
        {
            return false;
        }

        var so = new SerializedObject(island);
        var slots = so.FindProperty("slots");
        if (slots == null || slots.arraySize != prices.Length)
        {
            Debug.LogWarning($"[IslandPricing] {island.IslandId}: {slots?.arraySize} slots, price table has {prices.Length}. Skipped.");
            return false;
        }

        for (var i = 0; i < prices.Length; i++)
        {
            slots.GetArrayElementAtIndex(i).FindPropertyRelative("starCost").intValue = prices[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(island);
        return true;
    }
}
