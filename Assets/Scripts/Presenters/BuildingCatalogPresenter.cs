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
        private readonly MenuActionButton _buildActionButton;
        private BuildingCatalogPanel _panel;
        private UnityAction _openAction;

        public BuildingCatalogPresenter(MainMenuPanel mainMenu, IslandBuildService buildService)
        {
            _mainMenu = mainMenu;
            _buildService = buildService;
            _buildActionButton = mainMenu != null && mainMenu.MenuActionPanel != null
                ? mainMenu.MenuActionPanel.BuildButton
                : null;
            _buildButton = _buildActionButton != null ? _buildActionButton.Button : null;
        }

        public void Initialize()
        {
            _panel = _mainMenu != null ? _mainMenu.BuildingCatalogPanel : null;
            if (_panel != null)
            {
                _panel.HideImmediate();
            }

            if (_buildService != null)
            {
                _buildService.Changed += RefreshIfOpen;
                _buildService.Changed += RefreshBuildCost;
            }

            RefreshBuildCost();

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
                _buildService.Changed -= RefreshBuildCost;
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
            _panel.Show(_buildService.ActiveIsland, _buildService, Build, true);
        }

        private void RefreshIfOpen()
        {
            if (_panel != null && _panel.IsOpen)
            {
                _panel.Show(_buildService.ActiveIsland, _buildService, Build, false);
            }
        }

        /// <summary>
        /// The menu Build button shows the star cost of the next building that can be built now
        /// (the cheapest unbuilt slot whose requirements are met). Empty when the island is finished.
        /// </summary>
        private void RefreshBuildCost()
        {
            if (_buildActionButton == null || _buildService == null)
            {
                return;
            }

            var island = _buildService.ActiveIsland;
            var cost = -1;
            if (island != null && island.Slots != null)
            {
                foreach (var slot in island.Slots)
                {
                    if (_buildService.CanSelect(island, slot) && (cost < 0 || slot.StarCost < cost))
                    {
                        cost = slot.StarCost;
                    }
                }
            }

            _buildActionButton.SetSubLabel(cost >= 0 ? cost.ToString() : string.Empty);
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
