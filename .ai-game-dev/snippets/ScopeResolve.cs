using UnityEngine;
using VContainer.Unity;

public static class ScopeResolve
{
    // First service of type T found in any active VContainer scope (Play mode only).
    public static T Find<T>() where T : class
    {
        foreach (var scope in Object.FindObjectsByType<LifetimeScope>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (scope == null || scope.Container == null)
                continue;
            try
            {
                if (scope.Container.Resolve(typeof(T)) is T service)
                    return service;
            }
            catch (System.Exception)
            {
            }
        }

        return null;
    }
}
