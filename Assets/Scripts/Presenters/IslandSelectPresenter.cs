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
                _panel.HideImmediate();
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
            if (_panel == null || _buildService == null || _buildService.Regions == null)
            {
                return;
            }

            _panel.ClearCards();
            var regions = _buildService.Regions;
            for (var r = 0; r < regions.Count; r++)
            {
                var islands = regions[r] != null ? regions[r].Islands : null;
                if (islands == null)
                {
                    continue;
                }

                _panel.BeginRegion(GameLocalization.L10n.Translate(regions[r].DisplayName));
                if (!AddCards(islands))
                {
                    return;
                }
            }
        }

        private bool AddCards(System.Collections.Generic.IReadOnlyList<Configs.IslandSO> islands)
        {
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
                    return false;
                }

                var built = _buildService.CountBuilt(island);
                var status = island.IsComingSoon ? GameLocalization.L10n.Get("soon", "Soon") : null;
                var captured = island;
                card.Bind(
                    GameLocalization.L10n.Translate(island.DisplayName),
                    island.Icon,
                    built,
                    island.SlotCount,
                    !island.IsComingSoon,
                    status,
                    () => Select(captured));
            }

            return true;
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
