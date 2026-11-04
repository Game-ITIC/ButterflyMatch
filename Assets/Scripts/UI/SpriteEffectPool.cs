using UnityEngine;

/// <summary>
/// A fixed set of pre-instantiated sprite effects (item explosions, flying flowers).
/// An effect is in use while its SpriteRenderer is enabled; renderers are cached so lookups don't call GetComponent.
/// </summary>
public sealed class SpriteEffectPool
{
    private readonly GameObject[] _effects;
    private readonly SpriteRenderer[] _renderers;

    public SpriteEffectPool(GameObject prefab, int size, Vector3 position)
    {
        _effects = new GameObject[size];
        _renderers = new SpriteRenderer[size];
        if(prefab == null)
        {
            return;
        }

        for(int i = 0; i < size; i++)
        {
            GameObject instance = Object.Instantiate(prefab, position, Quaternion.identity);
            SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
            if(renderer != null)
            {
                renderer.enabled = false;
            }

            _effects[i] = instance;
            _renderers[i] = renderer;
        }
    }

    public int Count => _effects.Length;

    public GameObject this[int index] => _effects[index];

    /// <summary>The first effect that is not showing, or null when all are busy.</summary>
    public GameObject FindFree(out SpriteRenderer renderer)
    {
        for(int i = 0; i < _effects.Length; i++)
        {
            renderer = _renderers[i];
            if(_effects[i] != null && renderer != null && !renderer.enabled)
            {
                return _effects[i];
            }
        }

        renderer = null;
        return null;
    }

    public bool AnyInUse()
    {
        for(int i = 0; i < _effects.Length; i++)
        {
            SpriteRenderer renderer = _renderers[i];
            if(_effects[i] != null && renderer != null && renderer.enabled)
            {
                return true;
            }
        }

        return false;
    }
}
