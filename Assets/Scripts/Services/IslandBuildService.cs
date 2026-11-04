using System;
using System.Collections.Generic;
using Configs;
using Services.Islands;
using Systems.CurrencySystem;
using Systems.CurrencySystem.Interfaces;

namespace Services
{
    /// <summary>
    /// Island building: which island is active, what can be built and paid for, and building it.
    /// Progression rules live in <see cref="IslandProgression"/>, the model pieces in IslandPieceRig, trees in
    /// IslandTreeSpawner and the build animations in IslandBuildAnimator (all in Services.Islands).
    /// </summary>
    public class IslandBuildService
    {
        private readonly IslandBuildProgressStore _store;
        private readonly ICurrencyService _currencyService;
        private readonly RegionService _regionService;
        private readonly RegionUpgradeService _regionUpgradeService;

        private readonly IslandProgression _progression;
        private readonly IslandPieceRig _rig;
        private readonly IslandTreeSpawner _trees;
        private readonly IslandBuildAnimator _animator;

        public IslandBuildService(
            IReadOnlyList<RegionSO> regions,
            IslandBuildProgressStore store,
            ICurrencyService currencyService,
            RegionService regionService,
            RegionUpgradeService regionUpgradeService)
        {
            _store = store;
            _currencyService = currencyService;
            _regionService = regionService;
            _regionUpgradeService = regionUpgradeService;

            _progression = new IslandProgression(regions, store);
            _rig = new IslandPieceRig(regionService, regionUpgradeService, () => ActiveIsland);
            _trees = new IslandTreeSpawner(_rig);
            _animator = new IslandBuildAnimator(_rig, _trees, regionUpgradeService, Refund);
        }

        public event Action Changed;

        /// <summary>Fires once when the last slot of an island is built (reward + next island, GDD 5.3).</summary>
        public event Action<IslandSO> IslandCompleted;

        #region Progression

        /// <summary>All islands in progression order (regions in order, islands in region order), no coming-soon ones.</summary>
        public List<IslandSO> OrderedIslands() => _progression.OrderedIslands();

        public bool IsCompleted(IslandSO island) => _progression.IsCompleted(island);

        /// <summary>
        /// Islands open strictly one after another (GDD 5.1): the first one is always open, the next one after the
        /// previous is fully built. An island the player already started stays open (saves from before this rule).
        /// </summary>
        public bool IsUnlocked(IslandSO island) => _progression.IsUnlocked(island);

        /// <summary>The island the player should build now: the first open island that is not finished.</summary>
        public IslandSO CurrentProgressIsland() => _progression.CurrentProgressIsland();

        /// <summary>True when every island is fully built ("new islands coming soon").</summary>
        public bool AllIslandsCompleted() => _progression.AllIslandsCompleted();

        // Regions in progression order (Asia, North, ...).
        public IReadOnlyList<RegionSO> Regions => _progression.Regions;

        /// <summary>The region that contains the island (null when unknown).</summary>
        public RegionSO RegionOf(IslandSO island) => _progression.RegionOf(island);

        public int CountBuilt(IslandSO island) => _progression.CountBuilt(island);

        #endregion

        #region Active island

        public IslandSO ActiveIsland
        {
            get
            {
                var selected = _progression.FindIsland(_store.ActiveIslandId);
                if (selected != null && !selected.IsComingSoon && IsUnlocked(selected))
                {
                    return selected;
                }

                return CurrentProgressIsland() ?? _progression.FirstPlayable();
            }
        }

        public void EnsureActiveIslandSaved()
        {
            var island = ActiveIsland;
            if (island != null)
            {
                _store.SetActiveIsland(island.IslandId);
            }
        }

        public void SelectIsland(IslandSO island)
        {
            if (island == null || island.IsComingSoon || !IsUnlocked(island))
            {
                return;
            }

            _store.SetActiveIsland(island.IslandId);
            _regionService?.LoadPrefab(island.IslandPrefab);
            Restore();
            Changed?.Invoke();
        }

        #endregion

        #region Building

        public bool IsBuilt(BuildingSlot slot)
        {
            var island = ActiveIsland;
            return island != null && slot != null && _store.IsBuilt(island.IslandId, slot.SlotId);
        }

        public bool CanSelect(IslandSO island, BuildingSlot slot)
        {
            if (island == null || slot == null || slot.Building == null || !island.Contains(slot))
            {
                return false;
            }

            if (_store.IsBuilt(island.IslandId, slot.SlotId))
            {
                return false;
            }

            return slot.RequirementsMet(island, _store.GetBuilt(island.IslandId));
        }

        public bool CanAfford(BuildingSlot slot)
        {
            if (slot == null)
            {
                return false;
            }

            var stars = _currencyService?.GetCurrency(CurrencyType.Star)?.Value ?? 0f;
            return stars >= slot.StarCost;
        }

        public bool TryBuild(BuildingSlot slot)
        {
            var island = ActiveIsland;
            if (!CanSelect(island, slot) || !CanAfford(slot))
            {
                return false;
            }

            _rig.Prepare();
            if (!CanPlace(slot))
            {
                return false;
            }

            if (_currencyService != null && !_currencyService.SpendCurrency(CurrencyType.Star, slot.StarCost))
            {
                return false;
            }

            _store.MarkBuilt(island.IslandId, slot.SlotId);
            _animator.Enqueue(slot);
            GameAnalyticsEvents.BuildingBuilt(island.IslandId, slot.SlotId);
            Changed?.Invoke();

            if (IsCompleted(island))
            {
                GameAnalyticsEvents.IslandCompleted(island.IslandId);
                IslandCompleted?.Invoke(island);
            }

            return true;
        }

        // The model can show the slot: a tree anchor with a real size, or pieces in the build clip.
        private bool CanPlace(BuildingSlot slot)
        {
            if (slot == null || slot.Building == null)
            {
                return false;
            }

            if (slot.IsTree)
            {
                return _trees.CanPlace(slot);
            }

            return _rig.PieceSteps(slot).Count > 0;
        }

        // A paid slot the model could not show after all: give the stars back.
        private void Refund(BuildingSlot slot)
        {
            var island = ActiveIsland;
            if (island == null || slot == null)
            {
                return;
            }

            _store.UnmarkBuilt(island.IslandId, slot.SlotId);
            _currencyService?.AddCurrency(CurrencyType.Star, slot.StarCost);
            Changed?.Invoke();
        }

        #endregion

        #region Model state

        /// <summary>Shows the active island's model as saved: built pieces and trees in place, no animation.</summary>
        public void Restore()
        {
            var island = ActiveIsland;
            if (island == null)
            {
                return;
            }

            _animator.Cancel();
            _rig.StopPlaying();
            _regionUpgradeService?.CancelPlayback();
            _trees.Clear();
            _rig.ClearShown();
            _rig.Prepare();
            PlaceBuilt(island);
            _rig.ApplyMask();
            _regionUpgradeService?.HoldPose();
        }

        private void PlaceBuilt(IslandSO island)
        {
            var slots = island != null ? island.Slots : null;
            var built = island != null ? _store.GetBuilt(island.IslandId) : null;
            if (slots == null || built == null)
            {
                return;
            }

            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.Building == null || !built.Contains(slot.SlotId))
                {
                    continue;
                }

                if (slot.IsTree)
                {
                    _trees.Spawn(slot);
                }
                else
                {
                    _rig.ShowPieces(_rig.PieceIds(slot));
                }
            }
        }

        #endregion
    }
}
