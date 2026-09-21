using System;
using System.Collections.Generic;
using System.Linq;
using Configs;
using Models;
using Services;
using UnityEngine.Events;
using UnityEngine.UI;
using VContainer.Unity;

namespace Presenters
{
    public class IslandPresenter : IInitializable, IDisposable
    {
        private readonly IslandPanel _islandPanel;
        private readonly RegionConfig _regionConfig;
        private readonly RegionModel _regionModel;
        private readonly RegionService _regionService;
        private readonly RegionUpgradeService _regionUpgradeService;
        private readonly Button _islandButton;
        private readonly List<Button> _closeOrNavButtons = new();
        private readonly List<(Button Button, UnityAction Action)> _boundActions = new();

        public IslandPresenter(
            IslandPanel islandPanel,
            RegionConfig regionConfig,
            RegionModel regionModel,
            RegionService regionService,
            RegionUpgradeService regionUpgradeService,
            Button islandButton,
            IEnumerable<Button> closeOrNavButtons = null
        )
        {
            _islandPanel = islandPanel;
            _regionConfig = regionConfig;
            _regionModel = regionModel;
            _regionService = regionService;
            _regionUpgradeService = regionUpgradeService;
            _islandButton = islandButton;

            if (closeOrNavButtons != null)
            {
                _closeOrNavButtons.AddRange(closeOrNavButtons.Where(b => b != null));
            }
        }

        public void Initialize()
        {
            if (_islandPanel == null) return;

            _islandPanel.Initialize(_regionConfig, _regionModel);
            _islandPanel.OnRegionSelected += OnRegionSelected;

            if (_islandButton != null)
            {
                UnityAction openAction = OnIslandButtonClicked;
                _islandButton.onClick.RemoveListener(openAction);
                _islandButton.onClick.AddListener(openAction);
                _boundActions.Add((_islandButton, openAction));
            }

            foreach (var navButton in _closeOrNavButtons)
            {
                if (navButton == null || navButton == _islandButton) continue;

                UnityAction closeAction = OnCloseOrNavButtonClicked;
                navButton.onClick.RemoveListener(closeAction);
                navButton.onClick.AddListener(closeAction);
                _boundActions.Add((navButton, closeAction));
            }
        }

        private void OnIslandButtonClicked()
        {
            _islandPanel.Show();
        }

        private void OnCloseOrNavButtonClicked()
        {
            _islandPanel.Hide();
        }

        private void OnRegionSelected(int index)
        {
            if (_regionModel == null || !_regionModel.IsRegionUnlocked(index)) return;

            if (_regionService != null && _regionService.CurrentRegionIndex != index)
            {
                _regionService.SetRegion(index);

                if (_regionUpgradeService != null)
                {
                    _regionUpgradeService.Initialize(_regionModel);
                    _regionUpgradeService.JumpToFrame(0);

                    int progress = _regionModel.GetRegionProgress(index);
                    if (progress > 0
                        && _regionService.ActiveRegion?.data != null
                        && progress <= _regionService.ActiveRegion.data.Count)
                    {
                        int endFrame = _regionService.ActiveRegion.data[progress - 1].endFrame;
                        _regionUpgradeService.JumpToFrame(endFrame);
                    }
                }
            }

            _islandPanel.Hide();
        }

        public void Dispose()
        {
            if (_islandPanel != null)
            {
                _islandPanel.OnRegionSelected -= OnRegionSelected;
            }

            foreach (var (button, action) in _boundActions)
            {
                if (button != null && action != null)
                {
                    button.onClick.RemoveListener(action);
                }
            }

            _boundActions.Clear();
        }
    }
}
