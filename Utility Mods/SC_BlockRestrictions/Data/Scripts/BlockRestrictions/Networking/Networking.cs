using System;
using System.Text;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace SC_BlockRestrictions
{
  public class Networking
  {
    public readonly ushort NetworkId;
    public readonly long PublishedId;
    List<IMyPlayer> _tempPlayers;
    StringBuilder _debugSB;
    SC_BlockRestrictions _mod;

    public Networking(long publishedId, ushort networkId, SC_BlockRestrictions mod, StringBuilder sb)
    {
      PublishedId = publishedId;
      NetworkId = networkId;
      _tempPlayers = new List<IMyPlayer>(MyAPIGateway.Session.SessionSettings.MaxPlayers);
      _debugSB = sb;
      _mod = mod;
    }

    public void Register()
    {
      MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetworkId, ReceivedPacket);
      MyAPIGateway.Utilities.RegisterMessageHandler(PublishedId, ReceivedModPacket);
    }

    public void Unregister()
    {
      MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetworkId, ReceivedPacket);
      MyAPIGateway.Utilities.UnregisterMessageHandler(PublishedId, ReceivedModPacket);
    }

    private void ReceivedModPacket(object obj)
    {
      var modPacket = obj as ModPacket;
      if (modPacket?.SenderId != 2307665159)
        return;
    }

    private void ReceivedPacket(ushort handlerId, byte[] rawData, ulong senderId, bool fromServer)
    {
      try
      {
        var packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(rawData);
        if (packet == null)
          return;

        if (!string.IsNullOrEmpty(packet.Message))
        {
          _mod.ShowMessage(packet.Message);
        }
        else if (packet.AdminUpdate && _mod.IsServer)
        {
          _mod.ModSaveData.CreativeModeAllowed = packet.CreativeModeAllowed;
          _mod.SaveConfig();

          packet.BlockSettings = _mod.ModSaveData.Settings;
          RelayToClients(packet);

          if (!packet.CreativeModeAllowed)
          {
            foreach (var blockDict in _mod.PlayerBlockDict.Values)
              blockDict.Clear();

            foreach (var ent in _mod.EntityDict.Values)
            {
              if (ent == null || ent.MarkedForClose)
                continue;

              ent.FactionUpdateCheck = true;
              ent.NeedsOwnerUpdate = true;
              ent.NeedsBlockOwnerUpdate = true;
              ent.NeedsCreativeRecalc = true;
            }
          }
        }
        else if (packet.BlockSettings != null)
        {
          _mod.ReceiveSettings(packet);
        }
        else if (_mod.IsServer)
        {
          bool playerCopyPaste, playerCreative;
          _mod.PlayerCreativeDict.TryGetValue(packet.SenderId, out playerCreative);
          _mod.PlayerCopyPasteDict.TryGetValue(packet.SenderId, out playerCopyPaste);
          _mod.PlayerCreativeDict[packet.SenderId] = packet.CreativeEnabled;
          _mod.PlayerCopyPasteDict[packet.SenderId] = packet.CopyPasteEnabled;

          if (playerCreative != packet.CreativeEnabled || playerCopyPaste != packet.CopyPasteEnabled)
          {
            var playerId = MyAPIGateway.Players.TryGetIdentityId(packet.SenderId);
            if (playerId > 0)
            {
              Dictionary<MyDefinitionId, int> playerBlocks;
              if (_mod.PlayerBlockDict.TryGetValue(playerId, out playerBlocks))
                playerBlocks.Clear();

              _mod.SetUpdateNeeded(playerId, true);
            }

            _mod.AddOrUpdatePlayerData(packet.SenderId);
          }

          if (!packet.CreativeUpdateOnly)
            SendToPlayer(packet.SenderId, new Packet(_mod.ModSaveData));
        }
      }
      catch (Exception e)
      {
        _mod?.Logger?.Log($"Error in ReceivedPacket:\n{e.Message}\n{e.StackTrace}", MessageType.ERROR);

        if (MyAPIGateway.Session?.LocalHumanPlayer != null)
          MyAPIGateway.Utilities.ShowNotification($"[SC_BlockRestrictions] ERROR: {GetType().FullName} -- {e.Message} | Send SC_BlockRestrictions.log to mod author", 10000, MyFontEnum.Red);
      }
    }

    public void SendToMod(long modId, ModPacket packet)
    {
      var xml = MyAPIGateway.Utilities.SerializeToXML(packet);
      MyAPIGateway.Utilities.SendModMessage(modId, xml);
    }

    public void SendToServer(Packet packet)
    {
      if (_mod.IsServer)
        return;

      var data = MyAPIGateway.Utilities.SerializeToBinary(packet);
      MyAPIGateway.Multiplayer.SendMessageToServer(NetworkId, data);
    }

    public void SendToPlayer(long ownerId, string text)
    {
      if (!_mod.IsServer)
        return;

      var steamUserId = MyAPIGateway.Players.TryGetSteamId(ownerId);
      if (steamUserId == 0)
        return;

      var data = MyAPIGateway.Utilities.SerializeToBinary(new Packet(text));
      MyAPIGateway.Multiplayer.SendMessageTo(NetworkId, data, steamUserId);
    }

    public void SendToPlayer(ulong playerSteamId, Packet packet, byte[] rawData = null)
    {
      if (!_mod.IsServer)
        return;

      if (rawData == null)
        rawData = MyAPIGateway.Utilities.SerializeToBinary(packet);

      MyAPIGateway.Multiplayer.SendMessageTo(NetworkId, rawData, playerSteamId);
    }

    public void RelayToClients(Packet packet, byte[] rawData = null)
    {
      if (!_mod.IsServer)
        return;

      if (rawData == null)
        rawData = MyAPIGateway.Utilities.SerializeToBinary(packet);

      _tempPlayers.Clear();
      MyAPIGateway.Players.GetPlayers(_tempPlayers);

      foreach (var p in _tempPlayers)
      {
        if (p.IsBot || p.SteamUserId == MyAPIGateway.Multiplayer.ServerId || p.SteamUserId == packet.SenderId)
          continue;

        MyAPIGateway.Multiplayer.SendMessageTo(NetworkId, rawData, p.SteamUserId);
      }

      _tempPlayers.Clear();
    }
  }
}
