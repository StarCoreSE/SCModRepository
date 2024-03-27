namespace DefenseShields
{
    using Support;
    using ProtoBuf;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using VRage.Game.Entity;
    using VRage.Utils;

    [ProtoInclude(3, typeof(DataControllerState))]
    [ProtoInclude(4, typeof(DataControllerSettings))]
    [ProtoInclude(5, typeof(DataModulatorState))]
    [ProtoInclude(6, typeof(DataModulatorSettings))]
    [ProtoInclude(7, typeof(DataPlanetShieldState))]
    [ProtoInclude(8, typeof(DataPlanetShieldSettings))]
    [ProtoInclude(9, typeof(DataO2GeneratorState))]
    [ProtoInclude(10, typeof(DataO2GeneratorSettings))]
    [ProtoInclude(11, typeof(DataEnhancerState))]
    [ProtoInclude(12, typeof(DataEnhancerSettings))]
    [ProtoInclude(13, typeof(DataEmitterState))]
    [ProtoInclude(14, typeof(DataShieldHit))]
    [ProtoInclude(15, typeof(DataEnforce))]

    [ProtoContract]
    public abstract class PacketBase
    {
        [ProtoMember(1)] public ulong SenderId;

        [ProtoMember(2)] public long EntityId;

        private MyEntity _ent;

        internal MyEntity Entity
        {
            get
            {
                if (EntityId == 0) return null;

                if (_ent == null) _ent = MyEntities.GetEntityById(EntityId, true);

                if (_ent == null || _ent.MarkedForClose) return null;
                return _ent;
            }
        }

        public PacketBase(long entityId = 0)
        {
            SenderId = MyAPIGateway.Multiplayer.MyId;
            EntityId = entityId;
        }

        /// <summary>
        /// Called when this packet is received on this machine
        /// </summary>
        /// <param name="rawData">the bytes from the packet, useful for relaying or other stuff without needing to re-serialize the packet</param>
        public abstract bool Received(bool isServer);
    }

    [ProtoContract]
    public class DataControllerState : PacketBase
    {
        public DataControllerState()
        {
        } // Empty constructor required for deserialization

        [ProtoMember(1)] public ControllerStateValues State = null;

        public DataControllerState(long entityId, ControllerStateValues state) : base(entityId)
        {
            State = state;
        }

        public override bool Received(bool isServer)
        {
            if (!isServer)
            {
                if (Entity?.GameLogic == null) return false;
                var logic = Entity.GameLogic.GetAs<DefenseShields>();
                logic?.UpdateState(State);
                return false;
            }
            return true;
        }
    }

    [ProtoContract]
    public class DataControllerSettings : PacketBase
    {
        public DataControllerSettings()
        {
        } // Empty constructor required for deserialization

        [ProtoMember(1)] public ControllerSettingsValues Settings = null;

        public DataControllerSettings(long entityId, ControllerSettingsValues settings) : base(entityId)
        {
            Settings = settings;
        }

        public override bool Received(bool isServer)
        {
            if (Entity?.GameLogic == null) return false;
            var logic = Entity.GameLogic.GetAs<DefenseShields>();
            logic?.UpdateSettings(Settings);
            return isServer;
        }
    }

    [ProtoContract]
    public class DataModulatorState : PacketBase
    {
        public DataModulatorState()
        {
        } // Empty constructor required for deserialization

        [ProtoMember(1)] public ModulatorStateValues State = null;

        public DataModulatorState(long entityId, ModulatorStateValues state) : base(entityId)
        {
            State = state;
        }

        public override bool Received(bool isServer)
        {
            if (!isServer)
            {
                if (Entity?.GameLogic == null) return false;
                var logic = Entity.GameLogic.GetAs<Modulators>();
                logic?.UpdateState(State);
                return false;
            }
            return true;
        }
    }

    [ProtoContract]
    public class DataModulatorSettings : PacketBase
    {
        public DataModulatorSettings()
        {
        } // Empty constructor required for deserialization

        [ProtoMember(1)] public ModulatorSettingsValues Settings = null;

        public DataModulatorSettings(long entityId, ModulatorSettingsValues settings) : base(entityId)
        {
            Settings = settings;
        }

        public override bool Received(bool isServer)
        {
            if (Entity?.GameLogic == null) return false;
            var logic = Entity.GameLogic.GetAs<Modulators>();
            logic?.UpdateSettings(Settings);
            return isServer;
        }
    }

    [ProtoContract]
    public class DataPlanetShieldState : PacketBase
    {
        public DataPlanetShieldState()
        {
        } // Empty constructor required for deserialization

        [ProtoMember(1)] public PlanetShieldStateValues State = null;

        public DataPlanetShieldState(long entityId, PlanetShieldStateValues state) : base(entityId)
        {
            State = state;
        }

        public override bool Received(bool isServer)
        {
            if (!isServer)
            {
                if (Entity?.GameLogic == null) return false;
                var logic = Entity.GameLogic.GetAs<PlanetShields>();
                //logic?.UpdateState(State);
                return false;
            }
            return true;
        }
    }

    [ProtoContract]
    public class DataPlanetShieldSettings : PacketBase
    {
        public DataPlanetShieldSettings()
        {
        } // Empty constructor required for deserialization

        [ProtoMember(1)] public PlanetShieldSettingsValues Settings = null;

        public DataPlanetShieldSettings(long entityId, PlanetShieldSettingsValues settings) : base(entityId)
        {
            Settings = settings;
        }

        public override bool Received(bool isServer)
        {
            if (Entity?.GameLogic == null) return false;
            var logic = Entity.GameLogic.GetAs<PlanetShields>();
            //logic?.UpdateSettings(Settings);
            return isServer;
        }
    }

    [ProtoContract]
    public class DataO2GeneratorState : PacketBase
    {
        public DataO2GeneratorState()
        {
        } // Empty constructor required for deserialization

        [ProtoMember(1)] public O2GeneratorStateValues State = null;

        public DataO2GeneratorState(long entityId, O2GeneratorStateValues state) : base(entityId)
        {
            State = state;
        }

        public override bool Received(bool isServer)
        {
            if (!isServer)
            {
                if (Entity?.GameLogic == null) return false;
                var logic = Entity.GameLogic.GetAs<O2Generators>();
                logic?.UpdateState(State);
                return false;
            }
            return true;
        }
    }

    [ProtoContract]
    public class DataO2GeneratorSettings : PacketBase
    {
        public DataO2GeneratorSettings()
        {
        } // Empty constructor required for deserialization

        [ProtoMember(1)] public O2GeneratorSettingsValues Settings = null;

        public DataO2GeneratorSettings(long entityId, O2GeneratorSettingsValues settings) : base(entityId)
        {
            Settings = settings;
        }

        public override bool Received(bool isServer)
        {
            if (Entity?.GameLogic == null) return false;
            var logic = Entity.GameLogic.GetAs<O2Generators>();
            logic?.UpdateSettings(Settings);
            return isServer;
        }
    }

    [ProtoContract]
    public class DataEnhancerState : PacketBase
    {
        public DataEnhancerState()
        {
        } // Empty constructor required for deserialization

        [ProtoMember(1)] public EnhancerStateValues State = null;

        public DataEnhancerState(long entityId, EnhancerStateValues state) : base(entityId)
        {
            State = state;
        }

        public override bool Received(bool isServer)
        {
            if (!isServer)
            {
                if (Entity?.GameLogic == null) return false;
                var logic = Entity.GameLogic.GetAs<Enhancers>();
                logic?.UpdateState(State);
                return false;
            }
            return true;
        }
    }

    [ProtoContract]
    public class DataEnhancerSettings : PacketBase
    {
        public DataEnhancerSettings()
        {
        } // Empty constructor required for deserialization

        [ProtoMember(1)] public EnhancerSettingsValues Settings = null;

        public DataEnhancerSettings(long entityId, EnhancerSettingsValues settings) : base(entityId)
        {
            Settings = settings;
        }

        public override bool Received(bool isServer)
        {
            if (Entity?.GameLogic == null) return false;
            var logic = Entity.GameLogic.GetAs<Enhancers>();
            //logic?.UpdateSettings(Settings);
            return isServer;
        }
    }

    [ProtoContract]
    public class DataEmitterState : PacketBase
    {
        public DataEmitterState()
        {
        } // Empty constructor required for deserialization

        [ProtoMember(1)] public EmitterStateValues State = null;

        public DataEmitterState(long entityId, EmitterStateValues state) : base(entityId)
        {
            State = state;
        }

        public override bool Received(bool isServer)
        {
            if (!isServer)
            {
                if (Entity?.GameLogic == null) return false;
                var logic = Entity.GameLogic.GetAs<Emitters>();
                logic?.UpdateState(State);
                return false;
            }
            return true;
        }
    }


    [ProtoContract]
    public class DataShieldHit : PacketBase
    {
        public DataShieldHit()
        {
        } // Empty constructor required for deserialization

        [ProtoMember(1)] public ShieldHitValues State = null;

        public DataShieldHit(long entityId, ShieldHitValues state) : base(entityId)
        {
            State = state;
        }

        public override bool Received(bool isServer)
        {
            if (isServer || Entity?.GameLogic == null) return false;
            var shield = Entity.GameLogic.GetAs<DefenseShields>();
            if (shield == null) return false;

            var attacker = MyEntities.GetEntityById(State.AttackerId);
            shield.ShieldHits.Add(new ShieldHit(attacker, State.Amount, MyStringHash.GetOrCompute(State.DamageType), State.HitPos));
            return false;
        }
    }

    [ProtoContract]
    public class DataEnforce : PacketBase
    {
        public DataEnforce()
        {
        } // Empty constructor required for deserialization

        [ProtoMember(1)] public DefenseShieldsEnforcement State = null;

        public DataEnforce(long entityId, DefenseShieldsEnforcement state) : base(entityId)
        {
            State = state;
        }

        public override bool Received(bool isServer)
        {
            if (!isServer)
            {
                Session.Enforced = State;
                Session.EnforceInit = true;
                if (State.Debug >= 2) Log.Line($"Saving Enforcement version: {State.Version}");
                return false;
            }
            if (State.Debug >= 2) Log.Line($"Sending Enforcement version: {Session.Enforced.Version}");
            var data = new DataEnforce(0, Session.Enforced);
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(data);
            MyAPIGateway.Multiplayer.SendMessageTo(Session.PACKET_ID, bytes, State.SenderId);
            return false;
        }
    }
}
