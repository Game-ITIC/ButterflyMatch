using System;
using DG.Tweening;
using Services;
using UnityEngine;
using UnityEngine.UI;
using VContainer.Unity;

namespace Presenters
{
    /// <summary>
    /// Swaps the main menu background to the active island's region (Asia / North / Europe).
    /// Regions without their own background keep the scene's default sprite.
    /// </summary>
    public class RegionBackgroundPresenter : IInitializable, IDisposable
    {
        private const float FadeDuration = 0.35f;

        private readonly IslandBuildService _buildService;
        private readonly Image _background;
        private readonly Sprite _defaultSprite;
        private Tween _fade;

        public RegionBackgroundPresenter(IslandBuildService buildService, Image background)
        {
            _buildService = buildService;
            _background = background;
            _defaultSprite = background != null ? background.sprite : null;
        }

        public void Initialize()
        {
            if (_buildService == null || _background == null)
            {
                return;
            }

            _buildService.Changed += Refresh;
            Apply(TargetSprite(), animate: false);
        }

        public void Dispose()
        {
            if (_buildService != null)
            {
                _buildService.Changed -= Refresh;
            }

            _fade?.Kill();
        }

        private void Refresh()
        {
            Apply(TargetSprite(), animate: true);
        }

        private Sprite TargetSprite()
        {
            var region = _buildService.RegionOf(_buildService.ActiveIsland);
            return region != null && region.MenuBackground != null ? region.MenuBackground : _defaultSprite;
        }

        private void Apply(Sprite sprite, bool animate)
        {
            if (_background == null || sprite == null || _background.sprite == sprite)
            {
                return;
            }

            _fade?.Kill();
            if (!animate || !_background.gameObject.activeInHierarchy)
            {
                _background.sprite = sprite;
                return;
            }

            // Quick dip to the tint colour and back so the swap does not pop.
            var color = _background.color;
            _fade = DOTween.Sequence()
                .Append(_background.DOFade(0f, FadeDuration * 0.5f))
                .AppendCallback(() => _background.sprite = sprite)
                .Append(_background.DOFade(color.a, FadeDuration * 0.5f))
                .SetLink(_background.gameObject);
        }
    }
}
