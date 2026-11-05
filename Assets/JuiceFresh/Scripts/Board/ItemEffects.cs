using UnityEngine;

namespace JuiceFresh.Board
{
    /// <summary>
    /// One-shot visual effects of a piece: pop particles, the striped-piece appear effect and the +5 s bonus.
    /// </summary>
    public class ItemEffects
    {
        private readonly Item _item;
        private bool _stripeShown;

        public ItemEffects(Item item)
        {
            _item = item;
        }

        public void PlayPop()
        {
            GameObject partcl = LevelManager.THIS.GetExplFromPool();
            if (partcl == null)
                return;

            Vector3 position = _item.transform.position;
            var events = partcl.GetComponent<ItemAnimEvents>();
            events.item = _item;
            partcl.transform.localScale = Vector3.one;
            partcl.transform.position = position;

            GameObject psObj = Object.Instantiate(events.particals, position, Quaternion.identity);
            var ps = psObj.GetComponent<ParticleSystem>();
            var ts = ps.textureSheetAnimation;
            ts.frameOverTime = new ParticleSystem.MinMaxCurve((float)ParticleTileFor(_item.color) / ts.numTilesX);
            ps.Play();
            Object.Destroy(psObj, 2);

            partcl.transform.rotation = Quaternion.Euler(new Vector3(0, 0, Random.Range(0f, 360f)));
            partcl.GetComponent<Animator>().SetInteger("color", _item.color);
            SoundBase.Instance.PlaySoundsRandom(SoundBase.Instance.pops);
            GameFeelManager.Instance?.OnItemDestroyed(position);
        }

        // Tile of the pop particle sheet for each piece colour.
        private static int ParticleTileFor(int color)
        {
            switch (color)
            {
                case 0: return 3;
                case 1: return 2;
                case 2: return 4;
                case 3: return 1;
                case 5: return 5;
                default: return 0;
            }
        }

        public void PlayStripeAppear(ItemsTypes type)
        {
            // A new piece can get here twice: from ChangeType and from its own Start.
            if (_stripeShown)
                return;
            _stripeShown = true;

            Transform spriteTransform = _item.transform.Find("Sprite");
            GameObject obj = Object.Instantiate(Resources.Load("Prefabs/StripeEffect")) as GameObject;
            obj.transform.SetParent(spriteTransform);
            obj.transform.localScale = Vector3.one;
            obj.GetComponent<StripesWrappEffect>().itemSprite = spriteTransform.GetComponent<SpriteRenderer>();
            obj.transform.localPosition = Vector3.zero;

            if (type == ItemsTypes.HORIZONTAL_STRIPPED)
                obj.transform.eulerAngles = new Vector3(0, 0, 90);

            spriteTransform.GetComponent<SpriteRenderer>().sortingOrder = 3;

            GameObject effect = Object.Instantiate(_item.appearingEffect);
            effect.transform.position = _item.transform.position;
            Object.Destroy(effect, 2);
        }

        /// <summary>Timed levels: a piece carrying the "5sec" marker adds five seconds when destroyed.</summary>
        public void ReleaseTimeBonus()
        {
            if (LevelManager.THIS.limitType != LIMIT.TIME)
                return;

            Transform marker = _item.transform.Find("5sec");
            if (marker == null)
                return;

            GameObject fiveSec = marker.gameObject;
            fiveSec.transform.SetParent(null);
            fiveSec.GetComponent<Animation>().Play("5secfly");
            Object.Destroy(fiveSec, 1);

            if (LevelManager.THIS.gameStatus == GameState.Playing)
                LevelManager.THIS.Limit += 5;
        }
    }
}
