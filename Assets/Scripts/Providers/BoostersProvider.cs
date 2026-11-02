using System;
using System.Collections.Generic;
using Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Utils.Save;

namespace Providers
{
    public class BoostersProvider
    {
        private static readonly BoostType[] AllTypes =
        {
            BoostType.Shovel,
            BoostType.Energy,
            BoostType.Bomb,
            BoostType.ExtraMoves,
        };

        public List<BoosterModel> BoostersModels { get; private set; } = new();

        public BoostersProvider()
        {
            Load();
        }

        public BoosterModel GetBoosterModel(BoostType boostType)
        {
            for (var i = 0; i < BoostersModels.Count; i++)
            {
                if(BoostersModels[i].Type == boostType)
                {
                    return BoostersModels[i];
                }
            }

            // Missing in an old save: create it instead of throwing.
            var model = new BoosterModel(boostType);
            BoostersModels.Add(model);
            return model;
        }

        public void Load()
        {
            BoostersModels = new List<BoosterModel>();

            var value = PlayerPrefs.GetString(PlayerPrefsKeys.Boosters, "empty");
            if(value != "empty")
            {
                try
                {
                    foreach (var token in JArray.Parse(value))
                    {
                        var type = (BoostType)token.Value<int>("Type");
                        var countToken = token["Count"];

                        // Old saves serialized the whole ReactiveProperty: {"Count":{"Value":3,...}}.
                        var count = countToken == null ? 0
                            : countToken.Type == JTokenType.Object ? countToken.Value<int>("Value")
                            : countToken.Value<int>();

                        var model = GetBoosterModel(type);
                        model.Add(Mathf.Max(0, count) - model.Count.Value);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[BoostersProvider] Failed to load boosters, resetting: {ex.Message}");
                    BoostersModels = new List<BoosterModel>();
                }
            }

            foreach (var type in AllTypes)
            {
                GetBoosterModel(type);
            }
        }

        public void Save()
        {
            var data = new List<BoosterSaveData>(BoostersModels.Count);
            foreach (var model in BoostersModels)
            {
                data.Add(new BoosterSaveData { Type = (int)model.Type, Count = model.Count.Value });
            }

            PlayerPrefs.SetString(PlayerPrefsKeys.Boosters, JsonConvert.SerializeObject(data));
            PlayerPrefs.Save();
        }

        [Serializable]
        private struct BoosterSaveData
        {
            public int Type;
            public int Count;
        }
    }
}
