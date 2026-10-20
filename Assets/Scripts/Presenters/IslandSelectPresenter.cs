using System;
using Services;
using UnityEngine.Events;
using UnityEngine.UI;
using VContainer.Unity;

namespace Presenters
{
    public class IslandSelectPresenter : IInitializable, IDisposable
    {
        private readonly IslandPanel _panel;
        private readonly Button _islandButton;
        private readonly IslandBuildService _buildService;
        private UnityAction _openAction;

        public IslandSelectPresenter(MainMenuPanel mainMenu, MenuNavPanel navPanel, IslandBuildService buildService)
        {
            _panel = mainMenu != null ? mainMenu.IslandPanel : null;
            _islandButton = navPanel != null ? navPanel.IslandButton : null;
            _buildService = buildService;
        }

        public void Initialize()
        {
            if (_panel != null)
            {
                _panel.Hide();
            }

            if (_buildService != null)
            {
                _buildService.Changed += RefreshIfOpen;
            }

            if (_islandButton == null)
            {
                return;
            }

            _openAction = Toggle;
            _islandButton.onClick.AddListener(_openAction);
        }

        public void Dispose()
        {
            if (_buildService != null)
            {
                _buildService.Changed -= RefreshIfOpen;
            }

            if (_islandButton != null && _openAction != null)
            {
                _islandButton.onClick.RemoveListener(_openAction);
            }
        }

        private void Toggle()
        {
            if (_panel == null)
            {
                return;
            }

            if (_panel.IsOpen)
            {
                _panel.Hide();
                return;
            }

            Refresh();
            _panel.Show();
        }

        private void RefreshIfOpen()
        {
            if (_panel != null && _panel.IsOpen)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (_panel == null || _buildService == null || _buildService.Region == null)
            {
                return;
            }

            _panel.ClearCards();
            var islands = _buildService.Region.Islands;
            if (islands == null)
            {
                return;
            }

            for (var i = 0; i < islands.Count; i++)
            {
                var island = islands[i];
                if (island == null)
                {
                    continue;
                }

                var card = _panel.CreateCard();
                if (card == null)
                {
                    return;
                }

                var built = _buildService.CountBuilt(island);
                var status = island.IsComingSoon ? "Soon" : null;
                var captured = island;
                card.Bind(
                    island.DisplayName,
                    island.Icon,
                    built,
                    island.SlotCount,
                    !island.IsComingSoon,
                    status,
                    () => Select(captured));
            }
        }

        private void Select(Configs.IslandSO island)
        {
            _buildService.SelectIsland(island);
            if (_panel != null)
            {
                _panel.Hide();
            }
        }
    }
}
