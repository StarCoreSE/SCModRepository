using System;
using System.Collections.Generic;
using Digi;
using MIG.Shared.SE;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using F = System.Func<object, bool>;
using FF = System.Func<Sandbox.ModAPI.IMyTerminalBlock, object, bool>;
using A = System.Action<object>;
using C = System.Func<Sandbox.ModAPI.IMyTerminalBlock, object>;
using U = System.Collections.Generic.List<int>;
using L = System.Collections.Generic.IDictionary<int, float>;

namespace MIG.SpecCores
{
    public class Hooks
    {
        
        private static Action<string, C, A, FF, F, F, A> registerCustomLimitConsumer = RegisterCustomLimitConsumer;
        private static Func<IMyCubeGrid, object> getMainSpecCore = GetMainSpecCore;
        private static Func<IMyCubeGrid, IMyTerminalBlock> getMainSpecCoreBlock = GetMainSpecCoreBlock;
        private static Action<object, L, int> getSpecCoreLimits = GetSpecCoreLimits;
        private static Action<object, U> getSpecCoreUpgrades = GetSpecCoreUpgrades;
        private static Action<object, L,L> setSpecCoreUpgrades = SetSpecCoreCustomValues;
        
        private static Func<IMyTerminalBlock, object> getSpecCoreBlock = GetSpecCoreBlock;
        
        private static Func<IMyCubeGrid, Dictionary<Type, HashSet<IMyCubeBlock>>> getGridBlocksByType = GetGridBlocksByType;
        private static Func<IMyCubeGrid, Dictionary<MyDefinitionId, HashSet<IMyCubeBlock>>> getGridBlocksById = GetGridBlocksById;
        
        private static Func<object, IMyTerminalBlock> getBlockSpecCore = GetBlockSpecCore;
        private static Func<IMyTerminalBlock, object> getLimitedBlock = GetLimitedBlock;
        private static Func<object, IMyTerminalBlock> getLimitedBlockBlock = GetLimitedBlockBlock;
        
        private static Action <int, Func<object, List<IMyCubeGrid>, float>> registerSpecCoreCurrentPointCustomFx = RegisterSpecCoreCurrentPointCustomFx;
        
        
        private static Action<Action<IMyTerminalBlock, object, Dictionary<int, float>, Dictionary<int, float>>> addSpecCoreLimitsInterceptor = AddSpecCoreLimitsInterceptor;
        private static event Action<IMyTerminalBlock, object, Dictionary<int, float>, Dictionary<int, float>> SpecCoreLimitsInterceptor = null;

        public static void InvokeLimitsInterceptor(ISpecBlock block, Dictionary<int, float> st, Dictionary<int, float> dynam)
        {
            SpecCoreLimitsInterceptor?.Invoke(block.block, block, st, dynam);
        }
        
        private static Dictionary<int, Func<object, List<IMyCubeGrid>, float>> SpecCoreCurrentCustom = new Dictionary<int, Func<object, List<IMyCubeGrid>, float>>();
        
        
        private static Func<object, List<IMyCubeGrid>, string> CanSpecCoreWork;
        
        public static Dictionary<string, HookedLimiterInfo> HookedConsumerInfos = new Dictionary<string, HookedLimiterInfo>();
        
        
        public static event Action<object> OnSpecBlockCreated;
        public static event Action<object> OnSpecBlockDestroyed;
        public static event Action<object> OnLimitedBlockCreated;
        public static event Action<object> OnLimitedBlockDestroyed;

        public static event Action<object, List<IMyCubeGrid>> OnSpecCoreChanged;

        public static void TriggerOnSpecCoreChanged(ISpecBlock block, List<IMyCubeGrid> grids)
        {
            OnSpecCoreChanged?.Invoke(block, grids);
        }

        public static void TriggerOnSpecBlockCreated(ISpecBlock block)
        {
            OnSpecBlockCreated?.Invoke(block);
        }

        public static void TriggerOnSpecBlockDestroyed(ISpecBlock block)
        {
            OnSpecBlockDestroyed?.Invoke(block);
        }

        public static void TriggerOnLimitedBlockCreated(ILimitedBlock block)
        {
            OnLimitedBlockCreated?.Invoke(block);
        }

        public static void TriggerOnLimitedBlockDestroyed(ILimitedBlock block)
        {
            OnLimitedBlockDestroyed?.Invoke(block);
        }
        
        public static object GetSpecCoreBlock(IMyTerminalBlock block)
        {
            var grid = block.CubeGrid;
            var ship = grid.GetShip();
            if (ship == null) return null;
            ISpecBlock specBlock;
            ship.SpecBlocks.TryGetValue(block, out specBlock);
            return specBlock;
        }
        
        public static Dictionary<Type, HashSet<IMyCubeBlock>> GetGridBlocksByType(IMyCubeGrid grid)
        {
            var ship = grid.GetShip();
            return ship?.BlocksCache;
        }
        
        public static Dictionary<MyDefinitionId, HashSet<IMyCubeBlock>> GetGridBlocksById(IMyCubeGrid grid)
        {
            var ship = grid.GetShip();
            return ship?.BlocksCacheByType;
        }

        public static IMyTerminalBlock GetBlockSpecCore(object block)
        {
            return ((ISpecBlock) block).block;
        }
        
        public static object GetLimitedBlock(IMyTerminalBlock block)
        {
            var grid = block.CubeGrid;
            var ship = grid.GetShip();
            if (ship == null) return null;
            ILimitedBlock limitedBlock;
            ship.LimitedBlocks.TryGetValue(block, out limitedBlock);
            return limitedBlock;
        }

        public static IMyTerminalBlock GetLimitedBlockBlock(object block)
        {
            return ((ILimitedBlock) block).GetBlock();
        }



        public static object GetMainSpecCore(IMyCubeGrid grid)
        {
            Ship ship;
            if (OriginalSpecCoreSession.Instance.gridToShip.TryGetValue(grid.EntityId, out ship))
            {
                return ship.CachedCore;
            }

            return null;
        }
        
        public static IMyTerminalBlock GetMainSpecCoreBlock(IMyCubeGrid grid)
        {
            Ship ship;
            if (OriginalSpecCoreSession.Instance.gridToShip.TryGetValue(grid.EntityId, out ship))
            {
                return ship.CachedCore.block;
            }

            return null;
        }
        
        public static void GetSpecCoreLimits(object specCore, IDictionary<int, float> dictionary, int mode)
        {
            var specBlock = specCore as SpecBlock;
            if (specBlock == null) return;

            switch (mode)
            {
                case 1: dictionary.Sum(specBlock.StaticLimits); break;
                case 2: dictionary.Sum(specBlock.DynamicLimits); break;
                case 3: dictionary.Sum(specBlock.FoundLimits); break;
                case 4: dictionary.Sum(specBlock.TotalLimits); break;
                case 5: dictionary.Sum(specBlock.Settings.CustomStatic); break;
                case 6: dictionary.Sum(specBlock.Settings.CustomDynamic); break;
                case 7: dictionary.Sum(specBlock.GetLimits()); break;
            }
        }
        
        public static void GetSpecCoreUpgrades(object specCore, List<int> copyTo, int mode)
        {
            var specBlock = specCore as SpecBlock;
            if (specBlock == null) return;
            copyTo.AddRange(specBlock.Settings.Upgrades);
        }
        
        public static void GetSpecCoreUpgrades(object specCore, List<int> copyTo)
        {
            var specBlock = specCore as SpecBlock;
            if (specBlock == null) return;
            copyTo.AddRange(specBlock.Settings.Upgrades);
        }

        public static void SetSpecCoreCustomValues(object specCore, IDictionary<int, float> staticValues, IDictionary<int, float> dynamicValues)
        {
            var specBlock = specCore as SpecBlock;
            if (specBlock == null) return;
            specBlock.Settings.CustomStatic.Sum(staticValues);
            specBlock.Settings.CustomDynamic.Sum(dynamicValues);
            specBlock.ApplyUpgrades();
            specBlock.SaveSettings();
        }

        

        /// <summary>
        /// Must be inited in LoadData of MySessionComponentBase
        /// </summary>
        public static void Init()
        {
            ModConnection.Init();

            TorchExtensions.Init();
            
            ModConnection.SetValue("MIG.SpecCores.RegisterCustomLimitConsumer", registerCustomLimitConsumer);
            ModConnection.SetValue("MIG.SpecCores.GetMainSpecCore", getMainSpecCore);
            ModConnection.SetValue("MIG.SpecCores.GetMainSpecCoreBlock", getMainSpecCoreBlock);
            ModConnection.SetValue("MIG.SpecCores.GetSpecCoreLimits", getSpecCoreLimits);
            ModConnection.SetValue("MIG.SpecCores.GetSpecCoreUpgrades", getSpecCoreUpgrades);
            ModConnection.SetValue("MIG.SpecCores.SetSpecCoreCustomValues", setSpecCoreUpgrades);

            ModConnection.SetValue("MIG.SpecCores.GetGridBlocksByType", getGridBlocksByType);
            ModConnection.SetValue("MIG.SpecCores.GetGridBlocksById", getGridBlocksById);

            ModConnection.SetValue("MIG.SpecCores.GetSpecCoreBlock", getSpecCoreBlock);
            ModConnection.SetValue("MIG.SpecCores.GetBlockSpecCore", getBlockSpecCore);
            ModConnection.SetValue("MIG.SpecCores.GetLimitedBlock", getLimitedBlock);
            ModConnection.SetValue("MIG.SpecCores.GetLimitedBlockBlock", getLimitedBlockBlock);
            
            ModConnection.Subscribe("MIG.SpecCores.RegisterCustomLimitConsumer", registerCustomLimitConsumer, (x) => { registerCustomLimitConsumer = x; });
            
            ModConnection.SetValue("MIG.SpecCores.RegisterSpecCorePointCustomFx", registerSpecCoreCurrentPointCustomFx);
            ModConnection.SetValue("MIG.SpecCores.AddSpecCoreLimitsInterceptor", addSpecCoreLimitsInterceptor);
           
            ModConnection.Subscribe("MIG.SpecCores.OnSpecBlockCreated", OnSpecBlockCreated, (a) => { OnSpecBlockCreated += a; });
            ModConnection.Subscribe("MIG.SpecCores.OnSpecBlockDestroyed", OnSpecBlockDestroyed, (a) => { OnSpecBlockDestroyed += a; });
            ModConnection.Subscribe("MIG.SpecCores.OnLimitedBlockCreated", OnLimitedBlockCreated, (a) => { OnLimitedBlockCreated += a; });
            ModConnection.Subscribe("MIG.SpecCores.OnLimitedBlockDestroyed", OnLimitedBlockDestroyed, (a) => { OnLimitedBlockDestroyed += a; });

            ModConnection.Subscribe("MIG.SpecCores.OnSpecBlockChanged", OnSpecCoreChanged, (a) => { OnSpecCoreChanged += a; });
            ModConnection.Subscribe("MIG.SpecCores.CanSpecCoreWork", CanSpecCoreWork, (a) => { CanSpecCoreWork = a; });
        }
        
        public static void Close()
        {
            ModConnection.Close();
        }

        public static void RegisterCustomLimitConsumer(string Id, C OnNewConsumerRegistered, A CanWork, FF CheckConditions, F CanBeDisabled, F IsDrainingPoints, A Disable)
        {
            HookedConsumerInfos[Id] = new HookedLimiterInfo()
            {
                OnNewConsumerRegistered = OnNewConsumerRegistered,
                CanWork = CanWork,
                CheckConditions = CheckConditions,
                IsDrainingPoints = IsDrainingPoints,
                Disable = Disable,
            };
        }

        
        public static void RegisterSpecCoreCurrentPointCustomFx(int id, Func<object, List<IMyCubeGrid>, float> fx)
        {
            SpecCoreCurrentCustom[id] = fx;
        }
        
        public static void AddSpecCoreLimitsInterceptor(Action<IMyTerminalBlock, object, Dictionary<int, float>, Dictionary<int, float>> fx)
        {
            SpecCoreLimitsInterceptor += fx;
        }
        
        public static string CanBeApplied(ISpecBlock specBlock, List<IMyCubeGrid> grids)
        {
            return CanSpecCoreWork?.Invoke(specBlock, grids) ?? null;
        }

        public static float GetCurrentPointValueForSpecCore(ISpecBlock specBlock, List<IMyCubeGrid> grids, LimitPoint lp)
        {
            var fx = SpecCoreCurrentCustom.GetOr(lp.Id, null);
            if (fx == null && OriginalSpecCoreSession.IsDebug)
            {
                Log.ChatError($"Custom function for PointId={lp.Id} is not found");
            }
            return fx?.Invoke(specBlock, grids) ?? 0;
        }
        
        public static float GetMaxPointValueForSpecCore(ISpecBlock specBlock, List<IMyCubeGrid> grids, LimitPoint lp)
        {
            var fx = SpecCoreCurrentCustom.GetOr(lp.Id, null);
            if (fx == null && OriginalSpecCoreSession.IsDebug)
            {
                Log.ChatError($"Custom function for PointId={lp.Id} is not found");
            }
            return fx?.Invoke(specBlock, grids) ?? 0;
        }
    }

    public class HookedLimiterInfo
    {
        public C OnNewConsumerRegistered;
        public A CanWork;
        public FF CheckConditions;
        public F IsDrainingPoints;
        public A Disable;
    }
}