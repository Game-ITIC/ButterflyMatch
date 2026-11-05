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

            GameLocalization.L10n.Changed += RefreshIfOpen;

            // A paged island screen can be swiped into at any time, so its cards are always kept filled.
            if (_panel != null && _panel.IsPaged)
            {
                Refresh();
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

            GameLocalization.L10n.Changed -= RefreshIfOpen;

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

            if (_panel.IsPaged)
            {
                // Nav tab: always slides to the island screen (cards are already up to date).
                _panel.Show();
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
            if (_panel != null && (_panel.IsOpen || _panel.IsPaged))
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

            _panel.MatchCardNameSizes();
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
                // Islands open one after another (GDD 5.1); finished islands stay open for viewing.
                var unlocked = _buildService.IsUnlocked(island);
                string status = null;
                if (island.IsComingSoon)
                {
                    status = GameLocalization.L10n.Get("soon", "Soon");
                }
                // Locked islands show a lock icon on the card (status text is the fallback if the prefab has no icon).
                var locked = !island.IsComingSoon && !unlocked;
                if (locked)
                {
                    status = GameLocalization.L10n.Get("island_locked", "Locked");
                }

                var captured = island;
                card.Bind(
                    GameLocalization.L10n.Translate(island.DisplayName),
                    island.Icon,
                    built,
                    island.SlotCount,
                    !island.IsComingSoon && unlocked,
                    locked,
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
