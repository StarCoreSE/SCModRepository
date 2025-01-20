
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using IMyCubeBlock = VRage.Game.ModAPI.IMyCubeBlock;
using IMySlimBlock = VRage.Game.ModAPI.IMySlimBlock;

namespace KillFeed
{
    class Utilities
    {
        public static bool IsCockpit(object target)
        {
            if (!(target is IMySlimBlock)) { return false; }
            if (!(((IMySlimBlock)target).FatBlock is IMyCockpit)) { return false; }
            return true;
        }

        public static IMyCockpit GetCockpit(object target)
        {
            if (!IsCockpit(target)) { return null; }
            return (IMyCockpit)((IMySlimBlock)target).FatBlock;
        }

        public static IMyIdentity CharacterToIdentity(IMyCharacter character)
        {
            if (character == null) { return null; }
            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p.Character == character);
            var player = players.FirstOrDefault();
            if (player == null) { return null; }
            return player.Identity;
        }

        public static IMyIdentity CubeBlockOwnerToIdentity(IMyCubeBlock block)
        {
            if (block == null) { return null; }
            var identities = new List<IMyIdentity>();
            MyAPIGateway.Players.GetAllIdentites(identities, i => i.IdentityId == block.OwnerId);
            return identities.FirstOrDefault();
        }

        public static IMyIdentity CubeBlockBuiltByToIdentity(long builtBy)
        {
            var identities = new List<IMyIdentity>();
            MyAPIGateway.Players.GetAllIdentites(identities, i => i.IdentityId == builtBy);
            return identities.FirstOrDefault();
        }

        public static IMyIdentity IdentityIdToIdentity(long identityId)
        {
            var identities = new List<IMyIdentity>();
            MyAPIGateway.Players.GetAllIdentites(identities, i => i.IdentityId == identityId);
            return identities.FirstOrDefault();
        }

        public static IMyIdentity GridToIdentity(IMyCubeGrid grid)
        {
            if (grid == null) { return null; }

            var gridOwnerId = grid.BigOwners.FirstOrDefault();
            var identities = new List<IMyIdentity>();
            MyAPIGateway.Players.GetAllIdentites(identities, i => i.IdentityId == gridOwnerId);
            var ownerIdentity = identities.FirstOrDefault();
            if (ownerIdentity != null) { return ownerIdentity; }

            // can't find owner, go by the first built by
            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks);
            var block = blocks.FirstOrDefault();
            if (block == null || !(block is MyCubeBlock)) { return null; }
            return CubeBlockBuiltByToIdentity(((MyCubeBlock)block).BuiltBy);
        }

        public static IMyIdentity EntityToIdentity(IMyEntity entity)
        {
            if (entity == null) { return null; }

            if (entity is IMyCharacter)
            {
                return CharacterToIdentity((IMyCharacter)entity);
            }
            else if (entity is IMyEngineerToolBase)
            {
                var tool = (IMyEngineerToolBase)entity;
                if (tool == null) { return null; }

                var toolOwner = MyAPIGateway.Entities.GetEntityById(tool.OwnerId);
                if (toolOwner == null) { return null; }

                var character = (IMyCharacter)toolOwner;
                if (character == null) { return null; }

                return CharacterToIdentity(character);
            }
            else if (entity is MyCubeBlock)
            {
                var block = (MyCubeBlock)entity;
                if (block == null) { return null; }
                return CubeBlockOwnerToIdentity(block);
            }
            else if (entity is IMyGunBaseUser)
            {
                var weapon = (IMyGunBaseUser)entity;
                if (weapon == null) { return null; }

                var weaponOwner = weapon.Owner;
                if (weaponOwner == null) { return null; }

                var character = (IMyCharacter)weaponOwner;
                if (character == null) { return null; }

                return CharacterToIdentity(character);
            }
            else if (entity is IMyCubeGrid)
            {
                return GridToIdentity((IMyCubeGrid)entity);
            }

            return null;
        }

        public static void SendMessageToAllPlayers(MessageData message)
        {
            byte[] byteData = MyAPIGateway.Utilities.SerializeToBinary(message);
            List<IMyPlayer> players = new List<IMyPlayer>();
            List<ulong> steamIds = new List<ulong>();
            MyAPIGateway.Players.GetPlayers(players, p => p != null && !p.IsBot);
            foreach (IMyPlayer player in players)
            {
                if (steamIds.Contains(player.SteamUserId)) { continue; }
                steamIds.Add(player.SteamUserId);
                MyAPIGateway.Multiplayer.SendMessageTo(Config.NetworkMessageId, byteData, player.SteamUserId);
            }
        }

        public static string TagPlayerName(IMyIdentity identity)
        {
            IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(identity.IdentityId);
            return (faction != null) ? (faction.Tag + "." + identity.DisplayName) : identity.DisplayName;
        }

        public static ulong IdentitySteamId(IMyIdentity identity)
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p != null && p.IdentityId == identity.IdentityId);
            return (players.Count > 0) ? (players[0].SteamUserId) : 0;
        }

        public static void Loggy(string title, ref MyDamageInformation info)
        {
            Logging.Instance.WriteLine("----------");
            Logging.Instance.WriteLine("- " + title + " -");
            Logging.Instance.WriteLine("----------");
            Logging.Instance.WriteLine("Amount: " + info.Amount);
            Logging.Instance.WriteLine("AttackerId: " + info.AttackerId);
            Logging.Instance.WriteLine("IsDeformation: " + info.IsDeformation);
            Logging.Instance.WriteLine("Type: " + info.Type);

            IMyEntity entity = MyAPIGateway.Entities.GetEntityById(info.AttackerId);
            if (entity == null)
            {
                Logging.Instance.WriteLine("Entity: null");
                return;
            }
            Logging.Instance.WriteLine("Entity: " + entity.ToString());

            if (entity is IMyCharacter)
            {
                var identity = Utilities.CharacterToIdentity((IMyCharacter)entity);
                Logging.Instance.WriteLine("  Identity: " + ((identity != null) ? identity.DisplayName : "null"));
            }
            else if (entity is IMyEngineerToolBase)
            {
                var tool = (IMyEngineerToolBase)entity;
                var toolOwner = MyAPIGateway.Entities.GetEntityById(tool.OwnerId);
                Logging.Instance.WriteLine("  Tool Owner: " + ((toolOwner != null) ? toolOwner.ToString() : "null"));
                var character = (IMyCharacter)toolOwner;
                Logging.Instance.WriteLine("  Tool Character: " + ((character != null) ? (character.EntityId + "") : "null"));

                var identity = CharacterToIdentity(character);
                Logging.Instance.WriteLine("  Identity: " + ((identity != null) ? identity.DisplayName : "null"));
            }
            else if (entity is MyCubeBlock)
            {
                var block = (MyCubeBlock)entity;
                var blockOwner = MyAPIGateway.Entities.GetEntityById(block.OwnerId);
                var blockBuiltBy = MyAPIGateway.Entities.GetEntityById(block.BuiltBy);
                Logging.Instance.WriteLine("  Block Owner: " + block.OwnerId + ", " + ((blockOwner != null) ? blockOwner.ToString() : "null"));
                Logging.Instance.WriteLine("  Block BuiltBy: " + block.BuiltBy + ", " + ((blockBuiltBy != null) ? blockBuiltBy.ToString() : "null"));

                var identity = CubeBlockOwnerToIdentity(block);
                Logging.Instance.WriteLine("  Identity: " + ((identity != null) ? identity.DisplayName : "null"));
            }
            else if (entity is IMyGunBaseUser)
            {
                var weapon = (IMyGunBaseUser)entity;
                var weaponOwner = weapon.Owner;
                Logging.Instance.WriteLine("  Weapon Owner: " + ((weaponOwner != null) ? weaponOwner.ToString() : "null"));
                var character = (IMyCharacter)weaponOwner;
                Logging.Instance.WriteLine("  Weapon Character: " + ((character != null) ? (character.EntityId + "") : "null"));

                var identity = CharacterToIdentity(character);
                Logging.Instance.WriteLine("  Identity: " + ((identity != null) ? identity.DisplayName : "null"));
            }
            else if (entity is IMyCubeGrid)
            {
                var grid = (IMyCubeGrid)entity;
                var identity = GridToIdentity(grid);
                Logging.Instance.WriteLine("  Identity: " + ((identity != null) ? identity.DisplayName : "null"));
            }
        }

        public static void Debug(string message, int length = 1000)
        {
            MyVisualScriptLogicProvider.ShowNotificationToAll(message, length);
        }
    }
}
