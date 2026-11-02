using System;
using System.Collections.Generic;
using UnityEngine;
using Utils.Save;

namespace Services
{
    public class IslandBuildProgressStore
    {
        private readonly IslandProgressSave _save;

        public IslandBuildProgressStore()
        {
            var json = PlayerPrefs.GetString(PlayerPrefsKeys.IslandBuildProgress, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                _save = new IslandProgressSave();
                return;
            }

            try
            {
                _save = JsonUtility.FromJson<IslandProgressSave>(json) ?? new IslandProgressSave();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[IslandBuildProgressStore] Could not read island progress: {exception.Message}");
                _save = new IslandProgressSave();
            }

            if (_save.islands == null)
            {
                _save.islands = new List<IslandBuiltSlots>();
            }
        }

        public string ActiveIslandId => _save.activeIslandId;

        public void SetActiveIsland(string islandId)
        {
            if (string.IsNullOrEmpty(islandId) || _save.activeIslandId == islandId)
            {
                return;
            }

            _save.activeIslandId = islandId;
            Persist();
        }

        public bool IsBuilt(string islandId, string slotId)
        {
            var entry = FindEntry(islandId);
            return entry != null && entry.slotIds != null && entry.slotIds.Contains(slotId);
        }

        public IList<string> GetBuilt(string islandId)
        {
            var entry = FindEntry(islandId);
            if (entry == null || entry.slotIds == null)
            {
                return EmptySlots;
            }

            return entry.slotIds;
        }

        private static readonly List<string> EmptySlots = new();

        public void MarkBuilt(string islandId, string slotId)
        {
            if (string.IsNullOrEmpty(islandId) || string.IsNullOrEmpty(slotId) || IsBuilt(islandId, slotId))
            {
                return;
            }

            var entry = FindEntry(islandId);
            if (entry == null)
            {
                entry = new IslandBuiltSlots { islandId = islandId, slotIds = new List<string>() };
                _save.islands.Add(entry);
            }

            if (entry.slotIds == null)
            {
                entry.slotIds = new List<string>();
            }

            entry.slotIds.Add(slotId);
            Persist();
        }

        public void UnmarkBuilt(string islandId, string slotId)
        {
            var entry = FindEntry(islandId);
            if (entry == null || entry.slotIds == null || !entry.slotIds.Remove(slotId))
            {
                return;
            }

            Persist();
        }

        private IslandBuiltSlots FindEntry(string islandId)
        {
            if (_save.islands == null || string.IsNullOrEmpty(islandId))
            {
                return null;
            }

            for (var i = 0; i < _save.islands.Count; i++)
            {
                var entry = _save.islands[i];
                if (entry != null && entry.islandId == islandId)
                {
                    return entry;
                }
            }

            return null;
        }

        private void Persist()
        {
            PlayerPrefs.SetString(PlayerPrefsKeys.IslandBuildProgress, JsonUtility.ToJson(_save));
            PlayerPrefs.Save();
        }

        [Serializable]
        private class IslandProgressSave
        {
            public string activeIslandId;
            public List<IslandBuiltSlots> islands = new();
        }

        [Serializable]
        private class IslandBuiltSlots
        {
            public string islandId;
            public List<string> slotIds = new();
        }
    }
}
