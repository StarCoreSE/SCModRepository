using DefenseShields.Support;
using VRage;

namespace DefenseShields
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Sandbox.ModAPI;

    public class ApiServer
    {
        private const long Channel = 1365616918;

        /// <summary>
        /// Is the API ready to be serve
        /// </summary>
        public static bool IsReady { get; private set; }

        private static void HandleMessage(object o)
        {
            if ((o as string) == "ApiEndpointRequest")
                MyAPIGateway.Utilities.SendModMessage(Channel, Session.Instance.Api.ModApiMethods);
            else if (o is ImmutableDictionary<string, Delegate>)
                Protection((ImmutableDictionary<string, Delegate>) o);
        }

        private static void Protection(ImmutableDictionary<string, Delegate> validate)
        {
            if (validate != null && Session.Instance.Api.DetectedTampering(validate))
                MyAPIGateway.Utilities.SendModMessage(Channel, "Compromised");
        }

        private static bool _isRegistered;

        /// <summary>
        /// Prepares the client to receive API endpoints and requests an update.
        /// </summary>
        public static void Load()
        {
            if (!_isRegistered)
            {
                _isRegistered = true;
                MyAPIGateway.Utilities.RegisterMessageHandler(Channel, HandleMessage);
            }
            IsReady = true;
            MyAPIGateway.Utilities.SendModMessage(Channel, Session.Instance.Api.ModApiMethods);
        }


        /// <summary>
        /// Unloads all API endpoints and detaches events.
        /// </summary>
        public static void Unload()
        {
            if (_isRegistered)
            {
                _isRegistered = false;
                MyAPIGateway.Utilities.UnregisterMessageHandler(Channel, HandleMessage);
            }
            IsReady = false;
            MyAPIGateway.Utilities.SendModMessage(Channel, new Dictionary<string, Delegate>());
        }
    }
}
