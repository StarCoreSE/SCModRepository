﻿using System;
using RichHudFramework.Internal;
using VRage;
using ApiMemberAccessor = System.Func<object, int, object>;
using GlyphFormatMembers = VRage.MyTuple<byte, float, VRageMath.Vector2I, VRageMath.Color>;

namespace RichHudFramework.UI.Client
{
    using ControlMembers = MyTuple<
        ApiMemberAccessor, // GetOrSetMember
        object // ID
    >;

    /// <summary>
    ///     Base type for all controls in the Rich Hud Terminal.
    /// </summary>
    public abstract class TerminalControlBase : ITerminalControl
    {
        protected readonly ApiMemberAccessor GetOrSetMember;
        protected ToolTip _toolTip;

        public TerminalControlBase(MenuControls controlEnum) : this(RichHudTerminal.GetNewMenuControl(controlEnum))
        {
            // Register event callback
            GetOrSetMember(new Action(ControlChangedCallback), (int)TerminalControlAccessors.GetOrSetControlCallback);
        }

        public TerminalControlBase(ControlMembers data)
        {
            GetOrSetMember = data.Item1;
            ID = data.Item2;
        }

        /// <summary>
        ///     Invoked whenver a change occurs to a control that requires a response, like a change
        ///     to a value.
        /// </summary>
        public event EventHandler ControlChanged;

        /// <summary>
        ///     The name of the control as it appears in the terminal.
        /// </summary>
        public string Name
        {
            get { return GetOrSetMember(null, (int)TerminalControlAccessors.Name) as string; }
            set { GetOrSetMember(value, (int)TerminalControlAccessors.Name); }
        }

        /// <summary>
        ///     Determines whether or not the control should be visible in the terminal.
        /// </summary>
        public bool Enabled
        {
            get { return (bool)GetOrSetMember(null, (int)TerminalControlAccessors.Enabled); }
            set { GetOrSetMember(value, (int)TerminalControlAccessors.Enabled); }
        }

        /// <summary>
        ///     Optional tooltip for the control
        /// </summary>
        public ToolTip ToolTip
        {
            get { return _toolTip; }
            set
            {
                _toolTip = value;
                GetOrSetMember(value.GetToolTipFunc, (int)TerminalControlAccessors.ToolTip);
            }
        }

        /// <summary>
        ///     Unique identifier
        /// </summary>
        public object ID { get; }

        public EventHandler ControlChangedHandler { get; set; }

        public ControlMembers GetApiData()
        {
            return new ControlMembers
            {
                Item1 = GetOrSetMember,
                Item2 = ID
            };
        }

        protected virtual void ControlChangedCallback()
        {
            ExceptionHandler.Run(() =>
            {
                ControlChanged?.Invoke(this, EventArgs.Empty);
                ControlChangedHandler?.Invoke(this, EventArgs.Empty);
            });
        }
    }

    /// <summary>
    ///     Base type for settings menu controls associated with a value of a given type.
    /// </summary>
    public abstract class TerminalValue<TValue> : TerminalControlBase, ITerminalValue<TValue>
    {
        public TerminalValue(MenuControls controlEnum) : base(controlEnum)
        {
        }

        /// <summary>
        ///     Value associated with the control.
        /// </summary>
        public virtual TValue Value
        {
            get { return (TValue)GetOrSetMember(null, (int)TerminalControlAccessors.Value); }
            set { GetOrSetMember(value, (int)TerminalControlAccessors.Value); }
        }

        /// <summary>
        ///     Used to periodically update the value associated with the control. Optional.
        /// </summary>
        public Func<TValue> CustomValueGetter
        {
            get { return GetOrSetMember(null, (int)TerminalControlAccessors.ValueGetter) as Func<TValue>; }
            set { GetOrSetMember(value, (int)TerminalControlAccessors.ValueGetter); }
        }
    }
}