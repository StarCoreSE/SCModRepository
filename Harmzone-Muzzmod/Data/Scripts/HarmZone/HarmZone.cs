using System;
using System.Collections.Generic;
using System.Linq;
using ObjectBuilders.SafeZone;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Interfaces;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.Entities.Blocks.SafeZone;
using SpaceEngineers.Game.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;


namespace Klime.HarmZone
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "HarmBeacon")]
    public class HarmZone : MyGameLogicComponent
    {
        private IMyBeacon beacon_block;
        private List
        <MyCubeGrid>
        all_grids = new List
        <MyCubeGrid>
        ();
        private MyCubeGrid reuse_grid;
        private ListReader
        <MyCubeBlock>
        reuse_blocks = new ListReader
        <MyCubeBlock>
        ();
        private int timer = 0;
        private List
        <string>
        words = new List
        <string>
        ();
        private Vector3D cam_pos = Vector3D.Zero;
        private Vector3 col = new Vector3(0, 255, 0);
        private MatrixD worldmat = MatrixD.Zero;
        private Color sphere_col = new Color(Color.Green, 0.1f);
        private MyStringId shield_mat;
        List
        <IMyPlayer>
        allplayers = new List
        <IMyPlayer>
        ();
        private bool sphere_visuals = false;
        private ushort netId = 42349;
        private bool is_shrinking = false;
        private float shrink_rate_per_tick = 0f;
		private int harmdist = 7500;



        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            beacon_block = Entity as IMyBeacon;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        [ProtoContract]
        public class ShrinkPacket
        {
            [ProtoMember(30)]
            public long beaconEntityId;

            [ProtoMember(31)]
            public float serverRadius;

            public ShrinkPacket() { }

            public ShrinkPacket(long beaconEntityId, float serverRadius)
            {
                this.beaconEntityId = beaconEntityId;
                this.serverRadius = serverRadius;
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (beacon_block.CubeGrid.Physics != null)
            {
                MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;
                MyAPIGateway.Utilities.MessageEntered += Utilities_DepreciatedMessage;
                shield_mat = MyStringId.GetOrCompute("SafeZoneShield_Material");

                if (MyAPIGateway.Session.IsServer)
                {
                    IMyCubeGrid grid = beacon_block.CubeGrid;
                    grid.Name = grid.EntityId.ToString();
                    MyEntities.SetEntityName((MyEntity)grid);
                }

                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(netId, shrinkHandler);
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }
        }

        private void shrinkHandler(ushort arg1, byte[] arg2, ulong arg3, bool arg4)
        {
            if (!MyAPIGateway.Session.IsServer && beacon_block != null && !beacon_block.MarkedForClose)
            {
                ShrinkPacket sp = MyAPIGateway.Utilities.SerializeFromBinary<ShrinkPacket>(arg2);

                if (sp != null && sp.beaconEntityId == beacon_block.EntityId)
                {
                    beacon_block.Radius = sp.serverRadius;
                }
            }
        }

        private void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
        {

            if (messageText.Contains("/harmdist"))
            {	
	        	try
                {
				string[] tempdist = messageText.Split(' ');
				MyAPIGateway.Utilities.ShowNotification("Harmsphere visuals changed to " + tempdist[1].ToString() + "m from center.");
				harmdist = int.Parse(tempdist[1]);
                //sphere_visuals = !sphere_visuals;
                sendToOthers = false;
				}
				catch(Exception)
				{}
            }
        }

        private void Utilities_DepreciatedMessage(string messageText, ref bool sendToOthers)
        {
            if (messageText.Contains("/harmsphere"))
            {

                MyAPIGateway.Utilities.ShowNotification("This command is depreciated. Use /harmdist DISTANCE (example: /harmdist 1000)", 10000);
                sendToOthers = false;

            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (beacon_block.Enabled)
                {
                    if (MyAPIGateway.Session != null && MyAPIGateway.Session.IsServer)
                    {
                        if (is_shrinking && beacon_block.Radius > 200000)
                        {
                            beacon_block.Radius -= shrink_rate_per_tick;
                        }

                        if (timer % 60 == 0)
                        {
                            int dmg = 100;
                            float dmg_radius = 2f;

                            if (beacon_block.CustomData != null && beacon_block.CustomData == "")
                            {
                                beacon_block.CustomData = "100\n" + "2\n" + "false\n" + "10";
                            }

                            words.Clear();
                            words = beacon_block.CustomData.Split('\n').ToList();

                            if (words != null && words.Count == 4)
                            {
                                int fake_dmg = 0;
                                float fake_dmg_radius = 0f;
                                bool fake_shrink = false;
                                float fake_shrink_rate = 0f;

                                if (int.TryParse(words[0], out fake_dmg))
                                {
                                    if (fake_dmg >= 0)
                                    {
                                        dmg = fake_dmg;
                                    }
                                }

                                if (float.TryParse(words[1], out fake_dmg_radius))
                                {
                                    if (fake_dmg_radius >= 0)
                                    {
                                        dmg_radius = fake_dmg_radius;
                                    }
                                }

                                if (bool.TryParse(words[2], out fake_shrink))
                                {
                                    is_shrinking = fake_shrink;
                                }

                                if (float.TryParse(words[3], out fake_shrink_rate))
                                {
                                    if (fake_shrink_rate >= 0)
                                    {
                                        shrink_rate_per_tick = fake_shrink_rate / 60f;
                                    }
                                }
                            }
                            else if (words != null && string.IsNullOrEmpty(beacon_block.CustomData))
                            {
                                beacon_block.CustomData = "100\n" + "2";
                            }

                            all_grids.Clear();
                            BoundingSphereD sph = new BoundingSphereD(beacon_block.WorldMatrix.Translation, 200000);

                            foreach (var ent in MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sph))
                            {
                                if (ent is MyCubeGrid)
                                {
                                    reuse_grid = ent as MyCubeGrid;

                                    if (reuse_grid.Physics != null && !reuse_grid.IsStatic)
                                    {
                                        all_grids.Add(reuse_grid);
                                    }
                                }
                            }

                            // Define constants for radius and damage scaling
                            const float base_radius = 5f;  // The starting radius for the explosion
                            const float max_radius = 5f;  // The maximum radius for the explosion
                            const float base_dmg = 100f; // The starting damage for the explosion
                            const float max_dmg = 100f; // The maximum damage for the explosion
                            const float max_speed = 1; // The maximum speed for grid in m/s

                            DateTime explosion_start_time = DateTime.MinValue; // The start time of the current explosion
                            float radius = base_radius; // Initialize the starting radius
                            float dmg2 = base_dmg; // Initialize the starting damage

                            MyAPIGateway.Parallel.For(0, all_grids.Count, i =>
                            {
                                var grid = all_grids[i];
                                if (!grid.MarkedForClose)
                                {
                                    var dist = Vector3.Distance(grid.WorldMatrix.Translation, beacon_block.WorldMatrix.Translation);

                                    if (dist > beacon_block.Radius)
                                    {
                                        List<MyCubeBlock> blocks_to_damage = new List<MyCubeBlock>(grid.GetFatBlocks());
                                        blocks_to_damage.RemoveAll(block => block == null
                                                                            || block is IMyCockpit
                                                                            || block is IMyMotorAdvancedRotor
                                                                            || block is IMyMotorAdvancedStator
                                                                            || block is IMyMotorRotor
                                                                            || block is IMyMotorStator
                                                                            || block is IMyThrust
                                                                            || block is Sandbox.ModAPI.IMyGasTank
                                                                            || !(block is IMyFunctionalBlock));
                                        if (blocks_to_damage.Count > 0)
                                        {
                                            // Scale the radius and damage based on the time since last explosion
                                            float elapsed_time = (float)(DateTime.Now - explosion_start_time).TotalSeconds;
                                            if (elapsed_time < 5)
                                            {
                                                float radius_scale = elapsed_time / 5f;
                                                radius = base_radius + ((max_radius - base_radius) * radius_scale);
                                                dmg2 = base_dmg + ((max_dmg - base_dmg) * radius_scale);
                                            }
                                            else
                                            {
                                                radius = base_radius;
                                                dmg = (int)base_dmg;
                                                explosion_start_time = DateTime.Now;
                                            }

                                            // Scale the damage based on the speed of the grid
                                            //float speed_scale = grid.Physics.LinearVelocity.Length() / max_speed;
                                            //dmg2 *= MathHelper.Clamp(speed_scale, 1f, 5f);

                                            var block_to_damage = blocks_to_damage[MyUtils.GetRandomInt(0, blocks_to_damage.Count - 1)];
                                            MyVisualScriptLogicProvider.CreateExplosion(block_to_damage.WorldMatrix.Translation, radius, dmg);
                                            //MyAPIGateway.Utilities.ShowNotification("Last explosion: " + block_to_damage.DisplayNameText + " | Explosion scaled by speed: " + Math.Round(speed_scale, 2));
                                        }
                                    }
                                }
                            });
/*
                            bool hurtplayers = false;

                            if (hurtplayers) { 
                            allplayers.Clear();

                            MyAPIGateway.Multiplayer.Players.GetPlayers(allplayers);
                            ShrinkPacket sp = new ShrinkPacket(beacon_block.EntityId, beacon_block.Radius);

                            foreach (var player in allplayers)
                            {
                                if (player.Character != null && !player.Character.IsDead)
                                {
                                    double dist = Vector3D.Distance(beacon_block.WorldMatrix.Translation, player.Character.WorldMatrix.Translation);

                                    if (dist >= beacon_block.Radius)
                                    {
                                        player.Character.DoDamage(20, MyStringHash.GetOrCompute("Fire"), true);
                                    }
                                }

                                MyAPIGateway.Multiplayer.SendMessageTo(netId, MyAPIGateway.Utilities.SerializeToBinary(sp), player.SteamUserId);
                            }
                            }
*/
                        }

                        timer += 1;
                    }

                    if (!MyAPIGateway.Utilities.IsDedicated) 
{ 
    if (MyAPIGateway.Session.Player != null && MyAPIGateway.Session.Camera != null) 
    { 
        var camMat = MyAPIGateway.Session.Camera.WorldMatrix; 
        var distance = camMat.Translation.Length(); // Distance from 0,0,0
        
        // If the camera is within 5000 to 12500 meters of the origin, set sphere_visuals to true
        bool sphere_visuals = distance >= harmdist && distance <= 12500;

        if (sphere_visuals) 
        { 
            worldmat = beacon_block.WorldMatrix; 
            MySimpleObjectDraw.DrawTransparentSphere( ref worldmat, beacon_block.Radius, ref sphere_col, MySimpleObjectRasterizer.Solid, 35, shield_mat, null, -1, -1, null, BlendTypeEnum.PostPP, 1); 
        } 
    } 
}
                }
            }
            catch (System.Exception e)
            {
                MyLog.Default.WriteLine("KLIME HARMZONE: " + e);
            }
        }
        public override void Close()
        {
            MyAPIGateway.Utilities.MessageEntered -= Utilities_MessageEntered;
            MyAPIGateway.Utilities.MessageEntered -= Utilities_DepreciatedMessage;
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(netId, shrinkHandler);
        }
    }
}