using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace capacitorlimited
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, "CapacitorLarge")]
    public class capacitorlimited : MyGameLogicComponent
    {
        private Sandbox.ModAPI.IMyBatteryBlock Capacitor;
        private int triggerTick = 0;
        private const double OFFSET_DISTANCE = 3;
        private const int COUNTDOWN_SECONDS = 30 * 60; // 5 seconds in game time

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return; // Only do explosions serverside
            Capacitor = Entity as Sandbox.ModAPI.IMyBatteryBlock;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (Capacitor == null || Capacitor.CubeGrid.Physics == null) return;
            Capacitor.ChargeMode = ChargeMode.Recharge; // Set to recharge mode by default
            Capacitor.IsWorkingChanged += CapacitorIsWorkingChanged;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            if (Capacitor == null || Capacitor.CubeGrid.Physics == null) return;
            if (Capacitor.ChargeMode != ChargeMode.Recharge)
            {
                triggerTick += 1;
                if (triggerTick >= COUNTDOWN_SECONDS)
                {
                    DoExplosion();
                }
                else if (triggerTick % 60 == 0) // Show notification every second
                {
                    int remainingSeconds = COUNTDOWN_SECONDS - triggerTick;
                    int seconds = remainingSeconds / 60;
                    string name = Capacitor.CustomName;
                    string message = string.Format("Capacitor ({0}) explodes in {1} seconds", name, seconds);

                    MyVisualScriptLogicProvider.ShowNotificationLocal(message, 1000, "Red");
                }
            }
            else
            {
                triggerTick = 0; // Reset countdown if in recharge mode
            }
        }

        private void DoExplosion()
        {
            if (Capacitor == null || Capacitor.CubeGrid.Physics == null) return;
            double radius = 30;
            BoundingSphereD sphere = new BoundingSphereD(Capacitor.WorldMatrix.Translation + (Capacitor.WorldMatrix.Forward * OFFSET_DISTANCE), radius); // Apply offset, 10);
            MyExplosionInfo explosion = new MyExplosionInfo(0f, 10000f, sphere, MyExplosionTypeEnum.CUSTOM, true);

            MyExplosions.AddExplosion(ref explosion);
        }

        private void CapacitorIsWorkingChanged(IMyCubeBlock obj)
        {
            if (obj.EntityId != Capacitor.EntityId) return;
            if (Capacitor.ChargeMode != ChargeMode.Recharge && Capacitor.Enabled)
            {
                triggerTick += 1;
            }
        }

        public override void Close()
        {
            if (Capacitor != null)
            {
                Capacitor.IsWorkingChanged -= CapacitorIsWorkingChanged;
            }
        }
    }
}
