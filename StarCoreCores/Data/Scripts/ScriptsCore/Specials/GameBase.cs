using System;
using System.Collections.Generic;
using Digi;
using VRage.Game.Components;

namespace MIG.SpecCores
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class GameBase : MySessionComponentBase
    {
        public static GameBase instance = null;
        private readonly List<Action> unloadActions = new List<Action>();
        public GameBase()
        {
            instance = this;
        }

        protected override void UnloadData()
        {
            SpaceEngineers.Game.ModAPI.Ingame.IMyTurretControlBlock b = null;
            foreach (var x in unloadActions)
            {
                try
                {
                    x.Invoke();
                }
                catch (Exception e)
                {
                    Log.ChatError(e);
                }
            }
        }

        public static void AddUnloadAction(Action a)
        {
            instance.unloadActions.Add(a);
        }
    }
}