﻿using ApiMemberAccessor = System.Func<object, int, object>;

namespace RichHudFramework.UI.Client
{
    public enum ListControlAccessors
    {
        ListAccessors = 16
    }

    /// <summary>
    ///     A fixed size list box with a label. Designed to mimic the appearance of the list box in the SE terminal.
    /// </summary>
    public class TerminalList<T> : TerminalValue<EntryData<T>>
    {
        public TerminalList() : base(MenuControls.ListControl)
        {
            var listData = GetOrSetMember(null, (int)ListControlAccessors.ListAccessors) as ApiMemberAccessor;

            List = new ListBoxData<T>(listData);
        }

        /// <summary>
        ///     Currently selected list member.
        /// </summary>
        public override EntryData<T> Value
        {
            get { return List.Selection; }
            set { List.SetSelection(value); }
        }

        public ListBoxData<T> List { get; }
    }
}