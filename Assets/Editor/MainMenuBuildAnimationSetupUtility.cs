using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MainMenuBuildAnimationSetupUtility
{
    private const string MenuLifetimeScopeName = "MenuLifetimeScope";
    private const string WorldName = "World";

    private static readonly string[] RegionNames =
    {
        "Japan_Island_Pref",
        "Korea Environment",
        "ChinaPrefabs"
    };

    [MenuItem("Tools/JigsawMatch2/Setup MainMenu Build Animations")]
    public static void Setup()
    {
        var menuLifetimeScope = FindRoot(MenuLifetimeScopeName);

        if(menuLifetimeScope == null)
        {
            Debug.LogError($"{MenuLifetimeScopeName} was not found in the active scene.");
            return;
        }

        var lifetimeScope = menuLifetimeScope.GetComponent<Scopes.Country.BaseCountryLifetimeScope>();

        if(lifetimeScope == null)
        {
            Debug.LogError($"BaseCountryLifetimeScope was not found on {MenuLifetimeScopeName}.", menuLifetimeScope);
            return;
        }

        var world = menuLifetimeScope.transform.Find(WorldName);

        if(world == null)
        {
            Debug.LogError($"{WorldName} was not found under {MenuLifetimeScopeName}.", menuLifetimeScope);
            return;
        }

        var serializedScope = new SerializedObject(lifetimeScope);
        var regionParentProperty = serializedScope.FindProperty("regionParent");
        if (regionParentProperty != null && world != null)
        {
            regionParentProperty.objectReferenceValue = world;
        }

        serializedScope.ApplyModifiedProperties();
        EditorUtility.SetDirty(lifetimeScope);
        EditorSceneManager.MarkSceneDirty(lifetimeScope.gameObject.scene);

        Debug.Log("Configured MainMenu BaseCountryLifetimeScope World parent.", lifetimeScope);
    }

    private static void RemoveLegacyAnimationComponents(GameObject root)
    {
        var legacyAnimations = root.GetComponentsInChildren<Animation>(true);

        for(var i = legacyAnimations.Length - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(legacyAnimations[i]);
        }
    }

    private static GameObject FindRoot(string rootName)
    {
        var activeScene = SceneManager.GetActiveScene();
        var roots = activeScene.GetRootGameObjects();

        for(var i = 0; i < roots.Length; i++)
        {
            if(roots[i].name == rootName)
            {
                return roots[i];
            }
        }

        return null;
    }
}
