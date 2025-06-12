using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage;
using VRageMath;

namespace StarCore.ShareTrack.TrackerApi
{
    internal class ApiProvider
    {
        private const long Channel = 3033234540;
        private readonly IReadOnlyDictionary<string, Delegate> _apiDefinitions;
        private readonly MyTuple<Vector2I, IReadOnlyDictionary<string, Delegate>> _endpointTuple;

        /// <summary>
        ///     Registers for API requests and updates any pre-existing clients.
        /// </summary>
        public ApiProvider()
        {
            _apiDefinitions = new ApiMethods().ModApiMethods;
            _endpointTuple =
                new MyTuple<Vector2I, IReadOnlyDictionary<string, Delegate>>(MasterSession.ModVersion,
                    _apiDefinitions);

            MyAPIGateway.Utilities.RegisterMessageHandler(Channel, HandleMessage);

            IsReady = true;
            try
            {
                MyAPIGateway.Utilities.SendModMessage(Channel, _endpointTuple);
            }
            catch (Exception ex)
            {
                Log.Info($"Exception in Api Load: {ex}");
            }

            Log.Info($"ShareTrackAPI v{MasterSession.ModVersion.Y} initialized.");
        }

        /// <summary>
        ///     Is the API ready?
        /// </summary>
        public bool IsReady { get; private set; }

        private void HandleMessage(object o)
        {
            if (o as string == "ApiEndpointRequest")
            {
                MyAPIGateway.Utilities.SendModMessage(Channel, _endpointTuple);
                Log.Info("ShareTrackAPI sent definitions.");
            }
        }


        /// <summary>
        ///     Unloads all API endpoints and detaches events.
        /// </summary>
        public void Unload()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(Channel, HandleMessage);

            IsReady = false;
            // Clear API client's endpoints
            MyAPIGateway.Utilities.SendModMessage(Channel,
                new MyTuple<Vector2I, IReadOnlyDictionary<string, Delegate>>(MasterSession.ModVersion, null));

            Log.Info("ShareTrackAPI unloaded.");
        }
    }
}
