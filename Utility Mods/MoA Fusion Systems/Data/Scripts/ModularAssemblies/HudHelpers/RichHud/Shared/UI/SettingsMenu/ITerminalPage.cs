﻿using VRage;
using ApiMemberAccessor = System.Func<object, int, object>;

namespace RichHudFramework
{
    using ControlMembers = MyTuple<
        ApiMemberAccessor, // GetOrSetMember
        object // ID
    >;

    namespace UI
    {
        public enum TerminalPageAccessors
        {
            /// <summary>
            ///     string
            /// </summary>
            Name = 1,

            /// <summary>
            ///     bool
            /// </summary>
            Enabled = 2
        }

        public interface ITerminalPage : IModRootMember
        {
            /// <summary>
            ///     Retrieves information used by the Framework API
            /// </summary>
            ControlMembers GetApiData();
        }
    }
}