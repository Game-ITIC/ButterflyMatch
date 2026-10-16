using System;
using Configs;
using Services;
using UnityEngine.Events;
using UnityEngine.UI;
using VContainer.Unity;

namespace Presenters
{
    public class BuildingCatalogPresenter : IInitializable, IDisposable
    {
        private readonly MainMenuPanel _mainMenu;
        private readonly IslandBuildService _buildService;
        private readonly Button _buildButton;
        private BuildingCatalogPanel _panel;
        private UnityAction _openAction;

        public BuildingCatalogPresenter(MainMenuPanel mainMenu, IslandBuildService buildService)
        {
            _mainMenu = mainMenu;
            _buildService = buildService;
            _buildButton = mainMenu != null && mainMenu.MenuActionPanel != null
                ? mainMenu.MenuActionPanel.BuildButton != null ? mainMenu.MenuActionPanel.BuildButton.Button : null
                : null;
        }

        public void Initialize()
        {
            _panel = _mainMenu != null ? _mainMenu.BuildingCatalogPanel : null;
            if (_panel != null)
            {
                _panel.Hide();
            }

            if (_buildService != null)
            {
                _buildService.Changed += RefreshIfOpen;
            }

            if (_buildButton == null)
            {
                return;
            }

            _openAction = Open;
            _buildButton.onClick.RemoveListener(_openAction);
            _buildButton.onClick.AddListener(_openAction);
        }

        public void Dispose()
        {
            if (_buildService != null)
            {
                _buildService.Changed -= RefreshIfOpen;
            }

            if (_buildButton != null && _openAction != null)
            {
                _buildButton.onClick.RemoveListener(_openAction);
            }
        }

        private void Open()
        {
            if (_panel == null || _buildService == null)
            {
                return;
            }

            _mainMenu?.IslandPanel?.Hide();
            _panel.Show(_buildService.ActiveIsland, _buildService, Build);
        }

        private void RefreshIfOpen()
        {
            if (_panel != null && _panel.gameObject.activeSelf)
            {
                _panel.Show(_buildService.ActiveIsland, _buildService, Build);
            }
        }

        private void Build(BuildingSlot slot)
        {
            if (_buildService != null && _buildService.TryBuild(slot))
            {
                _panel?.Hide();
            }
        }
    }
}
