using System;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
 
namespace Digi.Examples
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeMissileTurret), false, "2cmFlakvierling", "20mmOerlikonSingle", "20mmOerlikonDouble", "13mmMLE1929", "13mmCAQMLE1929", "2cmFlak38")]
    public class MissileTurret : MyGameLogicComponent
    {
        private IMyFunctionalBlock block;
        private IMyGunObject<MyGunBase> gun;
        private bool firstFunctional = true;
        private long lastShotTime;
        private MySoundPair soundPair;
        private MyEntity3DSoundEmitter soundEmitter;
 
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }
 
        public override void UpdateOnceBeforeFrame()
        {
            try
            {
                block = (IMyFunctionalBlock)Entity;
 
                if(block?.CubeGrid?.Physics == null)
                    return;
 
                gun = (IMyGunObject<MyGunBase>)Entity;
 
                if(!gun.GunBase.HasProjectileAmmoDefined)
                    return; // ignore non-projectile missile turrets
 
                var def = (MyWeaponBlockDefinition)block.SlimBlock.BlockDefinition;
                var weaponDef = MyDefinitionManager.Static.GetWeaponDefinition(def.WeaponDefinitionId);
 
                soundPair = weaponDef.WeaponAmmoDatas[0].ShootSound;
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }
            catch(Exception e)
            {
                Error(e);
            }
        }
 
        public override void Close()
        {
            soundEmitter?.StopSound(true);
            soundEmitter = null;
        }
 
        public override void UpdateBeforeSimulation()
        {
            try
            {
                if(!block.IsFunctional)
                    return;
 
                if(firstFunctional)
                {
                    firstFunctional = false;
                    lastShotTime = gun.GunBase.LastShootTime.Ticks;
                    soundEmitter = new MyEntity3DSoundEmitter((MyEntity)Entity);
                }
 
                var shotTime = gun.GunBase.LastShootTime.Ticks;
 
                if(shotTime > lastShotTime)
                {
                    lastShotTime = shotTime;
 
                    if(gun.GunBase.IsAmmoProjectile)
                    {
                        soundEmitter.PlaySound(soundPair);
                        //MyAPIGateway.Utilities.ShowNotification("[DEBUG] shot projectile", 500);
                    }
                    else
                    {
                        //MyAPIGateway.Utilities.ShowNotification("[DEBUG] shot missile", 500);
                    }
                }
            }
            catch(Exception e)
            {
                Error(e);
            }
        }
 
        private void Error(Exception e)
        {
            MyLog.Default.WriteLine(e);
            MyAPIGateway.Utilities.ShowNotification($"[ Error in {GetType().FullName}: {e.Message} ]", 10000, MyFontEnum.Red);
        }
    }
}