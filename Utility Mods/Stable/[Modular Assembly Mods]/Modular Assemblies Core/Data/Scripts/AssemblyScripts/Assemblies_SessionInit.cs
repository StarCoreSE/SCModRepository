using Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Utils;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Assemblies_SessionInit : MySessionComponentBase
    {
        public static Assemblies_SessionInit I;
        AssemblyPartManager AssemblyPartManager = new AssemblyPartManager();
        DefinitionHandler DefinitionHandler = new DefinitionHandler();
        public bool DebugMode = false;

        #region Base Methods

        public override void LoadData()
        {
            I = this;

            AssemblyPartManager.Init();
            DefinitionHandler.Init();

            if (!MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                MyAPIGateway.Utilities.ShowMessage("Modular Assemblies", $"Run !mwHelp for commands. | {DefinitionHandler.I.ModularDefinitions.Count} definitions loaded.");
                MyAPIGateway.Utilities.MessageEnteredSender += ChatCommandHandler;
            }
            else
                MyAPIGateway.Utilities.ShowMessage("Modular Assemblies", $"Commands disabled, load into a singleplayer world for testing. | {DefinitionHandler.I.ModularDefinitions.Count} definitions loaded.");
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                AssemblyPartManager.UpdateAfterSimulation();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("Handled exception in Modular Assemblies!\n" + e.ToString());
            }
        }

        protected override void UnloadData()
        {
            // None of this should run on client.

            MyLog.Default.WriteLineAndConsole("Modular Assemblies: AssemblyPartManager closing...");

            if (!MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                MyAPIGateway.Utilities.MessageEnteredSender -= ChatCommandHandler;
            }

            AssemblyPartManager.Unload();
            DefinitionHandler.Unload();

            I = null;
            MyLog.Default.WriteLineAndConsole("Modular Assemblies: Finished unloading.");
        }

        #endregion

        private void ChatCommandHandler(ulong sender, string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith("!"))
                return;

            string[] split = messageText.Split(' ');
            switch (split[0].ToLower())
            {
                case "!mwhelp":
                    MyAPIGateway.Utilities.ShowMessage("Modular Assemblies", "Commands:\n!mwHelp - Prints all commands\n!mwDebug - Toggles debug draw");
                    sendToOthers = false;
                    break;
                case "!mwdebug":
                    DebugMode = !DebugMode;
                    sendToOthers = false;
                    break;
            }
        }
    }
}
