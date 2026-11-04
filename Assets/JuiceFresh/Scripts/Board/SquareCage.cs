using UnityEngine;

namespace JuiceFresh.Board
{
    /// <summary>
    /// Wire cage on a square: hit points, damage from chains and striped blasts, ice crack effect.
    /// The hit points stay in <see cref="Square.cageHP"/> (serialized, read by the level code).
    /// </summary>
    public class SquareCage
    {
        private readonly Square _square;
        private int _hpBeforeHit;

        public SquareCage(Square square)
        {
            _square = square;
        }

        public void Set(int hp)
        {
            _square.cageHP = hp;
            _hpBeforeHit = hp;
        }

        public void SetActive(bool active)
        {
            if (_square.type == SquareTypes.WIREBLOCK && active)
                _hpBeforeHit = _square.cageHP;
        }

        /// <summary>Applies the damage of the current chain; true when the cage breaks (or there is none).</summary>
        public bool IsGoingToBreak(int extraDamage)
        {
            if (_square.type != SquareTypes.WIREBLOCK || _square.cageHP < 0)
                return true;

            return Hit(LevelManager.THIS.destroyAnyway.Count + extraDamage);
        }

        /// <summary>Applies a fixed damage; true when the cage breaks (or there is none).</summary>
        public bool CheckDamage(int damage)
        {
            if (_square.type != SquareTypes.WIREBLOCK || _square.cageHP < 0)
                return true;

            return Hit(damage);
        }

        private bool Hit(int damage)
        {
            int hp = _square.cageHP;
            int estimate = Mathf.Clamp(hp - damage, 0, hp);
            _hpBeforeHit = hp;
            AddDamage(damage);
            return estimate == 0;
        }

        public void AddDamage(int damage)
        {
            BurstIce();
            _square.cageHP = Mathf.Max(0, _hpBeforeHit - damage);
        }

        private void BurstIce()
        {
            iTween.ShakePosition(_square.block[_square.block.Count - 1].gameObject, Vector3.one * 0.3f, 0.2f);
            GameObject ice = Object.Instantiate(_square.icePrefab, _square.transform.position, Quaternion.identity);
            SoundBase.Instance.PlaySound(SoundBase.Instance.iceCrack);
            Object.Destroy(ice, 1);
        }
    }
}
