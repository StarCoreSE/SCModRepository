using System;
using System.Collections.Generic;
using System.ComponentModel;
using SC.SUGMA.GameModes.DeathMatch;
using SC.SUGMA.GameState;
using SC.SUGMA.HeartNetworking;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace SC.SUGMA
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class SUGMA_SessionComponent : MySessionComponentBase
    {
        public const float ModVersion = 0.0f;

        private readonly Dictionary<string, ComponentBase> _components = new Dictionary<string, ComponentBase>
        {
            ["GameTimer"] = new MatchTimer(),
            ["HeartNetwork"] = new HeartNetwork(),
            ["PointTracker"] = new PointTracker(),
            ["PlayerTracker"] = new PlayerTracker(),
            ["DeathmatchGamemode"] = new DeathmatchGamemode("PointTracker"),
        };

        public static SUGMA_SessionComponent I { get; private set; }

        #region Base Methods

        public override void LoadData()
        {
            I = this;
            Log.Init();
            try
            {
                foreach (var component in _components)
                    component.Value.Init(component.Key);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(SUGMA_SessionComponent));
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                foreach (var component in _components.Values)
                    component.UpdateTick();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(SUGMA_SessionComponent));
            }
        }

        protected override void UnloadData()
        {
            try
            {
                foreach (var component in _components.Values)
                    component.Close();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(SUGMA_SessionComponent));
            }

            I = null;
            Log.Close();
        }

        #endregion

        public T GetComponent<T>(string id) where T : ComponentBase
        {
            ComponentBase component;
            _components.TryGetValue(id, out component);
            return (T) component;
        }
    }
}
