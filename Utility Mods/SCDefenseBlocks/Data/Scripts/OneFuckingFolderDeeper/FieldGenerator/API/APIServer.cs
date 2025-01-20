using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.ModAPI;
using FieldGeneratorCore = Starcore.FieldGenerator.FieldGenerator;

namespace Starcore.FieldGenerator.API
{
    public class APIProvider
    {
        private const long HandlerID = 917632;
        private bool _registered;

        public bool IsReady { get; private set; }

        public void LoadAPI()
        {
            if (!_registered)
            {
                _registered = true;
                MyAPIGateway.Utilities.RegisterMessageHandler(HandlerID, HandleMessage);
            }
            
            IsReady = true;

            try
            {
                MyAPIGateway.Utilities.SendModMessage(HandlerID, FieldGeneratorSession.I.APIBackend.APIMethods);
            }
            catch (Exception ex)
            {
                Log.Error($"Field Generator API Load Failed!" + "\n" + ex);
            }
        }

        public void UnloadAPI()
        {
            if (_registered)
            {
                _registered = false;
                MyAPIGateway.Utilities.UnregisterMessageHandler(HandlerID, HandleMessage);
            }
            IsReady = false;
        }

        private void HandleMessage(object o)
        {
            if ((o as string) == "APIRequest")
                MyAPIGateway.Utilities.SendModMessage(HandlerID, FieldGeneratorSession.I.APIBackend.APIMethods);
        }
    }

    internal class APIBackend
    {
        internal readonly Dictionary<string, Delegate> APIMethods;

        internal APIBackend()
        {
            APIMethods = new Dictionary<string, Delegate>()
            {
                ["GetFirstFieldGeneratorOnGrid"] = new Func<long, IMyFunctionalBlock>(GetFirstFieldGeneratorOnGrid),

                ["IsSiegeActive"] = new Func<IMyFunctionalBlock, bool>(IsSiegeActive),
                ["SetSiegeActive"] = new Action<IMyFunctionalBlock, bool>(SetSiegeActive),

                ["IsSiegeCooldownActive"] = new Func<IMyFunctionalBlock, bool>(IsSiegeCooldownActive),
                ["SetSiegeCooldownActive"] = new Action<IMyFunctionalBlock, bool>(SetSiegeCooldownActive),

                ["GetSiegeCooldown"] = new Func<IMyFunctionalBlock, int>(GetSiegeCooldown),
                ["SetSiegeCooldown"] = new Action<IMyFunctionalBlock, int>(SetSiegeCooldown),

                ["GetFieldPower"] = new Func<IMyFunctionalBlock, float>(GetFieldPower),
                ["SetFieldPower"] = new Action<IMyFunctionalBlock, float>(SetFieldPower),

                ["GetMaximumFieldPower"] = new Func<IMyFunctionalBlock, float>(GetMaximumFieldPower),
                ["GetMinimumFieldPower"] = new Func<IMyFunctionalBlock, float>(GetMinimumFieldPower),

                ["GetPowerDraw"] = new Func<IMyFunctionalBlock, float>(GetPowerDraw),

                ["GetStability"] = new Func<IMyFunctionalBlock, float>(GetStability),
                ["SetStability"] = new Action<IMyFunctionalBlock, float>(SetStability),
            };
        }

        private IMyFunctionalBlock GetFirstFieldGeneratorOnGrid(long entityID)
        {
            if (entityID == 0)
                return null;

            HashSet<long> generators;
            if (!FieldGeneratorSession.ActiveGenerators.TryGetValue(entityID, out generators))
            {
                return null;
            }

            var fieldGeneratorID = generators.FirstOrDefault();
            if (fieldGeneratorID == 0)
            {
                return null;
            }

            IMyEntity generatorEntity;
            if (!MyAPIGateway.Entities.TryGetEntityById(fieldGeneratorID, out generatorEntity))
            {
                return null;
            } 

            return generatorEntity as IMyFunctionalBlock;
        }

        private bool IsSiegeActive(IMyFunctionalBlock block)
        {
            var logic = FieldGeneratorCore.GetLogic<FieldGeneratorCore>(block.EntityId);
            if (logic != null)
            {
                return logic.SiegeMode.Value;
            }

            return false;
        }
        private void SetSiegeActive(IMyFunctionalBlock block, bool Active)
        {
            var logic = FieldGeneratorCore.GetLogic<FieldGeneratorCore>(block.EntityId);
            if (logic != null)
            {
                logic.SiegeMode.Value = Active;
            }
        }

        private bool IsSiegeCooldownActive(IMyFunctionalBlock block)
        {
            var logic = FieldGeneratorCore.GetLogic<FieldGeneratorCore>(block.EntityId);
            if (logic != null)
            {
                return logic.SiegeCooldownActive.Value;
            }

            return false;
        }
        private void SetSiegeCooldownActive(IMyFunctionalBlock block, bool Active)
        {
            var logic = FieldGeneratorCore.GetLogic<FieldGeneratorCore>(block.EntityId);
            if (logic != null)
            {
                logic.SiegeCooldownActive.Value = Active;
            }
        }

        private int GetSiegeCooldown(IMyFunctionalBlock block)
        {
            var logic = FieldGeneratorCore.GetLogic<FieldGeneratorCore>(block.EntityId);
            if (logic != null)
            {
                return logic.SiegeCooldownTime.Value;
            }

            return 0;
        }
        private void SetSiegeCooldown(IMyFunctionalBlock block, int Time)
        {
            var logic = FieldGeneratorCore.GetLogic<FieldGeneratorCore>(block.EntityId);
            if (logic != null)
            {
                logic.SiegeCooldownTime.Value = Time;
            }
        }

        private float GetFieldPower (IMyFunctionalBlock block)
        {
            var logic = FieldGeneratorCore.GetLogic<FieldGeneratorCore>(block.EntityId);
            if (logic != null)
            {
                return logic.FieldPower.Value;
            }

            return 0;
        }
        private void SetFieldPower(IMyFunctionalBlock block, float Power)
        {
            var logic = FieldGeneratorCore.GetLogic<FieldGeneratorCore>(block.EntityId);
            if (logic != null)
            {
                logic.FieldPower.Value = Power;
            }
        }

        private float GetMaximumFieldPower(IMyFunctionalBlock block)
        {
            var logic = FieldGeneratorCore.GetLogic<FieldGeneratorCore>(block.EntityId);
            if (logic != null)
            {
                return logic.MaxFieldPower.Value;
            }

            return 0;
        }
        private float GetMinimumFieldPower(IMyFunctionalBlock block)
        {
            var logic = FieldGeneratorCore.GetLogic<FieldGeneratorCore>(block.EntityId);
            if (logic != null)
            {
                return logic.MinFieldPower.Value;
            }

            return 0;
        }

        private float GetPowerDraw(IMyFunctionalBlock block)
        {
            var logic = FieldGeneratorCore.GetLogic<FieldGeneratorCore>(block.EntityId);
            if (logic != null)
            {
                return logic.CalculatePowerDraw();
            }

            return 0;
        }

        private float GetStability(IMyFunctionalBlock block)
        {
            var logic = FieldGeneratorCore.GetLogic<FieldGeneratorCore>(block.EntityId);
            if (logic != null)
            {
                return logic.Stability.Value;
            }

            return 0;
        }
        private void SetStability(IMyFunctionalBlock block, float Stability)
        {
            var logic = FieldGeneratorCore.GetLogic<FieldGeneratorCore>(block.EntityId);
            if (logic != null)
            {
                logic.Stability.Value = Stability;
            }
        }
    }
}
