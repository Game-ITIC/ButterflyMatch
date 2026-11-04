using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Board-wide visual effects of a level: the pooled item explosions and flying flowers,
/// striped-booster rays, the random-color booster reveal and the double-bomb column waves.
/// </summary>
[DisallowMultipleComponent]
public sealed class LevelEffectsController : MonoBehaviour
{
    private const string ExplosionPrefabPath = "Prefabs/Effects/ItemExpl";

    [SerializeField] private GameObject stripesEffectPrefab;
    [SerializeField] private GameObject flowerPrefab;
    [SerializeField, Min(1)] private int poolSize = 20;
    [SerializeField, Min(0f)] private float explosionLifetime = 1f;
    [SerializeField, Min(0f)] private float doubleBombStepDelay = 0.3f;

    private LevelManager levelManager;
    private SpriteEffectPool explosionPool;
    private SpriteEffectPool flowerPool;

    public void Initialize(LevelManager owner)
    {
        if(levelManager == owner && explosionPool != null && flowerPool != null)
        {
            return;
        }

        levelManager = owner;
        explosionPool = new SpriteEffectPool(Resources.Load<GameObject>(ExplosionPrefabPath), poolSize, transform.position);
        flowerPool = new SpriteEffectPool(flowerPrefab, poolSize, transform.position);
    }

    public void PlayRandomColorReveal()
    {
        StartCoroutine(RandomColorRevealCoroutine());
    }

    public void PlayDoubleBombWave(int column)
    {
        StartCoroutine(DoubleBombWaveForward(column));
        StartCoroutine(DoubleBombWaveBackward(column));
    }

    public void PlayStripedEffect(GameObject source, bool horizontal)
    {
        if(stripesEffectPrefab == null || source == null)
        {
            return;
        }

        GameObject effect = Instantiate(stripesEffectPrefab, source.transform.position, Quaternion.identity);
        if(!horizontal)
        {
            effect.transform.Rotate(Vector3.back, 90f);
        }

        Destroy(effect, 1f);
    }

    /// <summary>Shows a free pooled explosion and hides it after <see cref="explosionLifetime"/>.</summary>
    public GameObject GetExplosion()
    {
        if(explosionPool == null)
        {
            return null;
        }

        GameObject effect = explosionPool.FindFree(out SpriteRenderer renderer);
        if(effect == null)
        {
            return null;
        }

        renderer.enabled = true;
        StartCoroutine(HideExplosion(effect, renderer));
        return effect;
    }

    /// <summary>A free pooled flower; the Flower itself shows it while it flies.</summary>
    public GameObject GetFlower() => flowerPool?.FindFree(out _);

    public bool HasFlyingFlowers() => flowerPool != null && flowerPool.AnyInUse();

    public void ForceCleanupAllFlowers()
    {
        if(flowerPool == null)
        {
            return;
        }

        for(int i = 0; i < flowerPool.Count; i++)
        {
            GameObject flower = flowerPool[i];
            if(flower == null)
            {
                continue;
            }

            Flower flowerComponent = flower.GetComponent<Flower>();
            if(flowerComponent != null)
            {
                flowerComponent.StopAllCoroutines();

                SpriteRenderer renderer = flower.GetComponent<SpriteRenderer>();
                if(renderer != null)
                {
                    renderer.enabled = false;
                }

                ParticleSystem particles = flower.GetComponent<ParticleSystem>();
                if(particles != null)
                {
                    particles.Stop();
                }
            }
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator RandomColorRevealCoroutine()
    {
        if(levelManager == null)
        {
            yield break;
        }

        int color = Random.Range(0, levelManager.colorLimit);
        List<Item> items = levelManager.GetRandomItems(Item.Active.Count / 3);

        foreach(Item item in items)
        {
            yield return new WaitForSeconds(0.01f);
            item.SetColor(color);
            item.PlayAppearAnimation();
        }
    }

    private IEnumerator DoubleBombWaveForward(int column)
    {
        for(int current = column; current < levelManager.maxCols; current++)
        {
            DestroyColumn(current);
            yield return new WaitForSeconds(doubleBombStepDelay);
        }

        if(column <= levelManager.maxCols - column - 1)
        {
            levelManager.FindMatches();
        }
    }

    private IEnumerator DoubleBombWaveBackward(int column)
    {
        for(int current = column - 1; current >= 0; current--)
        {
            DestroyColumn(current);
            yield return new WaitForSeconds(doubleBombStepDelay);
        }

        if(column > levelManager.maxCols - column - 1)
        {
            levelManager.FindMatches();
        }
    }

    private void DestroyColumn(int column)
    {
        foreach(Item item in levelManager.GetColumn(column))
        {
            if(item != null)
            {
                item.DestroyItem(true, "", true);
            }
        }
    }

    private IEnumerator HideExplosion(GameObject effect, SpriteRenderer renderer)
    {
        yield return new WaitForSeconds(explosionLifetime);

        Animator animator = effect.GetComponent<Animator>();
        if(animator != null)
        {
            animator.SetTrigger("stop");
            animator.SetInteger("color", 10);
        }

        renderer.enabled = false;
    }
}
